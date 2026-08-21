using UnityEngine;

[CreateAssetMenu(fileName = "NewCarPart", menuName = "F1BuildSim/Car Part")]
public class CarPart : ScriptableObject
{
    [Header("Basic Info")]
    public string partName;
    public string description;
    public Sprite icon;
    public GameObject modelPrefab;

    [Header("Category")]
    public PartCategory category;

    [Header("Performance Stats")]
    [Range(0f, 100f)] public float downforce;
    [Range(0f, 100f)] public float drag;
    [Range(0f, 100f)] public float weight;
    [Range(0f, 100f)] public float power;
    [Range(0f, 100f)] public float reliability;
}

public enum PartCategory
{
    FrontWing,
    RearWing,
    Engine,
    Suspension,
    Tyres,
    Brakes,
    Sidepods,
    Floor
}