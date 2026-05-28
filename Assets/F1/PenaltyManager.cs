using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class PenaltyManager : MonoBehaviour
{
    public static PenaltyManager Instance;

    [Header("Car")]
    public Transform car;
    public Rigidbody carRigidbody;
    public CarControl carControl;

    [Header("Waypoints")]
    public Transform waypointParent;

    [Header("Settings")]
    public float wrongWayTime = 2f;
    public float minCrashForce = 10f;
    public float falseStartCreepSpeed = 0.8f;

    [Header("Penalty Values (seconds)")]
    public float falseStartPenalty = 5f;
    public float collisionPenalty = 3f;
    public float wallHitPenalty = 2f;
    public float offTrackPenalty = 2f;
    public float wrongWayPenalty = 3f;

    [Header("Cooldowns")]
    public float collisionCooldown = 2f;
    public float offTrackCooldown = 4f;     // so driving on grass doesn't spam

    [Header("UI")]
    public GameObject penaltyPanel;
    public TextMeshProUGUI penaltyText;
    public Image penaltyBG;
    [Range(0f, 1f)]
    public float backgroundAlpha = 0.35f;

    // Internal state
    private bool lightsOut = false;
    private bool armedForFalseStart = false;
    private bool falseStartTriggered = false;
    private bool raceStarted = false;
    private bool checkingStarted = false;

    private float wrongWayTimer = 0f;
    private float collisionTimer = 0f;
    private float offTrackTimer = 0f;

    // Queue so no penalty is ever lost while one is displaying
    private Queue<(string msg, Color color, float amount)> penaltyQueue
        = new Queue<(string, Color, float)>();
    private bool penaltyShowing = false;

    private Transform[] waypoints;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (penaltyPanel == null)
            Debug.LogError("[PenaltyManager] penaltyPanel NOT ASSIGNED");
        if (penaltyText == null)
            Debug.LogError("[PenaltyManager] penaltyText NOT ASSIGNED");

        penaltyPanel.SetActive(false);

    }

    void Update()
    {
        // Tick cooldowns
        if (collisionTimer > 0f) collisionTimer -= Time.deltaTime;
        if (offTrackTimer > 0f) offTrackTimer -= Time.deltaTime;

        // False start window
        if (armedForFalseStart && !lightsOut && !falseStartTriggered)
            CheckFalseStartPassive();

        if (!raceStarted || !checkingStarted) return;

        CheckWrongWay();
    }

    // =========================================================
    // RACE STATE
    // =========================================================

    public void ArmFalseStartCheck()
    {
        armedForFalseStart = true;
        falseStartTriggered = false;
        Debug.Log("[PenaltyManager] Armed for false start");
    }

    public void OnLightsOut()
    {
        lightsOut = true;
        raceStarted = true;
        armedForFalseStart = false;
        Invoke(nameof(StartChecking), 2f);
        Debug.Log("[PenaltyManager] Lights out");
    }

    void StartChecking()
    {
        checkingStarted = true;
        Debug.Log("[PenaltyManager] Penalty checking active");
    }

    // =========================================================
    // FALSE START
    // =========================================================

    void CheckFalseStartPassive()
    {
        if (carRigidbody == null) return;

        if (carRigidbody.linearVelocity.magnitude > falseStartCreepSpeed)
        {
            falseStartTriggered = true;
            RegisterPenalty(
                "FALSE START",
                falseStartPenalty,
                new Color(1f, 0.3f, 0f));
        }
    }

    // =========================================================
    // COLLISIONS — called by CarCollision.cs
    // =========================================================

    // Player hit an AI car
    public void CarHitOtherCar(float force)
    {
        if (!raceStarted || force < minCrashForce) return;
        if (collisionTimer > 0f) return;

        collisionTimer = collisionCooldown;
        RegisterPenalty(
            "COLLISION WITH AI CAR",
            collisionPenalty,
            new Color(0.9f, 0.2f, 0.2f));
    }

    // Player hit a wall
    public void CarHitWall(float force)
    {
        if (!raceStarted || force < minCrashForce) return;
        if (collisionTimer > 0f) return;

        collisionTimer = collisionCooldown;
        RegisterPenalty(
            "WALL CONTACT",
            wallHitPenalty,
            new Color(0.9f, 0.4f, 0.1f));
    }

    // =========================================================
    // OFF TRACK — called by OffTrackTrigger.cs
    // =========================================================

    public void TriggerOffTrackInstant()
    {
        if (!raceStarted) return;
        if (offTrackTimer > 0f) return;

        offTrackTimer = offTrackCooldown;
        RegisterPenalty(
            "OFF TRACK",
            offTrackPenalty,
            new Color(1f, 0.75f, 0f));
    }

    // =========================================================
    // WRONG WAY
    // =========================================================

    void CheckWrongWay()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        if (carRigidbody.linearVelocity.magnitude < 5f)
        {
            wrongWayTimer = 0f;
            return;
        }

        int closest = 0;
        float minDist = Mathf.Infinity;
        for (int i = 0; i < waypoints.Length; i++)
        {
            float d = Vector3.Distance(car.position, waypoints[i].position);
            if (d < minDist) { minDist = d; closest = i; }
        }

        int next = (closest + 1) % waypoints.Length;
        Vector3 wpDir = (waypoints[next].position -
                         waypoints[closest].position).normalized;

        float angle = Vector3.Angle(car.forward, wpDir);

        if (angle > 110f)
        {
            wrongWayTimer += Time.deltaTime;
            if (wrongWayTimer > wrongWayTime)
            {
                RegisterPenalty(
                    "WRONG WAY",
                    wrongWayPenalty,
                    new Color(0.9f, 0.1f, 0.1f));
                wrongWayTimer = 0f;
            }
        }
        else
        {
            wrongWayTimer = 0f;
        }
    }

    // =========================================================
    // CORE — queue-based so nothing is dropped
    // =========================================================

    public void RegisterPenalty(string reason, float amount, Color color)
    {
        string msg = $"{reason}\n+{amount:F0}s penalty";
        penaltyQueue.Enqueue((msg, color, amount));

        if (!penaltyShowing)
            StartCoroutine(DrainQueue());
    }

    IEnumerator DrainQueue()
    {
        while (penaltyQueue.Count > 0)
        {
            var (msg, color, amount) = penaltyQueue.Dequeue();

            penaltyShowing = true;
            penaltyPanel.SetActive(true);
            penaltyText.text = msg;
            penaltyText.color = Color.white;

            if (penaltyBG != null)
            {
                penaltyBG.color = new Color(
                    color.r * 0.6f,
                    color.g * 0.6f,
                    color.b * 0.6f,
                    backgroundAlpha);
            }

            if (amount > 0)
                GameManager.Instance?.AddPenalty(amount);

            yield return new WaitForSeconds(2f);

            penaltyPanel.SetActive(false);
            penaltyShowing = false;

            // Small gap between back-to-back penalties
            if (penaltyQueue.Count > 0)
                yield return new WaitForSeconds(0.3f);
        }
    }
}