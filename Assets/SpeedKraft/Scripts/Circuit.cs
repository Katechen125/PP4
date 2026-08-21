using UnityEngine;

[CreateAssetMenu(fileName = "NewCircuit", menuName = "F1BuildSim/Circuit")]
public class Circuit : ScriptableObject
{
    [Header("Basic Info")]
    public string circuitName;
    public string country;
    public string region;
    public string circuitType;
    public Sprite trackLayout;


    [Header("Circuit Stats")]
    public float lapLengthKm;
    public int raceLaps;
    public int topSpeedKmh;

    [Header("Flag")]
    public Sprite flagSprite;

    [Header("Setup Demands")]
    [Range(0f, 100f)] public float downforceRequirement;
    [Range(0f, 100f)] public float dragSensitivity;
    [Range(0f, 100f)] public float tyreStress;
    [Range(0f, 100f)] public float brakeStress;
    [Range(0f, 100f)] public float engineDemand;

    [Header("Tags")]
    public string[] tags;

    [Header("Historical Reference (approximate — verify before shipping)")]
    public string lapRecordTime; // format: "1:31.447" (m:ss.sss)
    public string lapRecordHolder;
}