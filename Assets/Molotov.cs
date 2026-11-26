using UnityEngine;

/// The thrown bottle. Needs a Rigidbody + Collider on the prefab.
/// Shatters on impact and spawns a FireZone.
[RequireComponent(typeof(Rigidbody))]
public class Molotov : MonoBehaviour
{
    [Header("Fire")]
    public FireZone fireZonePrefab;
    public float maxFlightTime = 6f;      // safety: shatter midair if it never hits anything
    public float spinSpeed = 540f;        // degrees/sec, purely visual

    Rigidbody rb;
    bool shattered;

    void Awake() => rb = GetComponent<Rigidbody>();

    public void Launch(Vector3 velocity)
    {
        rb.linearVelocity = velocity;     // use rb.velocity on Unity 2022 and older
        rb.angularVelocity = Random.onUnitSphere * spinSpeed * Mathf.Deg2Rad;
        Invoke(nameof(Shatter), maxFlightTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        // Spawn the fire at the contact point, oriented along the surface normal
        ContactPoint contact = collision.GetContact(0);
        Shatter(contact.point, contact.normal);
    }

    void Shatter() => Shatter(transform.position, Vector3.up);

    void Shatter(Vector3 point, Vector3 normal)
    {
        if (shattered) return;
        shattered = true;

        Instantiate(fireZonePrefab, point + normal * 0.05f,
                    Quaternion.FromToRotation(Vector3.up, normal));
        Destroy(gameObject);
    }
}
