using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class Arrow : MonoBehaviour
{
    [Header("Settings")]
    public int damage = 1;
    public float lifetime = 5f;           // Destroy arrow after this many seconds if it never hits
    public LayerMask hitLayers;           // Which layers the arrow can damage

    private Rigidbody rb;
    private bool hasHit = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Called by BowItem immediately after instantiation
    public void Launch(Vector3 direction, float force)
    {
        rb.isKinematic = false;
        rb.linearVelocity = direction * force;

        // Point the arrow in the direction it's travelling
        transform.forward = direction;

        // Destroy if it never hits anything
        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        hasHit = true;

        // Try to deal damage
        if (collision.gameObject.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(damage);
        }

        // Also support MobHealth (matches your Tool.cs pattern)
        if (collision.gameObject.TryGetComponent<MobHealth>(out var mobHealth))
        {
            mobHealth.TakeDamage(damage, transform.position);
        }

        // Stick into whatever was hit by parenting and killing physics
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        transform.SetParent(collision.transform);

        // Optionally destroy after a few seconds once stuck
        Destroy(gameObject, 5f);
    }

    // Rotate the arrow to match its velocity direction every physics tick
    // (gives it a nice arc / tumbling feel if gravity is on)
    private void FixedUpdate()
    {
        if (hasHit || rb.isKinematic) return;

        if (rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            transform.forward = rb.linearVelocity.normalized;
        }
    }
}