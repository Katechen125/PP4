using UnityEngine;
using TMPro;
using System.Collections;

public class CarControl : MonoBehaviour
{
    [Header("Car Properties")]
    public float motorTorque = 2000f;
    public float brakeTorque = 2000f;
    public float maxSpeed = 20f;
    public float steeringRange = 30f;
    public float steeringRangeAtMaxSpeed = 10f;
    public float centreOfGravityOffset = -1f;
    public AudioSource Engine;

    [Header("Gear System")]
    public int totalGears = 6;

    [Header("Gear HUD")]
    public TextMeshProUGUI gearText;

    [HideInInspector]
    public bool raceStarted = false;

    private bool platformConnected = false;
    private int currentGear = 0; // -1=R, 0=N, 1-6=forward
    private WheelControl[] wheels;
    private Rigidbody rigidBody;
    private CarInputActions carControls;
    private bool wasGrounded = true;
    private float airTime = 0f;

    private float[] ratios = new float[]
    {
        8.0f,  // G1
        4.5f,  // G2
        2.5f,  // G3
        1.5f,  // G4
        1.0f,  // G5
        0.6f   // G6
    };

    void Awake()
    {
        carControls = new CarInputActions();
        platformConnected = false;

        // Attempt connection with a real round-trip check
        try
        {
            if (PlatformController.singleton == null)
            {
                Debug.LogWarning("[Platform] No PlatformController in scene.");
                platformConnected = false;
                return;
            }

            PlatformController.singleton.Init("COM5", 115200);

            // Give the serial port a tiny moment, then probe
            System.Threading.Thread.Sleep(50);

            // Try a write and verify it didn't immediately disconnect
            PlatformController.singleton.Pitch = 0f;
            PlatformController.singleton.Roll = 0f;
            PlatformController.singleton.Yaw = 0f;
            PlatformController.singleton.Heave = 0f;
            PlatformController.singleton.Surge = 0f;

            // If your PlatformController exposes IsOpen / IsConnected, check it.
            // Adjust this line to whatever property your controller has.
            // Common patterns: .IsConnected, .IsOpen, .Connected, .Port.IsOpen
            var connectedProp = PlatformController.singleton.GetType()
                .GetProperty("IsConnected");
            if (connectedProp != null)
            {
                object val = connectedProp.GetValue(PlatformController.singleton);
                platformConnected = val is bool b && b;
            }
            else
            {
                // Fall back: assume connected only if no exception fired
                platformConnected = true;
            }

            if (platformConnected)
                Debug.Log("[Platform] Connected on COM5");
            else
                Debug.LogWarning("[Platform] Init ran but port not open.");
        }
        catch (System.Exception e)
        {
            platformConnected = false;
            Debug.LogWarning($"[Platform] Offline — {e.Message}");
        }
    }
    void OnEnable() { carControls.Enable(); }
    void OnDisable() { carControls.Disable(); }

    void Start()
    {
        rigidBody = GetComponent<Rigidbody>();

        Vector3 centerOfMass = rigidBody.centerOfMass;
        centerOfMass.y += centreOfGravityOffset;
        rigidBody.centerOfMass = centerOfMass;

        wheels = GetComponentsInChildren<WheelControl>();

        if (Engine != null)
        {
            Engine.loop = true;
            Engine.Play();
        }

        currentGear = 0;
        UpdateGearHUD();
    }

    // Safe platform setter — does nothing if platform not connected
    void SetPlatform(string property, float value)
    {
        if (!platformConnected) return;

        try
        {
            switch (property)
            {
                case "Yaw": PlatformController.singleton.Yaw = value; break;
                case "Roll": PlatformController.singleton.Roll = value; break;
                case "Heave": PlatformController.singleton.Heave = value; break;
                case "Surge": PlatformController.singleton.Surge = value; break;
                case "Pitch": PlatformController.singleton.Pitch = value; break;
            }
        }
        catch (System.Exception e)
        {
            // Disable permanently for this session, no spam
            platformConnected = false;
            Debug.LogWarning($"[Platform] Lost connection: {e.Message}");
        }
    }

    void UpdateHeave()
    {
        if (!platformConnected) return;

        // Check if all wheels are off the ground
        bool anyWheelGrounded = false;
        foreach (var wheel in wheels)
        {
            WheelHit hit;
            if (wheel.WheelCollider.GetGroundHit(out hit))
            {
                anyWheelGrounded = true;
                break;
            }
        }

        bool isAirborne = !anyWheelGrounded;

        if (isAirborne)
        {
            airTime += Time.fixedDeltaTime;

            // The longer in the air the more heave
            float heaveIntensity = Mathf.Clamp(airTime * 3f, 0f, 8f);

            // Oscillate while airborne — bumpy feeling
            float heave = Mathf.Sin(Time.time * 8f) * heaveIntensity;
            SetPlatform("Heave", heave);

            wasGrounded = false;
        }
        else if (!wasGrounded && !isAirborne)
        {
            // Just landed — big thump
            float landingForce = Mathf.Clamp(airTime * 10f, 3f, 12f);
            SetPlatform("Heave", -landingForce);
            Invoke(nameof(ResetHeave), 0.3f);

            airTime = 0f;
            wasGrounded = true;


        }

        // Wall hit detection — sudden lateral velocity change
        float lateralVelocity = Mathf.Abs(
            Vector3.Dot(rigidBody.linearVelocity, transform.right));

        if (lateralVelocity > 5f && !GameManager.Instance.isCrashing)
        {
            // Sideways slam into wall
            float wallForce = Mathf.Clamp(lateralVelocity * 0.5f, 2f, 10f);
            SetPlatform("Heave", wallForce);
            SetPlatform("Roll", lateralVelocity > 0 ? wallForce : -wallForce);
            Invoke(nameof(ResetHeaveAndRoll), 0.25f);


        }
    }

    void ResetHeave()
    {
        SetPlatform("Heave", 0f);
    }

    void ResetHeaveAndRoll()
    {
        SetPlatform("Heave", 0f);
        SetPlatform("Roll", 0f);
    }
    void Update()
    {
        bool shiftUp = false;
        bool shiftDown = false;

        try
        {
            shiftUp = carControls.Car.ShiftUp.WasPressedThisFrame();
            shiftDown = carControls.Car.ShiftDown.WasPressedThisFrame();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Shift error: {e.Message}");
        }

        if (shiftUp && currentGear < totalGears)
        {
            currentGear++;
            UpdateGearHUD();
            OnShift();
        }

        if (shiftDown && currentGear > -1)
        {
            currentGear--;
            UpdateGearHUD();
            OnShift();
        }
    }

    void OnShift()
    {
        if (Engine != null)
            StartCoroutine(ShiftDip());

        if (currentGear > 0)
        {
            // Upshift — forward lurch
            SetPlatform("Heave", currentGear % 2 == 0 ? 5f : -5f);
            SetPlatform("Surge", -4f);
            Invoke(nameof(ResetPlatformShift), 0.25f);
        }
        else if (currentGear >= 0)
        {
            // Downshift — backward push from engine braking
            SetPlatform("Surge", 6f);
            SetPlatform("Heave", -3f);
            Invoke(nameof(ResetPlatformShift), 0.3f);
        }
    }
    void ResetPlatformShift()
    {
        SetPlatform("Heave", 0f);
        SetPlatform("Surge", 0f);
    }

    IEnumerator ShiftDip()
    {
        float originalPitch = Engine.pitch;
        Engine.pitch = originalPitch * 0.5f;
        yield return new WaitForSeconds(0.05f);

        float t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            Engine.pitch = Mathf.Lerp(
                originalPitch * 0.5f,
                originalPitch * 1.2f,
                t / 0.2f);
            yield return null;
        }

        Engine.pitch = originalPitch;
    }

    void UpdateGearHUD()
    {
        if (gearText != null)
            gearText.text = GetGearName();
    }

    string GetGearName()
    {
        if (currentGear == -1) return "R";
        if (currentGear == 0) return "N";
        return $"G{currentGear}";
    }

    void FixedUpdate()
    {
        // Read input FIRST so gas/brake available throughout
        Vector2 inputVector = carControls.Car.Movement.ReadValue<Vector2>();
        float hInput = inputVector.x;
        float rawY = inputVector.y;
        float gas = Mathf.Max(0f, rawY);
        float brake = Mathf.Max(0f, -rawY);

        // Platform motion
        Vector3 localAngVelocity = transform.InverseTransformDirection(
            rigidBody.angularVelocity);
        Vector3 localVelocity = transform.InverseTransformDirection(
            rigidBody.linearVelocity);

        float targetYaw = localAngVelocity.y * 15f;
        float physicalRoll = Mathf.DeltaAngle(transform.eulerAngles.z, 0);
        float lateralG = localVelocity.x * 0.3f;
        float targetRoll = (-physicalRoll * 0.5f) + lateralG;

        float targetPitch = 0f;
        if (gas > 0.1f)
            targetPitch = -gas * 2f;
        else if (brake > 0.1f)
            targetPitch = brake * 3f;

        if (GameManager.Instance != null && !GameManager.Instance.isCrashing)
        {
            float currentYaw = platformConnected ?
                PlatformController.singleton.Yaw : 0f;
            float currentRoll = platformConnected ?
                PlatformController.singleton.Roll : 0f;
            float currentPitch = platformConnected ?
                PlatformController.singleton.Pitch : 0f;

            SetPlatform("Yaw", Mathf.Lerp(
                currentYaw, targetYaw, Time.deltaTime * 8f));
            SetPlatform("Roll", Mathf.Lerp(
                currentRoll, targetRoll, Time.deltaTime * 4f));
            SetPlatform("Pitch", Mathf.Lerp(
                currentPitch, targetPitch, Time.deltaTime * 6f));
        }

        // Engine pitch
        float speedKMH = rigidBody.linearVelocity.magnitude * 3.6f;
        if (Engine != null)
            Engine.pitch = 0.4f + (speedKMH / 180f) * 0.8f;

        // Block before race
        if (!raceStarted)
        {
            foreach (var wheel in wheels)
            {
                // Apply gas weakly so player CAN creep forward for a false start
                if (wheel.motorized && gas > 0.01f)
                    wheel.WheelCollider.motorTorque = gas * motorTorque * 0.3f;
                else
                    wheel.WheelCollider.motorTorque = 0f;

                if (wheel.steerable)
                    wheel.WheelCollider.steerAngle = hInput * steeringRange;

                // Brake holds the car only when no gas given
                wheel.WheelCollider.brakeTorque = gas > 0.01f ? 0f : brakeTorque * 0.5f;
            }
            return;
        }

        // Heave
        UpdateHeave();

        // Platform surge
        if (gas > 0.1f)
            SetPlatform("Surge", -gas * 3f);
        else if (brake > 0.1f)
            SetPlatform("Surge", brake * 5f);
        else
            SetPlatform("Surge", 0f);

        float forwardSpeed = Vector3.Dot(
            transform.forward, rigidBody.linearVelocity);

        float speedFactor = Mathf.InverseLerp(
            0, maxSpeed, Mathf.Abs(forwardSpeed));

        float currentSteerRange = Mathf.Lerp(
            steeringRange, steeringRangeAtMaxSpeed, speedFactor);

        foreach (var wheel in wheels)
        {
            if (wheel.steerable)
                wheel.WheelCollider.steerAngle = hInput * currentSteerRange;

            if (currentGear == 0)
            {
                wheel.WheelCollider.motorTorque = 0f;
                wheel.WheelCollider.brakeTorque = 0f;
            }
            else if (currentGear == -1)
            {
                if (gas > 0.01f)
                {
                    if (wheel.motorized)
                        wheel.WheelCollider.motorTorque =
                            -gas * motorTorque * 0.5f;
                    wheel.WheelCollider.brakeTorque = 0f;
                }
                else if (brake > 0.01f)
                {
                    wheel.WheelCollider.motorTorque = 0f;
                    wheel.WheelCollider.brakeTorque = brake * brakeTorque;
                }
                else
                {
                    wheel.WheelCollider.motorTorque = 0f;
                    wheel.WheelCollider.brakeTorque = 0f;
                }
            }
            else
            {
                if (gas > 0.01f)
                {
                    if (wheel.motorized)
                        wheel.WheelCollider.motorTorque =
                            GetGearTorque(gas, forwardSpeed);
                    wheel.WheelCollider.brakeTorque = 0f;
                }
                else if (brake > 0.01f)
                {
                    wheel.WheelCollider.motorTorque = 0f;
                    wheel.WheelCollider.brakeTorque = brake * brakeTorque;
                }
                else
                {
                    wheel.WheelCollider.motorTorque = 0f;
                    wheel.WheelCollider.brakeTorque = 0f;
                }
            }
        }
    }
    float GetGearTorque(float gas, float forwardSpeed)
    {
        if (currentGear == 0) return 0f;
        if (currentGear == -1)
            return -gas * motorTorque * 0.5f;

        float ratio = ratios[currentGear - 1];
        float gearTorque = motorTorque * ratio;
        float gearMaxSpeed = maxSpeed * ratio;

        float gearSpeedFactor = Mathf.InverseLerp(
            0, gearMaxSpeed, Mathf.Abs(forwardSpeed));

        // Hard cap — if over this gear's max speed, 
        // apply engine braking automatically
        if (Mathf.Abs(forwardSpeed) > gearMaxSpeed)
        {
            float overSpeed = Mathf.Abs(forwardSpeed) - gearMaxSpeed;
            return -overSpeed * 500f; // engine braking force
        }

        return gas * Mathf.Lerp(gearTorque, 0f, gearSpeedFactor);
    }
}