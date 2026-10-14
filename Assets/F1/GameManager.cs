using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Race Settings")]
    public int totalLaps = 2;
    public float threeStarTime = 120f;
    public float twoStarTime = 180f;

    [Header("Car")]
    public Transform car;
    public Rigidbody carRigidbody;
    public CarControl carControl;
    public BuildImporter buildImporter;

    [Header("Collision")]
    public float collisionFeedbackDuration = 0.5f;

    [Header("HUD References")]
    public TextMeshProUGUI lapText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI bestLapText;
    public GameObject collisionFeedbackPanel;
    public Image collisionFlash;
    public TextMeshProUGUI currentLapText;
    public TextMeshProUGUI penaltyTotalText;

    [Header("End Screen")]
    public GameObject endScreenPanel;
    public TextMeshProUGUI finalTimeText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI predictionText;
    public StarDisplay starDisplay;
    public TextMeshProUGUI leaderboardText;
    public Button replayButton;
    public Button quitButton;

    [Header("End Screen Timing")]
    public float endScreenDelay = 2f;

    [Header("Leaderboard Sound")]
    public AudioSource verstappenAudioSource;
    public AudioClip verstappenClip;

    // State
    private int currentLap = 0;
    private float raceTimer = 0f;
    private float lapTimer = 0f;
    private float bestLapTime = Mathf.Infinity;
    private bool raceStarted = false;
    private bool raceFinished = false;
    private bool hasCrossedStartLine = false;
    private float lapCooldown = 0f;
    private float totalPenaltyTime = 0f;

    // Leaderboard
    private List<float> leaderboard = new List<float>();
    private const string LeaderboardKey = "Leaderboard";

    // Crash
    // Multiple collisions in quick succession (e.g. sliding along a wall)
    // each start their own NshakePlatform coroutine. With a plain bool,
    // whichever coroutine finished LAST decided the final state, so an
    // earlier-finishing coroutine could set isCrashing = false while a
    // later one was still running, or vice versa, leaving it stuck. A
    // ref-count fixes this: isCrashing is only false when every active
    // shake coroutine has actually finished.
    private int crashShakeCount = 0;
    public bool isCrashing => crashShakeCount > 0;

    [Header("Prediction")]
    public float trackScale = 0.66f;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        LoadLeaderboard();
        endScreenPanel.SetActive(false);
        collisionFeedbackPanel.SetActive(false);
        lapText.text = $"LAP 1 / {totalLaps}";
        timerText.text = "0:00.000";
        bestLapText.text = "BEST --:--.---";

        if (replayButton != null)
        {
            replayButton.onClick.RemoveAllListeners();
            replayButton.onClick.AddListener(ReplayRace);
        }
        else
        {
            Debug.LogError("[GameManager] replayButton NOT ASSIGNED");
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(QuitGame);
        }
        else
        {
            Debug.LogError("[GameManager] quitButton NOT ASSIGNED");
        }

        // Ensure an EventSystem exists, or buttons can't be clicked
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            Debug.LogWarning("[GameManager] No EventSystem found, creating one");
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
    }

    public void ReplayRace()
    {
        Debug.Log("[GameManager] Replay clicked");
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        Debug.Log("[GameManager] Quit clicked");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }

    public void StartRace()
    {
        raceStarted = true;
        raceFinished = false;
        currentLap = 1;
        raceTimer = 0f;
        lapTimer = 0f;
        bestLapTime = Mathf.Infinity;
        hasCrossedStartLine = false;
        lapCooldown = 0f;
        totalPenaltyTime = 0f;

        if (penaltyTotalText != null)
            penaltyTotalText.text = "";

    }


    public void TriggerLap()
    {
        if (!raceStarted || raceFinished) return;

        if (!hasCrossedStartLine)
        {
            hasCrossedStartLine = true;
            lapTimer = 0f;
            lapCooldown = 5f;
            Debug.Log("Crossed start line, lap 1 begins");
            return;
        }

        // Minimum lap time guard
        if (lapTimer < 15f) return;
        if (lapCooldown > 0f) return;

        lapCooldown = 5f;

        if (lapTimer < bestLapTime)
            bestLapTime = lapTimer;

        Debug.Log($"Lap {currentLap} done: {FormatTime(lapTimer)}");
        lapTimer = 0f;

        if (currentLap >= totalLaps)
        {
            FinishRace();
            return;
        }

        currentLap++;
        UpdateHUD();
    }

    void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
            ResetAllSavedData();

        if (!raceStarted || raceFinished) return;

        raceTimer += Time.deltaTime;
        lapTimer += Time.deltaTime;
        lapCooldown -= Time.deltaTime;

        UpdateHUD();
    }

    void UpdateHUD()
    {
        timerText.text = FormatTime(raceTimer);
        lapText.text = $"LAP {currentLap} / {totalLaps}";

        if (currentLapText != null)
            currentLapText.text = $"LAP  {FormatTime(lapTimer)}";

        if (bestLapTime < Mathf.Infinity)
            bestLapText.text = $"BEST  {FormatTime(bestLapTime)}";
        else
            bestLapText.text = "BEST  --:--.---";
    }

    void FinishRace()
    {
        raceFinished = true;

        if (carControl != null)
            carControl.raceStarted = false;

        PhysicsAICar[] aiCars = FindObjectsByType<PhysicsAICar>(
            FindObjectsInactive.Exclude);
        foreach (var ai in aiCars)
            ai.raceStarted = false;

        int stars = CalculateStars(raceTimer);
        int score = CalculateScore(raceTimer);

        leaderboard.Add(raceTimer);
        leaderboard.Sort();
        if (leaderboard.Count > 10)
            leaderboard.RemoveAt(leaderboard.Count - 1);

        SaveLeaderboard();
        SaveBestScore(score, raceTimer);

        StartCoroutine(ShowEndScreen(stars, score));
    }

    IEnumerator ShowEndScreen(int stars, int score)
    {
        // Longer delay so the player sees the finish moment first
        yield return new WaitForSeconds(endScreenDelay);

        endScreenPanel.SetActive(true);

        if (finalTimeText != null)
            finalTimeText.text = $"TIME  {FormatTime(raceTimer)}";

        if (bestLapText != null)
        {
            if (bestLapTime < Mathf.Infinity)
                bestLapText.text = $"BEST LAP  {FormatTime(bestLapTime)}";
            else
                bestLapText.text = "BEST LAP  --:--.---";
        }

        float starFloat = (score / 100f) * 3f;
        if (starDisplay != null)
            starDisplay.SetStars(starFloat);

        ShowPredictionComparison();

        StartCoroutine(AnimateScore(score));

        string lbText = "LEADERBOARD\n";

        // Show all-time best at top of leaderboard
        string scene = SceneManager.GetActiveScene().name;
        float histBest = PlayerPrefs.GetFloat(
            $"BestRaceTime_{scene}", float.MaxValue);
        int histScore = PlayerPrefs.GetInt(
            $"BestScore_{scene}", 0);

        if (histBest < float.MaxValue)
            lbText += $"ALL-TIME  {FormatTime(histBest)}  ({histScore}/100)\n\n";

        for (int i = 0; i < Mathf.Min(leaderboard.Count, 5); i++)
            lbText += $"{GetOrdinal(i)}  {FormatTime(leaderboard[i])}\n";
        leaderboardText.text = lbText;

        int playerPosition = leaderboard.IndexOf(raceTimer);
        if (playerPosition >= 0 && playerPosition < 3)
        {
            if (verstappenAudioSource != null && verstappenClip != null)
            {
                verstappenAudioSource.PlayOneShot(verstappenClip);
                Debug.Log($"[GameManager] Top 3 finish at position {playerPosition + 1}, playing clip");
            }
        }
    }

    void ShowPredictionComparison()
    {
        if (predictionText == null) return;

        if (buildImporter == null || buildImporter.LoadedResult == null)
        {
            predictionText.text = "";
            return;
        }

        var result = buildImporter.LoadedResult;
        var quali = buildImporter.LoadedQuali;
        var race = buildImporter.LoadedRace;

        var lines = new System.Text.StringBuilder();
        lines.AppendLine($"SPEEDKRAFT PREDICTION — {buildImporter.LoadedCircuitName.ToUpper()} ({result.circuitRating})");

        // Best lap vs SPEEDKRAFT's estimated qualifying lap
        if (quali != null && quali.isValid && bestLapTime < Mathf.Infinity)
        {
            float predLap = quali.estimatedLapTimeSeconds * trackScale;
            float lapDiff = bestLapTime - predLap;
            string lapVerdict = Mathf.Abs(lapDiff) <= 2f
                ? "SPOT ON"
                : (lapDiff < 0f ? "BEAT THE PREDICTION" : "SLOWER THAN PREDICTED");

            lines.AppendLine(
                $"Predicted lap {FormatTime(predLap)}  |  " +
                $"Actual best {FormatTime(bestLapTime)}  |  {lapVerdict} " +
                $"({(lapDiff >= 0f ? "+" : "")}{lapDiff:0.0}s)");
        }

        // Total race time vs SPEEDKRAFT's projected race time (same number of laps)
        if (race != null && race.isValid)
        {
            float predRace = race.avgRaceLapSeconds * trackScale * totalLaps;
            float raceDiff = raceTimer - predRace;
            string raceVerdict = Mathf.Abs(raceDiff) <= 3f
                ? "MATCHED THE PROJECTION"
                : (raceDiff < 0f ? "FASTER THAN PROJECTED" : "SLOWER THAN PROJECTED");

            lines.AppendLine(
                $"Projected race {FormatTime(predRace)}  |  " +
                $"Actual {FormatTime(raceTimer)}  |  {raceVerdict} " +
                $"({(raceDiff >= 0f ? "+" : "")}{raceDiff:0.0}s)");
        }
        predictionText.text = lines.ToString();
    }

    void SaveBestScore(int score, float time)
    {
        string scene = SceneManager.GetActiveScene().name;

        float bestTime = PlayerPrefs.GetFloat(
            $"BestRaceTime_{scene}", float.MaxValue);
        int bestScore = PlayerPrefs.GetInt(
            $"BestScore_{scene}", 0);

        bool improved = false;

        if (time < bestTime)
        {
            PlayerPrefs.SetFloat($"BestRaceTime_{scene}", time);
            improved = true;
        }

        if (score > bestScore)
        {
            PlayerPrefs.SetInt($"BestScore_{scene}", score);
            improved = true;
        }

        if (bestLapTime < Mathf.Infinity)
        {
            float savedBestLap = PlayerPrefs.GetFloat(
                $"BestLap_{scene}", float.MaxValue);
            if (bestLapTime < savedBestLap)
                PlayerPrefs.SetFloat($"BestLap_{scene}", bestLapTime);
        }

        if (improved)
            PlayerPrefs.Save();
    }

    void SaveLeaderboard()
    {
        string scene = SceneManager.GetActiveScene().name;

        // Wipe old entries first
        int oldCount = PlayerPrefs.GetInt(
            $"{LeaderboardKey}_{scene}_count", 0);
        for (int i = 0; i < oldCount; i++)
            PlayerPrefs.DeleteKey($"{LeaderboardKey}_{scene}_{i}");

        // Write fresh entries — player only, AI times never added here
        for (int i = 0; i < leaderboard.Count; i++)
            PlayerPrefs.SetFloat(
                $"{LeaderboardKey}_{scene}_{i}", leaderboard[i]);

        PlayerPrefs.SetInt(
            $"{LeaderboardKey}_{scene}_count", leaderboard.Count);
        PlayerPrefs.Save();

        Debug.Log($"[Leaderboard] Saved {leaderboard.Count} player entries");
    }

    void LoadLeaderboard()
    {
        string scene = SceneManager.GetActiveScene().name;
        leaderboard.Clear();

        int count = PlayerPrefs.GetInt(
            $"{LeaderboardKey}_{scene}_count", 0);

        for (int i = 0; i < count; i++)
        {
            float t = PlayerPrefs.GetFloat(
                $"{LeaderboardKey}_{scene}_{i}", -1f);
            if (t > 0) leaderboard.Add(t);
        }

        leaderboard.Sort();
        Debug.Log($"[Leaderboard] Loaded {leaderboard.Count} entries");
    }

    public void ResetAllSavedData()
    {
        string scene = SceneManager.GetActiveScene().name;

        // Clear leaderboard
        int count = PlayerPrefs.GetInt(
            $"{LeaderboardKey}_{scene}_count", 0);

        for (int i = 0; i < count; i++)
            PlayerPrefs.DeleteKey($"{LeaderboardKey}_{scene}_{i}");

        PlayerPrefs.DeleteKey($"{LeaderboardKey}_{scene}_count");

        // Clear best scores
        PlayerPrefs.DeleteKey($"BestRaceTime_{scene}");
        PlayerPrefs.DeleteKey($"BestScore_{scene}");
        PlayerPrefs.DeleteKey($"BestLap_{scene}");

        // Nuclear option — wipes ALL PlayerPrefs if above doesn't work
        // PlayerPrefs.DeleteAll();

        PlayerPrefs.Save();

        leaderboard.Clear();
        totalPenaltyTime = 0f;

        // Refresh the leaderboard text immediately if end screen is open
        if (leaderboardText != null)
            leaderboardText.text = "LEADERBOARD\n(no entries)";

        Debug.Log("[GameManager] All saved data wiped");
    }
    string GetOrdinal(int i)
    {
        switch (i)
        {
            case 0: return "1st";
            case 1: return "2nd";
            case 2: return "3rd";
            default: return $"{i + 1}th";
        }
    }


    IEnumerator AnimateScore(int targetScore)
    {
        float duration = 1.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            int displayed = Mathf.RoundToInt(
                Mathf.Lerp(0, targetScore, elapsed / duration));

            // Use scoreText — separate from finalTimeText
            if (scoreText != null)
                scoreText.text = $"SCORE: {displayed}/100";

            yield return null;
        }

        if (scoreText != null)
            scoreText.text = $"SCORE: {targetScore}/100";
    }

    int CalculateScore(float time)
    {
        float totalScore;
        float oneStarTime = twoStarTime * 1.5f;

        if (time <= threeStarTime)
        {
            // Perfect run — scale from 85 to 100 within the 3-star window
            float t = Mathf.InverseLerp(threeStarTime, 0f, time);
            totalScore = Mathf.Lerp(85f, 100f, t);
        }
        else if (time <= twoStarTime)
        {
            float t = Mathf.InverseLerp(threeStarTime, twoStarTime, time);
            totalScore = Mathf.Lerp(84f, 55f, t);
        }
        else if (time <= oneStarTime)
        {
            float t = Mathf.InverseLerp(twoStarTime, oneStarTime, time);
            totalScore = Mathf.Lerp(54f, 25f, t);
        }
        else
        {
            totalScore = 20f;
        }

        // Best lap bonus: 30% weight
        float lapScore = 50f;
        if (bestLapTime < Mathf.Infinity && totalLaps > 0)
        {
            float idealLap = threeStarTime / totalLaps;
            float worstLap = twoStarTime / totalLaps;

            if (bestLapTime <= idealLap)
                lapScore = 100f;
            else if (bestLapTime <= worstLap)
            {
                float t = Mathf.InverseLerp(idealLap, worstLap, bestLapTime);
                lapScore = Mathf.Lerp(100f, 50f, t);
            }
            else
                lapScore = 30f;
        }

        float combined = (totalScore * 0.7f) + (lapScore * 0.3f);
        return Mathf.Clamp(Mathf.RoundToInt(combined), 0, 100);
    }

    int CalculateStars(float time)
    {
        if (time <= threeStarTime) return 3;
        if (time <= twoStarTime) return 2;
        return 1;
    }

    public void OnCarCollision(float impactForce)
    {
        if (!raceStarted || raceFinished) return;
        StartCoroutine(CollisionFeedback(impactForce));

        float perForce = Mathf.Clamp01(impactForce / 40f);
        StartCoroutine(NshakePlatform(perForce * 10, 2));
    }

    public void NormalShake(float intensity, float duration)
    {
        StartCoroutine(NshakePlatform(intensity, duration));
    }

    IEnumerator NshakePlatform(float intensity, float duration)
    {
        crashShakeCount++;
        float curTime = duration;

        while (curTime > 0)
        {
            curTime -= Time.deltaTime;
            float per = curTime / duration;

            try
            {
                PlatformController.singleton.Pitch =
                    per * Random.Range(-intensity, intensity);
                PlatformController.singleton.Roll =
                    per * Random.Range(-intensity, intensity);
            }
            catch { }

            yield return null;
        }

        crashShakeCount = Mathf.Max(0, crashShakeCount - 1);
    }

    IEnumerator CollisionFeedback(float force)
    {
        collisionFeedbackPanel.SetActive(true);

        float intensity = Mathf.Clamp01(force / 100f);
        collisionFlash.color = new Color(1f, 0f, 0f, intensity * 0.6f);

        yield return new WaitForSeconds(collisionFeedbackDuration);

        collisionFeedbackPanel.SetActive(false);
    }

    public void AddPenalty(float seconds)
    {
        raceTimer += seconds;
        totalPenaltyTime += seconds;

        Debug.Log($"Penalty: +{seconds}s | Total penalties: {totalPenaltyTime:F0}s");

        if (penaltyTotalText != null)
            penaltyTotalText.text = $"PENALTIES  +{totalPenaltyTime:F0}s";
    }

    string FormatTime(float seconds)
    {
        int mins = (int)(seconds / 60);
        int secs = (int)(seconds % 60);
        int ms = (int)((seconds * 1000) % 1000);
        return $"{mins}:{secs:00}.{ms:000}";
    }

}