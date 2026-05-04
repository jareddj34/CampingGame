using System.Collections;
using UnityEngine;

namespace ithappy.Animals_FREE
{
    /// <summary>
    /// Aggressive chicken variant. Wanders like a normal chicken, but will
    /// chase and attack the player when aggro'd.
    ///
    /// Aggressiveness is determined at spawn by either:
    ///   • m_AlwaysAggressive = true  → always aggro when player enters range
    ///   • m_AlwaysAggressive = false → random roll against m_AggroChance (0–1)
    ///
    /// Animation events in the chickenAttack clip should call:
    ///   EnableHitbox()  – at the frame the beak/foot should deal damage
    ///   DisableHitbox() – when the strike window closes
    ///
    /// Attach this component INSTEAD OF ChickenMob on the aggressive prefab.
    /// </summary>
    [RequireComponent(typeof(CreatureMover))]
    [RequireComponent(typeof(MobHealth))]
    [DisallowMultipleComponent]
    public class AggressiveChickenMob : MonoBehaviour
    {
        // ── Aggressiveness ─────────────────────────────────────────────────────
        [Header("Aggressiveness")]
        [Tooltip("If true, this chicken is always aggressive regardless of AggroChance.")]
        [SerializeField] private bool m_AlwaysAggressive = false;

        [Tooltip("If not always aggressive, the 0–1 probability that this chicken will be aggressive on spawn.")]
        [SerializeField, Range(0f, 1f)] private float m_AggroChance = 0.5f;

        private bool startAggro = false;

        [Tooltip("Distance at which an aggressive chicken detects the player and begins chasing.")]
        [SerializeField] private float m_AggroRange = 10f;

        // ── Attack ─────────────────────────────────────────────────────────────
        [Header("Attack")]
        [Tooltip("Distance from the player at which the chicken stops and attacks.")]
        [SerializeField] private float m_AttackRange = 1.5f;

        [Tooltip("Seconds between attacks (also used as the attack-state duration — set this to roughly the length of your attack animation).")]
        [SerializeField] private float m_AttackCooldown = 1.5f;

        [Tooltip("The Animator trigger name that plays the attack animation.")]
        [SerializeField] private string m_AttackTrigger = "Attack";

        [Tooltip("The child GameObject that holds the Hitbox collider. Drag it here in the Inspector.")]
        [SerializeField] private GameObject m_HitboxObject;

        // ── Wandering ──────────────────────────────────────────────────────────
        [Header("Wandering")]
        [SerializeField] private float m_IdleTimeMin = 2f;
        [SerializeField] private float m_IdleTimeMax = 5f;
        [SerializeField] private float m_WalkTimeMin = 1f;
        [SerializeField] private float m_WalkTimeMax = 3f;
        [SerializeField] private float m_WanderRadius = 8f;

        // ── Death ──────────────────────────────────────────────────────────────
        [Header("Death")]
        [SerializeField] private string m_DeathTrigger = "Death";
        private bool m_HasDeathTrigger;

        // ── Ambient Sounds ─────────────────────────────────────────────────────
        [Header("Ambient Sounds")]
        [SerializeField] private AudioSource m_AudioSource;
        [SerializeField] private AudioClip[] m_AmbientClips;
        [SerializeField] private float m_SoundIntervalMin = 4f;
        [SerializeField] private float m_SoundIntervalMax = 10f;

        // ── Internal State ─────────────────────────────────────────────────────
        private enum State { Idle, Walking, Chasing, Attacking }
        private State m_State = State.Idle;

        private CreatureMover m_Mover;
        private Animator      m_Animator;
        private MobHealth     m_Health;
        private Transform     m_Transform;
        private Transform     m_PlayerTransform;

        private Vector3 m_SpawnPoint;
        private Vector3 m_Destination;
        private Vector2 m_MoveAxis;

        private float m_StateTimer;        // how long to remain in the current state
        private float m_AttackCooldownTimer; // time until the next attack is allowed
        private bool  m_IsAggressive;      // final aggressiveness flag set at Awake

        // ──────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            m_Mover     = GetComponent<CreatureMover>();
            m_Animator  = GetComponent<Animator>();
            m_Health    = GetComponent<MobHealth>();
            m_Transform = transform;
            m_SpawnPoint = m_Transform.position;

            // Roll aggressiveness once at spawn
            m_IsAggressive = m_AlwaysAggressive || (Random.value < m_AggroChance);

            // Find the player by tag (make sure your player is tagged "Player")
            GameObject playerObj = GameObject.Find("PlayerCapsule");
            if (playerObj != null)
                m_PlayerTransform = playerObj.transform;
            else
                Debug.LogWarning("[AggressiveChickenMob] No GameObject tagged 'Player' found in scene.");

            // Hitbox starts disabled — animation events enable it at the right moment
            if (m_HitboxObject != null)
                m_HitboxObject.SetActive(false);
            else
                Debug.LogWarning("[AggressiveChickenMob] HitboxObject is not assigned in the Inspector.", this);

            m_HasDeathTrigger = HasAnimatorParameter(m_DeathTrigger, AnimatorControllerParameterType.Trigger);

            m_Health.OnDamaged += HandleDamage;
            m_Health.OnDied    += HandleDeath;

            EnterIdle();

            if (m_AmbientClips != null && m_AmbientClips.Length > 0 && m_AudioSource != null)
                StartCoroutine(AmbientSoundRoutine());
        }

        private void OnDestroy()
        {
            m_Health.OnDamaged -= HandleDamage;
            m_Health.OnDied    -= HandleDeath;
        }

        // ──────────────────────────────────────────────────────────────────────
        private void Update()
        {
            m_StateTimer         -= Time.deltaTime;
            m_AttackCooldownTimer -= Time.deltaTime;

            // Aggressive chickens constantly check for the player
            if (m_IsAggressive && m_PlayerTransform != null)
                CheckAggroRange();

            switch (m_State)
            {
                case State.Idle:     UpdateIdle();     break;
                case State.Walking:  UpdateWalking();  break;
                case State.Chasing:  UpdateChasing();  break;
                case State.Attacking:UpdateAttacking(); break;
            }

            m_Mover.SetInput(
                axis:   m_MoveAxis,
                target: m_Destination,
                isRun:  m_State == State.Chasing,
                isJump: false
            );
        }

        // ── Aggro Check ────────────────────────────────────────────────────────
        private void CheckAggroRange()
        {
            // Don't interrupt an attack or an active chase
            if (m_State == State.Chasing || m_State == State.Attacking) return;

            float distSq = (m_Transform.position - m_PlayerTransform.position).sqrMagnitude;
            if (distSq <= m_AggroRange * m_AggroRange)
                EnterChasing();
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

            m_MoveAxis = Vector2.up;
        }

        // ── State: Chasing ─────────────────────────────────────────────────────
        private void UpdateChasing()
        {
            if (m_PlayerTransform == null) { EnterIdle(); return; }

            float distSq = (m_Transform.position - m_PlayerTransform.position).sqrMagnitude;

            // Close enough AND cooldown expired → attack
            if (distSq <= m_AttackRange * m_AttackRange && m_AttackCooldownTimer <= 0f)
            {
                EnterAttacking();
                return;
            }

            // Otherwise keep running toward player
            m_Destination = m_PlayerTransform.position;
            m_MoveAxis    = Vector2.up;
        }

        // ── State: Attacking ───────────────────────────────────────────────────
        private void UpdateAttacking()
        {
            // Stand still and face the player while the animation plays
            m_MoveAxis    = Vector2.zero;
            m_Destination = m_Transform.position;

            if (m_PlayerTransform != null)
            {
                Vector3 dir = m_PlayerTransform.position - m_Transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f)
                    m_Transform.rotation = Quaternion.LookRotation(dir);
            }

            // When the attack window closes, resume chasing
            if (m_StateTimer <= 0f)
            {
                // Safety: make sure the hitbox is off if DisableHitbox() was never called
                if (m_HitboxObject != null)
                    m_HitboxObject.SetActive(false);

                EnterChasing();
            }
        }

        // ── Animation Event Callbacks ──────────────────────────────────────────

        /// <summary>
        /// Call this from an Animation Event at the frame the strike begins.
        /// </summary>
        public void EnableHitbox()
        {
            if (m_HitboxObject != null)
                m_HitboxObject.SetActive(true);
        }

        /// <summary>
        /// Call this from an Animation Event at the frame the strike ends.
        /// </summary>
        public void DisableHitbox()
        {
            if (m_HitboxObject != null)
                m_HitboxObject.SetActive(false);
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

        private void EnterChasing()
        {
            m_State      = State.Chasing;
            m_StateTimer = float.MaxValue; // no timer — chase indefinitely
        }

        private void EnterAttacking()
        {
            m_State               = State.Attacking;
            m_MoveAxis            = Vector2.zero;
            m_StateTimer          = m_AttackCooldown;   // state lasts one cooldown cycle
            m_AttackCooldownTimer = m_AttackCooldown;   // block re-entry

            m_Animator.SetTrigger(m_AttackTrigger);

            // Snap to face the player before the animation fires
            if (m_PlayerTransform != null)
            {
                Vector3 dir = m_PlayerTransform.position - m_Transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f)
                    m_Transform.rotation = Quaternion.LookRotation(dir);
            }
        }

        // ── Damage / Death Callbacks ───────────────────────────────────────────
        private void HandleDamage(Vector3 attackerPosition)
        {
            // Getting hit always makes the chicken aggressive, even if it rolled non-aggressive
            if (!m_IsAggressive)
                m_IsAggressive = true;

            // If not already fighting, start chasing immediately
            if (m_State != State.Chasing && m_State != State.Attacking)
                EnterChasing();
        }

        private void HandleDeath()
        {
            m_State    = State.Idle;
            m_MoveAxis = Vector2.zero;
            m_Mover.SetInput(Vector2.zero, m_Transform.position, false, false);

            if (m_HitboxObject != null)
                m_HitboxObject.SetActive(false);

            enabled = false; // stop Update()

            if (m_HasDeathTrigger)
                m_Animator.SetTrigger(m_DeathTrigger);
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        private Vector3 PickWanderDestination()
        {
            Vector2 randomCircle = Random.insideUnitCircle * m_WanderRadius;
            Vector3 candidate = m_SpawnPoint + new Vector3(randomCircle.x, 0f, randomCircle.y);

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
