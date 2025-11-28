using UnityEngine;
using System;

public class ShieldBlock : MonoBehaviour
{
    [Header("Shield Settings")]
    [SerializeField] private float damageReductionPercent = 50f;   // % reduced while blocking
    [SerializeField] private float blockMovementSpeedMultiplier = 0.5f;

    [Header("References")]
    [SerializeField] private Animator animator;        // character animator
    [SerializeField] private GameObject shieldVisual;  // shield mesh

    private bool isBlocking = false;
    private bool isEquipped = true;

    private readonly int blockHash = Animator.StringToHash("IsBlocking");

    // Optional event for VFX
    public event Action<int> OnShieldBlocked;

    void Start()
    {
        if (animator == null)
            animator = GetComponentInParent<Animator>();

        // ✨ KEEP SHIELD VISIBLE WHEN EQUIPPED, EVEN IF NOT BLOCKING
        if (shieldVisual != null)
            shieldVisual.SetActive(isEquipped);

        // Make sure we don't start in block state
        if (animator != null)
            animator.SetBool(blockHash, false);
    }

    // Called by controller when Block button is pressed/released
    public void SetBlocking(bool blocking)
    {
        if (!isEquipped) blocking = false;

        if (isBlocking == blocking)
            return;

        isBlocking = blocking;

        if (animator != null)
            animator.SetBool(blockHash, isBlocking);

        // We now keep the shield visible all the time while equipped
        if (shieldVisual != null)
            shieldVisual.SetActive(isEquipped);
    }

    // Called by controller when switching weapons
    public void SetEquipped(bool equipped)
    {
        isEquipped = equipped;

        if (!isEquipped)
        {
            isBlocking = false;
            if (animator != null)
                animator.SetBool(blockHash, false);
        }

        if (shieldVisual != null)
            shieldVisual.SetActive(isEquipped);
    }

    // Used by PlayerHealth
    public bool IsBlocking() => isEquipped && isBlocking;
    public float GetMovementMultiplier() =>
        (isEquipped && isBlocking) ? blockMovementSpeedMultiplier : 1f;
    public float GetDamageReductionPercent() => damageReductionPercent;

    // Called from PlayerHealth via event if you wire it up
    public void HandleDamageWithShield(int incomingDamage)
    {
        if (!IsBlocking()) return;

        float reduction = incomingDamage * (damageReductionPercent / 100f);
        int damageBlocked = Mathf.RoundToInt(reduction);

        Debug.Log($"Shield blocked {damageBlocked} damage! (Incoming: {incomingDamage})");
        OnShieldBlocked?.Invoke(damageBlocked);

        // TODO: VFX / SFX here
    }
}
