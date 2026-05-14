using UnityEngine;

public class CarCollision : MonoBehaviour
{
    [Header("Collision Settings")]
    public float minForceThreshold = 10f;
    public LayerMask ignoreLayers;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip lightCollisionClip;  // small bump
    public AudioClip heavyCollisionClip;  // big crash
    public float heavyCollisionThreshold = 40f;

    void Start()
    {
        // Auto grab AudioSource if not assigned
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    void OnCollisionEnter(Collision collision)
    {
        // Ignore specified layers
        if (((1 << collision.gameObject.layer) & ignoreLayers) != 0)
            return;

        // Ignore road surface
        string hitName = collision.gameObject.name.ToLower();
        if (hitName.Contains("tarmac")) return;
        if (hitName.Contains("terrain")) return;
        if (hitName.Contains("ground")) return;

        float force = collision.relativeVelocity.magnitude;

        if (force > minForceThreshold)
        {
            // Play sound based on impact force
            if (force >= heavyCollisionThreshold)
                PlaySound(heavyCollisionClip);
            else
                PlaySound(lightCollisionClip);

            // Tell GameManager
            GameManager.Instance?.OnCarCollision(force);

            Debug.Log($"Collision with {collision.gameObject.name} force: {force}");
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}
