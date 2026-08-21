using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class SplashController : MonoBehaviour
{
    [Header("UI Document")]
    public UIDocument uiDocument;

    [Header("Scene References")]
    public GameObject circuitSelectorObject;

    [Header("Assets")]
    public Sprite mclarenImage;
    public Font orbitronFont;
    public Font exo2Font;

    [Header("Garage Door Animation")]
    public float doorCloseDuration = 0.6f;
    public float flashFadeInDuration = 0.08f;
    public float flashHoldDuration = 0.12f;
    public float flashFadeOutDuration = 0.25f;
    public float pauseBeforeSwitch = 0.2f;

    private VisualElement root;
    private VisualElement doorLeft;
    private VisualElement doorRight;
    private VisualElement seamGlow;
    private VisualElement seamCore;
    private Button startBtn;
    private bool isTransitioning;

    void OnEnable()
    {
        root = uiDocument.rootVisualElement;

        var carBg = root.Q<VisualElement>("CarBg");
        if (carBg != null && mclarenImage != null)
            carBg.style.backgroundImage = new StyleBackground(mclarenImage);

        doorLeft = root.Q<VisualElement>("GarageDoorLeft");
        doorRight = root.Q<VisualElement>("GarageDoorRight");
        seamGlow = root.Q<VisualElement>("DoorSeamGlow");
        seamCore = root.Q<VisualElement>("DoorSeamCore");

        // These overlay elements sit on top of the button in the visual
        // tree and, even at opacity 0, still have a real hit area
        // (DoorSeamGlow is 80px wide, DoorSeamCore 4px, both centered).
        // That invisible strip was stealing mouse events from BtnStart
        // whenever the cursor happened to land inside it, this is the
        // actual root cause of the inconsistent hover. Ignoring pointer
        // events on all four makes them purely visual, as intended, they
        // only ever need to be seen, never clicked or hovered.
        if (doorLeft != null) doorLeft.pickingMode = PickingMode.Ignore;
        if (doorRight != null) doorRight.pickingMode = PickingMode.Ignore;
        if (seamGlow != null) seamGlow.pickingMode = PickingMode.Ignore;
        if (seamCore != null) seamCore.pickingMode = PickingMode.Ignore;

        startBtn = root.Q<Button>("BtnStart");
        if (startBtn != null)
        {
            startBtn.clicked += OnStart;

            // Unity's :hover CSS pseudo-class only re-evaluates on mouse
            // MOVEMENT, not continuously. If the cursor is already resting
            // on the button when this screen appears, the hover style
            // never applies until the mouse moves, that's the "sometimes
            // lights up, sometimes doesn't" bug. Handling it manually with
            // these events makes it reliable every time.
            startBtn.RegisterCallback<MouseEnterEvent>(_ =>
                startBtn.style.backgroundColor = new StyleColor(new Color(1f, 140f / 255f, 55f / 255f)));
            startBtn.RegisterCallback<MouseLeaveEvent>(_ =>
                startBtn.style.backgroundColor = new StyleColor(new Color(1f, 107f / 255f, 26f / 255f)));
        }

        ApplyFonts();
    }

    void OnStart()
    {
        Debug.Log("[Splash] OnStart fired");
        if (isTransitioning)
        {
            Debug.Log("[Splash] Already transitioning, ignoring click");
            return;
        }
        isTransitioning = true;

        if (startBtn != null)
            startBtn.SetEnabled(false);

        Debug.Log($"[Splash] doorLeft null? {doorLeft == null}, doorRight null? {doorRight == null}, seamGlow null? {seamGlow == null}, seamCore null? {seamCore == null}, circuitSelectorObject null? {circuitSelectorObject == null}");
        Debug.Log($"[Splash] Values: doorCloseDuration={doorCloseDuration}, flashFadeInDuration={flashFadeInDuration}, flashHoldDuration={flashHoldDuration}, flashFadeOutDuration={flashFadeOutDuration}, pauseBeforeSwitch={pauseBeforeSwitch}");

        StartCoroutine(CloseGarageDoorsThenSwitch());
    }

    IEnumerator CloseGarageDoorsThenSwitch()
    {
        float startTime = Time.realtimeSinceStartup;

        // 1. Doors close. Ease-out (fast start, slow finish) so the
        //    movement is immediately visible from frame one, instead of
        //    smoothstep's slow ease-in that made the first few frames
        //    look static.
        float elapsed = 0f;
        while (elapsed < doorCloseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / doorCloseDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float pct = eased * 50f;

            SetDoorWidth(doorLeft, pct);
            SetDoorWidth(doorRight, pct);

            yield return null;
        }
        SetDoorWidth(doorLeft, 50f);
        SetDoorWidth(doorRight, 50f);
        Debug.Log($"[Splash] Doors closed at t={Time.realtimeSinceStartup - startTime:0.00}s");

        // 2. Doors are now fully closed. THIS is the moment the flash
        //    triggers, synced to actual closure instead of appearing
        //    instantly at click time.
        yield return StartCoroutine(FlashSeam());
        Debug.Log($"[Splash] Flash done at t={Time.realtimeSinceStartup - startTime:0.00}s");

        yield return new WaitForSeconds(pauseBeforeSwitch);
        Debug.Log($"[Splash] About to switch at t={Time.realtimeSinceStartup - startTime:0.00}s");

        // IMPORTANT: activate the next screen BEFORE deactivating this one.
        // Disabling a GameObject stops any coroutine running on it, and in
        // some Unity versions that can happen before the following line
        // executes if the order is reversed, this was the actual root
        // cause of "nothing happens."
        if (circuitSelectorObject != null)
        {
            circuitSelectorObject.SetActive(true);
            Debug.Log("[Splash] circuitSelectorObject activated");
        }
        else
        {
            Debug.LogError("[Splash] circuitSelectorObject is NOT assigned in the Inspector!");
        }

        gameObject.SetActive(false);
    }

    IEnumerator FlashSeam()
    {
        yield return StartCoroutine(FadeOpacity(seamGlow, 0f, 1f, flashFadeInDuration));
        yield return StartCoroutine(FadeOpacity(seamCore, 0f, 1f, flashFadeInDuration));

        yield return new WaitForSeconds(flashHoldDuration);

        yield return StartCoroutine(FadeOpacity(seamCore, 1f, 0f, flashFadeOutDuration));
        yield return StartCoroutine(FadeOpacity(seamGlow, 1f, 0f, flashFadeOutDuration));
    }

    IEnumerator FadeOpacity(VisualElement el, float from, float to, float duration)
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

    void SetDoorWidth(VisualElement door, float percent)
    {
        if (door == null) return;
        door.style.width = new StyleLength(new Length(percent, LengthUnit.Percent));
    }

    void ApplyFonts()
    {
        if (orbitronFont != null)
        {
            SetFont(root.Q<Label>("TitleSpeed"), orbitronFont);
            SetFont(root.Q<Label>("TitleKraft"), orbitronFont);
            SetFont(root.Q<Label>("BadgeText"), orbitronFont);
            SetFont(root.Q<Label>("Presents"), orbitronFont);
            SetFont(root.Q<Label>("RuleText"), orbitronFont);
            SetFont(root.Q<Label>("HudTL1"), orbitronFont);
            SetFont(root.Q<Label>("HudTL2"), orbitronFont);
            SetFont(root.Q<Label>("HudTR1"), orbitronFont);
            SetFont(root.Q<Label>("HudTR2"), orbitronFont);

            foreach (var l in root.Query<Label>(className: "stat-num").ToList())
                SetFont(l, orbitronFont);
            foreach (var l in root.Query<Label>(className: "bottom-step").ToList())
                SetFont(l, orbitronFont);
        }

        if (exo2Font != null)
        {
            SetFont(root.Q<Label>("Tagline"), exo2Font);
            SetFont(root.Q<Label>("Tagline2"), exo2Font);
            SetFont(root.Q<Label>("BottomRight"), exo2Font);

            foreach (var l in root.Query<Label>(className: "stat-lbl").ToList())
                SetFont(l, exo2Font);
        }
    }

    void SetFont(Label label, Font font)
    {
        if (label == null || font == null) return;
        label.style.unityFont = new StyleFont(font);
    }
}