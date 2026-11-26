
using System.Collections;
using UnityEngine;

public class ChargingEnemy : MonoBehaviour
{
    private enum EnemyState
    {
        Idle,
        Approaching,
        Charging,
        Leaping,
        Recovering
    }

    [Header("Detection")]
    [SerializeField] private Transform player;
    [SerializeField] private float detectionRange = 20f;
    [SerializeField] private float attackRange = 5f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float rotationSpeed = 8f;

    [Header("Attack")]
    [SerializeField] private float chargeTime = 1.5f;
    [SerializeField] private float leapSpeed = 12f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float damage = 15f;

    [Header("Recovery")]
    [SerializeField] private float recoveryTime = 5f;

    private Rigidbody rb;
    private EnemyState state = EnemyState.Idle;

    private float timer;

    private bool isGrounded;
    private bool hasDamagedPlayer;

    private Vector3 leapDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (player == null)
            return;

        float distance = HorizontalDistance(
            transform.position,
            player.position
        );

        switch (state)
        {
            case EnemyState.Idle:

                if (distance <= detectionRange)
                {
                    state = EnemyState.Approaching;
                }

                break;

            case EnemyState.Approaching:

                if (distance > detectionRange)
                {
                    state = EnemyState.Idle;
                }
                else if (distance <= attackRange && isGrounded)
                {
                    state = EnemyState.Charging;
                    timer = chargeTime;
                }

                break;

            case EnemyState.Charging:

                FacePlayer();

                timer -= Time.deltaTime;

                if (timer <= 0f)
                {
                    Launch();
                }

                break;

            case EnemyState.Leaping:

                // No steering whatsoever while airborne.

                break;

            case EnemyState.Recovering:

                timer -= Time.deltaTime;

                if (timer <= 0f)
                {
                    state = EnemyState.Idle;
                }

                break;
        }
    }

    private void FixedUpdate()
    {
        // Reset before collision callbacks update grounded status.
        isGrounded = false;

        if (state == EnemyState.Approaching)
        {
            MoveTowardsPlayer();
        }
        else if (state == EnemyState.Charging ||
                 state == EnemyState.Idle ||
                 state == EnemyState.Recovering)
        {
            // Stop horizontal movement but preserve gravity.
            Vector3 velocity = rb.linearVelocity;

            velocity.x = 0f;
            velocity.z = 0f;

            rb.linearVelocity = velocity;
        }
    }

    private void MoveTowardsPlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        direction.Normalize();

        // Move horizontally towards the player.
        Vector3 velocity = rb.linearVelocity;

        velocity.x = direction.x * moveSpeed;
        velocity.z = direction.z * moveSpeed;

        rb.linearVelocity = velocity;

        // Face the direction of movement.
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        rb.MoveRotation(
            Quaternion.Slerp(
                rb.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime
            )
        );
    }

    private void FacePlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        rb.MoveRotation(
            Quaternion.Slerp(
                rb.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            )
        );
    }

    private void Launch()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        // Lock in the direction ONCE.
        leapDirection = direction.normalized;

        hasDamagedPlayer = false;

        state = EnemyState.Leaping;

        // Launch horizontally AND upwards.
        rb.linearVelocity =
            leapDirection * leapSpeed +
            Vector3.up * jumpForce;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (state != EnemyState.Leaping)
            return;

        HandleCollision(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        // Check whether we're standing on something.
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                isGrounded = true;

                // Only recover after landing from a leap.
                if (state == EnemyState.Leaping &&
                    rb.linearVelocity.y <= 0.1f)
                {
                    StopLeap();
                }

                break;
            }
        }
    }

    private void HandleCollision(Collision collision)
    {
        // Check for player collision.
        PlayerHealth health =
            collision.collider.GetComponentInParent<PlayerHealth>();

        if (health != null)
        {
            if (!hasDamagedPlayer)
            {
                health.TakeDamage(Mathf.RoundToInt(damage));
                hasDamagedPlayer = true;
            }

            return;
        }

        // Check for a wall.
        if (collision.contactCount == 0)
            return;

        Vector3 normal = collision.contacts[0].normal;

        // Ignore floors and ceilings.
        if (Mathf.Abs(normal.y) > 0.5f)
            return;

        // Reflect the HORIZONTAL direction.
        Vector3 horizontalNormal = new Vector3(
            normal.x,
            0f,
            normal.z
        ).normalized;

        leapDirection = Vector3.Reflect(
            leapDirection,
            horizontalNormal
        ).normalized;

        // Preserve the vertical velocity!
        // This means the enemy keeps falling naturally.
        float currentHorizontalSpeed = new Vector3(
            rb.linearVelocity.x,
            0f,
            rb.linearVelocity.z
        ).magnitude;

        rb.linearVelocity = new Vector3(
            leapDirection.x * currentHorizontalSpeed,
            rb.linearVelocity.y,
            leapDirection.z * currentHorizontalSpeed
        );
    }

    private void StopLeap()
    {
        state = EnemyState.Recovering;

        rb.linearVelocity = Vector3.zero;

        timer = recoveryTime;
    }

    public void Stun(float duration)
    {
        StopAllCoroutines();

        state = EnemyState.Recovering;

        rb.linearVelocity = Vector3.zero;

        timer = duration;
    }

    private float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;

        return Vector3.Distance(a, b);
    }
}
