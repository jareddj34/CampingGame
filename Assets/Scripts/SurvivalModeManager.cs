using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using TMPro;

public class SurvivalModeManager : MonoBehaviour
{
    // ── States ─────────────────────────────────────────────────────────────────
    public enum SurvivalState { Waiting, Countdown, WaveActive, WaveClear, GameOver }
    public SurvivalState State { get; private set; } = SurvivalState.Waiting;

    // ── Spawning ───────────────────────────────────────────────────────────────
    [Header("Spawn Points")]
    [Tooltip("Empty GameObjects placed around the ring perimeter. Chickens spawn near these.")]
    [SerializeField] private Transform[] spawnPoints;

    [Tooltip("How far from the spawn point we search for a valid NavMesh position.")]
    [SerializeField] private float navMeshSampleRadius = 3f;

    // ── Prefabs ────────────────────────────────────────────────────────────────
    [Header("Chicken Prefabs")]
    [Tooltip("Your normal chicken prefab(s). One is picked at random each spawn.")]
    [SerializeField] private GameObject[] chickenPrefabs;

    [Tooltip("(Optional) A harder chicken prefab that starts appearing at Elite Wave Threshold.")]
    [SerializeField] private GameObject eliteChickenPrefab;

    [Tooltip("Wave number at which elite chickens begin to appear.")]
    [SerializeField] private int eliteWaveThreshold = 5;

    [Tooltip("At the threshold wave, elite chance starts at 0. Each wave after it adds this much (0–1).")]
    [SerializeField] private float eliteChanceIncreasePerWave = 0.15f;

    // ── Wave Tuning ────────────────────────────────────────────────────────────
    [Header("Wave Settings")]
    [Tooltip("How many chickens spawn on wave 1.")]
    [SerializeField] private int baseChickenCount = 3;

    [Tooltip("Extra chickens added every wave.")]
    [SerializeField] private int countIncreasePerWave = 2;

    [Tooltip("Seconds between finishing one wave and starting the next.")]
    [SerializeField] private float timeBetweenWaves = 3f;

    [Tooltip("Stagger between individual chicken spawns so they don't all appear at once.")]
    [SerializeField] private float spawnDelayBetweenChickens = 0.25f;

    // ── UI ─────────────────────────────────────────────────────────────────────
    [Header("UI")]
    [Tooltip("Parent canvas/panel to show while survival mode is active. Hidden otherwise.")]
    [SerializeField] private GameObject survivalUI;

    [Tooltip("The SurvivalDeathScreen canvas. Shown when the player dies.")]
    [SerializeField] private SurvivalDeathScreen survivalDeathScreen;

    [Tooltip("Shows '3', '2', '1', 'GO!' during the countdown.")]
    [SerializeField] private TextMeshProUGUI countdownText;

    [Tooltip("Shows the current wave number (e.g. 'Wave 3').")]
    [SerializeField] private TextMeshProUGUI waveText;

    [Tooltip("Shows total kills so far.")]
    [SerializeField] private TextMeshProUGUI killCountText;

    [Tooltip("Shows 'Wave X Clear!' briefly between waves.")]
    [SerializeField] private TextMeshProUGUI waveCompleteText;

    // ── Internal ───────────────────────────────────────────────────────────────
    private int _currentWave = 0;
    private int _totalKills = 0;
    private int _aliveChickensThisWave = 0;
    private readonly List<GameObject> _activeChickens = new List<GameObject>();
    private Transform _playerTransform;
    private PlayerHealth _playerHealth;

    // ══════════════════════════════════════════════════════════════════════════

    private void Start()
    {
        if (survivalUI != null) survivalUI.SetActive(false);
        if (countdownText  != null) countdownText.text  = "";
        if (waveText       != null) waveText.text       = "";
        if (killCountText  != null) killCountText.text  = "";
        if (waveCompleteText != null) waveCompleteText.text = "";
    }

    // ── Ring entry ─────────────────────────────────────────────────────────────
    private void OnTriggerEnter(Collider other)
    {
        // Only fire once, and only for the player
        if (State != SurvivalState.Waiting) return;

        PlayerHealth ph = other.GetComponent<PlayerHealth>();
        if (ph == null) return;

        _playerTransform = other.transform;
        _playerHealth    = ph;

        StartSurvivalMode();
    }

    // ── Boot ───────────────────────────────────────────────────────────────────
    private void StartSurvivalMode()
    {
        _currentWave = 0;
        _totalKills  = 0;

        if (survivalUI != null) survivalUI.SetActive(true);
        UpdateKillUI();

        State = SurvivalState.Countdown;
        StartCoroutine(CountdownRoutine());
    }

    // ── Countdown 3 → 2 → 1 → GO! ─────────────────────────────────────────────
    private IEnumerator CountdownRoutine()
    {
        if (waveText        != null) waveText.text        = "";
        if (waveCompleteText!= null) waveCompleteText.text= "";

        for (int i = 3; i >= 1; i--)
        {
            if (countdownText != null) countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        if (countdownText != null) countdownText.text = "GO!";
        yield return new WaitForSeconds(0.8f);
        if (countdownText != null) countdownText.text = "";

        StartCoroutine(WaveLoop());
    }

    // ── Wave loop ──────────────────────────────────────────────────────────────
    private IEnumerator WaveLoop()
    {
        while (true)
        {
            // Check if player has died before starting next wave
            if (_playerHealth != null && _playerHealth.currentHealth <= 0)
            {
                EndSurvivalMode();
                yield break;
            }

            _currentWave++;
            State = SurvivalState.WaveActive;

            int chickenCount = baseChickenCount + (_currentWave - 1) * countIncreasePerWave;

            if (waveText        != null) waveText.text        = "Wave " + _currentWave;
            if (waveCompleteText!= null) waveCompleteText.text= "";

            // Spawn all chickens for this wave (staggered)
            yield return StartCoroutine(SpawnWave(chickenCount));

            // Wait until every chicken is dead (or player dies mid-wave)
            while (_aliveChickensThisWave > 0)
            {
                if (_playerHealth != null && _playerHealth.currentHealth <= 0)
                {
                    EndSurvivalMode();
                    yield break;
                }
                yield return null;
            }

            // Wave cleared!
            State = SurvivalState.WaveClear;
            if (waveCompleteText != null)
                waveCompleteText.text = "Wave " + _currentWave + " Clear!";

            yield return new WaitForSeconds(timeBetweenWaves);

            if (waveCompleteText != null) waveCompleteText.text = "";
        }
    }

    // ── Spawn a full wave ──────────────────────────────────────────────────────
    private IEnumerator SpawnWave(int count)
    {
        _aliveChickensThisWave = 0;

        // Remove any null entries left over from previous waves
        _activeChickens.RemoveAll(c => c == null);

        for (int i = 0; i < count; i++)
        {
            SpawnOneChicken();
            yield return new WaitForSeconds(spawnDelayBetweenChickens);
        }
    }

    // ── Spawn a single chicken ─────────────────────────────────────────────────
    private void SpawnOneChicken()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[SurvivalModeManager] No spawn points assigned!");
            return;
        }

        // Pick a random spawn point around the ring
        Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];

        // Find a valid NavMesh position near that point
        Vector3 spawnPos = point.position;
        if (NavMesh.SamplePosition(point.position, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            spawnPos = hit.position;

        // Choose prefab — at higher waves, elite chickens start mixing in
        GameObject prefab = PickPrefab();
        if (prefab == null) return;

        GameObject chicken = Instantiate(prefab, spawnPos, Quaternion.identity);
        _activeChickens.Add(chicken);

        // Track this chicken's death to count down the wave
        MobHealth health = chicken.GetComponent<MobHealth>();
        if (health != null)
        {
            _aliveChickensThisWave++;
            health.OnDied += OnChickenDied;
        }
        else
        {
            Debug.LogWarning("[SurvivalModeManager] Spawned chicken has no MobHealth — can't track its death.", chicken);
        }
    }

    // ── Prefab selection ───────────────────────────────────────────────────────
    private GameObject PickPrefab()
    {
        if (chickenPrefabs == null || chickenPrefabs.Length == 0)
        {
            Debug.LogError("[SurvivalModeManager] No chicken prefabs assigned!");
            return null;
        }

        // Before elite threshold → always normal
        if (eliteChickenPrefab == null || _currentWave < eliteWaveThreshold)
            return chickenPrefabs[Random.Range(0, chickenPrefabs.Length)];

        // After threshold → elite chance grows each wave
        float eliteChance = (_currentWave - eliteWaveThreshold) * eliteChanceIncreasePerWave;
        eliteChance = Mathf.Clamp01(eliteChance);

        return (Random.value < eliteChance)
            ? eliteChickenPrefab
            : chickenPrefabs[Random.Range(0, chickenPrefabs.Length)];
    }

    // ── Chicken death callback ─────────────────────────────────────────────────
    private void OnChickenDied()
    {
        _aliveChickensThisWave = Mathf.Max(0, _aliveChickensThisWave - 1);
        _totalKills++;
        UpdateKillUI();
    }

    // ── End / cleanup ──────────────────────────────────────────────────────────

    /// <summary>
    /// Called automatically when the player dies mid-mode.
    /// You can also call this from a UI "Give Up" button.
    /// </summary>
    public void EndSurvivalMode()
    {
        StopAllCoroutines();
        State = SurvivalState.GameOver;

        // Destroy any chickens still alive
        foreach (GameObject chicken in _activeChickens)
        {
            if (chicken != null) Destroy(chicken);
        }
        _activeChickens.Clear();
        _aliveChickensThisWave = 0;

        if (survivalUI != null) survivalUI.SetActive(false);

        // Show the death screen with this run's stats
        if (survivalDeathScreen != null)
            survivalDeathScreen.Show(_currentWave, _totalKills);
    }

    // ── UI helpers ─────────────────────────────────────────────────────────────
    private void UpdateKillUI()
    {
        if (killCountText != null)
            killCountText.text = "Kills: " + _totalKills;
    }

    // ── Read-only accessors (useful for other UI scripts) ──────────────────────
    public int CurrentWave  => _currentWave;
    public int TotalKills   => _totalKills;
    public int AliveThisWave => _aliveChickensThisWave;
}
