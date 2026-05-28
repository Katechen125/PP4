using UnityEngine;

public class OffTrackTrigger : MonoBehaviour
{
    void Start()
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError($"[OffTrack] {name} has no Collider!");
            return;
        }
        if (!col.isTrigger)
        {
            Debug.LogWarning($"[OffTrack] {name} collider is NOT a trigger, fixing");
            col.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[OffTrack] {name} entered by {other.name}");

        CarControl player = other.GetComponentInParent<CarControl>();
        if (player == null)
        {
            Debug.Log($"[OffTrack] {other.name} is not the player car, ignoring");
            return;
        }

        Debug.Log($"[OffTrack] Player off track, triggering penalty");

        if (PenaltyManager.Instance == null)
        {
            Debug.LogError($"[OffTrack] PenaltyManager.Instance is NULL");
            return;
        }

        PenaltyManager.Instance.TriggerOffTrackInstant();
    }
}