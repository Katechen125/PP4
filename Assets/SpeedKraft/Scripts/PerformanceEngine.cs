using UnityEngine;

public static class PerformanceEngine
{
    private static readonly PartCategory[] WingCategories = { PartCategory.FrontWing, PartCategory.RearWing };
    private static readonly PartCategory[] PowerCategories = { PartCategory.Engine };

    // Neutral baselines used for the additive-offset categories below.
    // These are the Standard-tier part values, chosen so "Standard" parts
    // contribute exactly 0 offset (no bonus, no penalty).
    private const float SidepodsNeutralDrag = 25f;
    private const float FloorNeutralDownforce = 50f;
    private const float BrakesNeutralReliability = 85f;
    private const float SuspensionNeutralReliability = 85f;

    // Tightened from 8/1.3 so mid-pack circuits stop clustering at OPTIMAL together.
    private const float MatchTolerance = 5f;
    private const float DecaySteepness = 1.6f;

    private static float AverageStat(CarBuild build, PartCategory[] categories, System.Func<CarPart, float> selector)
    {
        float total = 0f;
        int count = 0;
        foreach (var part in build.GetAllParts())
        {
            if (part == null) continue;
            bool inCategory = false;
            foreach (var cat in categories)
                if (part.category == cat) { inCategory = true; break; }
            if (!inCategory) continue;
            total += selector(part);
            count++;
        }
        return count == 0 ? 0f : total / count;
    }

    // Wings set the downforce baseline. Floor has a wider value range
    // (35-75) than Sidepods did, so it usually escaped the tolerance zone
    // on its own under the old flat 3-way average, but not always, it
    // depended on coincidence with whatever wings were equipped. Same
    // fix as Drag/Sidepods: full-weight offset from a neutral baseline
    // instead of dilution, so Floor is *guaranteed* to matter every time,
    // not just usually.
    private static float DownforceWithFloorOffset(CarBuild build)
    {
        float wingsDownforce = AverageStat(build, WingCategories, p => p.downforce);
        if (build.frontWing == null && build.rearWing == null) return 0f;

        float floorOffset = build.floor != null
            ? build.floor.downforce - FloorNeutralDownforce
            : 0f;

        return Mathf.Clamp(wingsDownforce + floorOffset, 0f, 100f);
    }

    // Wings set the drag baseline. Sidepods' own value range is narrow
    // (20-30), so folding it into a flat 3-way average with the wings
    // diluted its swing down to ~3 points, well under the match tolerance,
    // meaning no Sidepods choice could ever move the score. Applying it as
    // a full-weight offset from a neutral baseline instead means its whole
    // 10-point range actually counts.
    private static float DragWithSidepodsOffset(CarBuild build)
    {
        float wingsDrag = AverageStat(build, WingCategories, p => p.drag);
        if (build.frontWing == null && build.rearWing == null) return 0f;

        float sidepodsOffset = build.sidepods != null
          ? (build.sidepods.drag - SidepodsNeutralDrag) * 1.3f
          : 0f;

        return Mathf.Clamp(wingsDrag + sidepodsOffset, 0f, 100f);
    }

    // Tyres are the dominant factor for reliability, so they set the base
    // value directly (undiluted). Brakes and Suspension apply as
    // full-weight offsets from their neutral baseline, this keeps tyres in
    // the driver's seat while guaranteeing Brakes' 30-point range and
    // Suspension's 15-point range both comfortably clear the match
    // tolerance instead of getting flattened by averaging.
    private static float ReliabilityWithOffsets(CarBuild build)
    {
        float baseReliability = build.tyres != null ? build.tyres.reliability : 0f;

        float brakesOffset = build.brakes != null
            ? (build.brakes.reliability - BrakesNeutralReliability) * 0.5f
            : 0f;

        float suspensionOffset = build.suspension != null
            ? (build.suspension.reliability - SuspensionNeutralReliability) * 0.7f
            : 0f;

        return Mathf.Clamp(baseReliability + brakesOffset + suspensionOffset, 0f, 100f);
    }

    private static float MatchScore(float actual, float target)
    {
        float diff = Mathf.Abs(actual - target);
        if (diff <= MatchTolerance) return 100f;
        float excess = diff - MatchTolerance;
        return Mathf.Clamp(100f - excess * DecaySteepness, 0f, 100f);
    }

    public static PerformanceResult Calculate(CarBuild build, Circuit circuit = null, bool requireComplete = true)
    {
        if (requireComplete && !build.IsComplete())
            return new PerformanceResult { isValid = false };

        float totalWeight = 0f;
        int partCount = 0;
        foreach (CarPart part in build.GetAllParts())
        {
            if (part == null) continue;
            totalWeight += part.weight;
            partCount++;
        }
        float avgWeight = partCount == 0 ? 0f : totalWeight / partCount;

        float avgDownforce = DownforceWithFloorOffset(build);
        float avgDrag = DragWithSidepodsOffset(build);
        float avgPower = AverageStat(build, PowerCategories, p => p.power);
        float avgReliability = ReliabilityWithOffsets(build);

        float aeroEfficiency = avgDrag > 0
            ? (avgDownforce / (avgDownforce + avgDrag)) * 100f
            : 100f;

        float speedScore = Mathf.Clamp((avgPower * 1.4f) - (avgDrag * 0.4f), 0f, 100f);
        float corneringScore = Mathf.Clamp(avgDownforce - (avgWeight * 0.2f), 0f, 100f);

        float lapTime = 90f
            - (speedScore * 0.12f)
            - (corneringScore * 0.08f)
            + (avgWeight * 0.04f);

        lapTime = Mathf.Clamp(lapTime, 60f, 90f);

        string circuitRating = "";
        float circuitFitScore = 0f;
        float downforceMatch = 0f, dragMatch = 0f, engineMatch = 0f, tyreMatch = 0f;

        if (circuit != null)
        {
            downforceMatch = MatchScore(avgDownforce, circuit.downforceRequirement);
            dragMatch = MatchScore(avgDrag, 100f - circuit.dragSensitivity);
            engineMatch = MatchScore(avgPower, circuit.engineDemand);
            tyreMatch = MatchScore(avgReliability, circuit.tyreStress);

            circuitFitScore = (downforceMatch * 0.35f)
                            + (dragMatch * 0.25f)
                            + (engineMatch * 0.25f)
                            + (tyreMatch * 0.15f);

            lapTime -= (circuitFitScore / 100f) * 8f;
            lapTime = Mathf.Clamp(lapTime, 60f, 90f);

            // Thresholds raised so OPTIMAL requires genuine circuit-specific tuning,
            // not just a decent all-round build.
            if (circuitFitScore >= 93f) circuitRating = "OPTIMAL SETUP";
            else if (circuitFitScore >= 78f) circuitRating = "GOOD SETUP";
            else if (circuitFitScore >= 58f) circuitRating = "AVERAGE SETUP";
            else circuitRating = "POOR SETUP";
        }

        float finalScore = circuit != null
            ? circuitFitScore
            : ((aeroEfficiency * 0.25f) + (speedScore * 0.25f)
             + (corneringScore * 0.25f) + (avgReliability * 0.25f));

        finalScore = Mathf.Clamp(finalScore, 0f, 100f);

        return new PerformanceResult
        {
            isValid = true,
            efficiencyScore = Mathf.Round(finalScore),
            lapTimeSeconds = Mathf.Round(lapTime * 10f) / 10f,
            aeroEfficiency = Mathf.Round(aeroEfficiency),
            speedScore = Mathf.Round(speedScore),
            corneringScore = Mathf.Round(corneringScore),
            reliability = Mathf.Round(avgReliability),
            circuitRating = circuitRating,
            circuitFitScore = Mathf.Round(circuitFitScore),
            avgPower = Mathf.Round(avgPower),
            avgDownforce = Mathf.Round(avgDownforce),
            avgDrag = Mathf.Round(avgDrag),
            downforceMatch = Mathf.Round(downforceMatch),
            dragMatch = Mathf.Round(dragMatch),
            engineMatch = Mathf.Round(engineMatch),
            tyreMatch = Mathf.Round(tyreMatch)
        };
    }
}

[System.Serializable]
public class PerformanceResult
{
    public bool isValid;
    public float efficiencyScore;
    public float circuitFitScore;
    public float lapTimeSeconds;
    public float aeroEfficiency;
    public float speedScore;
    public float corneringScore;
    public float reliability;
    public string circuitRating;
    public float avgPower;
    public float avgDownforce;
    public float avgDrag;

    public float downforceMatch;
    public float dragMatch;
    public float engineMatch;
    public float tyreMatch;
}