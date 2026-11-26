using UnityEngine.UI;
using UnityEngine;
using TMPro;
using System;

public class PlayerHealth : MonoBehaviour
{
    public Image healthFill;
    public TextMeshProUGUI healthText;
    public float currentHealth;
    public float maxHealth = 100f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHealth = maxHealth;
        
    }

    void Update()
    {
        UpdateHealthBar();
    }

    // Update is called once per frame
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
    }

    void UpdateHealthBar()
    {
        healthFill.fillAmount = currentHealth / maxHealth;
        healthText.text = ""+ currentHealth + "/"+ maxHealth;
    }
}
