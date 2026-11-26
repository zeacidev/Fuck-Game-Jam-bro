using System.Collections;
using UnityEngine;

/// Attach to an enemy. When the player is in range and visible, the enemy
/// aims and throws a molotov so it lands on the player.
public class EnemyMolotovThrower : MonoBehaviour
{
    [Header("References")]
    public Molotov molotovPrefab;
    public Transform throwPoint;              // empty child at the enemy's hand
    public Transform target;                  // leave empty to auto-find the "Player" tag
    public Animator animator;                 // optional
    public string throwTrigger = "Throw";     // optional animator trigger
    public float throwDelay = 0.4f;           // wind-up time before the bottle leaves the hand

    [Header("Detection")]
    public float maxRange = 25f;
    public float minRange = 5f;               // won't throw at point-blank (would burn itself)
    public LayerMask lineOfSightBlockers = ~0;
    public float eyeHeight = 1.6f;
    public bool requireLineOfSight = true;

    [Header("Throw")]
    public float throwSpeed = 16f;
    public bool highArc = false;              // true = lobbed, false = flatter throw
    public bool leadTarget = true;            // aim where the player is going
    public float aimInaccuracy = 0.75f;       // random offset in meters so it isn't pixel perfect
    public float cooldown = 6f;
    public float turnSpeed = 8f;

    float nextThrowTime;
    bool throwing;
    Vector3 lastTargetPos;
    Vector3 targetVelocity;

    void Start()
    {
        if (target == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
        }
        if (target != null) lastTargetPos = target.position;
        nextThrowTime = Time.time + 1f;       // small grace period at spawn
    }

    void Update()
    {
        if (target == null) return;

        // Track target velocity for lead prediction
        if (Time.deltaTime > 0f)
        {
            Vector3 v = (target.position - lastTargetPos) / Time.deltaTime;
            targetVelocity = Vector3.Lerp(targetVelocity, v, 10f * Time.deltaTime);
        }
        lastTargetPos = target.position;

        if (throwing) return;

        float dist = Vector3.Distance(transform.position, target.position);
        if (dist > maxRange || dist < minRange) return;

        FaceTarget();

        if (Time.time < nextThrowTime) return;
        if (requireLineOfSight && !HasLineOfSight()) return;

        StartCoroutine(ThrowRoutine());
    }

    IEnumerator ThrowRoutine()
    {
        throwing = true;
        nextThrowTime = Time.time + cooldown;

        if (animator != null && !string.IsNullOrEmpty(throwTrigger))
            animator.SetTrigger(throwTrigger);

        // Keep facing the target during the wind-up
        float t = 0f;
        while (t < throwDelay)
        {
            t += Time.deltaTime;
            if (target != null) FaceTarget();
            yield return null;
        }

        if (target != null) Throw();
        throwing = false;
    }

    void Throw()
    {
        Vector3 start = throwPoint != null ? throwPoint.position : transform.position + Vector3.up * eyeHeight;
        Vector3 aimPoint = GetAimPoint(start);

        Vector3 velocity;
        if (!TrySolveLaunchVelocity(start, aimPoint, throwSpeed, highArc, out velocity))
        {
            // Out of reach at this speed: fall back to a 45 degree lob toward the target
            Vector3 flat = new Vector3(aimPoint.x - start.x, 0f, aimPoint.z - start.z).normalized;
            velocity = (flat + Vector3.up).normalized * throwSpeed;
        }

        Molotov m = Instantiate(molotovPrefab, start, Quaternion.identity);

        // Don't let the bottle collide with the thrower
        Collider bottleCol = m.GetComponent<Collider>();
        if (bottleCol != null)
        {
            foreach (Collider c in GetComponentsInChildren<Collider>())
                Physics.IgnoreCollision(bottleCol, c);
        }

        m.Launch(velocity);
    }

    Vector3 GetAimPoint(Vector3 start)
    {
        // Aim at the player's feet area so the fire lands on them
        Vector3 aim = target.position;

        if (leadTarget)
        {
            // Estimate flight time from distance and speed, then lead by that much
            float dist = Vector3.Distance(start, aim);
            float flightTime = dist / Mathf.Max(throwSpeed * 0.8f, 0.1f);
            Vector3 flatVel = new Vector3(targetVelocity.x, 0f, targetVelocity.z);
            aim += flatVel * flightTime;
        }

        // Random inaccuracy
        Vector2 offset = Random.insideUnitCircle * aimInaccuracy;
        aim += new Vector3(offset.x, 0f, offset.y);
        return aim;
    }

    /// Solves for the launch velocity that makes a projectile hit `end`
    /// given a fixed launch speed and the scene's gravity.
    static bool TrySolveLaunchVelocity(Vector3 start, Vector3 end, float speed, bool high, out Vector3 velocity)
    {
        velocity = Vector3.zero;

        Vector3 diff = end - start;
        Vector3 flat = new Vector3(diff.x, 0f, diff.z);
        float dx = flat.magnitude;
        float dy = diff.y;
        float g = -Physics.gravity.y;

        if (dx < 0.01f || g <= 0f) return false;

        float v2 = speed * speed;
        float disc = v2 * v2 - g * (g * dx * dx + 2f * dy * v2);
        if (disc < 0f) return false;          // target out of range at this speed

        float root = Mathf.Sqrt(disc);
        float angle = Mathf.Atan((v2 + (high ? root : -root)) / (g * dx));

        velocity = flat.normalized * (Mathf.Cos(angle) * speed) + Vector3.up * (Mathf.Sin(angle) * speed);
        return true;
    }

    void FaceTarget()
    {
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        Quaternion look = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
    }

    bool HasLineOfSight()
    {
        Vector3 from = transform.position + Vector3.up * eyeHeight;
        Vector3 to = target.position + Vector3.up * 1f;
        Vector3 dir = to - from;

        if (Physics.Raycast(from, dir.normalized, out RaycastHit hit, dir.magnitude, lineOfSightBlockers, QueryTriggerInteraction.Ignore))
        {
            // Visible if the first thing hit is the target (or part of it)
            return hit.transform == target || hit.transform.IsChildOf(target);
        }
        return true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, minRange);
    }
}
