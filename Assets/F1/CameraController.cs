using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance;

    [Header("Target")]
    public Transform car;

    [Header("Follow Cam Settings")]
    public float followDistance = 6f;
    public float followHeight = 2f;
    public float smoothSpeed = 5f;

    [Header("Driver View Offsets")]
    public float driverForwardOffset = 0.5f;
    public float driverHeightOffset = 0.8f;

    [Header("Rear View Settings")]
    public float rearDistance = 5f;
    public float rearHeight = 1.8f;

    [Header("Overview Settings")]
    public float overviewHeight = 80f;
    public float overviewDistance = 40f;

    private int currentView = 0;
    // 0 = follow, 1 = driver, 2 = rear, 3 = overview
    private const int TOTAL_VIEWS = 4;

    void Awake()
    {
        Instance = this;
    }
    void Update()
    {
        if (Keyboard.current.cKey.wasPressedThisFrame)
            CycleView();
    }

    public void CycleView()
    {
        currentView = (currentView + 1) % TOTAL_VIEWS;
    }

    string GetViewName()
    {
        switch (currentView)
        {
            case 0: return "Follow";
            case 1: return "Driver";
            case 2: return "Rear";
            case 3: return "Overview";
            default: return "Unknown";
        }
    }

    void LateUpdate()
    {
        if (car == null) return;

        switch (currentView)
        {
            case 0: FollowCar(); break;
            case 1: DriverView(); break;
            case 2: RearView(); break;
            case 3: TrackOverview(); break;
        }
    }

    void FollowCar()
    {
        Vector3 targetPos = car.position
            - car.forward * followDistance
            + Vector3.up * followHeight;

        transform.position = Vector3.Lerp(
            transform.position, targetPos, Time.deltaTime * smoothSpeed);

        transform.LookAt(car.position + Vector3.up * 0.5f);
    }

    void DriverView()
    {
        Vector3 cockpitPos = car.position
            + car.forward * driverForwardOffset
            + Vector3.up * driverHeightOffset;

        transform.position = Vector3.Lerp(
            transform.position, cockpitPos, Time.deltaTime * smoothSpeed);

        Vector3 lookTarget = car.position
            + car.forward * 10f
            + Vector3.up * (driverHeightOffset - 0.3f);

        transform.LookAt(lookTarget);
    }

    // Rear-view: camera sits just in front of the cockpit, looking BACKWARDS
    // so the player can see behind them (good for blocking AI cars)
    void RearView()
    {
        Vector3 rearPos = car.position
            + car.forward * 0.3f
            + Vector3.up * rearHeight;

        transform.position = Vector3.Lerp(
            transform.position, rearPos, Time.deltaTime * smoothSpeed);

        // Look behind the car
        Vector3 lookTarget = car.position
            - car.forward * rearDistance
            + Vector3.up * (rearHeight - 0.3f);

        transform.LookAt(lookTarget);
    }

    void TrackOverview()
    {
        Vector3 targetPos = new Vector3(
            car.position.x,
            car.position.y + overviewHeight,
            car.position.z - overviewDistance);

        transform.position = Vector3.Lerp(
            transform.position, targetPos,
            Time.deltaTime * (smoothSpeed * 0.5f));

        transform.LookAt(car.position);
    }
}