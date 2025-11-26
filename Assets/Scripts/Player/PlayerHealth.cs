using System.Collections;
using UnityEngine;

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
    [Tooltip("Disables CharacterController after the death animation finishes.")]
    [SerializeField] private bool disableCharacterController = true;

    [Tooltip("Extra time after animation completes before disabling physics.")]
    [SerializeField] private float postDeathDelay = 0.1f;

    // Internal flags
    private bool animatorHasHitTrigger = false;
    private bool animatorHasDeathBool = false;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    void Start()
    {
        currentHealth = maxHealth;

        // Auto-find animator if not assigned
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Debug.LogError($"[PlayerHealth] No Animator found on {name} or children!");
            return;
        }

        // Validate parameters exist
        foreach (var p in animator.parameters)
        {
            if (p.name == hitTriggerName && p.type == AnimatorControllerParameterType.Trigger)
                animatorHasHitTrigger = true;

            if (p.name == deathBoolName && p.type == AnimatorControllerParameterType.Bool)
                animatorHasDeathBool = true;
        }

        if (!animatorHasHitTrigger)
            Debug.LogWarning($"[PlayerHealth] Hit trigger '{hitTriggerName}' not found. Hit animation will be skipped.");

        if (!animatorHasDeathBool)
            Debug.LogWarning($"[PlayerHealth] Death bool '{deathBoolName}' not found. Death animation will not activate!");
    }

    // -----------------------------------------------------------------------
    // DAMAGE
    // -----------------------------------------------------------------------
    public void TakeDamage(int amount)
    {
        if (isDead)
            return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(0, currentHealth);
        Debug.Log($"[PlayerHealth] {name} took {amount} damage. HP = {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        // Play hit animation if exists
        if (animatorHasHitTrigger)
            animator.SetTrigger(hitTriggerName);
    }

    // -----------------------------------------------------------------------
    // DEATH
    // -----------------------------------------------------------------------
    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"[PlayerHealth] {name} DIED.");

        // Disable player controls
        var controller = GetComponent<SplitScreenFPSController>();
        if (controller != null) controller.enabled = false;

        // Disable sword hits
        var sword = GetComponentInChildren<SwordDamage>();
        if (sword != null) sword.DisableDamage();

        // Trigger death animation bool
        if (animatorHasDeathBool)
            animator.SetBool(deathBoolName, true);

        // Get actual clip length
        float duration = GetDeathAnimationLength();

        Debug.Log($"[PlayerHealth] Death animation length = {duration}s");

        // Schedule disabling physics after animation
        StartCoroutine(DisableAfterDelay(duration + postDeathDelay));
    }

    // -----------------------------------------------------------------------
    // DETECT ANIMATION LENGTH
    // -----------------------------------------------------------------------
    private float GetDeathAnimationLength()
    {
        if (animator == null)
            return 1.5f;

        RuntimeAnimatorController rac = animator.runtimeAnimatorController;

        foreach (var clip in rac.animationClips)
        {
            if (clip.name == deathStateOrClipName)
            {
                return clip.length;
            }
        }

        Debug.LogWarning($"[PlayerHealth] Could not find death clip '{deathStateOrClipName}'. Using fallback 1.5s.");
        return 1.5f;
    }

    // -----------------------------------------------------------------------
    // DISABLE CHARACTERCONTROLLER AFTER DEATH
    // -----------------------------------------------------------------------
    private IEnumerator DisableAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (disableCharacterController)
        {
            var cc = GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
                Debug.Log("[PlayerHealth] CharacterController disabled after death animation.");
            }
        }
    }
}
