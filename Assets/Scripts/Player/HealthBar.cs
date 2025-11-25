using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Image fillImage;

    void Start()
    {
        if (playerHealth == null)
            playerHealth = GetComponentInParent<PlayerHealth>();

        if (fillImage == null)
            Debug.LogWarning($"{name}: Fill Image not assigned on HealthBar.", this);
    }

    void Update()
    {
        if (playerHealth == null || fillImage == null) return;

        float normalized = (float)playerHealth.CurrentHealth / playerHealth.MaxHealth;
        fillImage.fillAmount = normalized;
    }
}

