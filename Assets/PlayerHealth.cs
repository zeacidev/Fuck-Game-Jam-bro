using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// IBurnable is defined in FireZone.cs
public class PlayerHealth : MonoBehaviour, IBurnable
{
    [Header("UI")]
    public Image healthFill;
    public TextMeshProUGUI healthText;

    [Header("Health")]
    public float currentHealth;
    public float maxHealth = 100f;
    public float drainDuration = 0.5f;

    public bool IsDead { get; private set; }
    public event Action OnDeath;

    private Coroutine smoothDrainCoroutine;

    void Start()
    {
        currentHealth = maxHealth;
        if (healthFill != null) healthFill.fillAmount = 1f;
        UpdateText();
    }

    // Called by your DamageScript (int damage)
    public void TakeDamage(int damage)
    {
        ApplyDamage(damage);
    }

    // Called by FireZone (molotov), which uses float damage
    public void TakeFireDamage(float amount)
    {
        ApplyDamage(amount);
    }

    private void ApplyDamage(float damage)
    {
        if (IsDead) return;

        currentHealth = Mathf.Clamp(currentHealth - damage, 0f, maxHealth);
        UpdateText();

        if (healthFill != null)
        {
            if (smoothDrainCoroutine != null)
            {
                StopCoroutine(smoothDrainCoroutine);
            }
            smoothDrainCoroutine = StartCoroutine(SmoothHealthBar(currentHealth / maxHealth));
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        IsDead = true;
        OnDeath?.Invoke();
        // Add your own death logic here (respawn, game over screen, disable controls, etc.)
        Debug.Log("Player died");
    }

    private void UpdateText()
    {
        if (healthText != null)
        {
            healthText.text = Mathf.CeilToInt(currentHealth) + "/" + Mathf.CeilToInt(maxHealth);
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