using UnityEngine;
using TMPro;
using System.Collections;

public class CarControl : MonoBehaviour
{
    public enum InputMode { Keyboard, DrivingWheel }

    [Header("Input Mode (auto-detected on Start)")]
    public InputMode inputMode = InputMode.Keyboard;

    [Header("Car Properties")]
    public float motorTorque = 2000f;
    public float brakeTorque = 2000f;
    public float maxSpeed = 20f;
    public float steeringRange = 30f;
    public float steeringRangeAtMaxSpeed = 10f;
    public float centreOfGravityOffset = -1f;
    public AudioSource Engine;

    [Header("Steering Feel")]
    public float steeringSpeed = 8f;
    public float steeringReturnSpeed = 5f;
    public float wheelInputExponent = 0.7f;
    private float currentSteerAngle = 0f;

    // Keyboard presets
    private const float KB_SteeringSpeed = 8f;
    private const float KB_SteeringReturnSpeed = 5f;
    private const float KB_InputExponent = 1.0f;
    private const float KB_SteeringRange = 30f;
    private const float KB_SteeringRangeMax = 10f;

    // Driving wheel presets
    private const float DW_SteeringSpeed = 12f;
    private const float DW_SteeringReturnSpeed = 6f;
    private const float DW_InputExponent = 0.65f;
    private const float DW_SteeringRange = 28f;
    private const float DW_SteeringRangeMax = 8f;

    [Header("Gear System")]
    public int totalGears = 6;

    [Header("Gear HUD")]
    public TextMeshProUGUI gearText;

    [HideInInspector]
    public bool raceStarted = false;

    private bool platformConnected = false;
    private int currentGear = 0;
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

        try
        {
            if (PlatformController.singleton == null)
            {
                Debug.LogWarning("[Platform] No PlatformController in scene.");
                platformConnected = false;
                return;
            }

            PlatformController.singleton.Init("COM5", 115200);
            System.Threading.Thread.Sleep(50);

            PlatformController.singleton.Pitch = 0f;
            PlatformController.singleton.Roll = 0f;
            PlatformController.singleton.Yaw = 0f;
            PlatformController.singleton.Heave = 0f;
            PlatformController.singleton.Surge = 0f;

            var connectedProp = PlatformController.singleton.GetType()
                .GetProperty("IsConnected");
            if (connectedProp != null)
            {
                object val = connectedProp.GetValue(PlatformController.singleton);
                platformConnected = val is bool b && b;
            }
            else
            {
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

  
    public void ApplyInputPreset()
    {
        if (inputMode == InputMode.Keyboard)
        {
            steeringSpeed = KB_SteeringSpeed;
            steeringReturnSpeed = KB_SteeringReturnSpeed;
            wheelInputExponent = KB_InputExponent;
            steeringRange = KB_SteeringRange;
            steeringRangeAtMaxSpeed = KB_SteeringRangeMax;
            Debug.Log("[CarControl] Keyboard preset applied");
        }
        else
        {
            steeringSpeed = DW_SteeringSpeed;
            steeringReturnSpeed = DW_SteeringReturnSpeed;
            wheelInputExponent = DW_InputExponent;
            steeringRange = DW_SteeringRange;
            steeringRangeAtMaxSpeed = DW_SteeringRangeMax;
            Debug.Log("[CarControl] Driving Wheel preset applied");
        }
    }

    // =========================================================
    // START
    // =========================================================

    void Start()
    {
        // Just apply whichever mode is set in the inspector
        ApplyInputPreset();

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

    // =========================================================
    // PLATFORM
    // =========================================================

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
            platformConnected = false;
            Debug.LogWarning($"[Platform] Lost connection: {e.Message}");
        }
    }

    void UpdateHeave()
    {
        if (!platformConnected) return;

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
            float heaveIntensity = Mathf.Clamp(airTime * 3f, 0f, 8f);
            float heave = Mathf.Sin(Time.time * 8f) * heaveIntensity;
            SetPlatform("Heave", heave);
            wasGrounded = false;
        }
        else if (!wasGrounded && !isAirborne)
        {
            float landingForce = Mathf.Clamp(airTime * 10f, 3f, 12f);
            SetPlatform("Heave", -landingForce);
            Invoke(nameof(ResetHeave), 0.3f);
            airTime = 0f;
            wasGrounded = true;
        }

        float lateralVelocity = Mathf.Abs(
            Vector3.Dot(rigidBody.linearVelocity, transform.right));

        if (lateralVelocity > 5f && !GameManager.Instance.isCrashing)
        {
            float wallForce = Mathf.Clamp(lateralVelocity * 0.5f, 2f, 10f);
            SetPlatform("Heave", wallForce);
            SetPlatform("Roll", lateralVelocity > 0 ? wallForce : -wallForce);
            Invoke(nameof(ResetHeaveAndRoll), 0.25f);
        }
    }

    void ResetHeave() { SetPlatform("Heave", 0f); }

    void ResetHeaveAndRoll()
    {
        SetPlatform("Heave", 0f);
        SetPlatform("Roll", 0f);
    }

    // =========================================================
    // UPDATE — input, gear shifts, camera, buttons
    // =========================================================

    void Update()
    {
        if (SimpleInput.GetKeyDown(KeyCode.M) ||
            carControls.Car.Replay.WasPerformedThisFrame())
            GameManager.Instance.ReplayRace();

        if (carControls.Car.Quit.WasPerformedThisFrame())
           Application.Quit();

        if (carControls.Car.Camera.WasPerformedThisFrame())
        {
            if (CameraController.Instance != null)
                CameraController.Instance.CycleView();
            else
                Debug.LogWarning("[CarControl] CameraController.Instance is null");
        }

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
            SetPlatform("Heave", currentGear % 2 == 0 ? 5f : -5f);
            SetPlatform("Surge", -4f);
            Invoke(nameof(ResetPlatformShift), 0.25f);
        }
        else if (currentGear >= 0)
        {
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

    // =========================================================
    // STEERING CURVE
    // =========================================================

    float ApplySteeringCurve(float input)
    {
        return Mathf.Sign(input) *
               Mathf.Pow(Mathf.Abs(input), wheelInputExponent);
    }

    // =========================================================
    // FIXED UPDATE — physics
    // =========================================================

    void FixedUpdate()
    {
        Vector2 inputVector = carControls.Car.Movement.ReadValue<Vector2>();
        float rawH = inputVector.x;
        float rawY = inputVector.y;
        float gas = Mathf.Max(0f, rawY);
        float brake = Mathf.Max(0f, -rawY);

        float hInput = ApplySteeringCurve(rawH);

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
            float currentYaw = platformConnected ? PlatformController.singleton.Yaw : 0f;
            float currentRoll = platformConnected ? PlatformController.singleton.Roll : 0f;
            float currentPitch = platformConnected ? PlatformController.singleton.Pitch : 0f;

            SetPlatform("Yaw", Mathf.Lerp(currentYaw, targetYaw, Time.deltaTime * 8f));
            SetPlatform("Roll", Mathf.Lerp(currentRoll, targetRoll, Time.deltaTime * 4f));
            SetPlatform("Pitch", Mathf.Lerp(currentPitch, targetPitch, Time.deltaTime * 6f));
        }

        float speedKMH = rigidBody.linearVelocity.magnitude * 3.6f;
        if (Engine != null)
            Engine.pitch = 0.4f + (speedKMH / 180f) * 0.8f;

        float forwardSpeed = Vector3.Dot(
            transform.forward, rigidBody.linearVelocity);

        float speedFactor = Mathf.InverseLerp(
            0, maxSpeed, Mathf.Abs(forwardSpeed));

        float targetSteerRange = Mathf.Lerp(
            steeringRange, steeringRangeAtMaxSpeed, speedFactor);

        float targetSteerAngle = hInput * targetSteerRange;

        float blendSpeed = Mathf.Abs(rawH) > 0.01f
            ? steeringSpeed
            : steeringReturnSpeed;

        currentSteerAngle = Mathf.Lerp(
            currentSteerAngle,
            targetSteerAngle,
            Time.fixedDeltaTime * blendSpeed);

        // Block before race — allow creep for false start detection
        if (!raceStarted)
        {
            foreach (var wheel in wheels)
            {
                if (wheel.motorized && gas > 0.01f)
                    wheel.WheelCollider.motorTorque = gas * motorTorque * 0.3f;
                else
                    wheel.WheelCollider.motorTorque = 0f;

                if (wheel.steerable)
                    wheel.WheelCollider.steerAngle = currentSteerAngle;

                wheel.WheelCollider.brakeTorque =
                    gas > 0.01f ? 0f : brakeTorque * 0.5f;
            }
            return;
        }

        UpdateHeave();

        if (gas > 0.1f)
            SetPlatform("Surge", -gas * 3f);
        else if (brake > 0.1f)
            SetPlatform("Surge", brake * 5f);
        else
            SetPlatform("Surge", 0f);

        foreach (var wheel in wheels)
        {
            if (wheel.steerable)
                wheel.WheelCollider.steerAngle = currentSteerAngle;

            if (currentGear == 0)
            {
                wheel.WheelCollider.motorTorque = 0f;
                wheel.WheelCollider.brakeTorque = 0f;
            }
            else if (currentGear == -1)
            {
                if (gas > 0.01f)
                {
                    rigidBody.linearDamping = 0f;
                    if (wheel.motorized)
                        wheel.WheelCollider.motorTorque =
                            -gas * motorTorque * 0.5f;
                    wheel.WheelCollider.brakeTorque = 0f;
                }
                else if (brake > 0.01f)
                {
                    rigidBody.linearDamping = 0f;
                    wheel.WheelCollider.motorTorque = 0f;
                    wheel.WheelCollider.brakeTorque = brake * brakeTorque;
                }
                else
                {
                    rigidBody.linearDamping = 0.1f;
                    wheel.WheelCollider.motorTorque = 0f;
                    wheel.WheelCollider.brakeTorque = 0f;
                }
            }
            else
            {
                if (gas > 0.01f)
                {
                    rigidBody.linearDamping = 0f;
                    if (wheel.motorized)
                        wheel.WheelCollider.motorTorque =
                            GetGearTorque(gas, forwardSpeed);
                    wheel.WheelCollider.brakeTorque = 0f;
                }
                else if (brake > 0.01f)
                {
                    rigidBody.linearDamping = 0f;
                    wheel.WheelCollider.motorTorque = 0f;

                    float brakeScale = Mathf.Lerp(0.5f, 1f,
                        Mathf.InverseLerp(200f, 0f, speedKMH));
                    wheel.WheelCollider.brakeTorque =
                        brake * brakeTorque * brakeScale;
                }
                else
                {
                    // Coasting — explicitly zero motor
                    wheel.WheelCollider.motorTorque = 0f;

                    if (wheel.motorized)
                    {
                        float engineBrake = Mathf.Lerp(0f, 600f,
                            Mathf.InverseLerp(0f, 200f, speedKMH));
                        wheel.WheelCollider.brakeTorque = engineBrake;
                    }
                    else
                    {
                        wheel.WheelCollider.brakeTorque = 0f;
                    }

                    rigidBody.linearDamping = 0.1f;
                }
            }
        }
    }

    // =========================================================
    // GEAR TORQUE
    // =========================================================

    float GetGearTorque(float gas, float forwardSpeed)
    {
        if (currentGear == 0) return 0f;
        if (currentGear == -1) return -gas * motorTorque * 0.5f;
        if (gas <= 0.01f) return 0f;

        float ratio = ratios[currentGear - 1];
        float gearTorque = motorTorque * ratio;
        float gearMaxSpeed = maxSpeed * ratio;

        if (Mathf.Abs(forwardSpeed) > gearMaxSpeed)
        {
            float overSpeed = Mathf.Abs(forwardSpeed) - gearMaxSpeed;
            return -overSpeed * 500f;
        }

        float gearSpeedFactor = Mathf.InverseLerp(
            0, gearMaxSpeed, Mathf.Abs(forwardSpeed));

        return gas * Mathf.Lerp(gearTorque, 0f, gearSpeedFactor);
    }
}