using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TelemetryHUD : MonoBehaviour
{
    [Header("Car")]
    public Rigidbody carRigidbody;

    [Header("Speed Display")]
    public TextMeshProUGUI speedText;
    public DiagonalSpeedBar speedBar;

    [Header("Speed Settings")]
    public float maxSpeed = 180f; // matches engine sound max in CarControl

    void Update()
    {
        if (carRigidbody == null) return;

        float speedKMH = carRigidbody.linearVelocity.magnitude * 3.6f;
        float t = Mathf.Clamp01(speedKMH / maxSpeed);

        speedText.text = Mathf.RoundToInt(speedKMH) + " km/h";

        if (speedBar != null)
            speedBar.SetFill(t);
    }
}