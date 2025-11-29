using UnityEngine;

public class BombController : MonoBehaviour
{
    [Header("Bomb Settings")]
    [SerializeField] private float defuseRadius = 3f;

    private float bombDuration;       // seconds until explosion
    private float defuseDuration;     // seconds holding to defuse

    private float remainingTime;
    private bool isPlanted;
    private bool isExploded;
    private bool isDefused;

    private RoundManager roundManager;

    // Defuse state
    private SplitScreenFPSController currentDefuser;
    private float defuseProgress;

    // NEW: effects (audio + VFX)
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

        // NEW: start fuse hissing when bomb becomes active
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
            float dist = Vector3.Distance(currentDefuser.transform.position, transform.position);

            // Moved out of range? cancel
            if (dist > defuseRadius)
            {
                currentDefuser = null;
                defuseProgress = 0f;
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
    // DEFUSE API (called from player controller)
    // --------------------------------------------------------------------
    public void StartDefuse(SplitScreenFPSController player)
    {
        if (!IsActive) return;

        float dist = Vector3.Distance(player.transform.position, transform.position);
        if (dist > defuseRadius) return;

        // Only one defuser at a time
        if (currentDefuser != null && currentDefuser != player)
            return;

        currentDefuser = player;
        defuseProgress = 0f;

        Debug.Log("[Bomb] Defuse started");
    }

    public void StopDefuse(SplitScreenFPSController player)
    {
        if (currentDefuser == player)
        {
            currentDefuser = null;
            defuseProgress = 0f;
            Debug.Log("[Bomb] Defuse cancelled");
        }
    }

    // --------------------------------------------------------------------
    // OUTCOMES
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
        BombEffects bombEffects = GetComponent<BombEffects>();
        if (bombEffects != null)
            bombEffects.Detonate();

        // Now remove the bomb itself (mesh, collider etc.)
        // VFX + explosion sound are on separate objects, so this is safe
        Destroy(gameObject, 0.05f);
    }



    private void Defuse()
    {
        if (!IsActive) return;

        isDefused = true;
        Debug.Log("[Bomb] DEFUSED!");

        // NEW: stop fuse hissing
        if (bombEffects != null)
            bombEffects.StopFuse();

        if (roundManager != null)
            roundManager.OnBombDefused();

        Destroy(gameObject, 0.5f);
    }
}
