using UnityEngine;

public class BowCrosshairUI : MonoBehaviour
{
    [SerializeField] private BowController bow;       // assign in inspector
    [SerializeField] private GameObject crosshairRoot; // the Crosshair GameObject

    [Header("When to show")]
    [SerializeField] private bool onlyWhileDrawing = false;

    void Awake()
    {
        if (crosshairRoot == null)
            crosshairRoot = gameObject; // fallback: this object IS the crosshair
    }

    void Update()
    {
        if (bow == null)
        {
            crosshairRoot.SetActive(false);
            return;
        }

        bool visible;

        if (onlyWhileDrawing)
        {
            // Show only while bow is equipped AND player is holding draw
            visible = bow.IsEquipped() && bow.IsDrawing();
        }
        else
        {
            // Show whenever bow is equipped
            visible = bow.IsEquipped();
        }

        crosshairRoot.SetActive(visible);
    }
}

