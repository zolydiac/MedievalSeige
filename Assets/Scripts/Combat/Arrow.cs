using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Arrow : MonoBehaviour
{
    [Header("Arrow Settings")]
    [SerializeField] private float lifetime = 10f;
    [SerializeField] private bool stickToSurface = true;
    [SerializeField] private float stickDepth = 0.2f;

    [Header("Effects")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip flySound;

    private Rigidbody rb;
    private Collider col;
    private bool hasHit = false;            // arrow has collided with *anything*
    private bool hasDealtDamage = false;    // arrow has already damaged a PlayerHealth
    private int damage = 25;
    private AudioSource audioSource;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        audioSource = GetComponent<AudioSource>();

        rb.isKinematic = false;
        rb.useGravity = true;
    }

    void Start()
    {
        // lifetime
        Destroy(gameObject, lifetime);

        // play flying sound once
        if (flySound != null && audioSource != null)
            audioSource.PlayOneShot(flySound);
    }

    void FixedUpdate()
    {
        if (!hasHit && rb != null && rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            // Face direction of travel
            transform.forward = rb.linearVelocity.normalized;
        }
    }

    public void Launch(Vector3 direction, float speed, int arrowDamage)
    {
        damage = arrowDamage;

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogError("[Arrow] Launch called but no Rigidbody found!");
                return;
            }
        }

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearVelocity = direction.normalized * speed;
    }

    void OnCollisionEnter(Collision collision)
    {
        // only handle the *first* collision
        if (hasHit) return;
        hasHit = true;

        // stop physics
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        HandleHit(collision);

        if (stickToSurface)
            StickToSurface(collision);
        else
        {
            // disable collider anyway so we never re-collide
            if (col != null)
                col.enabled = false;

            Destroy(gameObject, 0.1f);
        }
    }

    void HandleHit(Collision collision)
    {
        // spawn hit FX
        if (hitEffectPrefab != null && collision.contactCount > 0)
        {
            ContactPoint contact = collision.contacts[0];
            var effect = Instantiate(hitEffectPrefab, contact.point, Quaternion.LookRotation(contact.normal));
            Destroy(effect, 2f);
        }

        // play hit sound
        if (hitSound != null && audioSource != null)
            audioSource.PlayOneShot(hitSound);

        // if we already damaged someone, don't do it again
        if (hasDealtDamage)
            return;

        GameObject hitObject = collision.gameObject;

        PlayerHealth health = hitObject.GetComponentInParent<PlayerHealth>();
        if (health != null)
        {
            hasDealtDamage = true;  // lock it here
            health.TakeDamage(damage);
            Debug.Log($"[Arrow] Hit {hitObject.name} for {damage} damage");
        }
    }

    void StickToSurface(Collision collision)
    {
        // parent to whatever we hit so it moves with them
        transform.SetParent(collision.transform);

        if (collision.contactCount > 0)
        {
            ContactPoint contact = collision.contacts[0];
            transform.position = contact.point - transform.forward * stickDepth;
        }

        // absolutely disable collider so it can't keep colliding
        if (col != null)
            col.enabled = false;
    }
}
