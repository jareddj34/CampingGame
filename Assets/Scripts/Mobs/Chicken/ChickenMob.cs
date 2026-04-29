using System.Collections;
using UnityEngine;

namespace ithappy.Animals_FREE
{
    /// <summary>
    /// Minecraft-style chicken mob AI. Drives CreatureMover to wander, idle,
    /// and flee when attacked. Attach to the same GameObject as CreatureMover.
    /// </summary>
    [RequireComponent(typeof(CreatureMover))]
    [RequireComponent(typeof(MobHealth))]
    [DisallowMultipleComponent]
    public class ChickenMob : MonoBehaviour
    {
        // ── Wandering ──────────────────────────────────────────────────────────
        [Header("Wandering")]
        [Tooltip("Minimum seconds to stand still between walks.")]
        [SerializeField] private float m_IdleTimeMin = 2f;
        [Tooltip("Maximum seconds to stand still between walks.")]
        [SerializeField] private float m_IdleTimeMax = 5f;
        [Tooltip("Minimum seconds to walk before stopping again.")]
        [SerializeField] private float m_WalkTimeMin = 1f;
        [Tooltip("Maximum seconds to walk before stopping again.")]
        [SerializeField] private float m_WalkTimeMax = 3f;
        [Tooltip("How far the chicken can wander from its spawn point.")]
        [SerializeField] private float m_WanderRadius = 8f;

        // ── Fleeing ────────────────────────────────────────────────────────────
        [Header("Fleeing")]
        [Tooltip("How long (seconds) the chicken runs away after being hit.")]
        [SerializeField] private float m_FleeDuration = 4f;
        [Tooltip("How far from the attacker the chicken tries to flee to.")]
        [SerializeField] private float m_FleeDistance = 10f;

        [Header("Death")]
        [SerializeField] private string m_DeathTrigger = "Death";
        private bool m_HasDeathTrigger;

        // ── Internal state ─────────────────────────────────────────────────────
        private enum State { Idle, Walking, Fleeing }
        private State m_State = State.Idle;

        private CreatureMover m_Mover;
        private Animator m_Animator;
        private MobHealth m_Health;
        private Transform m_Transform;
        private Vector3 m_SpawnPoint;

        // Current navigation target (world position)
        private Vector3 m_Destination;
        // Direction input fed into CreatureMover each frame (-1..1 on each axis)
        private Vector2 m_MoveAxis;
        // Who last hit us — used to calculate flee direction
        private Vector3 m_AttackerPosition;

        private float m_StateTimer;

        // ──────────────────────────────────────────────────────────────────────

        [Header("Ambient Sounds")]
        [SerializeField] private AudioSource m_AudioSource;
        [SerializeField] private AudioClip[] m_AmbientClips;
        [Tooltip("Min seconds between random clucks.")]
        [SerializeField] private float m_SoundIntervalMin = 4f;
        [Tooltip("Max seconds between random clucks.")]
        [SerializeField] private float m_SoundIntervalMax = 10f;


        private void Awake()
        {
            m_Mover     = GetComponent<CreatureMover>();
            m_Animator  = GetComponent<Animator>();
            m_Health    = GetComponent<MobHealth>();
            m_Transform = transform;
            m_SpawnPoint = m_Transform.position;

            m_HasDeathTrigger = HasAnimatorParameter(m_DeathTrigger, AnimatorControllerParameterType.Trigger);
            m_Health.OnDied += HandleDeath;


            m_Health.OnDamaged += HandleDamage;

            if (m_AmbientClips != null && m_AmbientClips.Length > 0 && m_AudioSource != null)
                StartCoroutine(AmbientSoundRoutine());
        }

        private void OnDestroy()
        {
            m_Health.OnDamaged -= HandleDamage;
            m_Health.OnDied -= HandleDeath;
        }

        // ──────────────────────────────────────────────────────────────────────
        private void Update()
        {
            m_StateTimer -= Time.deltaTime;

            switch (m_State)
            {
                case State.Idle:    UpdateIdle();    break;
                case State.Walking: UpdateWalking(); break;
                case State.Fleeing: UpdateFleeing(); break;
            }

            // Always pass input to the mover. Target = destination, no jump.
            m_Mover.SetInput(
                axis:   m_MoveAxis,
                target: m_Destination,
                isRun:  m_State == State.Fleeing,
                isJump: false
            );
        }

        // ── State: Idle ────────────────────────────────────────────────────────
        private void UpdateIdle()
        {
            m_MoveAxis = Vector2.zero;

            if (m_StateTimer <= 0f)
                EnterWalking();
        }

        // ── State: Walking ─────────────────────────────────────────────────────
        private void UpdateWalking()
        {
            Vector3 toDestination = m_Destination - m_Transform.position;
            toDestination.y = 0f;

            if (toDestination.sqrMagnitude < 0.4f * 0.4f || m_StateTimer <= 0f)
            {
                EnterIdle();
                return;
            }

            // Drive the mover: forward = +Y axis in CreatureMover's 2D axis space
            m_MoveAxis = Vector2.up;
            m_Destination = m_Destination; // unchanged — mover handles turning
        }

        // ── State: Fleeing ─────────────────────────────────────────────────────
        private void UpdateFleeing()
        {
            Vector3 fleeDirection = (m_Transform.position - m_AttackerPosition).normalized;
            fleeDirection.y = 0f;
            m_Destination = m_Transform.position + fleeDirection * m_FleeDistance;

            m_MoveAxis = Vector2.up; // always run forward toward flee destination

            if (m_StateTimer <= 0f)
                EnterIdle();
        }

        // Death state
        private void HandleDeath()
        {
            m_State = State.Idle;        // stop all movement
            m_MoveAxis = Vector2.zero;
            m_Mover.SetInput(Vector2.zero, m_Transform.position, false, false);
            enabled = false;             // stop Update() from running

            if (m_HasDeathTrigger)
                m_Animator.SetTrigger(m_DeathTrigger);
        }

        // ── Transitions ────────────────────────────────────────────────────────
        private void EnterIdle()
        {
            m_State      = State.Idle;
            m_MoveAxis   = Vector2.zero;
            m_StateTimer = Random.Range(m_IdleTimeMin, m_IdleTimeMax);
        }

        private void EnterWalking()
        {
            m_Destination = PickWanderDestination();
            m_State       = State.Walking;
            m_StateTimer  = Random.Range(m_WalkTimeMin, m_WalkTimeMax);
        }

        private void EnterFleeing(Vector3 attackerPosition)
        {
            m_AttackerPosition = attackerPosition;
            m_State            = State.Fleeing;
            m_StateTimer       = m_FleeDuration;
        }

        // ── Damage callback ────────────────────────────────────────────────────
        private void HandleDamage(Vector3 attackerPosition)
        {
            EnterFleeing(attackerPosition);
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        private Vector3 PickWanderDestination()
        {
            // Pick a random direction and distance within the wander radius
            Vector2 randomCircle = Random.insideUnitCircle * m_WanderRadius;
            Vector3 candidate = m_SpawnPoint + new Vector3(randomCircle.x, 0f, randomCircle.y);

            // Snap to navmesh height if using navmesh, otherwise just use terrain
            if (Physics.Raycast(candidate + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f))
                candidate.y = hit.point.y;

            return candidate;
        }

        private bool HasAnimatorParameter(string paramName, AnimatorControllerParameterType type)
        {
            if (m_Animator == null) return false;
            foreach (var p in m_Animator.parameters)
                if (p.name == paramName && p.type == type) return true;
            return false;
        }

        // -- Sounds
        private IEnumerator AmbientSoundRoutine()
        {
            while (!m_Health.IsDead)
            {
                float wait = Random.Range(m_SoundIntervalMin, m_SoundIntervalMax);
                yield return new WaitForSeconds(wait);

                if (!m_Health.IsDead && m_AmbientClips.Length > 0)
                {
                    AudioClip clip = m_AmbientClips[Random.Range(0, m_AmbientClips.Length)];
                    m_AudioSource.PlayOneShot(clip);
                }
            }
        }
    }
}