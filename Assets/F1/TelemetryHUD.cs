using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TelemetryHUD : MonoBehaviour
{
    [Header("Car")]
    public Rigidbody carRigidbody;

    [Header("Speed Display")]
    public TextMeshProUGUI speedText;
    public Image speedBar;
    public float maxSpeed = 200f; // km/h

    [Header("Colors")]
    public Color slowColor = new Color(0.1f, 0.9f, 0.1f); // green
    public Color midColor = new Color(1.0f, 0.8f, 0.0f); // yellow
    public Color fastColor = new Color(0.9f, 0.1f, 0.1f); // red

    void Update()
    {
        if (carRigidbody == null) return;

        // Get speed in km/h
        float speedMS = carRigidbody.linearVelocity.magnitude;
        float speedKMH = speedMS * 3.6f;

        // Update speed number
        speedText.text = Mathf.RoundToInt(speedKMH) + " km/h";

        // Update bar fill
        float t = Mathf.Clamp01(speedKMH / maxSpeed);
        speedBar.fillAmount = t;

        // Color gradient green -> yellow -> red
        Color barColor;
        if (t < 0.5f)
            barColor = Color.Lerp(slowColor, midColor, t * 2f);
        else
            barColor = Color.Lerp(midColor, fastColor, (t - 0.5f) * 2f);

        speedBar.color = barColor;
    }
}