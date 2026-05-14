using UnityEngine;

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

    public bool raceStarted = false;

    private WheelControl[] wheels;
    private Rigidbody rigidBody;
    private CarInputActions carControls;

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
    }

    void FixedUpdate()
    {
        // Platform and audio always run regardless of race state
        Vector3 localAngVelocity = transform.InverseTransformDirection(
            rigidBody.angularVelocity);
        float rollAngle = Mathf.DeltaAngle(transform.eulerAngles.z, 0);

        PlatformController.singleton.Yaw = localAngVelocity.y * 10;

        if (!GameManager.Instance.isCrashing)
            PlatformController.singleton.Roll = Mathf.Lerp(
                PlatformController.singleton.Roll, -rollAngle, Time.deltaTime);

        float speedKMH = rigidBody.linearVelocity.magnitude * 3.6f;
        if (Engine != null)
            Engine.pitch = 0.4f + (speedKMH / 180f) * 0.8f;

        // Block movement before race starts
        if (!raceStarted)
        {
            foreach (var wheel in wheels)
            {
                wheel.WheelCollider.motorTorque = 0f;
                wheel.WheelCollider.brakeTorque = brakeTorque;
            }
            return;
        }

     
        Vector2 inputVector = carControls.Car.Movement.ReadValue<Vector2>();

        float vInput = inputVector.y;
        float hInput = inputVector.x;

        float forwardSpeed = Vector3.Dot(transform.forward, rigidBody.linearVelocity);
        float speedFactor = Mathf.InverseLerp(0, maxSpeed, Mathf.Abs(forwardSpeed));

        float currentMotorTorque = Mathf.Lerp(motorTorque, 0, speedFactor);
        float currentSteerRange = Mathf.Lerp(steeringRange,
            steeringRangeAtMaxSpeed, speedFactor);

        bool isAccelerating = Mathf.Sign(vInput) == Mathf.Sign(forwardSpeed);

        foreach (var wheel in wheels)
        {
            if (wheel.steerable)
                wheel.WheelCollider.steerAngle = hInput * currentSteerRange;

            if (isAccelerating)
            {
                if (wheel.motorized)
                    wheel.WheelCollider.motorTorque = vInput * currentMotorTorque;
                wheel.WheelCollider.brakeTorque = 0f;
            }
            else
            {
                wheel.WheelCollider.motorTorque = 0f;
                wheel.WheelCollider.brakeTorque = Mathf.Abs(vInput) * brakeTorque;
            }
        }
    }
}