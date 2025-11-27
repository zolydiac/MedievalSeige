using UnityEngine;

public class ShieldController : MonoBehaviour
{
    [SerializeField] private Collider shieldCollider;

    private void Reset()
    {
        if (shieldCollider == null)
            shieldCollider = GetComponent<Collider>();
    }

    public void SetBlocking(bool active)
    {
        if (shieldCollider != null)
            shieldCollider.enabled = active;
    }
}

