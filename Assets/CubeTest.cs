using UnityEngine;

public class CubeTest : MonoBehaviour
{
    void Start() { Debug.Log("[Test] VisionTest corriendo, cubo en " + transform.position); }
    void Update() { transform.Rotate(0, 45 * Time.deltaTime, 0); }
}