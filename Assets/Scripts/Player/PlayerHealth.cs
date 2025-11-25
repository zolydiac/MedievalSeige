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
        Debug.Log(gameObject.name + " took " + damage + " damage. Health: " + currentHealth);

        // Update UI (we'll add this later)

        if (currentHealth <= 0)
        {
            Debug.Log(gameObject.name + " health reached 0, calling Die()");
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
        if (isDead) return;
        isDead = true;
        Debug.Log(gameObject.name + " DIE() CALLED - Setting IsDead animation parameter");

        // Trigger death animation
        Animator animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
            Debug.Log("Looking for animator in children");
        }

        if (animator != null)
        {
            Debug.Log("Found animator, setting IsDead to true");
            animator.SetBool("IsDead", true);
        }
        else
        {
            Debug.LogError("ERROR: No Animator found!");
        }

        // Disable movement controller (but keep camera!)
        SplitScreenFPSController controller = GetComponent<SplitScreenFPSController>();
        if (controller != null)
        {
            controller.enabled = false;
        }

        Debug.Log("GAME OVER - Player is dead");
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