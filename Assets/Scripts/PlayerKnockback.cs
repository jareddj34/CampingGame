using System.Collections;
using UnityEngine;

/// <summary>
/// Attach to the Player GameObject alongside PlayerController.
/// BossEgg (or any other source) calls ApplyKnockback() to shove the player.
/// Uses CharacterController.Move() so it works alongside PlayerController
/// without needing to modify that script at all.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerKnockback : MonoBehaviour
{
    [Tooltip("How quickly the knockback velocity fades out. Higher = snappier recovery.")]
    [SerializeField] private float m_Decay = 8f;

    private CharacterController m_Controller;
    private Vector3 m_KnockbackVelocity;
    private Coroutine m_KnockbackRoutine;

    private void Awake()
    {
        m_Controller = GetComponent<CharacterController>();
    }

    /// <summary>
    /// Applies an impulse to the player in world space.
    /// Calling this while a knockback is already active replaces it.
    /// </summary>
    public void ApplyKnockback(Vector3 force)
    {
        m_KnockbackVelocity = force;

        if (m_KnockbackRoutine != null)
            StopCoroutine(m_KnockbackRoutine);

        m_KnockbackRoutine = StartCoroutine(KnockbackRoutine());
    }

    private IEnumerator KnockbackRoutine()
    {
        while (m_KnockbackVelocity.sqrMagnitude > 0.05f)
        {
            m_Controller.Move(m_KnockbackVelocity * Time.deltaTime);
            m_KnockbackVelocity = Vector3.Lerp(m_KnockbackVelocity, Vector3.zero, m_Decay * Time.deltaTime);
            yield return null;
        }

        m_KnockbackVelocity = Vector3.zero;
    }
}
