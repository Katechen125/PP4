using UnityEngine;

public class CarCollision : MonoBehaviour
{
    public float minForceThreshold = 15f; // raise this significantly

    [Header("Ignore these layers")]
    public LayerMask ignoreLayers; // assign Road and Terrain layers here

    void OnCollisionEnter(Collision collision)
    {
        // Ignore road and terrain collisions
        if (((1 << collision.gameObject.layer) & ignoreLayers) != 0)
            return;

        // Ignore if it's a wheel collider hitting the ground
        if (collision.gameObject.name.ToLower().Contains("tarmac"))
            return;
        if (collision.gameObject.name.ToLower().Contains("terrain"))
            return;
        if (collision.gameObject.name.ToLower().Contains("ground"))
            return;

        float force = collision.relativeVelocity.magnitude;

        if (force > minForceThreshold)
        {
            Debug.Log($"REAL Collision with {collision.gameObject.name} force: {force}");
            GameManager.Instance?.OnCarCollision(force);
        }
    }
}
