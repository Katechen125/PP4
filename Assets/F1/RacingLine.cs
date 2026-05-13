using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class RacingLine : MonoBehaviour
{
    [Header("Waypoints")]
    public Transform waypointParent;

    [Header("Car")]
    public Transform car;
    public Rigidbody carRigidbody;

    [Header("Display Settings")]
    public float lookaheadDistance = 150f;
    public float lineWidth = 0.5f;
    public float heightOffset = 0.05f;

    [Header("Speed Settings")]
    public float fadeInSpeed = 30f;   // km/h where line starts appearing
    public float fadeOutSpeed = 80f;  // km/h where line fully disappears
    public float dangerSpeed = 60f;   // km/h in a brake zone = danger flash

    [Header("Zone Colors")]
    public Color fullThrottleColor = new Color(0.1f, 0.9f, 0.2f, 1f);
    public Color liftColor = new Color(1.0f, 0.85f, 0.0f, 1f);
    public Color brakingColor = new Color(0.9f, 0.1f, 0.1f, 1f);
    public Color dangerColor = new Color(1.0f, 0.0f, 0.0f, 1f);

    [Header("Pulse Settings")]
    public float pulseSpeed = 3f;
    public float shimmerSpeed = 2f;
    public float minWidth = 0.2f;
    public float maxWidth = 0.8f;

    private LineRenderer lr;
    private Transform[] waypoints;
    private float shimmerOffset = 0f;
    private float pulseTime = 0f;
    private bool inDangerZone = false;
    private float currentAlpha = 1f;
    private float dangerFlashTimer = 0f;

    void Start()
    {
        lr = GetComponent<LineRenderer>();
        SetupLineRenderer();
        LoadWaypoints();
    }

    void SetupLineRenderer()
    {
        lr.useWorldSpace = true;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.numCapVertices = 8;
        lr.numCornerVertices = 8;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.sortingOrder = 10;

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        mat.SetColor("_BaseColor", fullThrottleColor);
        lr.material = mat;
    }

    void LoadWaypoints()
    {
        if (waypointParent == null)
        {
            Debug.LogError("Waypoint Parent not assigned!");
            return;
        }

        int count = waypointParent.childCount;
        waypoints = new Transform[count];
        for (int i = 0; i < count; i++)
            waypoints[i] = waypointParent.GetChild(i);

        Debug.Log($"Loaded {waypoints.Length} waypoints.");
    }

    void Update()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        if (car == null || carRigidbody == null) return;

        float speedKMH = carRigidbody.linearVelocity.magnitude * 3.6f;

        // Update timers
        pulseTime += Time.deltaTime * pulseSpeed;
        shimmerOffset += Time.deltaTime * shimmerSpeed;
        dangerFlashTimer += Time.deltaTime;

        // Fade line based on speed
        float targetAlpha;
        if (speedKMH < fadeInSpeed)
            targetAlpha = 1f;
        else if (speedKMH > fadeOutSpeed)
            targetAlpha = 0f;
        else
            targetAlpha = 1f - ((speedKMH - fadeInSpeed) / (fadeOutSpeed - fadeInSpeed));

        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * 3f);

        // Check if in danger zone
        int closestIndex = GetClosestWaypointIndex();
        string wpName = waypoints[closestIndex].name.ToUpper();
        inDangerZone = wpName.Contains("BRAKE") && speedKMH > dangerSpeed;

        // Pulse width when in danger
        float pulsedWidth = lineWidth;
        if (inDangerZone)
        {
            float pulse = (Mathf.Sin(pulseTime * 8f) + 1f) / 2f;
            pulsedWidth = Mathf.Lerp(minWidth, maxWidth, pulse);
        }

        lr.startWidth = pulsedWidth;
        lr.endWidth = pulsedWidth * 0.6f;

        int startIndex = closestIndex;
        DrawLineAhead(startIndex, speedKMH);

        Debug.Log($"Speed: {carRigidbody.linearVelocity.magnitude * 3.6f} | Alpha: {currentAlpha}");
    }

    int GetClosestWaypointIndex()
    {
        int closest = 0;
        float minDist = Mathf.Infinity;

        for (int i = 0; i < waypoints.Length; i++)
        {
            float dist = Vector3.Distance(car.position, waypoints[i].position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = i;
            }
        }
        return closest;
    }

    void DrawLineAhead(int startIndex, float speedKMH)
    {
        List<Vector3> points = new List<Vector3>();
        List<Color> colors = new List<Color>();

        float distanceCovered = 0f;
        int i = startIndex;

        while (distanceCovered < lookaheadDistance)
        {
            int next = (i + 1) % waypoints.Length;
            Vector3 pos = waypoints[i].position + Vector3.up * heightOffset;

            points.Add(pos);

            // Get base zone color
            Color zoneColor = GetZoneColor(waypoints[i]);

            // Danger flash — override to bright red
            if (inDangerZone)
            {
                float flash = (Mathf.Sin(dangerFlashTimer * 12f) + 1f) / 2f;
                zoneColor = Color.Lerp(brakingColor, dangerColor, flash);
            }

            // Shimmer effect — wave of brightness along the line
            float shimmerPos = (distanceCovered / lookaheadDistance + shimmerOffset) % 1f;
            float shimmer = Mathf.Pow(Mathf.Sin(shimmerPos * Mathf.PI * 4f), 2f) * 0.4f;
            zoneColor = new Color(
                Mathf.Clamp01(zoneColor.r + shimmer),
                Mathf.Clamp01(zoneColor.g + shimmer),
                Mathf.Clamp01(zoneColor.b + shimmer),
                currentAlpha
            );

            colors.Add(zoneColor);

            distanceCovered += Vector3.Distance(
                waypoints[i].position, waypoints[next].position);
            i = next;

            if (i == startIndex) break;
        }

        if (points.Count < 2) return;

        lr.positionCount = points.Count;
        lr.SetPositions(points.ToArray());
        ApplyColors(colors);
    }

    Color GetZoneColor(Transform wp)
    {
        string name = wp.name.ToUpper();
        if (name.Contains("BRAKE")) return brakingColor;
        if (name.Contains("LIFT")) return liftColor;
        return fullThrottleColor;
    }

    void ApplyColors(List<Color> colors)
    {
        int count = colors.Count;
        int maxKeys = 8;

        GradientColorKey[] colorKeys = new GradientColorKey[maxKeys];
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[maxKeys];

        for (int i = 0; i < maxKeys; i++)
        {
            float t = (float)i / (maxKeys - 1);
            int colorIndex = Mathf.RoundToInt(t * (count - 1));
            colorKeys[i] = new GradientColorKey(colors[colorIndex], t);
            alphaKeys[i] = new GradientAlphaKey(colors[colorIndex].a, t);
        }

        Gradient gradient = new Gradient();
        gradient.SetKeys(colorKeys, alphaKeys);
        lr.colorGradient = gradient;
    }
}