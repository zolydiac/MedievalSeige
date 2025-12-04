using UnityEngine;

public class BombController : MonoBehaviour
{
    [Header("Bomb Settings")]
    [SerializeField] private float defuseRadius = 3f;

    // Allow AI / other scripts to read the radius
    public float DefuseRadius => defuseRadius;

    private float bombDuration;       // seconds until explosion
    private float defuseDuration;     // seconds holding to defuse

    private float remainingTime;
    private bool isPlanted;
    private bool isExploded;
    private bool isDefused;

    private RoundManager roundManager;

    // Defuse state (now generic Transform so AI can use it too)
    private Transform currentDefuser;
    private float defuseProgress;

    // Effects (audio + VFX)
    private BombEffects bombEffects;

    // --------------------------------------------------------------------
    void Awake()
    {
        bombEffects = GetComponent<BombEffects>();
    }

    // --------------------------------------------------------------------
    // INITIALISE (called by RoundManager when bomb is planted)
    // --------------------------------------------------------------------
    public void Initialize(RoundManager manager, float duration, float defuseTime)
    {
        roundManager = manager;
        bombDuration = duration;
        defuseDuration = defuseTime;

        remainingTime = bombDuration;
        isPlanted = true;
        isExploded = false;
        isDefused = false;

        defuseProgress = 0f;
        currentDefuser = null;

        // start fuse hissing when bomb becomes active
        if (bombEffects != null)
            bombEffects.StartFuse();
    }

    public bool IsActive => isPlanted && !isExploded && !isDefused;

    // --------------------------------------------------------------------
    void Update()
    {
        if (!IsActive) return;

        // Tick down bomb timer
        remainingTime -= Time.deltaTime;

        if (roundManager != null)
            roundManager.UpdateBombTimerUI(remainingTime);

        if (remainingTime <= 0f)
        {
            Explode();
            return;
        }

        // Handle defuse progress
        if (currentDefuser != null)
        {
            float dist = Vector3.Distance(currentDefuser.position, transform.position);

            // Moved out of range? cancel
            if (dist > defuseRadius)
            {
                currentDefuser = null;
                defuseProgress = 0f;
                Debug.Log("[Bomb] Defuse cancelled (out of range)");
            }
            else
            {
                defuseProgress += Time.deltaTime;
                if (defuseProgress >= defuseDuration)
                {
                    Defuse();
                }
            }
        }
    }

    // --------------------------------------------------------------------
    // DEFUSE API (generic Transform, used by players + AI)
    // --------------------------------------------------------------------
    public void StartDefuse(Transform defuser)
    {
        if (!IsActive || defuser == null) return;

        float dist = Vector3.Distance(defuser.position, transform.position);
        if (dist > defuseRadius) return;

        // Nobody defusing yet -> start and reset progress ONCE
        if (currentDefuser == null)
        {
            currentDefuser = defuser;
            defuseProgress = 0f;
            Debug.Log("[Bomb] Defuse started");
        }
        // Same defuser calling again -> ignore to avoid resetting progress
        else if (currentDefuser == defuser)
        {
            return;
        }
        // Someone else is already defusing -> ignore
        else
        {
            return;
        }
    }

    public void StopDefuse(Transform defuser)
    {
        if (currentDefuser == defuser)
        {
            currentDefuser = null;
            defuseProgress = 0f;
            Debug.Log("[Bomb] Defuse cancelled");
        }
    }

    // --------------------------------------------------------------------
    // Backwards-compatible wrappers for SplitScreenFPSController
    // --------------------------------------------------------------------
    public void StartDefuse(SplitScreenFPSController player)
    {
        if (player != null)
            StartDefuse(player.transform);
    }

    public void StopDefuse(SplitScreenFPSController player)
    {
        if (player != null)
            StopDefuse(player.transform);
    }

    // --------------------------------------------------------------------
    private void Explode()
    {
        if (!IsActive) return;

        isExploded = true;
        Debug.Log("[Bomb] EXPLODED!");

        // Tell RoundManager first
        if (roundManager != null)
            roundManager.OnBombExploded();

        // Play VFX + SFX + physics
        if (bombEffects != null)
            bombEffects.Detonate();

        // Now remove the bomb itself (mesh, collider etc.)
        Destroy(gameObject, 0.05f);
    }

    private void Defuse()
    {
        if (!IsActive) return;

        isDefused = true;
        Debug.Log("[Bomb] DEFUSED!");

        // stop fuse hissing
        if (bombEffects != null)
            bombEffects.StopFuse();

        if (roundManager != null)
            roundManager.OnBombDefused();

        Destroy(gameObject, 0.5f);
    }
}
