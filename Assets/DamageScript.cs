using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class DamageScript : MonoBehaviour
{
    public PlayerHealth pHealth;
    bool isDamaging;
    private IEnumerator WaitForABit(float delay)
    {
        yield return new WaitForSeconds(delay);
        pHealth.TakeDamage(5);
        isDamaging = true;
    }
    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (!isDamaging)
            {
                StartCoroutine(WaitForABit(1f));
            }
            else
            {
                StopCoroutine(WaitForABit(0f));
                isDamaging = false;
            }
        }
    }
}
