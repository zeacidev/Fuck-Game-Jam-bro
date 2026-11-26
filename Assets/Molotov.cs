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
    public float groundCheckDistance = 5f; // how far below the impact to look for the floor

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

        // Find the ground below the impact so the fire is never sideways or floating
        Vector3 spawnPos = point;
        Quaternion spawnRot = Quaternion.identity;

        RaycastHit[] hits = Physics.RaycastAll(point + Vector3.up * 0.5f, Vector3.down,
                                               groundCheckDistance, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit h in hits)
        {
            // Skip characters (player/enemies) so we find the actual floor
            if (h.collider.GetComponentInParent<IBurnable>() != null) continue;
            if (h.collider.transform.IsChildOf(transform)) continue;

            spawnPos = h.point + h.normal * 0.05f;
            // Align to the floor, but only if it's a reasonably flat surface
            spawnRot = h.normal.y > 0.5f ? Quaternion.FromToRotation(Vector3.up, h.normal)
                                         : Quaternion.identity;
            break;
        }

        Instantiate(fireZonePrefab, spawnPos, spawnRot);
        Destroy(gameObject);
    }
}
