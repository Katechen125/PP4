using UnityEngine;

public class CarCollision : MonoBehaviour
{
    [Header("Collision Settings")]
    public float minForceThreshold = 10f;
    public LayerMask ignoreLayers;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip lightCollisionClip;
    public AudioClip heavyCollisionClip;

    public float heavyCollisionThreshold = 40f;

    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    void OnCollisionEnter(Collision collision)
    {
        // Ignore layers
        if (((1 << collision.gameObject.layer) & ignoreLayers) != 0)
            return;

        string hitName =
            collision.gameObject.name.ToLower();

        // Ignore road / terrain
        if (hitName.Contains("tarmac") ||
            hitName.Contains("terrain") ||
            hitName.Contains("ground"))
            return;

        float force =
            collision.relativeVelocity.magnitude;

        if (force <= minForceThreshold)
            return;

        // =====================================================
        // AUDIO
        // =====================================================

        if (force >= heavyCollisionThreshold)
            PlaySound(heavyCollisionClip);
        else
            PlaySound(lightCollisionClip);

        // Camera shake / effects
        GameManager.Instance?.OnCarCollision(force);

        GameObject hit = collision.gameObject;

        // =====================================================
        // AI CAR DETECTION
        // =====================================================

        PhysicsAICar aiCar =
            hit.GetComponent<PhysicsAICar>() ??
            hit.GetComponentInParent<PhysicsAICar>() ??
            hit.GetComponentInChildren<PhysicsAICar>();

        if (aiCar != null)
        {
            if (aiCar.isAggressor) return;
            PenaltyManager.Instance?.CarHitOtherCar(force);

            Debug.Log(
                $"Hit AI car: {hit.name} | Force: {force:F1}"
            );

            return;
        }

        // =====================================================
        // WALL DETECTION
        // =====================================================

        string hitTag = hit.tag;

        if (hitTag == "Untagged" &&
            hit.transform.parent != null)
        {
            hitTag = hit.transform.parent.tag;
        }

        bool isWall =
            hitTag == "Wall" ||
            hitName.Contains("wall") ||
            hitName.Contains("barrier") ||
            hitName.Contains("fence") ||
            hitName.Contains("tire") ||
            hitName.Contains("guardrail");

        if (isWall)
        {
            PenaltyManager.Instance?.CarHitWall(force);

            Debug.Log(
                $"Wall hit: {hit.name} | Force: {force:F1}"
            );
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}