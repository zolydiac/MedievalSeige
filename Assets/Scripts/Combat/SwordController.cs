using UnityEngine;

public class SwordController : MonoBehaviour
{
    [SerializeField] private SwordDamage swordDamage;

    void Start()
    {
        // Auto-find the sword damage script
        if (swordDamage == null)
        {
            swordDamage = GetComponentInChildren<SwordDamage>();
        }
    }

    // These are called by Animation Events
    public void EnableDamage()
    {
        if (swordDamage != null)
        {
            swordDamage.EnableDamage();
        }
    }

    public void DisableDamage()
    {
        if (swordDamage != null)
        {
            swordDamage.DisableDamage();
        }
    }
}