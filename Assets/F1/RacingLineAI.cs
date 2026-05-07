using UnityEngine;
using System.Collections.Generic;

public class RacingLineAI : MonoBehaviour
{
    [Header("Track Mesh")]
    public MeshFilter trackMesh;

    [Header("Settings")]
    public int sampleCount = 60;
    public float innerOffset = 0.3f;
    public float waypointY = -1.656f;

    [Header("Smoothing")]
    public int smoothingPasses = 5;

    [ContextMenu("Generate Racing Line")]
    public void GenerateRacingLine()
    {
        foreach (Transform child in transform)
            DestroyImmediate(child.gameObject);

        if (trackMesh == null)
        {
            Debug.LogError("Assign the track MeshFilter first!");
            return;
        }

        Mesh mesh = trackMesh.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        Matrix4x4 localToWorld = trackMesh.transform.localToWorldMatrix;

        // Convert all vertices to world space
        List<Vector3> worldVerts = new List<Vector3>();
        foreach (Vector3 v in vertices)
            worldVerts.Add(localToWorld.MultiplyPoint3x4(v));

        // Find the center point of all vertices
        Vector3 center = Vector3.zero;
        foreach (Vector3 v in worldVerts)
            center += v;
        center /= worldVerts.Count;

        // Sort vertices by angle around center
        worldVerts.Sort((a, b) =>
        {
            float angleA = Mathf.Atan2(a.z - center.z, a.x - center.x);
            float angleB = Mathf.Atan2(b.z - center.z, b.x - center.x);
            return angleA.CompareTo(angleB);
        });

        // Sample evenly spaced points
        List<Vector3> sampled = new List<Vector3>();
        int step = Mathf.Max(1, worldVerts.Count / sampleCount);
        for (int i = 0; i < worldVerts.Count; i += step)
        {
            Vector3 v = worldVerts[i];
            // Pull toward center by innerOffset to stay on road
            Vector3 toCenter = (center - v).normalized;
            Vector3 point = v + toCenter *
                (Vector3.Distance(v, center) * innerOffset);
            point.y = waypointY;
            sampled.Add(point);
        }

        // Smooth the line
        List<Vector3> smoothed = new List<Vector3>(sampled);
        for (int pass = 0; pass < smoothingPasses; pass++)
        {
            for (int i = 1; i < smoothed.Count - 1; i++)
            {
                smoothed[i] = (sampled[i - 1] +
                    sampled[i] + sampled[i + 1]) / 3f;
            }
        }

        // Create waypoint GameObjects
        for (int i = 0; i < smoothed.Count && i < sampleCount; i++)
        {
            // Detect corner by curvature for zone coloring
            string zone = "";
            if (i > 0 && i < smoothed.Count - 1)
            {
                Vector3 prev = smoothed[i - 1];
                Vector3 curr = smoothed[i];
                Vector3 next = smoothed[i + 1];

                Vector3 d1 = (curr - prev).normalized;
                Vector3 d2 = (next - curr).normalized;
                float curvature = 1f - Vector3.Dot(d1, d2);

                if (curvature > 0.05f)
                    zone = "_BRAKE";
                else if (curvature > 0.02f)
                    zone = "_LIFT";
            }

            GameObject wp = new GameObject($"WP_{i:00}{zone}");
            wp.transform.position = smoothed[i];
            wp.transform.parent = transform;
        }

        Debug.Log($"Racing line generated with {transform.childCount} waypoints.");
    }

    void OnDrawGizmos()
    {
        if (transform.childCount < 2) return;

        for (int i = 0; i < transform.childCount - 1; i++)
        {
            string name = transform.GetChild(i).name.ToUpper();
            if (name.Contains("BRAKE"))
                Gizmos.color = Color.red;
            else if (name.Contains("LIFT"))
                Gizmos.color = Color.yellow;
            else
                Gizmos.color = Color.green;

            Gizmos.DrawLine(
                transform.GetChild(i).position,
                transform.GetChild(i + 1).position
            );
        }
    }
}