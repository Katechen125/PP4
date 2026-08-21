using UnityEngine;
using UnityEngine.UIElements;

public class ResultsController : MonoBehaviour
{
    [Header("UI Document")]
    public UIDocument uiDocument;

    [Header("Scene References")]
    public GameObject circuitSelectorObject;
    public GameObject builderObject;

    [Header("Fonts")]
    public Font orbitronFont;
    public Font exo2Font;

    private VisualElement root;

    void OnEnable()
    {
        root = uiDocument.rootVisualElement;

        var result = BuildGameManager.Instance?.GetLastResult();
        var circuit = CircuitSelectionController.ChosenCircuit;
        var build = BuildGameManager.Instance?.currentBuild;

        PopulateHeader(circuit);
        PopulateScore(result);
        PopulateTelemetry(result, build);
        PopulateBars(result);
        PopulateDebrief(result, circuit);

        var newCircuitBtn = root.Q<Button>("BtnNewCircuit");
        if (newCircuitBtn != null)
            newCircuitBtn.clicked += OnNewCircuit;

        var rebuildBtn = root.Q<Button>("BtnRebuild");
        if (rebuildBtn != null)
            rebuildBtn.clicked += OnRebuild;

        ApplyFonts();
    }

    void PopulateHeader(Circuit circuit)
    {
        if (circuit == null) return;

        var headerCircuitName = root.Q<Label>("HeaderCircuitName");
        if (headerCircuitName != null)
            headerCircuitName.text = circuit.circuitName.ToUpper();

        var headerFlag = root.Q<VisualElement>("HeaderFlag");
        if (headerFlag != null && circuit.flagSprite != null)
            headerFlag.style.backgroundImage = new StyleBackground(circuit.flagSprite);
    }

    void PopulateScore(PerformanceResult result)
    {
        if (result == null) return;

        SetLabel("ScoreNumber", $"{result.efficiencyScore}");
        SetLabel("RatingValue", result.circuitRating);
        SetLabel("LapTimeValue", $"{result.lapTimeSeconds}s");
        SetLabel("CorneringValue", $"{result.corneringScore}");
        SetLabel("SpeedValue", $"{result.speedScore}");
    }

    void PopulateTelemetry(PerformanceResult result, CarBuild build)
    {
        if (result == null) return;

        float totalDownforce = 0f;
        float totalDrag = 0f;
        float totalPower = 0f;

        if (build != null)
        {
            foreach (var part in build.GetAllParts())
            {
                if (part == null) continue;
                totalDownforce += part.downforce;
                totalDrag += part.drag;
                totalPower += part.power;
            }
        }

        SetTeleValue("TeleDownforce", $"{totalDownforce:0}", totalDownforce > 200f);
        SetTeleValue("TeleDrag", $"{totalDrag:0}", false, totalDrag > 200f);
        SetTeleValue("TeleAero", $"{result.aeroEfficiency:0}", result.aeroEfficiency > 70f);
        SetTeleValue("TeleReliability", $"{result.reliability:0}", result.reliability > 80f, result.reliability < 65f);
        SetTeleValue("TelePower", $"{totalPower:0}", totalPower > 150f);
        SetTeleValue("TeleMatch", result.circuitRating.Contains("OPTIMAL") ? "PERFECT" :
                                      result.circuitRating.Contains("GOOD") ? "GOOD" : "POOR",
                                      result.circuitRating.Contains("OPTIMAL"));
    }

    void SetTeleValue(string name, string value, bool good = false, bool warn = false)
    {
        var lbl = root.Q<Label>(name);
        if (lbl == null) return;
        lbl.text = value;
        lbl.RemoveFromClassList("tc-val--good");
        lbl.RemoveFromClassList("tc-val--warn");
        if (good) lbl.AddToClassList("tc-val--good");
        else if (warn) lbl.AddToClassList("tc-val--warn");
    }

    void PopulateBars(PerformanceResult result)
    {
        if (result == null) return;

        SetBar("BarDownforce", "BarDownforceVal", result.aeroEfficiency);
        SetBar("BarSpeed", "BarSpeedVal", result.speedScore);
        SetBar("BarCornering", "BarCorneringVal", result.corneringScore);
        SetBar("BarReliability", "BarReliabilityVal", result.reliability);
    }

    void SetBar(string barName, string valName, float value)
    {
        var bar = root.Q<VisualElement>(barName);
        var val = root.Q<Label>(valName);

        if (bar != null)
        {
            bar.style.width = new StyleLength(new Length(Mathf.Clamp(value, 0f, 100f), LengthUnit.Percent));
            bar.RemoveFromClassList("bar-fill--good");
            bar.RemoveFromClassList("bar-fill--warn");
            if (value >= 85f) bar.AddToClassList("bar-fill--good");
            else if (value < 60f) bar.AddToClassList("bar-fill--warn");
        }

        if (val != null) val.text = $"{value:0}";
    }

    void PopulateDebrief(PerformanceResult result, Circuit circuit)
    {
        var container = root.Q<VisualElement>("DebriefItems");
        if (container == null || result == null) return;

        container.Clear();

        var tips = GenerateTips(result, circuit);
        foreach (string tip in tips)
        {
            var item = new VisualElement();
            item.AddToClassList("debrief-item");

            var dot = new VisualElement();
            dot.AddToClassList("debrief-dot");

            var text = new Label(tip);
            text.AddToClassList("debrief-text");

            item.Add(dot);
            item.Add(text);
            container.Add(item);
        }
    }

    System.Collections.Generic.List<string> GenerateTips(PerformanceResult result, Circuit circuit)
    {
        var tips = new System.Collections.Generic.List<string>();

        if (result.efficiencyScore >= 85f)
            tips.Add("Outstanding build. Your setup is well matched to this circuit's demands.");
        else if (result.efficiencyScore >= 70f)
            tips.Add("Solid build with room to improve. Focus on your weakest stat to push further.");
        else
            tips.Add("This setup is struggling at this circuit. Try a different aero configuration.");

        if (result.aeroEfficiency < 60f)
            tips.Add("Your drag is too high for the downforce generated. Consider a lower drag aero package.");
        else if (result.aeroEfficiency > 85f)
            tips.Add("Excellent aero efficiency. Your lift-to-drag ratio is well optimised.");

        if (result.reliability < 70f)
            tips.Add("Reliability is a concern. High stress components risk failure over race distance.");

        if (result.speedScore < 60f)
            tips.Add("Top speed is limited. A higher performance power unit would help on long straights.");

        if (result.corneringScore >= 95f)
            tips.Add("Maximum cornering grip achieved. Your downforce package is perfectly dialled in.");

        if (circuit != null && circuit.tyreStress > 80f && result.reliability < 75f)
            tips.Add("This circuit is extremely hard on tyres. A more durable compound is recommended.");

        return tips;
    }

    void SetLabel(string name, string value)
    {
        var lbl = root.Q<Label>(name);
        if (lbl != null) lbl.text = value;
    }

    void ApplyFonts()
    {
        if (orbitronFont != null)
        {
            SetFont(root.Q<Label>("HeaderTitle"), orbitronFont);
            SetFont(root.Q<Label>("HeaderCircuitName"), orbitronFont);
            SetFont(root.Q<Label>("ScoreNumber"), orbitronFont);
            SetFont(root.Q<Label>("RatingValue"), orbitronFont);
        }

        if (exo2Font != null)
        {
            foreach (var lbl in root.Query<Label>(className: "ms-val").ToList())
                SetFont(lbl, exo2Font);
            foreach (var lbl in root.Query<Label>(className: "tc-val").ToList())
                SetFont(lbl, exo2Font);
            foreach (var lbl in root.Query<Label>(className: "debrief-text").ToList())
                SetFont(lbl, exo2Font);
        }
    }

    void SetFont(Label label, Font font)
    {
        if (label == null || font == null) return;
        label.style.unityFont = new StyleFont(font);
    }

    void OnNewCircuit()
    {
        BuildGameManager.Instance?.ResetBuild();
        gameObject.SetActive(false);

        if (circuitSelectorObject != null)
            circuitSelectorObject.SetActive(true);
    }

    void OnRebuild()
    {
        BuildGameManager.Instance?.ResetBuild();
        gameObject.SetActive(false);

        if (builderObject != null)
            builderObject.SetActive(true);
    }
}