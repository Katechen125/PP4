using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class RacingLine : MonoBehaviour
{
    [Header("Waypoints")]
    public Transform waypointParent;

    [Header("Car")]
    public Transform car;

    [Header("Display Settings")]
    public float lookaheadDistance = 150f;
    public float lineWidth = 0.5f;
    public float heightOffset = 0.05f;

    [Header("Zone Colors")]
    public Color fullThrottleColor = new Color(0.1f, 0.9f, 0.2f, 1f);
    public Color liftColor = new Color(1.0f, 0.85f, 0.0f, 1f);
    public Color brakingColor = new Color(0.9f, 0.1f, 0.1f, 1f);

    private LineRenderer lr;
    private Transform[] waypoints;

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
        lr.numCapVertices = 4;
        lr.numCornerVertices = 4;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.sortingOrder = 10;

        // URP material
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
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
        if (car == null) return;

        int startIndex = GetClosestWaypointIndex();
        DrawLineAhead(startIndex);
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

    void DrawLineAhead(int startIndex)
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
            colors.Add(GetZoneColor(waypoints[i]));

            distanceCovered += Vector3.Distance(
                waypoints[i].position,
                waypoints[next].position
            );
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
            alphaKeys[i] = new GradientAlphaKey(1f, t);
        }

        Gradient gradient = new Gradient();
        gradient.SetKeys(colorKeys, alphaKeys);
        lr.colorGradient = gradient;
    }
}