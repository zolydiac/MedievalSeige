using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SwordDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private int damage = 20;

    [Tooltip("Which LAYER this sword is allowed to damage (e.g. 'Player2' on Player1's sword).")]
    [SerializeField] private string enemyLayer = "Player2";

    private bool canDamage = false;
    private Collider swordCollider;

    void Awake()
    {
        swordCollider = GetComponent<Collider>();

        if (!swordCollider.isTrigger)
        {
            Debug.LogWarning($"{name}: Sword collider should be set to IsTrigger = true.", this);
        }
    }

    // Called by SwordController via animation events
    public void EnableDamage()
    {
        canDamage = true;
        // Debug.Log($"{name}: Damage ENABLED");
    }

    public void DisableDamage()
    {
        canDamage = false;
        // Debug.Log($"{name}: Damage DISABLED");
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"{name}: Trigger hit {other.name} on layer {LayerMask.LayerToName(other.gameObject.layer)}");

        if (!canDamage)
        {
            Debug.Log($"{name}: Damage is currently disabled (animation window not in hit frames).");
            return;
        }

        // Check layer filter
        if (!string.IsNullOrEmpty(enemyLayer))
        {
            int expectedLayer = LayerMask.NameToLayer(enemyLayer);
            if (other.gameObject.layer != expectedLayer)
            {
                Debug.Log($"{name}: Wrong layer. Expected {enemyLayer}, got {LayerMask.LayerToName(other.gameObject.layer)}.");
                return;
            }
        }

        // Try to find a PlayerHealth on this object or its parents
        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health != null)
        {
            Debug.Log($"{name}: Dealing {damage} damage to {other.name}");
            health.TakeDamage(damage);
        }
        else
        {
            Debug.LogWarning($"{name}: Hit {other.name} but no PlayerHealth found in parents.");
        }
    }
    void Update()
{
    // TEMPORARY TEST - Remove after fixing animation events
    if (Input.GetKeyDown(KeyCode.T))
    {
        EnableDamage();
        Debug.Log("Manually enabled damage for testing");
    }
    
    if (Input.GetKeyDown(KeyCode.Y))
    {
        DisableDamage();
        Debug.Log("Manually disabled damage");
    }
}
}
