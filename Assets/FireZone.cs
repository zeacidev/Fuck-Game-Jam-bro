using System.Collections.Generic;
using UnityEngine;

/// Implement this on anything that should take fire damage (enemies, player, props).
public interface IBurnable
{
    void TakeFireDamage(float amount);
}

/// Area-of-effect fire. Spawned by Molotov on impact.
/// Add a ParticleSystem as a child for the visuals and scale it to match `radius`.
public class FireZone : MonoBehaviour
{
    [Header("Area")]
    public float radius = 3.5f;
    public LayerMask affectedLayers = ~0;     // set to e.g. Enemy | Player

    [Header("Damage")]
    public float duration = 6f;
    public float tickInterval = 0.5f;
    public float damagePerTick = 5f;
    public bool falloffWithDistance = true;   // more damage near the center

    [Header("Optional")]
    public ParticleSystem fireParticles;

    float endTime;
    float nextTick;
    readonly Collider[] hits = new Collider[64];
    readonly HashSet<IBurnable> alreadyHitThisTick = new HashSet<IBurnable>();

    void Start()
    {
        endTime = Time.time + duration;
        nextTick = Time.time;                 // first tick immediately

        if (fireParticles != null)
        {
            // Scale visuals to the damage radius
            transform.localScale = Vector3.one * (radius / 3.5f);
        }

        Destroy(gameObject, duration + 1f);   // let particles fade out
    }

    void Update()
    {
        if (Time.time >= endTime)
        {
            if (fireParticles != null && fireParticles.isEmitting)
                fireParticles.Stop();
            return;
        }

        if (Time.time >= nextTick)
        {
            ApplyDamage();
            nextTick += tickInterval;
        }
    }

    void ApplyDamage()
    {
        int count = Physics.OverlapSphereNonAlloc(
            transform.position, radius, hits, affectedLayers, QueryTriggerInteraction.Ignore);

        alreadyHitThisTick.Clear();

        for (int i = 0; i < count; i++)
        {
            // Look on the collider or its parents so child colliders work too
            var burnable = hits[i].GetComponentInParent<IBurnable>();
            if (burnable == null || !alreadyHitThisTick.Add(burnable)) continue;

            float dmg = damagePerTick;
            if (falloffWithDistance)
            {
                float dist = Vector3.Distance(transform.position, hits[i].ClosestPoint(transform.position));
                dmg *= Mathf.Clamp01(1f - dist / radius);
                dmg = Mathf.Max(dmg, damagePerTick * 0.25f); // never drop to zero inside the radius
            }
            burnable.TakeFireDamage(dmg);
        }
    }

    // Shows the area of effect in the Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.35f);
        Gizmos.DrawSphere(transform.position, radius);
    }
}
