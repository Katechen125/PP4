using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class CircuitSelectionController : MonoBehaviour
{
    [Header("UI Document")]
    public UIDocument uiDocument;

    [Header("All Circuits")]
    public Circuit[] allCircuits;

    [Header("Fonts")]
    public Font orbitronFont;
    public Font exo2Font;

    [Header("Builder Reference")]
    public GameObject builderObject;

    [Header("Mission Lock-In Timing")]
    public float zoomPunchDuration = 0.18f;
    public float zoomSettleDuration = 0.12f;
    public float cornerStaggerDelay = 0.05f;
    public float cornerFadeDuration = 0.12f;
    public float stampPunchDuration = 0.2f;
    public float flashFadeOutDuration = 0.3f;
    public float pauseBeforeSwitch = 0.55f;

    private VisualElement root;
    private VisualElement circuitGrid;
    private VisualElement trackImage;
    private Label circuitNameLabel;
    private Label circuitSubLabel;
    private Label lapLengthLabel;
    private Label raceLapsLabel;
    private Label topSpeedLabel;
    private VisualElement tagsContainer;
    private Button confirmBtn;
    private VisualElement circuitFlagLarge;

    // Mission lock-in overlay elements
    private VisualElement lockCornerTL;
    private VisualElement lockCornerTR;
    private VisualElement lockCornerBL;
    private VisualElement lockCornerBR;
    private VisualElement lockFlash;
    private VisualElement lockStampWrap;

    private Circuit selectedCircuit;
    private bool isLockingIn;

    public static Circuit ChosenCircuit;

    void OnEnable()
    {
        root = uiDocument.rootVisualElement;

        circuitGrid = root.Q<VisualElement>("CircuitGrid");
        trackImage = root.Q<VisualElement>("TrackImage");
        circuitNameLabel = root.Q<Label>("CircuitName");
        circuitSubLabel = root.Q<Label>("CircuitSub");
        lapLengthLabel = root.Q<Label>("LapLength");
        raceLapsLabel = root.Q<Label>("RaceLaps");
        topSpeedLabel = root.Q<Label>("TopSpeed");
        tagsContainer = root.Q<VisualElement>("TagsContainer");
        confirmBtn = root.Q<Button>("BtnConfirm");
        circuitFlagLarge = root.Q<VisualElement>("CircuitFlagImg");

        lockCornerTL = root.Q<VisualElement>("LockCornerTL");
        lockCornerTR = root.Q<VisualElement>("LockCornerTR");
        lockCornerBL = root.Q<VisualElement>("LockCornerBL");
        lockCornerBR = root.Q<VisualElement>("LockCornerBR");
        lockFlash = root.Q<VisualElement>("LockFlash");
        lockStampWrap = root.Q<VisualElement>("LockStampWrap");

        ResetLockInVisuals();

        ApplyFonts();

        if (confirmBtn != null)
            confirmBtn.clicked += OnConfirm;

        BuildCircuitGrid();

        if (allCircuits.Length > 0)
            SelectCircuit(allCircuits[0]);
    }

    void BuildCircuitGrid()
    {
        if (circuitGrid == null) return;
        circuitGrid.Clear();

        foreach (Circuit circuit in allCircuits)
        {
            var card = new VisualElement();
            card.AddToClassList("circuit-card");

            var flagImg = new Image();
            flagImg.AddToClassList("circuit-flag-img");
            if (circuit.flagSprite != null)
                flagImg.sprite = circuit.flagSprite;

            var nameCol = new VisualElement();
            nameCol.AddToClassList("circuit-name-col");

            var name = new Label(circuit.circuitName);
            name.AddToClassList("circuit-card-name");

            var type = new Label(circuit.circuitType.ToUpper());
            type.AddToClassList("circuit-card-type");

            nameCol.Add(name);
            nameCol.Add(type);

            card.Add(flagImg);
            card.Add(nameCol);

            Circuit captured = circuit;
            card.RegisterCallback<ClickEvent>(_ => SelectCircuit(captured));

            circuitGrid.Add(card);
        }
    }

    void SelectCircuit(Circuit circuit)
    {
        if (isLockingIn) return;

        selectedCircuit = circuit;

        foreach (var card in circuitGrid.Children())
        {
            card.RemoveFromClassList("circuit-card--selected");
            var nameLbl = card.Q<Label>(className: "circuit-card-name");
            if (nameLbl != null && nameLbl.text == circuit.circuitName)
                card.AddToClassList("circuit-card--selected");
        }

        if (trackImage != null && circuit.trackLayout != null)
            trackImage.style.backgroundImage = new StyleBackground(circuit.trackLayout);

        if (circuitNameLabel != null)
            circuitNameLabel.text = circuit.circuitName.ToUpper();
        if (circuitSubLabel != null)
            circuitSubLabel.text = $"{circuit.country.ToUpper()} · {circuit.region.ToUpper()}";
        if (lapLengthLabel != null)
            lapLengthLabel.text = $"{circuit.lapLengthKm} KM";
        if (raceLapsLabel != null)
            raceLapsLabel.text = $"{circuit.raceLaps}";
        if (topSpeedLabel != null)
            topSpeedLabel.text = $"{circuit.topSpeedKmh} KM/H";
        if (circuitFlagLarge != null && circuit.flagSprite != null)
            circuitFlagLarge.style.backgroundImage = new StyleBackground(circuit.flagSprite);

        if (tagsContainer != null)
        {
            tagsContainer.Clear();
            foreach (string tag in circuit.tags)
            {
                var tagEl = new Label(tag);
                tagEl.AddToClassList("circuit-tag");
                tagsContainer.Add(tagEl);
            }
        }
    }

    void OnConfirm()
    {
        if (selectedCircuit == null || isLockingIn) return;
        StartCoroutine(LockInSequence());
    }

    // "Accepting a mission" moment: the track image punches into a zoom,
    // targeting-style corner brackets snap onto it in sequence, a flash
    // sells the impact, and a "CIRCUIT LOCKED" stamp punches in before
    // switching to the builder.
    IEnumerator LockInSequence()
    {
        isLockingIn = true;
        ChosenCircuit = selectedCircuit;
        if (confirmBtn != null) confirmBtn.SetEnabled(false);

        // 1. Impact zoom with overshoot, then settle slightly higher than 1x
        yield return StartCoroutine(ScaleElement(trackImage, 1f, 1.16f, zoomPunchDuration));
        yield return StartCoroutine(ScaleElement(trackImage, 1.16f, 1.06f, zoomSettleDuration));

        // 2. Corner brackets snap in one at a time, targeting-lock feel
        StartCoroutine(FadeElement(lockCornerTL, 0f, 1f, cornerFadeDuration));
        yield return new WaitForSeconds(cornerStaggerDelay);
        StartCoroutine(FadeElement(lockCornerTR, 0f, 1f, cornerFadeDuration));
        yield return new WaitForSeconds(cornerStaggerDelay);
        StartCoroutine(FadeElement(lockCornerBL, 0f, 1f, cornerFadeDuration));
        yield return new WaitForSeconds(cornerStaggerDelay);
        StartCoroutine(FadeElement(lockCornerBR, 0f, 1f, cornerFadeDuration));
        yield return new WaitForSeconds(cornerFadeDuration);

        // 3. Flash + stamp punch, the actual "mission accepted" beat
        StartCoroutine(FadeElement(lockFlash, 0f, 0.9f, 0.05f));
        if (lockStampWrap != null)
        {
            lockStampWrap.style.opacity = 1f;
            lockStampWrap.style.scale = new Scale(new Vector2(1.5f, 1.5f));
        }
        yield return StartCoroutine(ScaleElement(lockStampWrap, 1.5f, 1f, stampPunchDuration));
        StartCoroutine(FadeElement(lockFlash, 0.9f, 0f, flashFadeOutDuration));

        yield return new WaitForSeconds(pauseBeforeSwitch);

        // Activate the next screen before deactivating this one, same
        // ordering fix as the splash screen transition.
        if (builderObject != null)
            builderObject.SetActive(true);
        else
            Debug.LogError("Builder reference not set!");

        gameObject.SetActive(false);
    }

    void ResetLockInVisuals()
    {
        SetOpacityImmediate(lockCornerTL, 0f);
        SetOpacityImmediate(lockCornerTR, 0f);
        SetOpacityImmediate(lockCornerBL, 0f);
        SetOpacityImmediate(lockCornerBR, 0f);
        SetOpacityImmediate(lockFlash, 0f);
        SetOpacityImmediate(lockStampWrap, 0f);

        if (trackImage != null)
            trackImage.style.scale = new Scale(Vector2.one);
        if (lockStampWrap != null)
            lockStampWrap.style.scale = new Scale(Vector2.one);
    }

    void SetOpacityImmediate(VisualElement el, float value)
    {
        if (el == null) return;
        el.style.opacity = value;
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
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float s = Mathf.Lerp(from, to, eased);
            el.style.scale = new Scale(new Vector2(s, s));
            yield return null;
        }
        el.style.scale = new Scale(new Vector2(to, to));
    }

    void ApplyFonts()
    {
        if (orbitronFont != null)
        {
            SetFont(root.Q<Label>("AppTitle"), orbitronFont);
            SetFont(root.Q<Label>("ScreenTitle"), orbitronFont);
            SetFont(root.Q<Label>("CircuitName"), orbitronFont);
            SetFont(root.Q<Label>("GridLabel"), orbitronFont);
            SetFont(root.Q<Label>("HeaderRight"), orbitronFont);
            SetFont(root.Q<Label>("LockStamp"), orbitronFont);
        }

        if (exo2Font != null)
        {
            SetFont(root.Q<Label>("CircuitSub"), exo2Font);
            SetFont(root.Q<Label>("LapLength"), exo2Font);
            SetFont(root.Q<Label>("RaceLaps"), exo2Font);
            SetFont(root.Q<Label>("TopSpeed"), exo2Font);

            foreach (var lbl in root.Query<Label>(className: "circuit-card-name").ToList())
                SetFont(lbl, exo2Font);
            foreach (var lbl in root.Query<Label>(className: "circuit-card-type").ToList())
                SetFont(lbl, exo2Font);
            foreach (var lbl in root.Query<Label>(className: "circuit-tag").ToList())
                SetFont(lbl, exo2Font);
            foreach (var lbl in root.Query<Label>(className: "stat-lbl").ToList())
                SetFont(lbl, exo2Font);
            foreach (var lbl in root.Query<Label>(className: "stat-val").ToList())
                SetFont(lbl, exo2Font);
        }
    }

    void SetFont(Label label, Font font)
    {
        if (label == null || font == null) return;
        label.style.unityFont = new StyleFont(font);
    }
}