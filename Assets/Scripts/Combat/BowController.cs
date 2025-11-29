using System.Collections;
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
    [SerializeField] private Transform aimTransform;

    [Header("Ammo")]
    [SerializeField] private int maxArrows = 20;
    private int currentArrows;

    [Header("Timing")]
    [SerializeField] private float shotDelay = 1.1f;   // delay between click and arrow firing
    [SerializeField] private float shotCooldown = 3f;  // cooldown after each shot

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip drawClip;
    [SerializeField] private AudioClip shootClip;

    // State
    private bool isEquipped = false;
    private bool isDrawing = false;
    private bool isOnCooldown = false;

    private float drawStartTime = 0f;
    private float currentDrawTime = 0f;

    // Animator params
    private readonly int aimHash = Animator.StringToHash("IsAiming");
    private readonly int drawHash = Animator.StringToHash("DrawAmount");
    private readonly int shootHash = Animator.StringToHash("Shoot");

    // You said you went with "equipbow" and "stowbow"
    private readonly int equipHash = Animator.StringToHash("equipbow");
    private readonly int stowHash = Animator.StringToHash("stowbow");

    void Start()
    {
        currentArrows = maxArrows;

        if (animator == null)
            animator = GetComponentInParent<Animator>();

        if (trajectoryLine != null)
            trajectoryLine.enabled = false;

        if (bowVisual != null)
            bowVisual.SetActive(isEquipped);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
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
            // Cancel drawing state
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

    // -------- OPTIONAL HOLD API (kept for flexibility) --------
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

        // play draw SFX
        if (audioSource != null && drawClip != null)
            audioSource.PlayOneShot(drawClip);
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

        // shoot SFX
        if (audioSource != null && shootClip != null)
            audioSource.PlayOneShot(shootClip);

        isDrawing = false;
        currentDrawTime = 0f;
    }

    // -------- Your press-once shooting with delay + cooldown --------

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

        if (isDrawing)
        {
            Debug.Log($"[BowController {name}] QuickShot blocked: already drawing");
            return;
        }

        if (isOnCooldown)
        {
            Debug.Log($"[BowController {name}] QuickShot blocked: on cooldown");
            return;
        }

        // Start "drawing" immediately on click
        isDrawing = true;
        drawStartTime = Time.time;

        if (animator != null)
        {
            animator.SetBool(aimHash, true);
            animator.SetTrigger(shootHash);   // start shoot animation
        }

        // NOTE: no drawClip here anymore

        StartCoroutine(ShotRoutine());
    }


    private IEnumerator ShotRoutine()
    {
        // Wait for the part of the animation where the arrow should leave the bow
        yield return new WaitForSeconds(shotDelay);   // e.g. 1.1f

        // If bow got unequipped or drawing was cancelled mid-way, abort
        if (!isEquipped || !isDrawing)
        {
            isDrawing = false;
            currentDrawTime = 0f;
            yield break;
        }

        // Compute speed based on how long we've been drawing (or clamp to max)
        currentDrawTime = Time.time - drawStartTime;
        float drawPercent = Mathf.Clamp01(currentDrawTime / maxDrawTime);
        float arrowSpeed = Mathf.Lerp(minArrowSpeed, maxArrowSpeed, drawPercent);

        // Actually spawn + launch the arrow
        SpawnArrow(arrowSpeed);
        currentArrows--;

        // shoot SFX at release
        if (audioSource != null && shootClip != null)
            audioSource.PlayOneShot(shootClip);

        // End drawing / aim state
        if (animator != null)
        {
            animator.SetBool(aimHash, false);
            animator.SetFloat(drawHash, 0f);
        }

        isDrawing = false;
        currentDrawTime = 0f;

        // Start cooldown AFTER the shot
        isOnCooldown = true;
        yield return new WaitForSeconds(shotCooldown);   // e.g. 3f
        isOnCooldown = false;
    }

    // -------- Internal helpers --------

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

        // Decide shoot direction: use camera if assigned, otherwise spawn forward
        Vector3 shootDir = (aimTransform != null)
            ? aimTransform.forward
            : arrowSpawnPoint.forward;

        Arrow arrowScript = arrow.GetComponent<Arrow>();
        if (arrowScript != null)
        {
            arrowScript.Launch(shootDir, speed, arrowDamage);
        }
        else
        {
            Rigidbody rb = arrow.GetComponent<Rigidbody>();
            if (rb != null)
                rb.linearVelocity = shootDir * speed;
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

    public int GetCurrentArrows() => currentArrows;
    public int GetMaxArrows() => maxArrows;
    public float GetDrawPercent() => isDrawing ? Mathf.Clamp01(currentDrawTime / maxDrawTime) : 0f;
}
