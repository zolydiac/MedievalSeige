using UnityEngine;

public class BombHandController : MonoBehaviour
{
    [SerializeField] private GameObject bombVisual;

    private bool isEquipped = false;

    void Start()
    {
        if (bombVisual != null)
            bombVisual.SetActive(false);
    }

    public void EquipBomb(bool equip)
    {
        isEquipped = equip;

        if (bombVisual != null)
            bombVisual.SetActive(equip);
    }

    public bool IsEquipped => isEquipped;
}

