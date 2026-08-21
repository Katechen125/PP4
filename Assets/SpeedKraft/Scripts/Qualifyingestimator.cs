using UnityEngine;

public static class QualifyingEstimator
{
    // Tuning constants: how much each point of fit score below 100
    // translates into lap time lost, and how gap% maps to a grid position.
    // Tightened so only a near-perfect, circuit-specific build gets close to
    // pole, and no build ever literally matches or beats the historical
    // record, that's the point of the MinimumGapPercent floor below.
    private const float PercentPerMissingPoint = 0.07f;
    private const float MaxGapPercentForLastPlace = 6f;
    private const float MinimumGapPercent = 0.05f;

    // Driver execution variance: even a perfectly-matched car doesn't
    // guarantee a perfect lap every time, the driver has to actually
    // deliver it. This is a one-sided random cost (0 to max), never a
    // gain, so the car's fit score still sets the ceiling, but that
    // ceiling is no longer a guarantee. Rolled separately for qualifying
    // and the race, since they're different sessions.
    private const float DriverVarianceMaxPercent = 0.35f;
    private const int GridSize = 20;

    // Race laps run a bit slower than a single qualifying push lap
    // (fuel load, tyre management, less risk-taking).
    private const float RacePaceOffsetMultiplier = 1.035f;

    // Parses "1:31.447" (m:ss.sss) into total seconds. Returns -1f if the
    // string is empty or not in the expected format, so callers can treat
    // that as "no record set" rather than crashing.
    private static float ParseLapTime(string time)
    {
        if (string.IsNullOrWhiteSpace(time)) return -1f;

        string[] parts = time.Split(':');
        if (parts.Length != 2) return -1f;

        if (!int.TryParse(parts[0], out int minutes)) return -1f;
        if (!float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float seconds)) return -1f;

        return (minutes * 60f) + seconds;
    }

    public static QualifyingResult Estimate(PerformanceResult result, Circuit circuit)
    {
        float recordSeconds = circuit != null ? ParseLapTime(circuit.lapRecordTime) : -1f;

        if (result == null || !result.isValid || circuit == null || recordSeconds <= 0f)
            return new QualifyingResult { isValid = false };

        // Position uses the RAW gap, unfloored, so only a literal 100
        // circuitFitScore ever earns pole. A 99 already costs at least one
        // grid slot, this is intentionally strict.
        float carGapPercent = Mathf.Clamp((100f - result.circuitFitScore) * PercentPerMissingPoint, 0f, 15f);
        float driverVariance = Random.Range(0f, DriverVarianceMaxPercent);
        float rawGapPercent = carGapPercent + driverVariance;

        // Displayed lap time uses a floored gap, so even a perfect P1 build
        // never literally ties or beats the historical record, execution
        // variance means there's always some real-world gap.
        float displayGapPercent = Mathf.Max(rawGapPercent, MinimumGapPercent);

        float estimatedLapTime = recordSeconds * (1f + displayGapPercent / 100f);
        float gapSeconds = estimatedLapTime - recordSeconds;

        int position = GapPercentToPosition(rawGapPercent);
        string label = PositionLabel(position);

        return new QualifyingResult
        {
            isValid = true,
            estimatedPosition = position,
            positionLabel = label,
            estimatedLapTimeSeconds = Mathf.Round(estimatedLapTime * 1000f) / 1000f,
            gapToRecordSeconds = Mathf.Round(gapSeconds * 1000f) / 1000f,
            gapPercent = Mathf.Round(displayGapPercent * 100f) / 100f,
            recordLapTimeSeconds = recordSeconds,
            recordLapTimeDisplay = circuit.lapRecordTime,
            recordHolder = circuit.lapRecordHolder,
            driverVarianceRolled = driverVariance
        };
    }

    // Race finish projection: weights reliability/tyre management (tyreMatch)
    // and downforce (tyre wear via cornering load) more heavily than raw
    // one-lap pace, since that's what actually decides places gained or lost
    // over a full race distance, not just qualifying speed.
    public static RaceEstimateResult EstimateRace(PerformanceResult result, Circuit circuit, QualifyingResult quali)
    {
        if (result == null || !result.isValid || circuit == null || quali == null || !quali.isValid)
            return new RaceEstimateResult { isValid = false };

        float raceFitScore = (result.downforceMatch * 0.30f)
                            + (result.dragMatch * 0.20f)
                            + (result.engineMatch * 0.20f)
                            + (result.tyreMatch * 0.30f);

        float carRaceGapPercent = Mathf.Clamp((100f - raceFitScore) * PercentPerMissingPoint, 0f, 15f);
        // Same driver, same day: reuse the variance rolled during qualifying
        // instead of an independent roll, so the two screens read as one
        // consistent driver performance rather than two unrelated dice
        // rolls.
        float rawRaceGapPercent = carRaceGapPercent + quali.driverVarianceRolled;
        float displayRaceGapPercent = Mathf.Max(rawRaceGapPercent, MinimumGapPercent);
        int racePosition = GapPercentToPosition(rawRaceGapPercent);

        int positionsGained = quali.estimatedPosition - racePosition;

        float avgRaceLapSeconds = quali.recordLapTimeSeconds * (1f + displayRaceGapPercent / 100f) * RacePaceOffsetMultiplier;
        int laps = Mathf.Max(circuit.raceLaps, 1);
        float totalRaceTimeSeconds = avgRaceLapSeconds * laps;

        string trend;
        if (positionsGained > 0) trend = $"PROJECTED TO GAIN {positionsGained} PLACE{(positionsGained == 1 ? "" : "S")}";
        else if (positionsGained < 0) trend = $"PROJECTED TO LOSE {Mathf.Abs(positionsGained)} PLACE{(positionsGained == -1 ? "" : "S")}";
        else trend = "PROJECTED TO HOLD POSITION";

        return new RaceEstimateResult
        {
            isValid = true,
            estimatedFinishPosition = racePosition,
            positionsGained = positionsGained,
            trendLabel = trend,
            avgRaceLapSeconds = Mathf.Round(avgRaceLapSeconds * 1000f) / 1000f,
            totalRaceTimeSeconds = totalRaceTimeSeconds,
            raceFitScore = Mathf.Round(raceFitScore)
        };
    }

    private static int GapPercentToPosition(float gapPercent)
    {
        if (gapPercent <= 0f) return 1;
        float positionRaw = 1f + (gapPercent / MaxGapPercentForLastPlace) * (GridSize - 1);
        return Mathf.Clamp(Mathf.CeilToInt(positionRaw), 1, GridSize);
    }

    private static string PositionLabel(int position)
    {
        if (position == 1) return "POLE POSITION PACE";
        if (position <= 3) return "FRONT ROW PACE";
        if (position <= 10) return "Q3 PACE";
        if (position <= 15) return "Q2 ELIMINATION PACE";
        return "BACK OF GRID PACE";
    }
}

[System.Serializable]
public class QualifyingResult
{
    public bool isValid;
    public int estimatedPosition;
    public string positionLabel;
    public float estimatedLapTimeSeconds;
    public float gapToRecordSeconds;
    public float gapPercent;
    public float recordLapTimeSeconds;
    public string recordLapTimeDisplay;
    public string recordHolder;
    public float driverVarianceRolled;
}

[System.Serializable]
public class RaceEstimateResult
{
    public bool isValid;
    public int estimatedFinishPosition;
    public int positionsGained;
    public string trendLabel;
    public float avgRaceLapSeconds;
    public float totalRaceTimeSeconds;
    public float raceFitScore;
}