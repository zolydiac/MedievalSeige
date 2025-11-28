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
    [Tooltip("Extra time after animation completes before we tell the RoundManager.")]
    [SerializeField] private float postDeathDelay = 0.1f;

    [Tooltip("Fallback length if we can't find the death clip.")]
    [SerializeField] private float fallbackDeathLength = 1.5f;

    // Internal flags
    private bool animatorHasHitTrigger = false;
    private bool animatorHasDeathBool = false;

    // Cached components
    private SplitScreenFPSController controller;
    private CharacterController characterController;
    private SwordDamage sword;

    // For respawn
    private Vector3 startPosition;
    private Quaternion startRotation;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    void Awake()
    {
        controller = GetComponent<SplitScreenFPSController>();
        characterController = GetComponent<CharacterController>();
        sword = GetComponentInChildren<SwordDamage>();

        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    void Start()
    {
        currentHealth = maxHealth;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Debug.LogError($"[PlayerHealth] No Animator found on {name} or children!");
            return;
        }

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

        // Disable live control & weapon
        if (controller != null) controller.enabled = false;
        if (sword != null) sword.DisableDamage();

        if (animatorHasDeathBool)
            animator.SetBool(deathBoolName, true);

        float duration = GetDeathAnimationLength();
        Debug.Log($"[PlayerHealth] Death animation length = {duration}s");

        StartCoroutine(NotifyDeathAfterDelay(duration + postDeathDelay));
    }

    private float GetDeathAnimationLength()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return fallbackDeathLength;

        foreach (var clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == deathStateOrClipName)
                return clip.length;
        }

        Debug.LogWarning($"[PlayerHealth] Could not find death clip '{deathStateOrClipName}'. Using fallback {fallbackDeathLength}s.");
        return fallbackDeathLength;
    }

    private IEnumerator NotifyDeathAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Let the RoundManager handle scores + respawn
        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.OnPlayerDied(this);
        }
        else
        {
            Debug.LogWarning("[PlayerHealth] No RoundManager in scene, player will never respawn.");
        }
    }

    // -----------------------------------------------------------------------
    // RESPAWN (called by RoundManager)
    // -----------------------------------------------------------------------
    public void RespawnAt(Transform spawnPoint)
    {
        // Turn off CC while we teleport
        if (characterController != null)
            characterController.enabled = false;

        if (spawnPoint != null)
        {
            transform.position = spawnPoint.position;
            transform.rotation = spawnPoint.rotation;
        }
        else
        {
            transform.position = startPosition;
            transform.rotation = startRotation;
        }

        if (characterController != null)
            characterController.enabled = true;

        // Reset state
        currentHealth = maxHealth;
        isDead = false;

        if (animatorHasDeathBool)
            animator.SetBool(deathBoolName, false);

        if (animator != null)
            animator.Play("idle", 0, 0f); // change "idle" if your idle state has a different name

        if (controller != null)
            controller.enabled = true;

        if (sword != null)
            sword.EnableDamage();

        Debug.Log($"[PlayerHealth] {name} respawned.");
    }
}

