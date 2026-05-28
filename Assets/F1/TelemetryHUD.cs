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
    public float maxSpeed = 180f;


    public TextMeshProUGUI penaltyTimeText;
    private float totalPenalties = 0f;

    void Update()
    {
        if (carRigidbody == null) return;

        float speedKMH = carRigidbody.linearVelocity.magnitude * 3.6f;
        float t = Mathf.Clamp01(speedKMH / maxSpeed);

        speedText.text = Mathf.RoundToInt(speedKMH) + " km/h";

        if (speedBar != null)
            speedBar.SetFill(t);
    }

    // Called by GameManager when penalty added
    public void AddPenaltyDisplay(float seconds)
    {
        totalPenalties += seconds;

        if (penaltyTimeText != null)
        {
            penaltyTimeText.text = $"+{totalPenalties:F0}s";
            penaltyTimeText.color = Color.red;
        }
    }

    public void ResetPenalties()
    {
        totalPenalties = 0f;
        if (penaltyTimeText != null)
            penaltyTimeText.text = "";
    }
}