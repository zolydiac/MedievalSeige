using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class WeaponHotbarUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject hotbarPanel;
    [SerializeField] private List<WeaponSlot> weaponSlots = new List<WeaponSlot>();
    
    [Header("Weapon Icons")]
    [SerializeField] private Sprite swordShieldIcon;
    [SerializeField] private Sprite bowIcon;
    
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
        // Initialize with sword selected
        SelectWeapon(0);
    }

    /// <summary>
    /// Call this from the controller when weapon changes
    /// 0 = Sword+Shield, 1 = Bow
    /// </summary>
    public void SelectWeapon(int weaponIndex)
    {
        currentSelectedIndex = weaponIndex;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        for (int i = 0; i < weaponSlots.Count; i++)
        {
            bool isSelected = (i == currentSelectedIndex);
            WeaponSlot slot = weaponSlots[i];

            // Update background color
            if (slot.slotBackground != null)
            {
                slot.slotBackground.color = isSelected ? selectedColor : unselectedColor;
            }

            // Update border color/thickness
            if (slot.border != null)
            {
                slot.border.color = isSelected ? selectedBorderColor : unselectedBorderColor;
            }

            // Update selection indicator (glow effect)
            if (slot.selectionIndicator != null)
            {
                slot.selectionIndicator.SetActive(isSelected);
            }

            // Make selected icon brighter
            if (slot.weaponIcon != null)
            {
                Color iconColor = slot.weaponIcon.color;
                iconColor.a = isSelected ? 1f : 0.6f;
                slot.weaponIcon.color = iconColor;
            }
        }
    }

    /// <summary>
    /// Call this to set up weapon icons (if not set in inspector)
    /// </summary>
    public void SetWeaponIcons(Sprite swordIcon, Sprite bow)
    {
        if (weaponSlots.Count >= 1 && weaponSlots[0].weaponIcon != null)
            weaponSlots[0].weaponIcon.sprite = swordIcon;
        
        if (weaponSlots.Count >= 2 && weaponSlots[1].weaponIcon != null)
            weaponSlots[1].weaponIcon.sprite = bow;
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
