using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
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
    private bool hasHit = false;
    private int damage = 25;
    private AudioSource audioSource;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();

        // IMPORTANT: make sure physics is actually active
        rb.isKinematic = false;
        rb.useGravity = true;
    }

    void Start()
    {
        // Lifetime
        Destroy(gameObject, lifetime);

        // Play flying sound
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

        // IMPORTANT: use Rigidbody.velocity, not linearVelocity
        rb.linearVelocity = direction.normalized * speed;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        hasHit = true;

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
            Destroy(gameObject, 0.1f);
    }

    void HandleHit(Collision collision)
    {
        if (hitEffectPrefab != null)
        {
            ContactPoint contact = collision.contacts[0];
            var effect = Instantiate(hitEffectPrefab, contact.point, Quaternion.LookRotation(contact.normal));
            Destroy(effect, 2f);
        }

        if (hitSound != null && audioSource != null)
            audioSource.PlayOneShot(hitSound);

        GameObject hitObject = collision.gameObject;

        PlayerHealth health = hitObject.GetComponentInParent<PlayerHealth>();
        if (health != null)
        {
            health.TakeDamage(damage);
            Debug.Log($"[Arrow] Hit {hitObject.name} for {damage} damage");
        }
    }

    void StickToSurface(Collision collision)
    {
        transform.SetParent(collision.transform);

        ContactPoint contact = collision.contacts[0];
        transform.position = contact.point - transform.forward * stickDepth;

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;
    }
}

