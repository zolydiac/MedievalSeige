using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class WeaponHotbarUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject hotbarPanel;
    [SerializeField] private List<WeaponSlot> weaponSlots = new List<WeaponSlot>();

    [Header("Weapon Icons")]
    [SerializeField] private Sprite swordShieldIcon;   // slot 0
    [SerializeField] private Sprite bowIcon;           // slot 1
    [SerializeField] private Sprite bombIcon;          // slot 2 (chalk / bomb)

    [Header("Colors")]
    [SerializeField] private Color selectedColor = new Color(1f, 0.8f, 0.3f, 1f); // Gold
    [SerializeField] private Color unselectedColor = new Color(0.3f, 0.3f, 0.3f, 0.8f); // Dark gray
    [SerializeField] private Color selectedBorderColor = Color.white;
    [SerializeField] private Color unselectedBorderColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [System.Serializable]
    public class WeaponSlot
    {
        public Image slotBackground;
        public Image weaponIcon;
        public Image border;
        public GameObject selectionIndicator; // Optional glow/highlight
    }

    private int currentSelectedIndex = 0;

    void Start()
    {
        // Set up default icons if provided
        ApplyDefaultIcons();

        // Initialize with sword selected
        SelectWeapon(0);
    }

    /// <summary>
    /// Call this from the controller when weapon changes
    /// 0 = Sword+Shield, 1 = Bow, 2 = Bomb/Chalk
    /// </summary>
    public void SelectWeapon(int weaponIndex)
    {
        currentSelectedIndex = Mathf.Clamp(weaponIndex, 0, weaponSlots.Count - 1);
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        for (int i = 0; i < weaponSlots.Count; i++)
        {
            bool isSelected = (i == currentSelectedIndex);
            WeaponSlot slot = weaponSlots[i];

            // Background color
            if (slot.slotBackground != null)
                slot.slotBackground.color = isSelected ? selectedColor : unselectedColor;

            // Border
            if (slot.border != null)
                slot.border.color = isSelected ? selectedBorderColor : unselectedBorderColor;

            // Glow / selection indicator
            if (slot.selectionIndicator != null)
                slot.selectionIndicator.SetActive(isSelected);

            // Icon brightness
            if (slot.weaponIcon != null)
            {
                Color iconColor = slot.weaponIcon.color;
                iconColor.a = isSelected ? 1f : 0.6f;
                slot.weaponIcon.color = iconColor;
            }
        }
    }

    /// <summary>
    /// Optional: call from code to set icons.
    /// </summary>
    public void SetWeaponIcons(Sprite swordIcon, Sprite bow, Sprite bomb = null)
    {
        swordShieldIcon = swordIcon;
        bowIcon = bow;
        bombIcon = bomb;

        ApplyDefaultIcons();
    }

    private void ApplyDefaultIcons()
    {
        if (weaponSlots.Count >= 1 && weaponSlots[0].weaponIcon != null && swordShieldIcon != null)
            weaponSlots[0].weaponIcon.sprite = swordShieldIcon;

        if (weaponSlots.Count >= 2 && weaponSlots[1].weaponIcon != null && bowIcon != null)
            weaponSlots[1].weaponIcon.sprite = bowIcon;

        if (weaponSlots.Count >= 3 && weaponSlots[2].weaponIcon != null && bombIcon != null)
            weaponSlots[2].weaponIcon.sprite = bombIcon;
    }

    /// <summary>
    /// Show/hide the hotbar
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (hotbarPanel != null)
            hotbarPanel.SetActive(visible);
    }
}
