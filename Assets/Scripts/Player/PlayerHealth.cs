using System.Collections;
using UnityEngine;
using System;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;
    private bool isDead = false;

    [Header("Animator Settings")]
    [SerializeField] private Animator animator;

    [Tooltip("Trigger played when the player takes hit damage.")]
    [SerializeField] private string hitTriggerName = "Hit";

    [Tooltip("Bool that activates the Death animation.")]
    [SerializeField] private string deathBoolName = "IsDead";

    [Tooltip("Exact name of the death animation clip (case sensitive).")]
    [SerializeField] private string deathStateOrClipName = "HumanM@Death01";

    [Header("Death Settings")]
    [Tooltip("Disables CharacterController after death animation finishes.")]
    [SerializeField] private bool disableCharacterController = true;

    [Tooltip("Extra time after animation completes before disabling physics.")]
    [SerializeField] private float postDeathDelay = 0.1f;

    // Event fired BEFORE health is reduced (for shield effects)
    public event Action<int> OnDamageTakenRaw;

    // References
    private ShieldBlock shieldBlock;
    private SplitScreenFPSController fpsController;

    // Animator parameter validation
    private bool animatorHasHitTrigger = false;
    private bool animatorHasDeathBool = false;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    void Start()
    {
        currentHealth = maxHealth;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Debug.LogError($"[PlayerHealth] No Animator found on {name}!");
            return;
        }

        // Validate Animator parameters
        foreach (var p in animator.parameters)
        {
            if (p.name == hitTriggerName && p.type == AnimatorControllerParameterType.Trigger)
                animatorHasHitTrigger = true;

            if (p.name == deathBoolName && p.type == AnimatorControllerParameterType.Bool)
                animatorHasDeathBool = true;
        }

        if (!animatorHasHitTrigger)
            Debug.LogWarning($"[PlayerHealth] Hit trigger '{hitTriggerName}' not found!");

        if (!animatorHasDeathBool)
            Debug.LogWarning($"[PlayerHealth] Death bool '{deathBoolName}' not found!");

        // Reference to controller & shield
        fpsController = GetComponent<SplitScreenFPSController>();
        shieldBlock = GetComponentInChildren<ShieldBlock>();
    }

    // -----------------------------------------------------------------------
    // DAMAGE
    // -----------------------------------------------------------------------
    public void TakeDamage(int rawDamage)
    {
        if (isDead)
            return;

        // Notify listeners (ShieldBlock will use raw damage for special effects only)
        OnDamageTakenRaw?.Invoke(rawDamage);

        int finalDamage = ApplyShieldReduction(rawDamage);

        currentHealth -= finalDamage;
        currentHealth = Mathf.Max(0, currentHealth);

        Debug.Log($"[PlayerHealth] {name} took {finalDamage} damage. HP = {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        // Play damage animation
        if (animatorHasHitTrigger)
            animator.SetTrigger(hitTriggerName);
    }

    // Apply shield reduction ONLY when blocking
    private int ApplyShieldReduction(int damage)
    {
        if (shieldBlock == null || !shieldBlock.IsBlocking())
            return damage;

        float reductionPercent = shieldBlock.GetDamageReductionPercent();
        int finalDamage = Mathf.RoundToInt(damage * (1f - reductionPercent / 100f));

        Debug.Log($"[PlayerHealth] Shield reduced damage from {damage} to {finalDamage}");

        return finalDamage;
    }

    // -----------------------------------------------------------------------
    // HEALING
    // -----------------------------------------------------------------------
    public void Heal(int amount)
    {
        if (isDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        Debug.Log($"[PlayerHealth] Player healed {amount}. Health = {currentHealth}/{maxHealth}");
    }

    // -----------------------------------------------------------------------
    // DEATH
    // -----------------------------------------------------------------------
    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"[PlayerHealth] {name} died.");

        // Disable player movement
        if (fpsController != null)
            fpsController.enabled = false;

        // Trigger animation
        if (animatorHasDeathBool)
            animator.SetBool(deathBoolName, true);

        float duration = GetDeathAnimationLength();
        Debug.Log($"[PlayerHealth] Death animation length = {duration}s");

        StartCoroutine(DisableAfterDelay(duration + postDeathDelay));
    }

    private float GetDeathAnimationLength()
    {
        if (animator == null) return 1.5f;

        RuntimeAnimatorController rac = animator.runtimeAnimatorController;

        foreach (var clip in rac.animationClips)
        {
            if (clip.name == deathStateOrClipName)
                return clip.length;
        }

        Debug.LogWarning($"[PlayerHealth] Could not find death clip '{deathStateOrClipName}'. Using fallback 1.5s.");
        return 1.5f;
    }

    private IEnumerator DisableAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (disableCharacterController)
        {
            CharacterController cc = GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
                Debug.Log("[PlayerHealth] CharacterController disabled after death animation.");
            }
        }
    }

    // -----------------------------------------------------------------------
    // PUBLIC GETTERS
    // -----------------------------------------------------------------------
    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
}
