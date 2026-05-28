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

    [Header("Wheel Colliders (for off-track detection)")]
    public WheelCollider frontLeftWheel;
    public WheelCollider frontRightWheel;
    public WheelCollider rearLeftWheel;
    public WheelCollider rearRightWheel;

    [Header("Layers")]
    public LayerMask penaltyLayer;
    public LayerMask wallLayer;

    [Header("Waypoints")]
    public Transform waypointParent;

    [Header("Settings")]
    public float wrongWayTime = 2f;
    public float minCrashForce = 10f;
    public float falseStartCreepSpeed = 0.8f;
    public int wheelsOffTrackThreshold = 4;

    [Header("Penalty Values (seconds)")]
    public float falseStartPenalty = 5f;
    public float collisionPenalty = 3f;
    public float wallHitPenalty = 2f;
    public float offTrackPenalty = 2f;
    public float wrongWayPenalty = 3f;

    [Header("Cooldowns")]
    public float collisionCooldown = 2f;
    public float offTrackCooldown = 4f;

    [Header("Display Duration")]
    public float penaltyDisplayTime = 2.5f;

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

    private Queue<(string msg, Color color, float amount)> penaltyQueue
        = new Queue<(string, Color, float)>();
    private bool penaltyShowing = false;

    private Transform[] waypoints;
    private WheelCollider[] allWheels;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Hard validate every reference
        if (penaltyPanel == null)
        {
            Debug.LogError("[PenaltyManager] penaltyPanel NOT ASSIGNED");
            return;
        }
        if (penaltyText == null)
            Debug.LogError("[PenaltyManager] penaltyText NOT ASSIGNED");

        penaltyPanel.SetActive(false);

        // Confirm the panel canvas is on a high sort order
        Canvas c = penaltyPanel.GetComponentInParent<Canvas>();
        if (c != null)
            Debug.Log($"[PenaltyManager] Panel canvas sort order: {c.sortingOrder}");

        allWheels = new WheelCollider[]
        {
            frontLeftWheel, frontRightWheel,
            rearLeftWheel,  rearRightWheel
        };

        // Warn about any unassigned wheels
        for (int i = 0; i < allWheels.Length; i++)
        {
            if (allWheels[i] == null)
                Debug.LogWarning($"[PenaltyManager] Wheel {i} is not assigned");
        }

        if (waypointParent != null)
        {
            waypoints = new Transform[waypointParent.childCount];
            for (int i = 0; i < waypointParent.childCount; i++)
                waypoints[i] = waypointParent.GetChild(i);
        }
        else
        {
            Debug.LogWarning("[PenaltyManager] waypointParent not assigned, " +
                             "wrong way detection disabled");
        }
    }

    void Update()
    {
        if (collisionTimer > 0f) collisionTimer -= Time.deltaTime;
        if (offTrackTimer > 0f) offTrackTimer -= Time.deltaTime;

        if (armedForFalseStart && !lightsOut && !falseStartTriggered)
            CheckFalseStartPassive();

        if (!raceStarted || !checkingStarted) return;

        CheckOffTrackByWheels();
        CheckWrongWay();
    }

    // =========================================================
    // OFF TRACK — wheel ground check against Penalty layer
    // =========================================================

    void CheckOffTrackByWheels()
    {
        if (offTrackTimer > 0f) return;
        if (allWheels == null) return;

        int wheelsOnPenalty = 0;
        int wheelsGrounded = 0;

        foreach (WheelCollider wheel in allWheels)
        {
            if (wheel == null) continue;

            if (wheel.GetGroundHit(out WheelHit hit))
            {
                wheelsGrounded++;
                int hitLayer = hit.collider.gameObject.layer;
                bool onPenalty = ((1 << hitLayer) & penaltyLayer.value) != 0;

                if (onPenalty)
                    wheelsOnPenalty++;
            }
        }

        // Only check if car is actually on the ground
        if (wheelsGrounded == 0) return;

        if (wheelsOnPenalty >= wheelsOffTrackThreshold)
        {
            offTrackTimer = offTrackCooldown;
            Debug.Log($"[PenaltyManager] Off track! " +
                      $"{wheelsOnPenalty}/{wheelsGrounded} wheels on penalty layer");
            RegisterPenalty(
                "CAR OFF TRACK",
                offTrackPenalty,
                new Color(1f, 0.75f, 0f));
        }
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
    // COLLISIONS
    // =========================================================

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
    // CORE
    // =========================================================

    public void RegisterPenalty(string reason, float amount, Color color)
    {
        string msg = $"{reason}\n+{amount:F0}s penalty";
        Debug.Log($"[PenaltyManager] RegisterPenalty: {msg} | " +
                  $"penaltyShowing={penaltyShowing} | " +
                  $"queueCount={penaltyQueue.Count}");

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

            if (penaltyText != null)
            {
                penaltyText.text = msg;
                penaltyText.color = Color.white;
                penaltyText.ForceMeshUpdate();
            }

            if (penaltyBG != null)
            {
                penaltyBG.color = new Color(
                    color.r * 0.6f,
                    color.g * 0.6f,
                    color.b * 0.6f,
                    backgroundAlpha);
            }

            // Add to race timer
            if (amount > 0)
                GameManager.Instance?.AddPenalty(amount);

            // Tell telemetry to log this individual penalty
            TelemetryHUD hud = FindAnyObjectByType<TelemetryHUD>();
            if (hud != null)
                hud.AddPenaltyDisplay(amount);

            yield return new WaitForSeconds(penaltyDisplayTime);

            penaltyPanel.SetActive(false);
            penaltyShowing = false;

            if (penaltyQueue.Count > 0)
                yield return new WaitForSeconds(0.3f);
        }
    }
}