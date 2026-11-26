using System.Collections;
using UnityEngine;

/// Trigger-based damage zone (Option 2).
/// Needs: a Collider with "Is Trigger" on, and a Rigidbody on either this object or the player.
public class DamageScript : MonoBehaviour
{
    public int damageAmount = 10;
    public float damageInterval = 1f;

    private Coroutine damageCoroutine;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health != null && damageCoroutine == null)
        {
            damageCoroutine = StartCoroutine(DealDamageOverTime(health));
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        StopDamage();
    }

    private void OnDisable()
    {
        StopDamage();
    }

    private void StopDamage()
    {
        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
            damageCoroutine = null;
        }
    }

    private IEnumerator DealDamageOverTime(PlayerHealth health)
    {
        // Stops automatically if the player dies or is destroyed
        while (health != null && !health.IsDead)
        {
            health.TakeDamage(damageAmount);
            yield return new WaitForSeconds(damageInterval);
        }

        damageCoroutine = null;
    }
}