using System.Collections;
using UnityEngine;

/// <summary>
/// Applies a knockback impulse to a mob whenever it takes damage.
/// Hooks into MobHealth.OnDamaged automatically — no changes to existing scripts needed.
/// Add this component to any mob prefab alongside MobHealth.
/// Works with CreatureMover because both just call CharacterController.Move(); the
/// displacements stack naturally each frame.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(MobHealth))]
public class MobKnockback : MonoBehaviour
{
    [Tooltip("Speed (m/s) of the initial knockback impulse.")]
    [SerializeField] private float m_KnockbackForce = 6f;

    [Tooltip("How quickly the knockback fades. Higher = snappier recovery.")]
    [SerializeField] private float m_Decay = 8f;

    [Tooltip("Upward component added to the knockback direction (0 = purely horizontal).")]
    [SerializeField, Range(0f, 1f)] private float m_UpwardKick = 0.3f;

    private CharacterController m_Controller;
    private MobHealth           m_Health;
    private Vector3             m_Velocity;
    private Coroutine           m_Routine;

    private void Awake()
    {
        m_Controller = GetComponent<CharacterController>();
        m_Health     = GetComponent<MobHealth>();
        m_Health.OnDamaged += OnDamaged;
    }

    private void OnDestroy()
    {
        m_Health.OnDamaged -= OnDamaged;
    }

    private void OnDamaged(Vector3 attackerPosition)
    {
        // Direction away from whoever hit us
        Vector3 dir = (transform.position - attackerPosition);
        dir.y = 0f;
        dir = dir.sqrMagnitude > 0.001f ? dir.normalized : transform.forward;

        // Add a small upward kick so it reads as a real impact
        dir = (dir + Vector3.up * m_UpwardKick).normalized;

        m_Velocity = dir * m_KnockbackForce;

        // Restart the routine so rapid hits each feel snappy
        if (m_Routine != null)
            StopCoroutine(m_Routine);
        m_Routine = StartCoroutine(KnockbackRoutine());
    }

    private IEnumerator KnockbackRoutine()
    {
        while (m_Velocity.sqrMagnitude > 0.05f)
        {
            m_Controller.Move(m_Velocity * Time.deltaTime);
            m_Velocity = Vector3.Lerp(m_Velocity, Vector3.zero, m_Decay * Time.deltaTime);
            yield return null;
        }

        m_Velocity = Vector3.zero;
    }
}
