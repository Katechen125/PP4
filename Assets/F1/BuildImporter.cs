using UnityEngine;

// Reads the build directly from BuildGameManager.Instance (SPEEDKRAFT's
// data, which survives the scene load via DontDestroyOnLoad) instead of a
// JSON file on disk. Requires both projects to be merged into the same
// Unity project, with SPEEDKRAFT's scene loaded before this one.
//
// NOTE: this used to also scale WheelCollider forwardFriction/sideways
// Friction stiffness based on Downforce Match, that's been removed. It
// was the likely cause of wheels permanently locking up after a crash:
// modifying friction stiffness combined with the odd contact angles a
// wheel can end up at after hitting a wall (partially embedded in track
// geometry, resting at a steep angle) can push Unity's WheelCollider
// solver into a degenerate state where the wheel reports effectively
// zero friction forever, torque/brake/speed multipliers don't touch
// contact physics at all, so they're safe and stay.
public class BuildImporter : MonoBehaviour
{
    [Header("References")]
    public CarControl carControl;

    [Header("How strongly the build defines handling")]
    [Tooltip("Multiplier applied when a match score is 0 (worst possible fit).")]
    public float minFactor = 0.55f;
    [Tooltip("Multiplier applied when a match score is 100 (perfect fit).")]
    public float maxFactor = 1.65f;

    [Header("Debug")]
    public bool logOnImport = true;

    private float baseMotorTorque;
    private float baseBrakeTorque;
    private float baseMaxSpeed;
    private bool baseValuesCaptured;

    // Exposed for GameManager's end-of-race prediction comparison.
    public PerformanceResult LoadedResult { get; private set; }
    public QualifyingResult LoadedQuali { get; private set; }
    public RaceEstimateResult LoadedRace { get; private set; }
    public string LoadedCircuitName { get; private set; } = "Unknown";

    void Start()
    {
        if (carControl == null)
            carControl = GetComponent<CarControl>();

        if (carControl == null)
        {
            Debug.LogWarning("[BuildImporter] No CarControl reference assigned, skipping import.");
            return;
        }

        CaptureBaseValues();
        LoadFromBuildGameManager();
    }

    void CaptureBaseValues()
    {
        if (baseValuesCaptured) return;

        baseMotorTorque = carControl.motorTorque;
        baseBrakeTorque = carControl.brakeTorque;
        baseMaxSpeed = carControl.maxSpeed;

        baseValuesCaptured = true;
    }

    void LoadFromBuildGameManager()
    {
        if (BuildGameManager.Instance == null)
        {
            Debug.LogWarning("[BuildImporter] No BuildGameManager found in the scene. " +
                              "Did you skip SPEEDKRAFT and load this scene directly? " +
                              "Using your CarControl values unchanged.");
            return;
        }

        var result = BuildGameManager.Instance.GetLastResult();
        if (result == null || !result.isValid)
        {
            Debug.LogWarning("[BuildImporter] No valid SPEEDKRAFT build found. " +
                              "Using your CarControl values unchanged.");
            return;
        }

        LoadedResult = result;

        Circuit circuit = CircuitSelectionController.ChosenCircuit;
        LoadedCircuitName = circuit != null ? circuit.circuitName : "Unknown";

        LoadedQuali = QualifyingEstimator.Estimate(result, circuit);
        LoadedRace = LoadedQuali.isValid
            ? QualifyingEstimator.EstimateRace(result, circuit, LoadedQuali)
            : null;

        if (logOnImport)
            Debug.Log($"[BuildImporter] Loaded build for {LoadedCircuitName}: " +
                      $"{result.efficiencyScore}/100 ({result.circuitRating})");

        ApplyToCar(result);
    }

    void ApplyToCar(PerformanceResult result)
    {
        float engineFactor = Mathf.Lerp(minFactor, maxFactor, result.engineMatch / 100f);
        float dragFactor = Mathf.Lerp(minFactor, maxFactor, result.dragMatch / 100f);
        float reliabilityFactor = Mathf.Lerp(minFactor, maxFactor, result.tyreMatch / 100f);

        carControl.motorTorque = baseMotorTorque * engineFactor;
        carControl.maxSpeed = baseMaxSpeed * dragFactor;
        carControl.brakeTorque = baseBrakeTorque * reliabilityFactor;

        if (logOnImport)
            Debug.Log($"[BuildImporter] Applied: motorTorque x{engineFactor:0.00}, " +
                      $"maxSpeed x{dragFactor:0.00}, brakeTorque x{reliabilityFactor:0.00}");
    }

    public void ResetToBaseValues()
    {
        if (!baseValuesCaptured || carControl == null) return;

        carControl.motorTorque = baseMotorTorque;
        carControl.brakeTorque = baseBrakeTorque;
        carControl.maxSpeed = baseMaxSpeed;

        Debug.Log("[BuildImporter] Reset to base CarControl values.");
    }
}