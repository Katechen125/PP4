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
    public AudioClip fullSequenceClip; // the full 5-second clip

    [Header("Sync these to match your audio file")]
    public float beep1Time = 0.0f;  // when first beep hits
    public float beep2Time = 0.8f;  // when second beep hits
    public float beep3Time = 1.6f;  // when third beep hits
    public float beep4Time = 2.4f;  // when fourth beep hits
    public float beep5Time = 3.2f;  // when fifth beep hits
    public float lightsOutTime = 4.5f; // when lights go out in audio

    [Header("Car")]
    public MonoBehaviour carControl;

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

        if (carControl != null)
            carControl.enabled = false;

        StartCoroutine(LightsSequence());
    }

    IEnumerator LightsSequence()
    {
        yield return new WaitForSeconds(1f);

        // Play the full audio clip once
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

        // Wait until lights out moment in the audio
        yield return new WaitForSeconds(lightsOutTime - beep5Time);

        // LIGHTS OUT
        SetLight(matA, offColor);
        SetLight(matB, offColor);
        SetLight(matC, offColor);

        yield return new WaitForSeconds(0.05f);

        // Release car
        if (carControl != null)
            carControl.enabled = true;
        GameManager.Instance?.StartRace();

        Debug.Log("LIGHTS OUT — GO!");
    }

    void SetLight(Material mat, Color emissionColor)
    {
        mat.SetColor("_EmissionColor", emissionColor);
    }
}