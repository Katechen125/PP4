using UnityEngine;

public class LapTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        // Check root object tag instead of the specific collider
        GameObject root = other.transform.root.gameObject;

        Debug.Log($"Trigger hit — collider: {other.gameObject.name} root: {root.name} rootTag: {root.tag}");

        if (root.CompareTag("Player"))
        {
            Debug.Log("LAP TRIGGERED!");
            GameManager.Instance?.TriggerLap();
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        BoxCollider bc = GetComponent<BoxCollider>();
        if (bc != null)
            Gizmos.DrawWireCube(
                transform.position + bc.center, bc.size);
    }
}