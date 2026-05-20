using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Race Settings")]
    public int totalLaps = 2;
    public float threeStarTime = 120f;  // seconds for 3 stars
    public float twoStarTime = 180f;  // seconds for 2 stars
    public float oneStarTime = 999f;  // anything else = 1 star

    [Header("Lap Detection")]
    public Transform startFinishLine;   // empty GameObject on the start line
    public float lapTriggerDistance = 10f;

    [Header("Car")]
    public Transform car;
    public Rigidbody carRigidbody;
    public CarControl carControl;

    [Header("Collision")]
    public float collisionFeedbackDuration = 0.5f;

    [Header("HUD References")]
    public TextMeshProUGUI lapText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI bestLapText;
    public GameObject collisionFeedbackPanel;
    public Image collisionFlash;

    [Header("End Screen")]
    public GameObject endScreenPanel;
    public TextMeshProUGUI finalTimeText;
    public TextMeshProUGUI starsText;
    public TextMeshProUGUI leaderboardText;
    public Button replayButton;
    public Button quitButton;

    // State
    private int currentLap = 0;
    private float raceTimer = 0f;
    private float lapTimer = 0f;
    private float bestLapTime = Mathf.Infinity;
    private bool raceStarted = false;
    private bool raceFinished = false;
    private bool crossedLineLastFrame = false;

    // Leaderboard
    private List<float> leaderboard = new List<float>();
    private const string LeaderboardKey = "Leaderboard";

    //Crash
    public bool isCrashing = false;

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

        replayButton.onClick.AddListener(ReplayRace);
        quitButton.onClick.AddListener(QuitGame);
    }

    // Called by StartLights when lights go out
    public void StartRace()
    {
        raceStarted = true;
        currentLap = 1;
        raceTimer = 0f;
        lapTimer = 0f;
        Debug.Log("RACE STARTED");
    }

    void Update()
    {
        if (!raceStarted || raceFinished) return;

        raceTimer += Time.deltaTime;
        lapTimer += Time.deltaTime;

        UpdateHUD();
        CheckLapCompletion();
    }

    void UpdateHUD()
    {
        timerText.text = FormatTime(raceTimer);
        lapText.text = $"LAP {currentLap} / {totalLaps}";

        if (bestLapTime < Mathf.Infinity)
            bestLapText.text = $"BEST {FormatTime(bestLapTime)}";
    }

    void CheckLapCompletion()
    {
        if (startFinishLine == null || car == null) return;

        float dist = Vector3.Distance(
            new Vector3(car.position.x, 0, car.position.z),
            new Vector3(startFinishLine.position.x, 0,
                startFinishLine.position.z)
        );

        // Show distance in console so we can tune it
        //if (raceStarted)
        //    Debug.Log($"Distance to line: {dist:F1}m | LapTimer: {lapTimer:F1}s");

        bool crossingNow = dist < lapTriggerDistance && lapTimer > 5f;

        if (crossingNow && !crossedLineLastFrame)
            CompleteLap();

        crossedLineLastFrame = crossingNow;
    }
    void CompleteLap()
    {
        // Update best lap
        if (lapTimer < bestLapTime)
            bestLapTime = lapTimer;

        lapTimer = 0f;

        if (currentLap >= totalLaps)
        {
            FinishRace();
            return;
        }

        currentLap++;
        Debug.Log($"Lap {currentLap - 1} complete! Total time: {FormatTime(raceTimer)}");
    }

    void FinishRace()
    {
        raceFinished = true;

        // Stop car
        if (carControl != null)
            carControl.raceStarted = false;

        // Calculate stars
        int stars = CalculateStars(raceTimer);

        // Save to leaderboard
        leaderboard.Add(raceTimer);
        leaderboard.Sort();
        if (leaderboard.Count > 10)
            leaderboard.RemoveAt(leaderboard.Count - 1);
        SaveLeaderboard();

        StartCoroutine(ShowEndScreen(stars));
    }

    IEnumerator ShowEndScreen(int stars)
    {
        yield return new WaitForSeconds(1.5f);

        endScreenPanel.SetActive(true);

        finalTimeText.text = $"FINAL TIME\n{FormatTime(raceTimer)}";

        // Stars display
        string starDisplay = "";
        for (int i = 0; i < 3; i++)
            starDisplay += i < stars ? "★" : "☆";
        starsText.text = starDisplay;

        // Leaderboard
        string lbText = "LEADERBOARD\n";
        for (int i = 0; i < Mathf.Min(leaderboard.Count, 5); i++)
            lbText += $"{i + 1}. {FormatTime(leaderboard[i])}\n";
        leaderboardText.text = lbText;
    }

    int CalculateStars(float time)
    {
        if (time <= threeStarTime) return 3;
        if (time <= twoStarTime) return 2;
        return 1;
    }

    // Call this from car collision script
    public void OnCarCollision(float impactForce)
    {
        if (!raceStarted || raceFinished) return;
        StartCoroutine(CollisionFeedback(impactForce));
        float perForce = Mathf.Clamp01((impactForce / 40.0f));
        StartCoroutine(NshakePlatform(perForce * 10, 2));
        //print(impactForce);
    }
    public void NormalShake(float intensity, float duration)
    {
        StartCoroutine(NshakePlatform(intensity, duration));
    }
    IEnumerator NshakePlatform(float intensity, float duration) //comes back after running thanks to yield
    {
        isCrashing = true;
        float curTime = duration;
        while (curTime > 0)
        {
            curTime -= Time.deltaTime; //based time, not frame (slower computer == slower "time")
            float per = curTime / duration;
            PlatformController.singleton.Pitch = per * Random.Range(-intensity, intensity);
            PlatformController.singleton.Roll = per * Random.Range(-intensity, intensity);
            yield return null;
        }
        isCrashing = false;
    }

    IEnumerator CollisionFeedback(float force)
    {
        collisionFeedbackPanel.SetActive(true);

        // Flash intensity based on impact force
        float intensity = Mathf.Clamp01(force / 100f);
        collisionFlash.color = new Color(1f, 0f, 0f, intensity * 0.6f);

        yield return new WaitForSeconds(collisionFeedbackDuration);

        collisionFeedbackPanel.SetActive(false);
    }

    void ReplayRace()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // Leaderboard persistence
    void SaveLeaderboard()
    {
        for (int i = 0; i < leaderboard.Count; i++)
            PlayerPrefs.SetFloat($"{LeaderboardKey}_{i}", leaderboard[i]);
        PlayerPrefs.SetInt($"{LeaderboardKey}_count", leaderboard.Count);
        PlayerPrefs.Save();
    }

    void LoadLeaderboard()
    {
        leaderboard.Clear();
        int count = PlayerPrefs.GetInt($"{LeaderboardKey}_count", 0);
        for (int i = 0; i < count; i++)
            leaderboard.Add(PlayerPrefs.GetFloat($"{LeaderboardKey}_{i}"));
    }

    string FormatTime(float seconds)
    {
        int mins = (int)(seconds / 60);
        int secs = (int)(seconds % 60);
        int ms = (int)((seconds * 1000) % 1000);
        return $"{mins}:{secs:00}.{ms:000}";
    }


}