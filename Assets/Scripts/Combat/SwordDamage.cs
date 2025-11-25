using UnityEngine;

public class SwordDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private int damage = 20;
    [SerializeField] private string enemyLayer = "Player2"; // Which layer to damage

    private bool canDamage = false; // Only damage during attack animation

    public void EnableDamage()
    {
        canDamage = true;
    }

    public void DisableDamage()
    {
        canDamage = false;
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Sword hit something: " + other.gameObject.name + " on layer: " + LayerMask.LayerToName(other.gameObject.layer));

        if (!canDamage)
        {
            Debug.Log("But damage is disabled");
            return;
        }

        // Check if we hit the enemy layer
        if (other.gameObject.layer == LayerMask.NameToLayer(enemyLayer))
        {
            Debug.Log("Hit correct enemy layer!");

            // Try to find PlayerHealth on the hit object or its parent
            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();

            if (health != null)
            {
                health.TakeDamage(damage);
                Debug.Log("Successfully dealt damage!");
            }
            else
            {
                Debug.Log("ERROR: Could not find PlayerHealth component!");
            }
        }
        else
        {
            Debug.Log("Wrong layer. Expected: " + enemyLayer);
        }
    }
}
