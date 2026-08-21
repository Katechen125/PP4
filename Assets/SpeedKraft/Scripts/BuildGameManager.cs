using UnityEngine;

public class BuildGameManager : MonoBehaviour
{
    public static BuildGameManager Instance { get; private set; }

    [Header("Current Build")]
    public CarBuild currentBuild = new CarBuild();

    [Header("All Available Parts")]
    public CarPart[] allParts;

    private PerformanceResult lastResult;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SelectPart(CarPart part)
    {
        switch (part.category)
        {
            case PartCategory.FrontWing:
                currentBuild.frontWing = part; break;
            case PartCategory.RearWing:
                currentBuild.rearWing = part; break;
            case PartCategory.Engine:
                currentBuild.engine = part; break;
            case PartCategory.Suspension:
                currentBuild.suspension = part; break;
            case PartCategory.Tyres:
                currentBuild.tyres = part; break;
            case PartCategory.Brakes:
                currentBuild.brakes = part; break;
            case PartCategory.Sidepods:
                currentBuild.sidepods = part; break;
            case PartCategory.Floor:
                currentBuild.floor = part; break;
        }

        Debug.Log($"Selected {part.partName} for {part.category}");
    }

    public PerformanceResult EvaluateBuild()
    {
        Circuit chosenCircuit = CircuitSelectionController.ChosenCircuit;
        lastResult = PerformanceEngine.Calculate(currentBuild, chosenCircuit);

        if (lastResult.isValid)
        {
            Debug.Log($"Efficiency: {lastResult.efficiencyScore}/100");
            Debug.Log($"Lap Time: {lastResult.lapTimeSeconds}s");
            Debug.Log($"Circuit Rating: {lastResult.circuitRating}");
        }
        else
        {
            Debug.LogWarning("Build incomplete");
        }

        return lastResult;
    }

    public PerformanceResult GetLastResult() => lastResult;

    public void ResetBuild()
    {
        currentBuild.Reset();
        Debug.Log("Build reset");
    }
}