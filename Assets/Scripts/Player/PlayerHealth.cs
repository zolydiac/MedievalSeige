using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("UI References")]
    // We'll add UI later

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log("Player took " + damage + " damage. Health: " + currentHealth);

        // Update UI (we'll add this later)

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        Debug.Log("Player healed " + amount + ". Health: " + currentHealth);
    }

    void Die()
    {
        if (isDead) return; // Prevent multiple calls

        isDead = true;
        Debug.Log("Player died!");

        // Disable movement (but keep camera!)
        FPSController controller = GetComponent<FPSController>();
        if (controller != null)
        {
            controller.enabled = false;
        }

        // DON'T disable the entire Player object
        // DON'T destroy the Player
        // Just disable movement

        // Show death UI (we'll add this later)
        // For now, just log it
        Debug.Log("GAME OVER - Player is dead");

        // Optional: Restart after delay
        // Invoke("RestartLevel", 3f);
    }

    // Public getters
    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public int GetMaxHealth()
    {
        return maxHealth;
    }

    public bool IsDead()
    {
        return isDead;
    }
}