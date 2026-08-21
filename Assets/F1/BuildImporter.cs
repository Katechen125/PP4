using System;
using System.IO;
using UnityEngine;

[Serializable]
public class ImportedBuildData
{
    public string circuitName;
    public float efficiencyScore;
    public string circuitRating;

    public float downforceMatch;
    public float dragMatch;
    public float engineMatch;
    public float tyreMatch;

    public float estimatedQualiLapSeconds;
    public float totalRaceTimeSeconds;

    public int estimatedGridPosition;
    public int estimatedFinishPosition;
}

// Reads Documents/F1BuildExport.json (written by SPEEDKRAFT's "RACE WITH
// THIS BUILD" button) and lets it strongly define this car's handling:
// engine power, braking, top speed, and cornering grip. This is no longer
// a subtle nudge, a poorly-matched build should feel genuinely worse to
// drive, and a well-matched one genuinely better.
public class BuildImporter : MonoBehaviour
{
    [Header("References")]
    public CarControl carControl;

    [Header("How strongly the build defines handling")]
    [Tooltip("Multiplier applied when a match score is 0 (worst possible fit).")]
    public float minFactor = 0.55f;
    [Tooltip("Multiplier applied when a match score is 100 (perfect fit).")]
    public float maxFactor = 1.65f;

    [Header("Grip (from Downforce Match)")]
    [Tooltip("Multiplies WheelCollider forward/sideways friction stiffness.")]
    public float minGripFactor = 0.6f;
    public float maxGripFactor = 1.5f;

    [Header("Debug")]
    public bool logOnImport = true;

    private float baseMotorTorque;
    private float baseBrakeTorque;
    private float baseMaxSpeed;
    private bool baseValuesCaptured;

    private WheelControl[] wheels;
    private float[] baseForwardStiffness;
    private float[] baseSidewaysStiffness;

    public ImportedBuildData LoadedBuild { get; private set; }

    void Start()
    {
        if (carControl == null)
            carControl = GetComponent<CarControl>();

        if (carControl == null)
        {
            Debug.LogWarning("[BuildImporter] No CarControl reference assigned, skipping import.");
            return;
        }

        wheels = GetComponentsInChildren<WheelControl>();
        CaptureBaseValues();
        LoadAndApply();
    }

    void CaptureBaseValues()
    {
        if (baseValuesCaptured) return;

        baseMotorTorque = carControl.motorTorque;
        baseBrakeTorque = carControl.brakeTorque;
        baseMaxSpeed = carControl.maxSpeed;

        baseForwardStiffness = new float[wheels.Length];
        baseSidewaysStiffness = new float[wheels.Length];
        for (int i = 0; i < wheels.Length; i++)
        {
            var wc = wheels[i].WheelCollider;
            baseForwardStiffness[i] = wc.forwardFriction.stiffness;
            baseSidewaysStiffness[i] = wc.sidewaysFriction.stiffness;
        }

        baseValuesCaptured = true;
    }

    void LoadAndApply()
    {
        string path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "F1BuildExport.json");

        if (!File.Exists(path))
        {
            Debug.LogWarning($"[BuildImporter] No SPEEDKRAFT export found at {path}. Using your CarControl values unchanged.");
            return;
        }

        try
        {
            string json = File.ReadAllText(path);
            LoadedBuild = JsonUtility.FromJson<ImportedBuildData>(json);

            if (logOnImport)
                Debug.Log($"[BuildImporter] Loaded build for {LoadedBuild.circuitName}: " +
                          $"{LoadedBuild.efficiencyScore}/100 ({LoadedBuild.circuitRating})");

            ApplyToCar();
        }
        catch (Exception e)
        {
            Debug.LogError($"[BuildImporter] Failed to parse SPEEDKRAFT export: {e.Message}");
        }
    }

    void ApplyToCar()
    {
        if (LoadedBuild == null) return;

        float engineFactor = Mathf.Lerp(minFactor, maxFactor, LoadedBuild.engineMatch / 100f);
        float dragFactor = Mathf.Lerp(minFactor, maxFactor, LoadedBuild.dragMatch / 100f);
        float reliabilityFactor = Mathf.Lerp(minFactor, maxFactor, LoadedBuild.tyreMatch / 100f);
        float gripFactor = Mathf.Lerp(minGripFactor, maxGripFactor, LoadedBuild.downforceMatch / 100f);

        carControl.motorTorque = baseMotorTorque * engineFactor;
        carControl.maxSpeed = baseMaxSpeed * dragFactor;
        carControl.brakeTorque = baseBrakeTorque * reliabilityFactor;

        for (int i = 0; i < wheels.Length; i++)
        {
            var wc = wheels[i].WheelCollider;

            WheelFrictionCurve fwd = wc.forwardFriction;
            fwd.stiffness = baseForwardStiffness[i] * gripFactor;
            wc.forwardFriction = fwd;

            WheelFrictionCurve side = wc.sidewaysFriction;
            side.stiffness = baseSidewaysStiffness[i] * gripFactor;
            wc.sidewaysFriction = side;
        }

        if (logOnImport)
            Debug.Log($"[BuildImporter] Applied: motorTorque x{engineFactor:0.00}, " +
                      $"maxSpeed x{dragFactor:0.00}, brakeTorque x{reliabilityFactor:0.00}, " +
                      $"grip x{gripFactor:0.00}");
    }

    // Strips the SPEEDKRAFT influence back off, races with your exact
    // hand-tuned values and stock grip.
    public void ResetToBaseValues()
    {
        if (!baseValuesCaptured || carControl == null) return;

        carControl.motorTorque = baseMotorTorque;
        carControl.brakeTorque = baseBrakeTorque;
        carControl.maxSpeed = baseMaxSpeed;

        for (int i = 0; i < wheels.Length; i++)
        {
            var wc = wheels[i].WheelCollider;

            WheelFrictionCurve fwd = wc.forwardFriction;
            fwd.stiffness = baseForwardStiffness[i];
            wc.forwardFriction = fwd;

            WheelFrictionCurve side = wc.sidewaysFriction;
            side.stiffness = baseSidewaysStiffness[i];
            wc.sidewaysFriction = side;
        }

        Debug.Log("[BuildImporter] Reset to base CarControl values.");
    }
}