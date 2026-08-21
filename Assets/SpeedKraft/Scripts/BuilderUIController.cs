using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class BuilderUIController : MonoBehaviour
{
    [Header("UI Document")]
    public UIDocument uiDocument;

    [Header("All Parts")]
    public CarPart[] allParts;

    [Header("Fonts")]
    public Font orbitronFont;
    public Font exo2Font;

    [Header("Scene References")]
    public GameObject circuitSelectorObject;
    public string racingSceneName = "RacingLevel";

    [Header("Part Icons")]
    public Sprite iconFrontWing;
    public Sprite iconRearWing;
    public Sprite iconEngine;
    public Sprite iconSuspension;
    public Sprite iconTyres;
    public Sprite iconBrakes;
    public Sprite iconSidepods;
    public Sprite iconFloor;

    private VisualElement root;
    private VisualElement partListContainer;
    private Label selectedCategoryLabel;
    private Label categoryCountLabel;
    private VisualElement introBox;
    private VisualElement progressFill;
    private Label progressCount;
    private VisualElement circuitBannerFlag;
    private Label circuitBannerName;
    private VisualElement circuitBannerTags;

    // Live sidebar match bars (right panel, while building)
    private VisualElement liveMatchDownforceFill;
    private Label liveMatchDownforceVal;
    private VisualElement liveMatchDragFill;
    private Label liveMatchDragVal;
    private VisualElement liveMatchEngineFill;
    private Label liveMatchEngineVal;
    private VisualElement liveMatchTyreFill;
    private Label liveMatchTyreVal;
    private Label liveMatchHint;

    // Modal references
    private VisualElement resultsModal;
    private Label modalScore;
    private Label modalRating;
    private Label modalLapTime;
    private Label modalSpeed;
    private Label modalCornering;
    private Label modalCircuitName;
    private VisualElement modalFlag;
    private VisualElement modalDebriefItems;

    // Suspense reveal screen shown between the Evaluate click and the modal
    private VisualElement evaluatingOverlay;
    private Label evaluatingText;
    private VisualElement evaluatingPulse;

    private PartCategory currentCategory = PartCategory.FrontWing;

    // Tab switcher (3 tabs: Race Result, All Circuits, Qualifying)
    private VisualElement tabPageRace;
    private VisualElement tabPageCompare;
    private VisualElement tabPageQualifying;
    private Button tabRace;
    private Button tabCompare;
    private Button tabQualifying;
    private VisualElement circuitLeaderboard;

    // Qualifying + Race Projection references
    private Label qualiPosition;
    private Label qualiPositionLabel;
    private Label qualiEstimatedLap;
    private Label qualiRecordLap;
    private Label qualiGap;
    private Label qualiRecordHolder;
    private Label raceGridPos;
    private Label raceFinishPos;
    private Label raceAvgLap;
    private Label raceTotalTime;
    private Label raceLapCompareLabel;
    private Label raceTrendLabel;

    // All circuits reference
    public Circuit[] allCircuits;

    void OnEnable()
    {
        root = uiDocument.rootVisualElement;

        partListContainer = root.Q<VisualElement>("PartList");
        selectedCategoryLabel = root.Q<Label>("CategoryLabel");
        categoryCountLabel = root.Q<Label>("CategoryCount");
        introBox = root.Q<VisualElement>("IntroBox");
        progressFill = root.Q<VisualElement>("ProgressFill");
        progressCount = root.Q<Label>("ProgressCount");
        circuitBannerFlag = root.Q<VisualElement>("CircuitBannerFlag");
        circuitBannerName = root.Q<Label>("CircuitBannerName");
        circuitBannerTags = root.Q<VisualElement>("CircuitBannerTags");

        // Live sidebar match bars
        liveMatchDownforceFill = root.Q<VisualElement>("LiveMatchDownforceFill");
        liveMatchDownforceVal = root.Q<Label>("LiveMatchDownforceVal");
        liveMatchDragFill = root.Q<VisualElement>("LiveMatchDragFill");
        liveMatchDragVal = root.Q<Label>("LiveMatchDragVal");
        liveMatchEngineFill = root.Q<VisualElement>("LiveMatchEngineFill");
        liveMatchEngineVal = root.Q<Label>("LiveMatchEngineVal");
        liveMatchTyreFill = root.Q<VisualElement>("LiveMatchTyreFill");
        liveMatchTyreVal = root.Q<Label>("LiveMatchTyreVal");
        liveMatchHint = root.Q<Label>("LiveMatchHint");

        // Modal references
        resultsModal = root.Q<VisualElement>("ResultsModal");
        modalScore = root.Q<Label>("ModalScore");
        modalRating = root.Q<Label>("ModalRating");
        modalLapTime = root.Q<Label>("ModalLapTime");
        modalSpeed = root.Q<Label>("ModalSpeed");
        modalCornering = root.Q<Label>("ModalCornering");
        modalCircuitName = root.Q<Label>("ModalCircuitName");
        modalFlag = root.Q<VisualElement>("ModalFlag");
        modalDebriefItems = root.Q<VisualElement>("ModalDebriefItems");

        evaluatingOverlay = root.Q<VisualElement>("EvaluatingOverlay");
        evaluatingText = root.Q<Label>("EvaluatingText");
        evaluatingPulse = root.Q<VisualElement>("EvaluatingPulse");

        // Tabs
        tabPageRace = root.Q<VisualElement>("TabPageRace");
        tabPageCompare = root.Q<VisualElement>("TabPageCompare");
        tabPageQualifying = root.Q<VisualElement>("TabPageQualifying");
        tabRace = root.Q<Button>("TabRace");
        tabCompare = root.Q<Button>("TabCompare");
        tabQualifying = root.Q<Button>("TabQualifying");
        circuitLeaderboard = root.Q<VisualElement>("CircuitLeaderboard");

        if (tabRace != null)
            tabRace.clicked += () => SwitchTab(0);
        if (tabCompare != null)
            tabCompare.clicked += () => SwitchTab(1);
        if (tabQualifying != null)
            tabQualifying.clicked += () => SwitchTab(2);

        // Qualifying + Race Projection references
        qualiPosition = root.Q<Label>("QualiPosition");
        qualiPositionLabel = root.Q<Label>("QualiPositionLabel");
        qualiEstimatedLap = root.Q<Label>("QualiEstimatedLap");
        qualiRecordLap = root.Q<Label>("QualiRecordLap");
        qualiGap = root.Q<Label>("QualiGap");
        qualiRecordHolder = root.Q<Label>("QualiRecordHolder");
        raceGridPos = root.Q<Label>("RaceGridPos");
        raceFinishPos = root.Q<Label>("RaceFinishPos");
        raceAvgLap = root.Q<Label>("RaceAvgLap");
        raceTotalTime = root.Q<Label>("RaceTotalTime");
        raceLapCompareLabel = root.Q<Label>("RaceLapCompareLabel");
        raceTrendLabel = root.Q<Label>("RaceTrendLabel");

        // Race with build button
        var raceBtn = root.Q<Button>("BtnRaceWithBuild");
        if (raceBtn != null)
            raceBtn.clicked += OnRaceWithBuild;

        var xBtn = root.Q<Button>("BtnModalX");
        if (xBtn != null)
            xBtn.clicked += () =>
            {
                if (resultsModal != null)
                    resultsModal.style.display = DisplayStyle.None;
            };

        // Category buttons
        BindCategoryBtn("BtnFrontWing", PartCategory.FrontWing);
        BindCategoryBtn("BtnRearWing", PartCategory.RearWing);
        BindCategoryBtn("BtnEngine", PartCategory.Engine);
        BindCategoryBtn("BtnSuspension", PartCategory.Suspension);
        BindCategoryBtn("BtnTyres", PartCategory.Tyres);
        BindCategoryBtn("BtnBrakes", PartCategory.Brakes);
        BindCategoryBtn("BtnSidepods", PartCategory.Sidepods);
        BindCategoryBtn("BtnFloor", PartCategory.Floor);

        // Evaluate
        var evaluateBtn = root.Q<Button>("BtnEvaluate");
        if (evaluateBtn != null)
            evaluateBtn.clicked += () => StartCoroutine(PlayEvaluateFeedbackThenRun(evaluateBtn));

        // Modal buttons
        var closeBtn = root.Q<Button>("BtnModalClose");
        if (closeBtn != null)
            closeBtn.clicked += () =>
            {
                if (resultsModal != null)
                    resultsModal.style.display = DisplayStyle.None;
            };

        var newCircuitBtn = root.Q<Button>("BtnModalNewCircuit");
        if (newCircuitBtn != null)
            newCircuitBtn.clicked += () =>
            {
                BuildGameManager.Instance?.ResetBuild();
                if (resultsModal != null)
                    resultsModal.style.display = DisplayStyle.None;
                gameObject.SetActive(false);
                if (circuitSelectorObject != null)
                    circuitSelectorObject.SetActive(true);
            };

        // Show intro on start
        if (introBox != null) introBox.style.display = DisplayStyle.Flex;
        if (partListContainer != null) partListContainer.style.display = DisplayStyle.None;
        if (selectedCategoryLabel != null) selectedCategoryLabel.style.display = DisplayStyle.None;
        if (categoryCountLabel != null) categoryCountLabel.style.display = DisplayStyle.None;

        // Show circuit banner
        if (CircuitSelectionController.ChosenCircuit != null)
        {
            var circuit = CircuitSelectionController.ChosenCircuit;
            if (circuitBannerName != null)
                circuitBannerName.text = circuit.circuitName.ToUpper();
            if (circuitBannerFlag != null && circuit.flagSprite != null)
                circuitBannerFlag.style.backgroundImage = new StyleBackground(circuit.flagSprite);
            if (circuitBannerTags != null)
            {
                circuitBannerTags.Clear();
                foreach (string tag in circuit.tags)
                {
                    var tagEl = new Label(tag);
                    tagEl.AddToClassList("banner-tag");
                    circuitBannerTags.Add(tagEl);
                }
            }
        }

        UpdateProgressBar();
        ApplyFonts();
    }

    void BindCategoryBtn(string btnName, PartCategory category)
    {
        var btn = root.Q<Button>(btnName);
        if (btn == null) return;
        btn.RegisterCallback<ClickEvent>(_ =>
        {
            if (introBox != null) introBox.style.display = DisplayStyle.None;
            if (partListContainer != null) partListContainer.style.display = DisplayStyle.Flex;
            if (selectedCategoryLabel != null) selectedCategoryLabel.style.display = DisplayStyle.Flex;
            if (categoryCountLabel != null) categoryCountLabel.style.display = DisplayStyle.Flex;
            ShowCategory(category);
        });
    }

    void ShowCategory(PartCategory category)
    {
        currentCategory = category;

        if (selectedCategoryLabel != null)
            selectedCategoryLabel.text = category.ToString().ToUpper();

        int count = 0;
        foreach (var p in allParts)
            if (p != null && p.category == category) count++;

        if (categoryCountLabel != null)
            categoryCountLabel.text = $"{count} OPTIONS AVAILABLE";

        string[] allBtns = {
            "BtnFrontWing","BtnRearWing","BtnEngine","BtnSuspension",
            "BtnTyres","BtnBrakes","BtnSidepods","BtnFloor"
        };
        foreach (var name in allBtns)
            root.Q<Button>(name)?.RemoveFromClassList("cat-btn--active");
        root.Q<Button>("Btn" + category.ToString())?.AddToClassList("cat-btn--active");

        partListContainer.Clear();

        var currentBuild = BuildGameManager.Instance?.currentBuild;

        foreach (CarPart part in allParts)
        {
            if (part == null || part.category != category) continue;

            bool selected = IsSelected(part, currentBuild);

            var card = new VisualElement();
            card.AddToClassList("part-card");
            if (selected) card.AddToClassList("part-card--selected");

            var topRow = new VisualElement();
            topRow.AddToClassList("card-top-row");

            var iconBox = new Image();
            iconBox.AddToClassList("part-icon");
            Sprite iconSprite = GetCategoryIcon(category);
            if (iconSprite != null) iconBox.sprite = iconSprite;

            var nameCol = new VisualElement();
            nameCol.AddToClassList("name-col");

            var nameLabel = new Label(part.partName);
            nameLabel.AddToClassList("part-name");

            var descLabel = new Label(part.description);
            descLabel.AddToClassList("part-desc");

            nameCol.Add(nameLabel);
            nameCol.Add(descLabel);
            topRow.Add(iconBox);
            topRow.Add(nameCol);

            if (selected)
            {
                var badge = new Label("● EQUIPPED");
                badge.AddToClassList("equipped-badge");
                topRow.Add(badge);
            }

            card.Add(topRow);

            var divider = new VisualElement();
            divider.AddToClassList("card-divider");
            card.Add(divider);

            var statsRow = new VisualElement();
            statsRow.AddToClassList("stats-row");
            if (part.downforce > 0) statsRow.Add(MakeStat("DOWNFORCE", "↓", part.downforce, false));
            if (part.drag > 0) statsRow.Add(MakeStat("DRAG", "→", part.drag, true));
            if (part.weight > 0) statsRow.Add(MakeStat("WEIGHT", "⊕", part.weight, false));
            if (part.power > 0) statsRow.Add(MakeStat("POWER", "⚡", part.power, false));
            statsRow.Add(MakeStat("RELIABILITY", "◎", part.reliability, false));
            card.Add(statsRow);

            var selectBtn = new Button();
            selectBtn.AddToClassList("select-btn");
            selectBtn.text = selected ? "EQUIPPED" : "SELECT PART →";
            if (selected) selectBtn.AddToClassList("select-btn--selected");

            CarPart captured = part;
            VisualElement capturedCard = card;
            PartCategory capturedCategory = category;
            selectBtn.clicked += () =>
            {
                BuildGameManager.Instance.SelectPart(captured);
                StartCoroutine(PlaySelectFeedbackThenRefresh(capturedCard, capturedCategory));
            };

            card.Add(selectBtn);
            partListContainer.Add(card);
        }

        UpdateSidebarMatchBars();
    }

    // Quick confirmation punch on the card the user just selected, same
    // animation every time for consistency, then rebuilds the category
    // list to show the new EQUIPPED state.
    IEnumerator PlaySelectFeedbackThenRefresh(VisualElement card, PartCategory category)
    {
        card.AddToClassList("part-card--punch");
        yield return new WaitForSeconds(0.16f);
        card.RemoveFromClassList("part-card--punch");
        yield return new WaitForSeconds(0.06f);

        ShowCategory(category);
        UpdateProgressBar();
    }

    Sprite GetCategoryIcon(PartCategory cat) => cat switch
    {
        PartCategory.FrontWing => iconFrontWing,
        PartCategory.RearWing => iconRearWing,
        PartCategory.Engine => iconEngine,
        PartCategory.Suspension => iconSuspension,
        PartCategory.Tyres => iconTyres,
        PartCategory.Brakes => iconBrakes,
        PartCategory.Sidepods => iconSidepods,
        PartCategory.Floor => iconFloor,
        _ => null
    };

    VisualElement MakeStat(string label, string icon, float value, bool isNegative)
    {
        var chip = new VisualElement();
        chip.AddToClassList("stat-chip");
        if (isNegative) chip.AddToClassList("stat-chip--negative");

        var row = new VisualElement();
        row.AddToClassList("stat-chip-row");

        var iconLbl = new Label(icon);
        iconLbl.AddToClassList("stat-chip-icon");

        var lbl = new Label($"{label} {value:0}");
        lbl.AddToClassList("stat-chip-label");

        row.Add(iconLbl);
        row.Add(lbl);
        chip.Add(row);
        return chip;
    }

    bool IsSelected(CarPart part, CarBuild b)
    {
        if (b == null) return false;
        return part == b.frontWing || part == b.rearWing ||
               part == b.engine || part == b.suspension ||
               part == b.tyres || part == b.brakes ||
               part == b.sidepods || part == b.floor;
    }

    void UpdateProgressBar()
    {
        int count = CountSelectedParts();

        if (progressCount != null)
            progressCount.text = $"{count}/8";

        if (progressFill != null)
            progressFill.style.width = new StyleLength(
                new Length((count / 8f) * 100f, LengthUnit.Percent));

        UpdateSidebarStatus();
        UpdateSidebarMatchBars(count);
    }

    void UpdateSidebarStatus()
    {
        var gm = BuildGameManager.Instance;
        if (gm == null) return;
        var b = gm.currentBuild;
        if (b == null) return;

        SetBtnStatus("BtnFrontWing", "StatusFrontWing", b.frontWing);
        SetBtnStatus("BtnRearWing", "StatusRearWing", b.rearWing);
        SetBtnStatus("BtnEngine", "StatusEngine", b.engine);
        SetBtnStatus("BtnSuspension", "StatusSuspension", b.suspension);
        SetBtnStatus("BtnTyres", "StatusTyres", b.tyres);
        SetBtnStatus("BtnBrakes", "StatusBrakes", b.brakes);
        SetBtnStatus("BtnSidepods", "StatusSidepods", b.sidepods);
        SetBtnStatus("BtnFloor", "StatusFloor", b.floor);
    }

    void SetBtnStatus(string btnName, string statusName, CarPart part)
    {
        var btn = root.Q<Button>(btnName);
        var statusLabel = root.Q<Label>(statusName);
        if (btn == null) return;

        if (part != null)
        {
            btn.AddToClassList("cat-btn--done");
            if (statusLabel != null) statusLabel.text = "DONE";
        }
        else
        {
            btn.RemoveFromClassList("cat-btn--done");
            if (statusLabel != null) statusLabel.text = "——";
        }
    }

    int CountSelectedParts()
    {
        if (BuildGameManager.Instance == null) return 0;
        var b = BuildGameManager.Instance.currentBuild;
        int count = 0;
        if (b.frontWing != null) count++;
        if (b.rearWing != null) count++;
        if (b.engine != null) count++;
        if (b.suspension != null) count++;
        if (b.tyres != null) count++;
        if (b.brakes != null) count++;
        if (b.sidepods != null) count++;
        if (b.floor != null) count++;
        return count;
    }

    // ---------------------------------------------------------------
    // Live sidebar match bars (right panel, updates as parts change)
    // ---------------------------------------------------------------

    void UpdateSidebarMatchBars(int? selectedCount = null)
    {
        var gm = BuildGameManager.Instance;
        if (gm == null) return;
        var build = gm.currentBuild;
        var circuit = CircuitSelectionController.ChosenCircuit;
        if (build == null || circuit == null) return;

        bool hasDownforce = build.frontWing != null || build.rearWing != null || build.floor != null;
        bool hasDrag = build.frontWing != null || build.rearWing != null || build.sidepods != null;
        bool hasEngine = build.engine != null;
        bool hasReliability = build.tyres != null || build.brakes != null || build.suspension != null;

        var r = PerformanceEngine.Calculate(build, circuit, requireComplete: false);

        SetLiveBar(liveMatchDownforceFill, liveMatchDownforceVal, r.downforceMatch, hasDownforce);
        SetLiveBar(liveMatchDragFill, liveMatchDragVal, r.dragMatch, hasDrag);
        SetLiveBar(liveMatchEngineFill, liveMatchEngineVal, r.engineMatch, hasEngine);
        SetLiveBar(liveMatchTyreFill, liveMatchTyreVal, r.tyreMatch, hasReliability);

        UpdateLiveMatchHint(r, selectedCount ?? CountSelectedParts());
    }

    void SetLiveBar(VisualElement fill, Label val, float matchScore, bool hasData)
    {
        if (fill == null) return;

        if (!hasData)
        {
            fill.style.width = new StyleLength(new Length(0f, LengthUnit.Percent));
            fill.RemoveFromClassList("live-match-fill--good");
            fill.RemoveFromClassList("live-match-fill--warn");
            if (val != null) val.text = "--";
            return;
        }

        fill.style.width = new StyleLength(new Length(Mathf.Clamp(matchScore, 0f, 100f), LengthUnit.Percent));
        fill.RemoveFromClassList("live-match-fill--good");
        fill.RemoveFromClassList("live-match-fill--warn");
        if (matchScore >= 85f) fill.AddToClassList("live-match-fill--good");
        else if (matchScore < 60f) fill.AddToClassList("live-match-fill--warn");
        if (val != null) val.text = $"{matchScore:0}%";
    }

    void UpdateLiveMatchHint(PerformanceResult r, int selectedCount)
    {
        if (liveMatchHint == null) return;
        if (selectedCount == 0)
        {
            liveMatchHint.text = "Select parts to see your fit";
            return;
        }

        var losses = new (string name, float loss)[]
        {
            ("Downforce", (100f - r.downforceMatch) * 0.35f),
            ("Drag", (100f - r.dragMatch) * 0.25f),
            ("Engine", (100f - r.engineMatch) * 0.25f),
            ("Tyre/Reliability", (100f - r.tyreMatch) * 0.15f),
        };

        var worst = losses[0];
        foreach (var l in losses)
            if (l.loss > worst.loss) worst = l;

        liveMatchHint.text = worst.loss > 1f
            ? $"Biggest gap: {worst.name} (\u2212{worst.loss:0.0} pts)"
            : "Your build is well matched to this circuit";
    }

    // ---------------------------------------------------------------
    // Evaluate / Results modal
    // ---------------------------------------------------------------

    IEnumerator PlayEvaluateFeedbackThenRun(Button btn)
    {
        btn.AddToClassList("btn-evaluate--punch");
        yield return new WaitForSeconds(0.14f);
        btn.RemoveFromClassList("btn-evaluate--punch");

        if (BuildGameManager.Instance == null) yield break;

        var result = BuildGameManager.Instance.EvaluateBuild();

        if (!result.isValid)
        {
            Debug.Log("Build incomplete, skipping suspense sequence.");
            yield break;
        }

        yield return StartCoroutine(PlaySuspenseSequence(result));
    }

    // "And your build is..." reveal screen: a short beat of suspense before
    // the actual Race Report modal appears, instead of the modal just
    // popping up instantly.
    IEnumerator PlaySuspenseSequence(PerformanceResult result)
    {
        if (evaluatingOverlay == null)
        {
            ShowResultsModal(result);
            yield break;
        }

        evaluatingOverlay.style.display = DisplayStyle.Flex;
        evaluatingOverlay.style.opacity = 0f;
        yield return StartCoroutine(FadeElement(evaluatingOverlay, 0f, 1f, 0.15f));

        string[] phases = { "ANALYZING BUILD...", "CHECKING CIRCUIT FIT...", "AND YOUR BUILD IS..." };
        foreach (var phase in phases)
        {
            if (evaluatingText != null) evaluatingText.text = phase;
            yield return StartCoroutine(PulseOnce(evaluatingPulse));
            yield return new WaitForSeconds(0.12f);
        }

        yield return new WaitForSeconds(0.2f);

        yield return StartCoroutine(FadeElement(evaluatingOverlay, 1f, 0f, 0.2f));
        evaluatingOverlay.style.display = DisplayStyle.None;

        ShowResultsModal(result);
    }

    IEnumerator PulseOnce(VisualElement el)
    {
        if (el == null) yield break;
        yield return StartCoroutine(ScaleElement(el, 1f, 1.22f, 0.22f));
        yield return StartCoroutine(ScaleElement(el, 1.22f, 1f, 0.22f));
    }

    IEnumerator FadeElement(VisualElement el, float from, float to, float duration)
    {
        if (el == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            el.style.opacity = Mathf.Lerp(from, to, t);
            yield return null;
        }
        el.style.opacity = to;
    }

    IEnumerator ScaleElement(VisualElement el, float from, float to, float duration)
    {
        if (el == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float s = Mathf.Lerp(from, to, t);
            el.style.scale = new Scale(new Vector2(s, s));
            yield return null;
        }
        el.style.scale = new Scale(new Vector2(to, to));
    }

    void ShowResultsModal(PerformanceResult result)
    {
        if (resultsModal == null) return;
        resultsModal.style.display = DisplayStyle.Flex;

        SwitchTab(0);

        var circuit = CircuitSelectionController.ChosenCircuit;

        if (modalCircuitName != null && circuit != null)
            modalCircuitName.text = circuit.circuitName.ToUpper();
        if (modalFlag != null && circuit?.flagSprite != null)
            modalFlag.style.backgroundImage = new StyleBackground(circuit.flagSprite);

        if (modalScore != null) modalScore.text = $"{result.efficiencyScore}";
        if (modalRating != null) modalRating.text = result.circuitRating;
        if (modalLapTime != null) modalLapTime.text = $"{result.lapTimeSeconds}s";
        if (modalSpeed != null) modalSpeed.text = $"{result.speedScore}";
        if (modalCornering != null) modalCornering.text = $"{result.corneringScore}";

        // These four bars show the SAME match scores as the sidebar
        // (downforceMatch/dragMatch/engineMatch/tyreMatch), not raw physical
        // stats, so the modal and sidebar always agree with each other and
        // with the weighted Efficiency number above them.
        SetModalBar("ModalBarDownforce", "ModalBarDownforceVal", result.downforceMatch);
        SetModalBar("ModalBarSpeed", "ModalBarSpeedVal", result.dragMatch);
        SetModalBar("ModalBarCornering", "ModalBarCorneringVal", result.engineMatch);
        SetModalBar("ModalBarReliability", "ModalBarReliabilityVal", result.tyreMatch);

        PopulateModalDebrief(result, circuit);

        if (BuildGameManager.Instance?.currentBuild != null)
            PopulateLeaderboard(BuildGameManager.Instance.currentBuild);

        PopulateQualifying(result, circuit);
    }

    void SetModalBar(string barName, string valName, float value)
    {
        var bar = root.Q<VisualElement>(barName);
        var val = root.Q<Label>(valName);

        if (bar != null)
        {
            bar.style.width = new StyleLength(
                new Length(Mathf.Clamp(value, 0f, 100f), LengthUnit.Percent));
            bar.RemoveFromClassList("modal-bar-fill--good");
            bar.RemoveFromClassList("modal-bar-fill--warn");
            if (value >= 85f) bar.AddToClassList("modal-bar-fill--good");
            else if (value < 60f) bar.AddToClassList("modal-bar-fill--warn");
        }
        if (val != null) val.text = $"{value:0}";
    }

    void PopulateModalDebrief(PerformanceResult result, Circuit circuit)
    {
        if (modalDebriefItems == null) return;
        modalDebriefItems.Clear();

        var tips = new System.Collections.Generic.List<string>();

        if (result.efficiencyScore >= 93f)
            tips.Add("Outstanding setup. Your build matches this circuit's demands very well.");
        else if (result.efficiencyScore >= 78f)
            tips.Add("Solid build. A few component tweaks could push you into optimal territory.");
        else if (result.efficiencyScore >= 58f)
            tips.Add("Average setup for this circuit. Try matching your aero to the circuit's demands.");
        else
            tips.Add("This setup is poorly matched. Consider rebuilding specifically for this circuit.");

        // Call out the single weakest category by name, with plain-language
        // meaning, so a low number never sits unexplained.
        var breakdown = new (string name, float score, string meaning)[]
        {
            ("Downforce", result.downforceMatch, "how well your wings and floor match this circuit's cornering load"),
            ("Drag", result.dragMatch, "how well your straight-line efficiency matches this circuit's top speed demands"),
            ("Engine", result.engineMatch, "how well your power unit's output matches what this circuit actually needs"),
            ("Tyre Wear", result.tyreMatch, "how well your tyres, brakes, and suspension durability match this circuit's wear demands"),
        };

        var weakest = breakdown[0];
        foreach (var b in breakdown)
            if (b.score < weakest.score) weakest = b;

        if (weakest.score < 60f)
            tips.Add($"{weakest.name} is your weakest area here ({weakest.score:0}/100): {weakest.meaning}.");

        if (circuit != null && BuildGameManager.Instance?.currentBuild != null)
        {
            var suggestions = BuildAdvisor.GetTopSuggestions(BuildGameManager.Instance.currentBuild, circuit, allParts, 2);

            if (suggestions.Count > 0)
            {
                foreach (var s in suggestions)
                {
                    string currentName = s.currentPart != null ? s.currentPart.partName : "no part selected";
                    tips.Add($"Swap {currentName} for {s.suggestedPart.partName}: {s.currentScore:0} \u2192 {s.projectedScore:0} (+{s.gain:0})");
                }

                if (suggestions.Count > 1)
                    tips.Add("Applying both changes together should push you further still, category effects don't stack perfectly, but this is your fastest path toward peak score.");
                else
                    tips.Add("That's the only single-part swap that improves your score here, everything else is already dialed in for this circuit.");
            }
            else if (weakest.score < 60f)
            {
                // No swap helps AND a category is genuinely weak: that means
                // no available part in that category is close enough, not
                // that the current choice was wrong. Say so honestly instead
                // of implying the build is already optimal.
                tips.Add($"No available {weakest.name.ToLower()} part gets closer to this circuit's demand right now, this is a gap in your part roster for this circuit, not a mistake in your build.");
            }
            else
            {
                tips.Add("Your current parts are already the strongest available combination for this circuit.");
            }
        }

        foreach (string tip in tips)
        {
            var item = new VisualElement();
            item.AddToClassList("modal-debrief-item");

            var dot = new VisualElement();
            dot.AddToClassList("modal-debrief-dot");

            var text = new Label(tip);
            text.AddToClassList("modal-debrief-text");

            item.Add(dot);
            item.Add(text);
            modalDebriefItems.Add(item);
        }
    }

    // ---------------------------------------------------------------
    // Qualifying + Race Projection tab
    // ---------------------------------------------------------------

    void PopulateQualifying(PerformanceResult result, Circuit circuit)
    {
        var q = QualifyingEstimator.Estimate(result, circuit);

        if (!q.isValid)
        {
            if (qualiPosition != null) qualiPosition.text = "--";
            if (qualiPositionLabel != null) qualiPositionLabel.text = "No lap record data for this circuit";
            if (qualiEstimatedLap != null) qualiEstimatedLap.text = "--";
            if (qualiRecordLap != null) qualiRecordLap.text = "--";
            if (qualiGap != null) qualiGap.text = "--";
            if (qualiRecordHolder != null) qualiRecordHolder.text = "";
            if (raceGridPos != null) raceGridPos.text = "--";
            if (raceFinishPos != null) raceFinishPos.text = "--";
            if (raceAvgLap != null) raceAvgLap.text = "--";
            if (raceTotalTime != null) raceTotalTime.text = "--";
            if (raceLapCompareLabel != null) raceLapCompareLabel.text = "";
            if (raceTrendLabel != null) raceTrendLabel.text = "";
            return;
        }

        if (qualiPosition != null) qualiPosition.text = $"P{q.estimatedPosition}";
        if (qualiPositionLabel != null) qualiPositionLabel.text = q.positionLabel;
        if (qualiEstimatedLap != null) qualiEstimatedLap.text = FormatLapTime(q.estimatedLapTimeSeconds);
        if (qualiRecordLap != null) qualiRecordLap.text = q.recordLapTimeDisplay;
        if (qualiGap != null) qualiGap.text = q.gapToRecordSeconds <= 0.001f
            ? "AT RECORD PACE"
            : $"+{q.gapToRecordSeconds:0.000}s";
        if (qualiRecordHolder != null)
            qualiRecordHolder.text = string.IsNullOrEmpty(q.recordHolder)
                ? "Record holder not set"
                : $"Track record held by {q.recordHolder}";

        var race = QualifyingEstimator.EstimateRace(result, circuit, q);
        if (!race.isValid) return;

        if (raceGridPos != null) raceGridPos.text = $"P{q.estimatedPosition}";
        if (raceFinishPos != null) raceFinishPos.text = $"P{race.estimatedFinishPosition}";
        if (raceAvgLap != null) raceAvgLap.text = FormatLapTime(race.avgRaceLapSeconds);
        if (raceTotalTime != null) raceTotalTime.text = FormatRaceTime(race.totalRaceTimeSeconds);
        if (raceLapCompareLabel != null)
        {
            float diff = race.avgRaceLapSeconds - q.estimatedLapTimeSeconds;
            raceLapCompareLabel.text = $"+{diff:0.000}s per lap vs qualifying pace (fuel + tyre management over a full stint)";
        }
        if (raceTrendLabel != null)
        {
            raceTrendLabel.text = race.trendLabel;
            raceTrendLabel.RemoveFromClassList("lb-rating--optimal");
            raceTrendLabel.RemoveFromClassList("lb-rating--poor");
            if (race.positionsGained > 0) raceTrendLabel.AddToClassList("lb-rating--optimal");
            else if (race.positionsGained < 0) raceTrendLabel.AddToClassList("lb-rating--poor");
        }
    }

    string FormatLapTime(float seconds)
    {
        int mins = Mathf.FloorToInt(seconds / 60f);
        float secs = seconds - (mins * 60f);
        return $"{mins}:{secs:00.000}";
    }

    string FormatRaceTime(float totalSeconds)
    {
        int hours = Mathf.FloorToInt(totalSeconds / 3600f);
        int mins = Mathf.FloorToInt((totalSeconds % 3600f) / 60f);
        float secs = totalSeconds % 60f;

        // Explicit h/m/s labels instead of colon notation, a total race
        // time like "1:20:42.7" reads too similarly to a single lap time
        // like "1:46.339", making it look (wrongly) like it could beat the
        // lap record. Spelling out the units removes that ambiguity.
        return hours > 0
            ? $"{hours}h {mins:00}m {secs:00.0}s"
            : $"{mins}m {secs:00.0}s";
    }

    void OnReset()
    {
        BuildGameManager.Instance?.ResetBuild();
        UpdateProgressBar();
        if (currentCategory != PartCategory.FrontWing)
            ShowCategory(currentCategory);
    }

    void ApplyFonts()
    {
        if (orbitronFont != null)
        {
            SetFont(root.Q<Label>("AppTitle"), orbitronFont);
            SetFont(root.Q<Label>("CategoryLabel"), orbitronFont);
            SetFont(root.Q<Label>("BuildTitle"), orbitronFont);
            SetFont(root.Q<Label>("IntroTitle"), orbitronFont);
            SetFont(root.Q<Label>("ProgressCount"), orbitronFont);
            SetFont(root.Q<Label>("ModalTitle"), orbitronFont);
            SetFont(root.Q<Label>("ModalScore"), orbitronFont);
        }

        if (exo2Font != null)
        {
            foreach (var el in root.Query<Label>(className: "part-name").ToList())
                SetFont(el, exo2Font);
            foreach (var el in root.Query<Label>(className: "part-desc").ToList())
                SetFont(el, exo2Font);
            foreach (var el in root.Query<Label>(className: "btn-lbl").ToList())
                SetFont(el, exo2Font);
            foreach (var el in root.Query<Label>(className: "modal-debrief-text").ToList())
                SetFont(el, exo2Font);
        }
    }

    void SetFont(Label label, Font font)
    {
        if (label == null || font == null) return;
        label.style.unityFont = new StyleFont(font);
    }

    void SwitchTab(int tab)
    {
        if (tabPageRace != null) tabPageRace.style.display = tab == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        if (tabPageCompare != null) tabPageCompare.style.display = tab == 1 ? DisplayStyle.Flex : DisplayStyle.None;
        if (tabPageQualifying != null) tabPageQualifying.style.display = tab == 2 ? DisplayStyle.Flex : DisplayStyle.None;

        tabRace?.RemoveFromClassList("modal-tab--active");
        tabCompare?.RemoveFromClassList("modal-tab--active");
        tabQualifying?.RemoveFromClassList("modal-tab--active");

        if (tab == 0) tabRace?.AddToClassList("modal-tab--active");
        else if (tab == 1) tabCompare?.AddToClassList("modal-tab--active");
        else if (tab == 2) tabQualifying?.AddToClassList("modal-tab--active");
    }

    void PopulateLeaderboard(CarBuild build)
    {
        if (circuitLeaderboard == null || allCircuits == null) return;
        circuitLeaderboard.Clear();

        var results = new System.Collections.Generic.List<(Circuit circuit, float score, string rating)>();

        foreach (Circuit c in allCircuits)
        {
            PerformanceResult r = PerformanceEngine.Calculate(build, c);
            if (!r.isValid) continue;
            results.Add((c, r.circuitFitScore, r.circuitRating));
        }

        if (results.Count == 0) return;

        results.Sort((a, b) => b.score.CompareTo(a.score));

        Circuit chosenCircuit = CircuitSelectionController.ChosenCircuit;

        for (int i = 0; i < results.Count; i++)
        {
            var (circuit, score, rating) = results[i];
            bool isChosen = circuit == chosenCircuit;

            var row = new VisualElement();
            row.AddToClassList("leaderboard-row");
            if (isChosen) row.AddToClassList("leaderboard-row--selected");

            var rank = new Label(i == 0 ? "★" : $"{i + 1}");
            rank.AddToClassList("lb-rank");
            if (i == 0) rank.AddToClassList("lb-rank--top");

            var flag = new VisualElement();
            flag.AddToClassList("lb-flag");
            if (circuit.flagSprite != null)
                flag.style.backgroundImage = new StyleBackground(circuit.flagSprite);

            var name = new Label(circuit.circuitName.ToUpper());
            name.AddToClassList("lb-name");
            if (isChosen) name.AddToClassList("lb-name--selected");

            var barTrack = new VisualElement();
            barTrack.AddToClassList("lb-bar-track");
            var barFill = new VisualElement();
            barFill.AddToClassList("lb-bar-fill");
            barFill.style.width = new StyleLength(
                new Length(Mathf.Clamp(score, 0f, 100f), LengthUnit.Percent));

            if (rating.Contains("OPTIMAL"))
                barFill.style.backgroundColor = new StyleColor(new Color(0.29f, 0.87f, 0.5f));
            else if (rating.Contains("POOR"))
                barFill.style.backgroundColor = new StyleColor(new Color(0.71f, 0.31f, 0.24f));

            barTrack.Add(barFill);

            var scoreLabel = new Label($"{score:0}");
            scoreLabel.AddToClassList("lb-score");

            var ratingLabel = new Label(rating.Replace(" SETUP", ""));
            ratingLabel.AddToClassList("lb-rating");
            if (rating.Contains("OPTIMAL"))
                ratingLabel.AddToClassList("lb-rating--optimal");
            else if (rating.Contains("GOOD"))
                ratingLabel.AddToClassList("lb-rating--good");
            else if (rating.Contains("AVERAGE"))
                ratingLabel.AddToClassList("lb-rating--average");
            else
                ratingLabel.AddToClassList("lb-rating--poor");

            row.Add(rank);
            row.Add(flag);
            row.Add(name);
            row.Add(barTrack);
            row.Add(scoreLabel);
            row.Add(ratingLabel);

            circuitLeaderboard.Add(row);
        }
    }

    void OnRaceWithBuild()
    {
        if (BuildGameManager.Instance == null) return;
        var build = BuildGameManager.Instance.currentBuild;
        if (build == null || !build.IsComplete()) return;

        var result = BuildGameManager.Instance.GetLastResult();
        if (result == null || !result.isValid) return;

        // No file to write anymore. BuildGameManager uses
        // DontDestroyOnLoad, so currentBuild/lastResult simply survive
        // the scene load, the racing scene's BuildImporter reads them
        // straight from BuildGameManager.Instance.
        SceneManager.LoadScene(racingSceneName);
    }
}