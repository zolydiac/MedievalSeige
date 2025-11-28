using UnityEngine;

public class BowController : MonoBehaviour
{
    [Header("Bow Settings")]
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private Transform arrowSpawnPoint;
    [SerializeField] private float minArrowSpeed = 10f;
    [SerializeField] private float maxArrowSpeed = 40f;
    [SerializeField] private float maxDrawTime = 2f; // time to fully charge
    [SerializeField] private int arrowDamage = 25;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject bowVisual;
    [SerializeField] private LineRenderer trajectoryLine; // optional

    [Header("Ammo")]
    [SerializeField] private int maxArrows = 20;
    private int currentArrows;

    // State
    private bool isEquipped = false;
    private bool isDrawing = false;
    private float drawStartTime = 0f;
    private float currentDrawTime = 0f;

    // Animator params (rename to match your Animator if needed)
    private readonly int aimHash = Animator.StringToHash("IsAiming");
    private readonly int drawHash = Animator.StringToHash("DrawAmount");
    private readonly int shootHash = Animator.StringToHash("Shoot");

    // You said you went with "equipbow" and "stowbow" – use those names:
    private readonly int equipHash = Animator.StringToHash("equipbow");
    private readonly int stowHash = Animator.StringToHash("stowbow");


    void Start()
    {
        currentArrows = maxArrows;

        if (animator == null)
            animator = GetComponentInParent<Animator>();

        // We DON'T auto-create arrowSpawnPoint here because we're in split-screen.
        // You will assign a child of the player camera in the inspector.

        if (trajectoryLine != null)
            trajectoryLine.enabled = false;

        if (bowVisual != null)
            bowVisual.SetActive(isEquipped);
    }

    void Update()
    {
        UpdateDrawAmount();
        UpdateTrajectoryLine();
    }

    // Called by SplitScreenFPSController when switching weapons
    public void EquipBow(bool equip)
    {
        if (isEquipped == equip) return;
        isEquipped = equip;

        Debug.Log($"[BowController {name}] EquipBow({equip})");

        if (!isEquipped)
        {
            isDrawing = false;
            currentDrawTime = 0f;

            if (animator != null)
            {
                animator.SetBool(aimHash, false);
                animator.SetFloat(drawHash, 0f);
            }
        }

        if (bowVisual != null)
            bowVisual.SetActive(isEquipped);

        if (animator != null)
        {
            if (isEquipped)
                animator.SetTrigger(equipHash); // optional
            else
                animator.SetTrigger(stowHash);  // optional
        }
    }


    public bool IsEquipped() => isEquipped;
    public bool IsDrawing() => isDrawing;

    // Called by controller when Attack is PRESSED in bow mode
    public void StartDrawing()
    {
        if (!isEquipped || isDrawing || currentArrows <= 0)
        {
            Debug.Log($"[BowController {name}] StartDrawing blocked. isEquipped={isEquipped}, isDrawing={isDrawing}, arrows={currentArrows}");
            return;
        }

        if (arrowSpawnPoint == null)
        {
            Debug.LogWarning("[BowController] Arrow spawn point not assigned!");
            return;
        }

        Debug.Log($"[BowController {name}] StartDrawing OK");
        isDrawing = true;
        drawStartTime = Time.time;

        if (animator != null)
            animator.SetBool(aimHash, true);
    }

    public void ReleaseArrow()
    {
        if (!isDrawing || currentArrows <= 0)
        {
            Debug.Log($"[BowController {name}] ReleaseArrow blocked. isDrawing={isDrawing}, arrows={currentArrows}");
            return;
        }

        float drawPercent = Mathf.Clamp01(currentDrawTime / maxDrawTime);
        float arrowSpeed = Mathf.Lerp(minArrowSpeed, maxArrowSpeed, drawPercent);

        Debug.Log($"[BowController {name}] ReleaseArrow, speed={arrowSpeed}");

        SpawnArrow(arrowSpeed);
        currentArrows--;

        if (animator != null)
        {
            Debug.Log($"[BowController {name}] Setting Shoot trigger");
            animator.SetTrigger(shootHash);
            animator.SetBool(aimHash, false);
            animator.SetFloat(drawHash, 0f);
        }

        isDrawing = false;
        currentDrawTime = 0f;
    }



    void UpdateDrawAmount()
    {
        if (!isDrawing) return;

        currentDrawTime = Time.time - drawStartTime;
        float drawPercent = Mathf.Clamp01(currentDrawTime / maxDrawTime);

        if (animator != null)
            animator.SetFloat(drawHash, drawPercent);
    }

    void SpawnArrow(float speed)
    {
        if (arrowPrefab == null)
        {
            Debug.LogError("[BowController] Arrow prefab not assigned!");
            return;
        }

        if (arrowSpawnPoint == null)
        {
            Debug.LogError("[BowController] Arrow spawn point not assigned!");
            return;
        }

        GameObject arrow = Instantiate(arrowPrefab, arrowSpawnPoint.position, arrowSpawnPoint.rotation);

        Arrow arrowScript = arrow.GetComponent<Arrow>();
        if (arrowScript != null)
        {
            arrowScript.Launch(arrowSpawnPoint.forward, speed, arrowDamage);
        }
        else
        {
            Rigidbody rb = arrow.GetComponent<Rigidbody>();
            if (rb != null)
                rb.linearVelocity = arrowSpawnPoint.forward * speed;
        }
    }

    void UpdateTrajectoryLine()
    {
        if (trajectoryLine == null || !isDrawing || arrowSpawnPoint == null)
            return;

        trajectoryLine.enabled = true;

        float drawPercent = Mathf.Clamp01(currentDrawTime / maxDrawTime);
        float arrowSpeed = Mathf.Lerp(minArrowSpeed, maxArrowSpeed, drawPercent);

        Vector3 velocity = arrowSpawnPoint.forward * arrowSpeed;
        Vector3 position = arrowSpawnPoint.position;

        int pointCount = 30;
        trajectoryLine.positionCount = pointCount;

        for (int i = 0; i < pointCount; i++)
        {
            float t = i * 0.1f;
            Vector3 point = position + velocity * t + 0.5f * Physics.gravity * t * t;
            trajectoryLine.SetPosition(i, point);
        }
    }

    // Public helpers
    public void AddArrows(int amount)
    {
        currentArrows = Mathf.Min(maxArrows, currentArrows + amount);
    }

    public void QuickShot()
    {
        if (!isEquipped)
        {
            Debug.Log($"[BowController {name}] QuickShot blocked: not equipped");
            return;
        }

        if (currentArrows <= 0)
        {
            Debug.Log($"[BowController {name}] QuickShot blocked: no arrows left");
            return;
        }

        if (arrowSpawnPoint == null)
        {
            Debug.LogWarning("[BowController] QuickShot: Arrow spawn point not assigned!");
            return;
        }

        // For simple press-to-shoot, just use max speed (or tweak as you like)
        float arrowSpeed = maxArrowSpeed;

        Debug.Log($"[BowController {name}] QuickShot fired, speed={arrowSpeed}");

        SpawnArrow(arrowSpeed);
        currentArrows--;

        // Play shoot animation
        if (animator != null)
        {
            animator.SetTrigger(shootHash);
            animator.SetBool(aimHash, false);
            animator.SetFloat(drawHash, 0f);
        }

        // Reset any draw state just in case
        isDrawing = false;
        currentDrawTime = 0f;
    }

    public int GetCurrentArrows() => currentArrows;
    public int GetMaxArrows() => maxArrows;
    public float GetDrawPercent() => isDrawing ? Mathf.Clamp01(currentDrawTime / maxDrawTime) : 0f;
}
