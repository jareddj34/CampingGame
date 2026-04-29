using System.Collections;
using UnityEngine;

namespace ithappy.Animals_FREE
{
    /// <summary>
    /// Boss chicken AI. Wanders and idles like a regular ChickenMob, but when the
    /// player enters its aggro radius it faces them, occasionally charges at them,
    /// and fires eggs using the "shootEgg" animator trigger.
    ///
    /// Requires: CreatureMover, MobHealth (same GameObject)
    /// </summary>
    [RequireComponent(typeof(CreatureMover))]
    [RequireComponent(typeof(MobHealth))]
    [DisallowMultipleComponent]
    public class ChickenBoss : MonoBehaviour
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
        [Tooltip("How far the boss can wander from its spawn point.")]
        [SerializeField] private float m_WanderRadius = 10f;

        // ── Aggro ──────────────────────────────────────────────────────────────
        [Header("Aggro")]
        [Tooltip("Outermost distance at which the boss notices the player and becomes aggressive.")]
        [SerializeField] private float m_AggroRadius = 12f;
        [Tooltip("Distance the player must reach before the boss gives up and wanders again.")]
        [SerializeField] private float m_DeaggroRadius = 20f;
        [SerializeField] private string m_PlayerTag = "Player";

        // ── Charging ──────────────────────────────────────────────────────────
        [Header("Charging")]
        [Tooltip("If the player is farther than this from the boss, the boss charges closer before shooting. " +
                 "Must be smaller than Aggro Radius.")]
        [SerializeField] private float m_ShootRadius = 7f;

        // ── Shooting ──────────────────────────────────────────────────────────
        [Header("Shooting")]
        [Tooltip("Animator trigger name for the egg-shoot animation.")]
        [SerializeField] private string m_ShootTrigger = "shootEgg";
        [Tooltip("Seconds after the trigger fires before the egg is actually spawned (sync to animation).")]
        [SerializeField] private float m_EggSpawnDelay = 0.4f;
        [Tooltip("Total duration of the shoot action before the boss picks its next action.")]
        [SerializeField] private float m_ShootActionDuration = 1.2f;
        [Tooltip("Transform at the beak/mouth — where the egg spawns. If left empty, defaults to just in front of the boss.")]
        [SerializeField] private Transform m_EggSpawnPoint;
        [Tooltip("The egg prefab to instantiate (must have a BossEgg component).")]
        [SerializeField] private GameObject m_EggPrefab;
        [Tooltip("Launch speed of the egg.")]
        [SerializeField] private float m_EggSpeed = 10f;

        // ── Action Cooldown ───────────────────────────────────────────────────
        [Header("Action Timing")]
        [Tooltip("Minimum seconds between actions (charge or shoot) while aggroed.")]
        [SerializeField] private float m_ActionCooldownMin = 1.5f;
        [Tooltip("Maximum seconds between actions (charge or shoot) while aggroed.")]
        [SerializeField] private float m_ActionCooldownMax = 3.5f;
        [Tooltip("Degrees per second the boss rotates to face the player while aggroed or shooting.")]
        [SerializeField] private float m_FacePlayerSpeed = 180f;

        // ── Death ──────────────────────────────────────────────────────────────
        [Header("Death")]
        [SerializeField] private string m_DeathTrigger = "Death";
        private bool m_HasDeathTrigger;

        // ── Ambient Sounds ─────────────────────────────────────────────────────
        [Header("Ambient Sounds")]
        [SerializeField] private AudioSource m_AudioSource;
        [SerializeField] private AudioClip[] m_AmbientClips;
        [Tooltip("Min seconds between random clucks.")]
        [SerializeField] private float m_SoundIntervalMin = 4f;
        [Tooltip("Max seconds between random clucks.")]
        [SerializeField] private float m_SoundIntervalMax = 10f;

        public AudioSource shootSound;

        // ── Internal State ─────────────────────────────────────────────────────
        private enum State { Idle, Walking, Aggroed, Charging, Shooting }
        private State m_State = State.Idle;

        private CreatureMover m_Mover;
        private Animator      m_Animator;
        private MobHealth     m_Health;
        private Transform     m_Transform;
        public Transform     m_Player;

        private Vector3 m_SpawnPoint;
        private Vector3 m_Destination;
        private Vector2 m_MoveAxis;

        // How long until the current non-action state times out
        private float m_StateTimer;
        // How long until the boss can take its next aggroed action
        private float m_ActionCooldown;
        // True while the ShootRoutine coroutine owns the state
        private bool m_IsPerformingAction;
        // Reference to the active shoot coroutine so we can stop only it (not ambient audio)
        private Coroutine m_ShootCoroutine;

        private bool m_HasShootTrigger;

        // ──────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            m_Mover     = GetComponent<CreatureMover>();
            m_Animator  = GetComponent<Animator>();
            m_Health    = GetComponent<MobHealth>();
            m_Transform = transform;
            m_SpawnPoint = m_Transform.position;

            m_HasDeathTrigger = HasAnimatorParameter(m_DeathTrigger, AnimatorControllerParameterType.Trigger);
            m_HasShootTrigger = HasAnimatorParameter(m_ShootTrigger, AnimatorControllerParameterType.Trigger);

            m_Health.OnDied    += HandleDeath;
            m_Health.OnDamaged += HandleDamage;

            GameObject playerObj = GameObject.Find("PlayerCapsule");
            if (playerObj != null)
                m_Player = playerObj.transform;

            EnterIdle();
        }

        private void Start()
        {
            // Started here instead of Awake() so that MobHealth.Awake() has already
            // run and set Current = m_MaxHealth. If we start this in Awake(), Current
            // is still 0 (default float), IsDead returns true, and the loop exits immediately.
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
            m_StateTimer     -= Time.deltaTime;
            m_ActionCooldown -= Time.deltaTime;

            // Aggro check runs every frame unless we're mid-shoot
            if (m_State != State.Shooting && m_Player != null)
                CheckAggroRange();

            switch (m_State)
            {
                case State.Idle:     UpdateIdle();     break;
                case State.Walking:  UpdateWalking();  break;
                case State.Aggroed:  UpdateAggroed();  break;
                case State.Charging: UpdateCharging(); break;
                // Shooting is driven entirely by the ShootRoutine coroutine
            }

            m_Mover.SetInput(
                axis:   m_MoveAxis,
                target: m_Destination,
                isRun:  m_State == State.Charging,
                isJump: false
            );

            // CreatureMover only rotates when moving, so we handle facing manually
            // when the boss is standing still but needs to look at the player.
            if ((m_State == State.Aggroed || m_State == State.Shooting) && m_Player != null)
                FacePlayerSmoothly();
        }

        // ── Aggro range check ──────────────────────────────────────────────────
        private void CheckAggroRange()
        {
            float distSq = (m_Transform.position - m_Player.position).sqrMagnitude;

            bool isAggroed = m_State == State.Aggroed || m_State == State.Charging;

            if (!isAggroed && distSq <= m_AggroRadius * m_AggroRadius)
            {
                EnterAggroed();
            }
            else if (isAggroed && distSq > m_DeaggroRadius * m_DeaggroRadius)
            {
                // Only stop the shoot coroutine — not the ambient sound routine
                if (m_ShootCoroutine != null)
                {
                    StopCoroutine(m_ShootCoroutine);
                    m_ShootCoroutine = null;
                }
                m_IsPerformingAction = false;
                EnterIdle();
            }
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

        // ── State: Aggroed ─────────────────────────────────────────────────────
        private void UpdateAggroed()
        {
            m_Destination = m_Player.position;
            m_MoveAxis    = Vector2.zero;

            float distSq = (m_Transform.position - m_Player.position).sqrMagnitude;

            // Player is outside shoot range — charge closer first
            if (distSq > m_ShootRadius * m_ShootRadius)
            {
                EnterCharging();
                return;
            }

            // Player is in range — shoot when the cooldown allows
            if (m_ActionCooldown <= 0f && !m_IsPerformingAction)
                m_ShootCoroutine = StartCoroutine(ShootRoutine());
        }

        // ── State: Charging ────────────────────────────────────────────────────
        private void UpdateCharging()
        {
            m_Destination = m_Player.position;
            m_MoveAxis    = Vector2.up;

            // Stop charging as soon as we've closed to shoot range
            float distSq = (m_Transform.position - m_Player.position).sqrMagnitude;
            if (distSq <= m_ShootRadius * m_ShootRadius)
                EnterAggroed();
        }

        // ── State: Shooting (coroutine) ────────────────────────────────────────
        private IEnumerator ShootRoutine()
        {

            m_IsPerformingAction = true;
            m_State    = State.Shooting;
            m_MoveAxis = Vector2.zero;

            // Keep facing the player while winding up
            m_Destination = m_Player != null
                ? m_Player.position
                : m_Transform.position + m_Transform.forward;

            if (m_HasShootTrigger)
                m_Animator.SetTrigger(m_ShootTrigger);

            // Wait for the right moment in the animation to release the egg
            yield return new WaitForSeconds(m_EggSpawnDelay);

            SpawnEgg();

            shootSound.Play();

            // Wait for the remainder of the action window
            float remaining = m_ShootActionDuration - m_EggSpawnDelay;
            if (remaining > 0f)
                yield return new WaitForSeconds(remaining);

            m_ActionCooldown     = Random.Range(m_ActionCooldownMin, m_ActionCooldownMax);
            m_IsPerformingAction = false;
            EnterAggroed();
        }

        /// <summary>
        /// Instantiates and launches an egg toward the player.
        /// Can also be called directly from an Animator Event on the shootEgg clip
        /// if you want frame-perfect timing instead of the delay approach.
        /// </summary>
        public void SpawnEgg()
        {
            if (m_EggPrefab == null || m_Player == null) return;

            Vector3 spawnPos = m_EggSpawnPoint != null
                ? m_EggSpawnPoint.position
                : m_Transform.position + m_Transform.forward * 0.6f + Vector3.up * 0.5f;

            // Aim for center-mass of the player
            Vector3 targetPos = m_Player.position + Vector3.up * 1f;
            Vector3 direction = (targetPos - spawnPos).normalized;

            GameObject eggObj = Instantiate(m_EggPrefab, spawnPos, Quaternion.LookRotation(direction));

            if (eggObj.TryGetComponent<BossEgg>(out var egg))
                egg.Launch(direction, m_EggSpeed);
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

        private void EnterAggroed()
        {
            m_State    = State.Aggroed;
            m_MoveAxis = Vector2.zero;

            // Only reset the cooldown if it isn't already ticking
            if (m_ActionCooldown <= 0f)
                m_ActionCooldown = Random.Range(m_ActionCooldownMin, m_ActionCooldownMax);
        }

        private void EnterCharging()
        {
            m_State = State.Charging;
            // No timer — charging ends naturally when the boss reaches m_ShootRadius
        }

        // ── Damage / Death callbacks ───────────────────────────────────────────
        private void HandleDamage(Vector3 attackerPosition)
        {
            // Snap to aggroed immediately if hit while passive
            if (m_State == State.Idle || m_State == State.Walking)
                EnterAggroed();
        }

        private void HandleDeath()
        {
            StopAllCoroutines();
            m_IsPerformingAction = false;
            m_State    = State.Idle;
            m_MoveAxis = Vector2.zero;
            m_Mover.SetInput(Vector2.zero, m_Transform.position, false, false);
            enabled    = false;

            if (m_HasDeathTrigger)
                m_Animator.SetTrigger(m_DeathTrigger);
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        /// <summary>
        /// Rotates the boss to face the player directly, bypassing CreatureMover's
        /// movement-based turning (which doesn't work when the axis is zero).
        /// </summary>
        private void FacePlayerSmoothly()
        {
            Vector3 dir = m_Player.position - m_Transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;

            Quaternion targetRot = Quaternion.LookRotation(dir);
            m_Transform.rotation = Quaternion.RotateTowards(
                m_Transform.rotation, targetRot, m_FacePlayerSpeed * Time.deltaTime);
        }

        private Vector3 PickWanderDestination()
        {
            Vector2 randomCircle = Random.insideUnitCircle * m_WanderRadius;
            Vector3 candidate    = m_SpawnPoint + new Vector3(randomCircle.x, 0f, randomCircle.y);

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

        // ── Gizmos ────────────────────────────────────────────────────────────
        private void OnDrawGizmosSelected()
        {
            // Shoot radius — green (inner: stand and shoot)
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, m_ShootRadius);

            // Aggro radius — yellow (outer: charge to get into shoot range)
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, m_AggroRadius);

            // De-aggro radius — red (give-up boundary)
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, m_DeaggroRadius);

            // Wander radius — cyan
            Vector3 origin = Application.isPlaying ? m_SpawnPoint : transform.position;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(origin, m_WanderRadius);
        }
    }
}
