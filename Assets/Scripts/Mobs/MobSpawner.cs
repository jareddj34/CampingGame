using UnityEngine;
using UnityEngine.AI;

public class MobSpawner : MonoBehaviour
{
    [Header("Spawning")]
    public GameObject[] mobPrefabs;
    public Transform player;
    public float spawnRadius = 30f;
    public float minDistanceFromPlayer = 10f;
    public int maxMobs = 20;
    public float spawnInterval = 5f;

    [Header("NavMesh")]
    public float navMeshSampleRadius = 5f;

    private float _timer;
    private int _currentMobCount;

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= spawnInterval && _currentMobCount < maxMobs)
        {
            _timer = 0f;
            TrySpawnMob();
        }
    }

    void TrySpawnMob()
    {
        for (int attempt = 0; attempt < 10; attempt++) // Max 10 tries
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized
                                   * Random.Range(minDistanceFromPlayer, spawnRadius);

            Vector3 candidate = player.position + new Vector3(randomCircle.x, 0, randomCircle.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            {
                GameObject prefab = mobPrefabs[Random.Range(0, mobPrefabs.Length)];
                GameObject mob = Instantiate(prefab, hit.position, Quaternion.identity);

                // Hook into MobHealth's existing OnDied event
                MobHealth health = mob.GetComponent<MobHealth>();
                if (health != null)
                    health.OnDied += OnMobDied;

                // Optional: track count via the mob's death event
                _currentMobCount++;
                return;
            }
        }

        Debug.LogWarning("MobSpawner: Failed to find valid NavMesh spawn point after 10 attempts.");
    }

    public void OnMobDied() {
        Debug.Log("MobSpawner: A mob died, reducing count.");
        _currentMobCount--;
    }
}