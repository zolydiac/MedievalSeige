using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SwordDamage : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damage = 20;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private bool damageActive = false;
    private Collider swordCollider;

    // Track who owns this sword (the player/enemy holding it)
    private PlayerHealth ownerHealth;

    // Tracks which targets we've damaged this swing
    private readonly HashSet<PlayerHealth> damagedThisSwing = new HashSet<PlayerHealth>();

    void Awake()
    {
        swordCollider = GetComponent<Collider>();

        if (!swordCollider.isTrigger)
        {
            Debug.LogWarning($"{name}: Sword collider MUST be set to IsTrigger = TRUE.");
        }

        // Find the owning PlayerHealth in the parent hierarchy
        ownerHealth = GetComponentInParent<PlayerHealth>();
        if (ownerHealth == null)
        {
            Debug.LogWarning($"{name}: Could not find PlayerHealth on parent. " +
                             "Sword will still work, but self-hit prevention is disabled.");
        }
    }

    // Called by animation event at start of hit window
    public void EnableDamage()
    {
        damageActive = true;
        damagedThisSwing.Clear();

        if (showDebugLogs)
            Debug.Log($"{name}: Damage ENABLED — starting new swing window.");
    }

    // Called by animation event at end of hit window
    public void DisableDamage()
    {
        damageActive = false;

        if (showDebugLogs)
            Debug.Log($"{name}: Damage DISABLED — swing ended.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!damageActive)
            return;

        // Find a PlayerHealth on the thing we hit (or its parents)
        PlayerHealth target = other.GetComponentInParent<PlayerHealth>();
        if (target == null)
            return;

        // Don't hit the sword's owner
        if (ownerHealth != null && target == ownerHealth)
            return;

        // Prevent multiple hits on the same target in one swing
        if (damagedThisSwing.Contains(target))
            return;

        damagedThisSwing.Add(target);

        // Apply damage
        target.TakeDamage(damage);

        if (showDebugLogs)
            Debug.Log($"{name}: Hit {target.name} for {damage} damage.");
    }
}
