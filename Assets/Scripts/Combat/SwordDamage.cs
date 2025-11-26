using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SwordDamage : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damage = 20;

    [Tooltip("Which LAYER this sword is allowed to damage (ex: 'Player2' for Player1 sword).")]
    [SerializeField] private string enemyLayer = "Player2";

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private bool damageActive = false;
    private Collider swordCollider;

    // Tracks which targets we've damaged this swing
    private readonly HashSet<PlayerHealth> damagedThisSwing = new HashSet<PlayerHealth>();

    private int enemyLayerIndex;

    void Awake()
    {
        swordCollider = GetComponent<Collider>();

        if (!swordCollider.isTrigger)
        {
            Debug.LogWarning($"{name}: Sword collider MUST be set to IsTrigger = TRUE.");
        }

        enemyLayerIndex = LayerMask.NameToLayer(enemyLayer);
        if (enemyLayerIndex == -1)
        {
            Debug.LogError($"{name}: Enemy layer '{enemyLayer}' does NOT exist! Fix in Inspector.");
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

        // Layer check
        if (other.gameObject.layer != enemyLayerIndex)
            return;

        // Try to find PlayerHealth on target
        PlayerHealth target = other.GetComponentInParent<PlayerHealth>();
        if (target == null)
            return;

        // Prevent double hits on same target this swing
        if (damagedThisSwing.Contains(target))
            return;

        damagedThisSwing.Add(target);

        // Apply damage
        target.TakeDamage(damage);

        if (showDebugLogs)
            Debug.Log($"{name}: Hit {other.name} for {damage} damage.");
    }
}
