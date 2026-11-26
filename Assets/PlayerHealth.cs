using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    public Image healthFill;
    public TextMeshProUGUI healthText;
    public float currentHealth;
    public float maxHealth = 100f;
    public float drainDuration = 0.5f;

    private Coroutine smoothDrainCoroutine;

    void Start()
    {
        currentHealth = maxHealth;
        if (healthFill != null) healthFill.fillAmount = 1f;
        if (healthText != null) healthText.text = currentHealth + "/" + maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth = Mathf.Clamp(currentHealth - damage, 0f, maxHealth);

        if (healthText != null)
        {
            healthText.text = currentHealth + "/" + maxHealth;
        }

        if (healthFill != null)
        {
            if (smoothDrainCoroutine != null)
            {
                StopCoroutine(smoothDrainCoroutine);
            }
            smoothDrainCoroutine = StartCoroutine(SmoothHealthBar(currentHealth / maxHealth));
        }
    }

    private IEnumerator SmoothHealthBar(float targetFill)
    {
        float startFill = healthFill.fillAmount;
        float elapsedTime = 0f;

        while (elapsedTime < drainDuration)
        {
            elapsedTime += Time.deltaTime;
            float percentComplete = elapsedTime / drainDuration;
            healthFill.fillAmount = Mathf.SmoothStep(startFill, targetFill, percentComplete);
            yield return null;
        }

        healthFill.fillAmount = targetFill;
    }
}
