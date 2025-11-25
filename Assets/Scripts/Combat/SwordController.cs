using UnityEngine;

public class SwordController : MonoBehaviour
{
    [SerializeField] private SwordDamage swordDamage;

    void Start()
    {
        if (swordDamage == null)
        {
            swordDamage = GetComponentInChildren<SwordDamage>();
        }
    }

    // These are called by Animation Events on the attack animation

    public void EnableDamage()
    {
        swordDamage?.EnableDamage();
    }

    public void DisableDamage()
    {
        swordDamage?.DisableDamage();
    }
}
