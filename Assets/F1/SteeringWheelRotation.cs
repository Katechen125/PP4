using UnityEngine;
using UnityEngine.InputSystem;

public class SteeringWheelRotation : MonoBehaviour
{
    [Header("Car")]
    public CarControl carControl;

    [Header("Rotation Settings")]
    public float maxRotationDegrees = 360f;  // max wheel turn
    public float smoothSpeed = 8f;    // how fast it follows input
    public float keyboardSmooth = 4f;    // smoother for keyboard

    [Header("Keyboard Steering")]
    public float keyboardSteerSpeed = 2f;   // how fast keyboard builds up
    public float keyboardReturnSpeed = 3f;   // how fast it returns to center

    private CarInputActions carControls;
    private float currentAngle = 0f;
    private float targetAngle = 0f;
    private float keyboardAxis = 0f;
    private bool isUsingKeyboard = false;

    void Start()
    {
        carControls = new CarInputActions();
        carControls.Enable();
    }

    void Update()
    {
        Vector2 input = carControls.Car.Movement.ReadValue<Vector2>();
        float steerInput = input.x;

        // Detect if keyboard or wheel is being used
        // Keyboard gives exact -1, 0, or 1
        // Wheel gives smooth analog values between
        bool keyboardLeft = Keyboard.current.aKey.isPressed ||
                             Keyboard.current.leftArrowKey.isPressed;
        bool keyboardRight = Keyboard.current.dKey.isPressed ||
                             Keyboard.current.rightArrowKey.isPressed;

        isUsingKeyboard = keyboardLeft || keyboardRight ||
                          (!keyboardLeft && !keyboardRight &&
                           (steerInput == 0f ||
                            steerInput == 1f ||
                            steerInput == -1f));

        if (isUsingKeyboard)
        {
            // Keyboard — gradually build up and return smoothly
            if (keyboardLeft)
                keyboardAxis = Mathf.MoveTowards(
                    keyboardAxis, -1f,
                    Time.deltaTime * keyboardSteerSpeed);
            else if (keyboardRight)
                keyboardAxis = Mathf.MoveTowards(
                    keyboardAxis, 1f,
                    Time.deltaTime * keyboardSteerSpeed);
            else
                keyboardAxis = Mathf.MoveTowards(
                    keyboardAxis, 0f,
                    Time.deltaTime * keyboardReturnSpeed);

            targetAngle = -keyboardAxis * maxRotationDegrees;

            // Smooth lerp for keyboard
            currentAngle = Mathf.LerpAngle(
                currentAngle, targetAngle,
                Time.deltaTime * keyboardSmooth);
        }
        else
        {
            // Steering wheel — direct but smoothed
            // Mimics the actual wheel position 1:1
            targetAngle = -steerInput * maxRotationDegrees;

            currentAngle = Mathf.LerpAngle(
                currentAngle, targetAngle,
                Time.deltaTime * smoothSpeed);
        }

        // Apply rotation to wheel mesh
        transform.localRotation = Quaternion.Euler(
            transform.localEulerAngles.x,
            transform.localEulerAngles.y,
            currentAngle);
    }

    void OnDisable()
    {
        carControls?.Disable();
    }
}