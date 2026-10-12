using UnityEngine;

// Pon este script SOLO en el AI car agresor, en el MISMO GameObject que tiene
// el Rigidbody (el de carRigidbody en PhysicsAICar).
public class AggressorRam : MonoBehaviour
{
    [Header("Spin")]
    [Tooltip("Fuerza del giro (Impulse). Sube si no hace spin, baja si lo manda a volar.")]
    public float spinImpulse = 6000f;
    [Tooltip("Empujon lateral extra, ayuda a que la parte trasera se vaya.")]
    public float sideImpulse = 2500f;
    [Tooltip("Velocidad relativa minima para que cuente como golpe.")]
    public float minRelativeSpeed = 4f;
    public float hitCooldown = 3f;

    [Header("Debug")]
    public bool log = true;

    float cooldownTimer;
    public bool IsCoolingDown => cooldownTimer > 0f;

    void Update()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (cooldownTimer > 0f) return;

        Rigidbody other = collision.rigidbody;
        if (other == null || other == GetComponent<Rigidbody>()) return;
        if (collision.relativeVelocity.magnitude < minRelativeSpeed) return;

        cooldownTimer = hitCooldown;

        // Lado desde el que golpeamos (+ = a su derecha) y si pegamos en la
        // parte delantera o trasera de la victima.
        Vector3 toMe = transform.position - other.worldCenterOfMass;
        float side = Vector3.Dot(toMe, other.transform.right);
        float lz = other.transform.InverseTransformPoint(collision.GetContact(0).point).z;
        float lever = lz < 0f ? 1f : -1f;
        float dir = (side >= 0f ? 1f : -1f) * lever;

        other.AddTorque(Vector3.up * dir * spinImpulse, ForceMode.Impulse);
        other.AddForce(other.transform.right * (side >= 0f ? -1f : 1f) * sideImpulse, ForceMode.Impulse);

        if (log)
            Debug.Log($"[Aggressor] Hit {other.name}, spin dir {dir}");
    }
}