using UnityEngine;

/// <summary>
/// Projectile fired by the ChickenBoss. Add this to your egg prefab alongside
/// a Rigidbody and a Collider. Call Launch() after instantiation (ChickenBoss
/// does this automatically).
///
/// Deals damage to anything tagged "Player" that implements IDamageable.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BossEgg : MonoBehaviour
{
    [Header("Knockback")]
    [Tooltip("Strength of the knockback shove applied to the player on hit.")]
    [SerializeField] private float m_KnockbackForce = 12f;
    [Tooltip("How much upward lift is mixed into the knockback direction (0 = pure horizontal).")]
    [SerializeField] private float m_KnockbackUpLift = 0.3f;
    [Tooltip("Tag used to identify the player GameObject.")]
    [SerializeField] private string m_PlayerTag = "Player";

    [Header("Lifetime")]
    [Tooltip("Seconds before the egg destroys itself if it never hits anything.")]
    [SerializeField] private float m_Lifetime = 6f;

    [Header("Visuals / Audio")]
    [Tooltip("Optional particle effect prefab spawned on impact.")]
    [SerializeField] private GameObject m_ImpactEffectPrefab;
    [Tooltip("Optional sound clip played on impact.")]
    [SerializeField] private AudioClip m_ImpactClip;
    [SerializeField] private AudioSource m_AudioSource;

    private Rigidbody m_Rb;
    private bool m_HasHit = false;
    private Vector3 m_LaunchDirection;

    private void Awake()
    {
        m_Rb = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// Sets the egg in motion. Called by ChickenBoss.SpawnEgg() right after
    /// the egg is instantiated.
    /// </summary>
    public void Launch(Vector3 direction, float speed)
    {
        m_LaunchDirection   = direction;
        m_Rb.linearVelocity = direction * speed;
        transform.forward   = direction;
        Destroy(gameObject, m_Lifetime);
    }

    private void FixedUpdate()
    {
        // Rotate the egg to always face its travel direction (looks nice on arcs)
        if (m_HasHit || m_Rb.isKinematic) return;

        if (m_Rb.linearVelocity.sqrMagnitude > 0.1f)
            transform.forward = m_Rb.linearVelocity.normalized;
    }

    private void OnCollisionEnter(Collision collision)
    {

        if(collision.gameObject.CompareTag("Boss"))
        {
            // Ignore collisions with other enemies (e.g. other eggs)
            Physics.IgnoreCollision(collision.collider, GetComponentInChildren<Collider>());
            return;
        }

        if (m_HasHit) return;
        m_HasHit = true;

        Debug.Log("Boss egg collided with " + collision.gameObject.name);

        // Knock the player back if this hit them.
        // GetComponentInParent handles the case where the egg hits a child collider
        // (e.g. a capsule mesh) rather than the parent that holds PlayerKnockback.
        PlayerKnockback knockback = collision.gameObject.GetComponentInParent<PlayerKnockback>();
        if (knockback != null)
        {
            Vector3 knockDir = (m_LaunchDirection + Vector3.up * m_KnockbackUpLift).normalized;
            knockback.ApplyKnockback(knockDir * m_KnockbackForce);
        }

        // Impact VFX
        if (m_ImpactEffectPrefab != null)
            Instantiate(m_ImpactEffectPrefab, transform.position, Quaternion.identity);

        // Impact SFX
        if (m_AudioSource != null && m_ImpactClip != null)
            m_AudioSource.PlayOneShot(m_ImpactClip);

        Destroy(gameObject);
    }
}
