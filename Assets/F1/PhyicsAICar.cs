using UnityEngine;

public class PhysicsAICar : MonoBehaviour
{
    [Header("Waypoints")]
    public Transform[] waypoints;
    public float waypointRadius = 5f;
    private int currentWaypointIndex = 0;
    private Vector3 currentWaypointOffset;

    [Header("Car Physics Components")]
    public WheelCollider frontLeftWheel;
    public WheelCollider frontRightWheel;
    public WheelCollider rearLeftWheel;
    public WheelCollider rearRightWheel;
    public Rigidbody carRigidbody;

    [Header("Car Settings")]
    public float maxEngineTorque = 1500f;
    public float maxBrakeTorque = 3000f;
    public float maxSteerAngle = 35f;

    [Header("Speed Relative to Player")]
    public CarControl playerCar;
    [Range(0.6f, 0.95f)]
    public float speedRatio = 0.85f;
    public float minSpeedKmh = 40f;
    public float maxSpeedKmh = 120f;
    private float targetSpeedKmh = 60f;

    [Header("Waypoint Offset")]
    public float lateralOffsetAmount = 1.5f;
    public float offsetChangeRadius = 8f;
    private float currentLateralOffset = 0f;

    [Header("Aggressor")]
    public bool isAggressor = false;
    public float aggressorSpeedKmh = 150f;
    public float targetSearchRadius = 80f;
    public float aimBehindDistance = 3f;
    [Range(0.3f, 1f)]
    public float lurkSpeedFactor = 0.6f;
    private Transform chaseTarget;
    private bool isChasing;
    private float retargetTimer;
    private PhysicsAICar[] allAI;
    private AggressorRam ram;

    [Header("Auto Respawn")]
    public float stuckSpeedKmh = 3f;
    public float stuckSeconds = 3f;
    public float flippedSeconds = 2f;
    public float respawnGraceSeconds = 4f;
    private float stuckTimer, flippedTimer, graceTimer;

    [Header("Race")]
    public bool raceStarted = false;

    [Header("Lap Tracking")]
    public int currentLap = 0;
    public int totalLaps = 2;
    public float lapTime = 0f;
    public float bestLap = Mathf.Infinity;
    public bool finished = false;

    void Start()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogError($"{gameObject.name}: no waypoints assigned");
            return;
        }

        if (playerCar == null)
            playerCar = FindAnyObjectByType<CarControl>();

        // Clear any stuck brake torque from editor state
        ClearAllBrakes();

        // Check Rigidbody setup
        if (carRigidbody == null)
        {
            Debug.LogError($"{gameObject.name}: Rigidbody not assigned");
            return;
        }

        if (carRigidbody.isKinematic)
        {
            Debug.LogWarning($"{gameObject.name}: Rigidbody is Kinematic — " +
                             $"fixing it now. Turn this off in the inspector.");
            carRigidbody.isKinematic = false;
        }

        // Log wheel setup so you can confirm they're wired correctly
        Debug.Log($"{gameObject.name} wheels: " +
                  $"FL={frontLeftWheel != null} " +
                  $"FR={frontRightWheel != null} " +
                  $"RL={rearLeftWheel != null} " +
                  $"RR={rearRightWheel != null}");

        currentLateralOffset = Random.Range(
            -lateralOffsetAmount, lateralOffsetAmount);
        GenerateWaypointOffset();

        if (isAggressor)
            Debug.Log($"[Aggressor] {gameObject.name} is the aggressor. " +
                      $"AggressorRam found: {GetComponentInChildren<AggressorRam>() != null}, " +
                      $"playerCar found: {playerCar != null}");
    }

    // Call this from StartLights when lights go out
    public void OnRaceStart()
    {
        raceStarted = true;
        lapTime = 0f;
        currentLap = 0;
        finished = false;
        ClearAllBrakes();
        Debug.Log($"{gameObject.name} race started");
    }

    void FixedUpdate()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        if (!raceStarted || finished)
        {
            ApplyBrakes(maxBrakeTorque);
            return;
        }

        lapTime += Time.fixedDeltaTime;

        CheckStuck();

        UpdateTargetSpeed();
        HandleWaypointProgress();
        DriveTowardsWaypoint();
    }

    void UpdateTargetSpeed()
    {
        if (playerCar == null)
        {
            targetSpeedKmh = 60f;
            return;
        }

        float playerTopKmh = playerCar.maxSpeed * 3.6f;
        targetSpeedKmh = Mathf.Clamp(
            playerTopKmh * speedRatio,
            minSpeedKmh,
            maxSpeedKmh);
    }

    void HandleWaypointProgress()
    {
        Vector3 targetPos = waypoints[currentWaypointIndex].position
                            + currentWaypointOffset;

        float dist = Vector3.Distance(transform.position, targetPos);

        if (dist < offsetChangeRadius)
        {
            int prev = currentWaypointIndex;
            currentWaypointIndex =
                (currentWaypointIndex + 1) % waypoints.Length;
            GenerateWaypointOffset();

            if (currentWaypointIndex == 0 && prev != 0)
                CompleteLap();
        }
    }

    void GenerateWaypointOffset()
    {
        int next = (currentWaypointIndex + 1) % waypoints.Length;

        Vector3 forward = (waypoints[next].position -
                           waypoints[currentWaypointIndex].position).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward);

        float newOffset = Mathf.Lerp(
            currentLateralOffset,
            Random.Range(-lateralOffsetAmount, lateralOffsetAmount),
            0.4f);

        currentLateralOffset = newOffset;
        currentWaypointOffset = right * currentLateralOffset;
    }

    void DriveTowardsWaypoint()
    {
        Vector3 targetPosition = GetDriveTarget();

        // Steering
        Vector3 localTarget = transform.InverseTransformPoint(targetPosition);
        float targetAngle = Mathf.Atan2(localTarget.x, localTarget.z)
                            * Mathf.Rad2Deg;
        float steer = Mathf.Clamp(targetAngle, -maxSteerAngle, maxSteerAngle);

        frontLeftWheel.steerAngle = steer;
        frontRightWheel.steerAngle = steer;

        // Throttle and braking
        float currentSpeedKmh = carRigidbody.linearVelocity.magnitude * 3.6f;
        float steeringSeverity = Mathf.Abs(steer) / maxSteerAngle;

        float engineTorque = 0f;
        float brakeTorque = 0f;

        float desiredSpeed = isChasing ? aggressorSpeedKmh
                           : (isAggressor ? targetSpeedKmh * lurkSpeedFactor
                                          : targetSpeedKmh);

        if (currentSpeedKmh < desiredSpeed)
        {
            engineTorque = maxEngineTorque * (1f - (steeringSeverity * 0.7f));
            brakeTorque = 0f;
        }
        else
        {
            engineTorque = 0f;
            if (steeringSeverity > 0.5f)
                brakeTorque = maxBrakeTorque;
        }

        rearLeftWheel.motorTorque = engineTorque;
        rearRightWheel.motorTorque = engineTorque;
        rearLeftWheel.brakeTorque = brakeTorque;
        rearRightWheel.brakeTorque = brakeTorque;

        // Keep front wheels from holding brake torque
        frontLeftWheel.brakeTorque = 0f;
        frontRightWheel.brakeTorque = 0f;
    }

    Vector3 GetDriveTarget()
    {
        Vector3 wp = waypoints[currentWaypointIndex].position + currentWaypointOffset;
        isChasing = false;
        if (!isAggressor) return wp;

        if (ram == null) ram = GetComponentInChildren<AggressorRam>();
        if (ram != null && ram.IsCoolingDown) { chaseTarget = null; return wp; }

        retargetTimer -= Time.fixedDeltaTime;
        if (retargetTimer <= 0f)
        {
            retargetTimer = 0.25f;
            chaseTarget = FindChaseTarget();
        }
        if (chaseTarget == null) return wp;

        // Apunta a la esquina trasera del lado desde el que llegamos.
        float side = Vector3.Dot(transform.position - chaseTarget.position,
                                 chaseTarget.right) >= 0f ? 1f : -1f;
        isChasing = true;
        return chaseTarget.position
               - chaseTarget.forward * aimBehindDistance
               + chaseTarget.right * side * 0.8f;
    }

    Transform FindChaseTarget()
    {
        if (allAI == null)
            allAI = FindObjectsByType<PhysicsAICar>();

        Transform best = null;
        float bestDist = targetSearchRadius;

        if (playerCar != null) Consider(playerCar.transform);
        foreach (var ai in allAI)
            if (ai != null && ai != this) Consider(ai.transform);

        if (best != null && best != chaseTarget)
            Debug.Log($"[Aggressor] Chasing {best.name}");

        return best;

        void Consider(Transform t)
        {
            float d = Vector3.Distance(transform.position, t.position);
            if (d >= bestDist) return;
            // Ignora coches que quedaron detras (daria vuelta en U).
            if (transform.InverseTransformPoint(t.position).z < -2f) return;
            bestDist = d;
            best = t;
        }
    }

    void CheckStuck()
    {
        if (graceTimer > 0f) { graceTimer -= Time.fixedDeltaTime; return; }
        if (lapTime < respawnGraceSeconds) return;

        float speedKmh = carRigidbody.linearVelocity.magnitude * 3.6f;
        stuckTimer = speedKmh < stuckSpeedKmh ? stuckTimer + Time.fixedDeltaTime : 0f;
        flippedTimer = Vector3.Dot(carRigidbody.transform.up, Vector3.up) < 0.3f
            ? flippedTimer + Time.fixedDeltaTime : 0f;

        if (stuckTimer > stuckSeconds || flippedTimer > flippedSeconds)
            Respawn();
    }

    void Respawn()
    {
        int prev = (currentWaypointIndex - 1 + waypoints.Length) % waypoints.Length;
        Vector3 pos = waypoints[prev].position + Vector3.up * 1f;
        Vector3 dir = waypoints[currentWaypointIndex].position - waypoints[prev].position;
        dir.y = 0f;
        Quaternion rot = dir.sqrMagnitude > 0.01f
            ? Quaternion.LookRotation(dir)
            : carRigidbody.transform.rotation;

        carRigidbody.linearVelocity = Vector3.zero;
        carRigidbody.angularVelocity = Vector3.zero;
        carRigidbody.transform.SetPositionAndRotation(pos, rot);
        Physics.SyncTransforms();

        ClearAllBrakes();
        chaseTarget = null;
        stuckTimer = 0f;
        flippedTimer = 0f;
        graceTimer = 2f;

        Debug.Log($"[Respawn] {gameObject.name} reset to waypoint {prev}");
    }

    void CompleteLap()
    {
        if (lapTime < bestLap) bestLap = lapTime;
        lapTime = 0f;
        currentLap++;

        if (currentLap >= totalLaps)
        {
            finished = true;
            ApplyBrakes(maxBrakeTorque);
            Debug.Log($"{gameObject.name} finished!");
        }
    }

    void ApplyBrakes(float force)
    {
        frontLeftWheel.motorTorque = 0f;
        frontRightWheel.motorTorque = 0f;
        rearLeftWheel.motorTorque = 0f;
        rearRightWheel.motorTorque = 0f;
        frontLeftWheel.brakeTorque = force;
        frontRightWheel.brakeTorque = force;
        rearLeftWheel.brakeTorque = force;
        rearRightWheel.brakeTorque = force;
    }

    // Zeros all torque and brake so nothing is stuck from a previous state
    void ClearAllBrakes()
    {
        if (frontLeftWheel) { frontLeftWheel.brakeTorque = 0f; frontLeftWheel.motorTorque = 0f; }
        if (frontRightWheel) { frontRightWheel.brakeTorque = 0f; frontRightWheel.motorTorque = 0f; }
        if (rearLeftWheel) { rearLeftWheel.brakeTorque = 0f; rearLeftWheel.motorTorque = 0f; }
        if (rearRightWheel) { rearRightWheel.brakeTorque = 0f; rearRightWheel.motorTorque = 0f; }
    }

    void OnDrawGizmos()
    {
        if (waypoints != null &&
            waypoints.Length > currentWaypointIndex &&
            waypoints[currentWaypointIndex] != null)
        {
            Vector3 target = waypoints[currentWaypointIndex].position
                             + currentWaypointOffset;

            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, target);
            Gizmos.DrawWireSphere(target, waypointRadius);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(
                waypoints[currentWaypointIndex].position, 0.5f);
        }
    }
}