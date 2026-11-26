using System.Collections;
using UnityEngine;

public class DamageScript : MonoBehaviour
{
    public int damageAmount = 10;
    public float damageInterval = 1f;

    private Coroutine damageCoroutine;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (damageCoroutine == null)
            {
                damageCoroutine = StartCoroutine(DealDamageOverTime(other.gameObject));
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (damageCoroutine != null)
            {
                StopCoroutine(damageCoroutine);
                damageCoroutine = null;
            }
        }
    }

    private IEnumerator DealDamageOverTime(GameObject player)
    {
        PlayerHealth health = player.GetComponentInParent<PlayerHealth>();

        while (true)
        {
            if (health != null)
            {
                health.TakeDamage(damageAmount);
            }

            yield return new WaitForSeconds(damageInterval);
        }
    }
}
