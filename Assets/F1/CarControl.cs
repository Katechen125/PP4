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

    private int currentGear = 0; // -1=R, 0=N, 1-6=forward
    private WheelControl[] wheels;
    private Rigidbody rigidBody;
    private CarInputActions carControls;

    private float[] ratios = new float[]
    {
        8.0f,  // G1 — brutal launch
        4.5f,  // G2
        2.5f,  // G3
        1.5f,  // G4
        1.0f,  // G5
        0.6f   // G6 — top speed
    };

    void Awake()
    {
        carControls = new CarInputActions();
        PlatformController.singleton.Init("COM5", 115200);
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
            Debug.LogError($"Shift paddle error: {e.Message}");
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
            PlatformController.singleton.Heave =
                currentGear % 2 == 0 ? 5f : -5f;
            PlatformController.singleton.Surge = -4f;
            Invoke(nameof(ResetPlatformShift), 0.25f);
        }
    }

    void ResetPlatformShift()
    {
        PlatformController.singleton.Heave = 0f;
        PlatformController.singleton.Surge = 0f;
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
        // Platform
        Vector3 localAngVelocity = transform.InverseTransformDirection(
            rigidBody.angularVelocity);
        float rollAngle = Mathf.DeltaAngle(transform.eulerAngles.z, 0);

        PlatformController.singleton.Yaw = localAngVelocity.y * 10;

        if (!GameManager.Instance.isCrashing)
            PlatformController.singleton.Roll = Mathf.Lerp(
                PlatformController.singleton.Roll,
                -rollAngle,
                Time.deltaTime);

        // Engine pitch
        float speedKMH = rigidBody.linearVelocity.magnitude * 3.6f;
        if (Engine != null)
            Engine.pitch = 0.4f + (speedKMH / 180f) * 0.8f;

        // Block before race
        if (!raceStarted)
        {
            foreach (var wheel in wheels)
            {
                wheel.WheelCollider.motorTorque = 0f;
                wheel.WheelCollider.brakeTorque = brakeTorque;
            }
            return;
        }

        // Read input
        Vector2 inputVector = carControls.Car.Movement.ReadValue<Vector2>();
        float hInput = inputVector.x;
        float rawY = inputVector.y;
       
        // Split Y into gas and brake
        // Wheel: positive Y = gas, negative Y = brake
        // Keyboard: W = positive Y = gas, S = negative Y = brake
        float gas = Mathf.Max(0f, rawY);   // 0 to 1
        float brake = Mathf.Max(0f, -rawY);  // 0 to 1

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
                // Neutral — free coast
                wheel.WheelCollider.motorTorque = 0f;
                wheel.WheelCollider.brakeTorque = 0f;
            }
            else if (currentGear == -1)
            {
                // Reverse — gas drives backward
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
                    wheel.WheelCollider.brakeTorque =
                        brake * brakeTorque;
                }
                else
                {
                    wheel.WheelCollider.motorTorque = 0f;
                    wheel.WheelCollider.brakeTorque = 0f;
                }
            }
            else
            {
                // Forward gears
                if (gas > 0.01f)
                {
                    // Gas — drive forward only
                    if (wheel.motorized)
                        wheel.WheelCollider.motorTorque =
                            GetGearTorque(gas, forwardSpeed);
                    wheel.WheelCollider.brakeTorque = 0f;
                }
                else if (brake > 0.01f)
                {
                    // Brake — never drive backward
                    wheel.WheelCollider.motorTorque = 0f;
                    wheel.WheelCollider.brakeTorque =
                        brake * brakeTorque;
                }
                else
                {
                    // Coast
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

        return gas * Mathf.Lerp(gearTorque, 0, gearSpeedFactor);
    }
}