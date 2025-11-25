using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private Animator animator;
    [SerializeField] private string hitTriggerName = "Hit";
    [SerializeField] private string deathBoolName = "IsDead"; // Changed from trigger to bool

    [Header("Death Settings")]
    [SerializeField] private bool disableCharacterController = true;
    [SerializeField] private float deathAnimationDelay = 0.1f;

    private int currentHealth;
    private bool isDead = false;

    // 👇 expose for UI
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    void Start()
    {
        currentHealth = maxHealth;

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
            
            if (animator == null)
            {
                Debug.LogError($"[PlayerHealth] No Animator found on {name} or its children!");
            }
            else
            {
                Debug.Log($"[PlayerHealth] Animator found on {animator.gameObject.name}");
            }
        }

        // Verify the animator has the IsDead parameter
        if (animator != null)
        {
            bool hasParameter = false;
            foreach (var param in animator.parameters)
            {
                Debug.Log($"[PlayerHealth] Animator parameter: {param.name} (Type: {param.type})");
                if (param.name == deathBoolName)
                {
                    hasParameter = true;
                    Debug.Log($"[PlayerHealth] ✓ Found '{deathBoolName}' parameter!");
                }
            }
            
            if (!hasParameter)
            {
                Debug.LogError($"[PlayerHealth] ✗ Animator does NOT have parameter '{deathBoolName}'!");
            }
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead)
        {
            Debug.Log($"[PlayerHealth] {name} is already dead, ignoring damage");
            return;
        }

        currentHealth -= amount;
        Debug.Log($"[PlayerHealth] {name} took {amount} damage. HP = {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
        else
        {
            // Play hit animation if not dead
            if (animator != null && !string.IsNullOrEmpty(hitTriggerName))
            {
                animator.SetTrigger(hitTriggerName);
                Debug.Log($"[PlayerHealth] Playing hit animation: {hitTriggerName}");
            }
        }
    }

    private void Die()
    {
        if (isDead)
        {
            Debug.LogWarning($"[PlayerHealth] Die() called but {name} is already dead!");
            return;
        }

        isDead = true;
        Debug.Log($"[PlayerHealth] ===== {name} IS DYING =====");

        // Disable movement controller
        var controller = GetComponent<SplitScreenFPSController>();
        if (controller != null)
        {
            controller.enabled = false;
            Debug.Log($"[PlayerHealth] Disabled SplitScreenFPSController");
        }

        // Disable character controller so they don't fall through floor
        if (disableCharacterController)
        {
            var charController = GetComponent<CharacterController>();
            if (charController != null)
            {
                charController.enabled = false;
                Debug.Log($"[PlayerHealth] Disabled CharacterController");
            }
        }

        // Disable sword damage
        var swordDamage = GetComponentInChildren<SwordDamage>();
        if (swordDamage != null)
        {
            swordDamage.DisableDamage();
            Debug.Log($"[PlayerHealth] Disabled sword damage");
        }

        // Trigger death animation with slight delay to ensure all systems are ready
        if (animator != null && !string.IsNullOrEmpty(deathBoolName))
        {
            Invoke(nameof(TriggerDeathAnimation), deathAnimationDelay);
        }
        else
        {
            if (animator == null)
                Debug.LogError($"[PlayerHealth] Cannot play death animation - No Animator!");
            if (string.IsNullOrEmpty(deathBoolName))
                Debug.LogError($"[PlayerHealth] Cannot play death animation - deathBoolName is empty!");
        }
    }

    private void TriggerDeathAnimation()
    {
        Debug.Log($"[PlayerHealth] ========== TRIGGERING DEATH ANIMATION ==========");
        
        // Check current animator state before setting
        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
        Debug.Log($"[PlayerHealth] BEFORE - Current State: {currentState.fullPathHash} Speed: {currentState.speed}");
        Debug.Log($"[PlayerHealth] BEFORE - Current Clip: {animator.GetCurrentAnimatorClipInfo(0)[0].clip.name}");
        
        // Get the current value of IsDead
        bool currentIsDead = animator.GetBool(deathBoolName);
        Debug.Log($"[PlayerHealth] BEFORE - {deathBoolName} = {currentIsDead}");
        
        // Set the IsDead bool to true
        animator.SetBool(deathBoolName, true);
        Debug.Log($"[PlayerHealth] SETTING {deathBoolName} = TRUE");
        
        // Verify it was set
        bool afterSet = animator.GetBool(deathBoolName);
        Debug.Log($"[PlayerHealth] AFTER - {deathBoolName} = {afterSet}");
        
        // Force animator update
        animator.Update(0f);
        
        // Check state again after a frame
        Invoke(nameof(CheckDeathAnimationState), 0.1f);
        
        Debug.Log($"[PlayerHealth] ================================================");
    }
    
    private void CheckDeathAnimationState()
    {
        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
        Debug.Log($"[PlayerHealth] 0.1s AFTER - Current State Hash: {currentState.fullPathHash}");
        Debug.Log($"[PlayerHealth] 0.1s AFTER - Is transitioning: {animator.IsInTransition(0)}");
        
        if (animator.GetCurrentAnimatorClipInfo(0).Length > 0)
        {
            Debug.Log($"[PlayerHealth] 0.1s AFTER - Current Clip: {animator.GetCurrentAnimatorClipInfo(0)[0].clip.name}");
        }
        
        // If not playing death, force it
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName("death") && 
            !animator.GetCurrentAnimatorStateInfo(0).IsName("Death"))
        {
            Debug.LogWarning("[PlayerHealth] Death animation NOT playing! Trying to force play...");
            animator.Play("death", 0, 0f); // Force play death animation
        }
    }

    public void Heal(int amount)
    {
        if (isDead) return;
        
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        
        Debug.Log($"[PlayerHealth] {name} healed {amount}. HP = {currentHealth}/{maxHealth}");
    }

    // Debug helper - call this from Update to test death manually
    void Update()
    {
        // Press K to test death animation (REMOVE THIS AFTER TESTING)
        if (Input.GetKeyDown(KeyCode.K))
        {
            Debug.Log("[PlayerHealth] Manual death test triggered with K key");
            Die();
        }
    }
}