using UnityEngine;

public class BombEffects : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip fuseClip;
    [SerializeField] private AudioClip explosionClip;
    [SerializeField][Range(0f, 1f)] private float explosionVolume = 1f;

    [Header("Explosion VFX")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private float explosionVfxLifetime = 4f;

    [Header("Explosion Physics")]
    [SerializeField] private float explosionRadius = 6f;
    [SerializeField] private float explosionForce = 800f;
    [SerializeField] private LayerMask physicsLayers;

    private bool fusePlaying = false;

    // ----------------------------------------------------
    // FUSE
    // ----------------------------------------------------
    public void StartFuse()
    {
        if (audioSource == null || fuseClip == null)
        {
            Debug.LogWarning("[BombEffects] StartFuse called but audioSource or fuseClip is missing.");
            return;
        }

        audioSource.clip = fuseClip;
        audioSource.loop = true;
        audioSource.spatialBlend = 1f; // 3D hissing
        audioSource.Play();
        fusePlaying = true;

        Debug.Log("[BombEffects] Fuse started.");
    }

    public void StopFuse()
    {
        if (audioSource == null) return;

        if (fusePlaying)
        {
            audioSource.Stop();
            fusePlaying = false;
            Debug.Log("[BombEffects] Fuse stopped.");
        }
    }

    // ----------------------------------------------------
    // DETONATE
    // ----------------------------------------------------
    public void Detonate()
    {
        Debug.Log("[BombEffects] Detonate called.");

        // Stop fuse loop if playing
        StopFuse();

        // 🔊 Play explosion as 2D audio so it's always audible
        if (explosionClip != null)
        {
            Debug.Log($"[BombEffects] Playing explosion clip (2D): {explosionClip.name}");

            GameObject audioObj = new GameObject("ExplosionAudio2D");
            AudioSource src = audioObj.AddComponent<AudioSource>();

            src.clip = explosionClip;
            src.volume = explosionVolume;
            src.spatialBlend = 0f;          // 0 = 2D
            src.playOnAwake = false;
            src.loop = false;

            src.Play();
            Destroy(audioObj, explosionClip.length + 0.1f);
        }
        else
        {
            Debug.LogWarning("[BombEffects] No explosionClip assigned!");
        }

        // Spawn explosion VFX
        if (explosionPrefab != null)
        {
            GameObject vfx = Instantiate(
                explosionPrefab,
                transform.position,
                Quaternion.identity
            );

            if (explosionVfxLifetime > 0f)
                Destroy(vfx, explosionVfxLifetime);
        }

        // Apply explosion physics
        if (explosionRadius > 0f && Mathf.Abs(explosionForce) > 0.01f)
        {
            Collider[] hits = Physics.OverlapSphere(
                transform.position,
                explosionRadius,
                physicsLayers
            );

            foreach (var hit in hits)
            {
                Rigidbody rb = hit.attachedRigidbody;
                if (rb == null) continue;

                rb.AddExplosionForce(
                    explosionForce,
                    transform.position,
                    explosionRadius
                );
            }
        }
    }
}
