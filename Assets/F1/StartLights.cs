using UnityEngine;
using System.Collections;

public class StartLights : MonoBehaviour
{
    [Header("The 3 LED renderers")]
    public Renderer ledA;
    public Renderer ledB;
    public Renderer ledC;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip fullSequenceClip;

    [Header("Sync to your audio file")]
    public float beep1Time = 0.0f;
    public float beep2Time = 0.8f;
    public float beep3Time = 1.6f;
    public float beep4Time = 2.4f;
    public float beep5Time = 3.2f;
    public float lightsOutTime = 4.5f;

    [Header("Car")]
    public GameObject carObject;
    public CarControl carControl;

    private Material matA;
    private Material matB;
    private Material matC;

    private Color onColor = new Color(2f, 0f, 0f);
    private Color offColor = new Color(0f, 0f, 0f);

    void Start()
    {
        matA = new Material(ledA.sharedMaterial);
        matB = new Material(ledB.sharedMaterial);
        matC = new Material(ledC.sharedMaterial);

        ledA.material = matA;
        ledB.material = matB;
        ledC.material = matC;

        SetLight(matA, offColor);
        SetLight(matB, offColor);
        SetLight(matC, offColor);

        // Player can free-roll until the sequence begins
        if (carControl != null)
            carControl.raceStarted = false;

        StartCoroutine(LightsSequence());
    }

    IEnumerator LightsSequence()
    {
        yield return new WaitForSeconds(1f);

        // Audio sequence begins NOW — arm false start at this moment
        PenaltyManager.Instance?.ArmFalseStartCheck();

        audioSource.clip = fullSequenceClip;
        audioSource.Play();

        // Beep 1 — light A on
        yield return new WaitForSeconds(beep1Time);
        SetLight(matA, onColor);

        // Beep 2
        yield return new WaitForSeconds(beep2Time - beep1Time);

        // Beep 3 — light B on
        yield return new WaitForSeconds(beep3Time - beep2Time);
        SetLight(matB, onColor);

        // Beep 4
        yield return new WaitForSeconds(beep4Time - beep3Time);

        // Beep 5 — light C on
        yield return new WaitForSeconds(beep5Time - beep4Time);
        SetLight(matC, onColor);

        // Wait until lights out
        yield return new WaitForSeconds(lightsOutTime - beep5Time);

        SetLight(matA, offColor);
        SetLight(matB, offColor);
        SetLight(matC, offColor);

        if (carControl != null)
            carControl.raceStarted = true;

        Debug.Log("Releasing AI cars...");
        PhysicsAICar[] aiCars = FindObjectsByType<PhysicsAICar>(
            FindObjectsInactive.Exclude);
        foreach (var ai in aiCars)
            ai.OnRaceStart();

        GameManager.Instance?.StartRace();
        PenaltyManager.Instance?.OnLightsOut();
        Debug.Log("LIGHTS OUT — GO!");
    }

    void SetLight(Material mat, Color emissionColor)
    {
        mat.SetColor("_EmissionColor", emissionColor);
    }
}