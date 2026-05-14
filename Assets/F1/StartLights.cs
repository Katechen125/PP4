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

        // Lock car using flag — input stays warm
        if (carControl != null)
            carControl.raceStarted = false;

        StartCoroutine(LightsSequence());
    }

    IEnumerator LaunchHeave()
    {
        // Quick drop then rise — like the car launching forward
        float duration = 0.6f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Heave curve: dip down then spring up
            float heave = Mathf.Sin(t * Mathf.PI) * -8f;
            PlatformController.singleton.Heave = heave;

            yield return null;
        }

        // Return to neutral
        PlatformController.singleton.Heave = 0f;
    }

    IEnumerator LightsSequence()
    {
        yield return new WaitForSeconds(1f);

        // Play full sequence audio once
        audioSource.clip = fullSequenceClip;
        audioSource.Play();

        // Beep 1 — light A
        yield return new WaitForSeconds(beep1Time);
        SetLight(matA, onColor);

        // Beep 2
        yield return new WaitForSeconds(beep2Time - beep1Time);

        // Beep 3 — light B
        yield return new WaitForSeconds(beep3Time - beep2Time);
        SetLight(matB, onColor);

        // Beep 4
        yield return new WaitForSeconds(beep4Time - beep3Time);

        // Beep 5 — light C
        yield return new WaitForSeconds(beep5Time - beep4Time);
        SetLight(matC, onColor);

        // Wait until lights out moment in audio
        yield return new WaitForSeconds(lightsOutTime - beep5Time);

        // LIGHTS OUT — instant release, zero delay
        SetLight(matA, offColor);
        SetLight(matB, offColor);
        SetLight(matC, offColor);

        if (carControl != null)
            carControl.raceStarted = true;

        GameManager.Instance?.StartRace();
        StartCoroutine(LaunchHeave());
        Debug.Log("LIGHTS OUT — GO!");
    }

    void SetLight(Material mat, Color emissionColor)
    {
        mat.SetColor("_EmissionColor", emissionColor);
    }
}