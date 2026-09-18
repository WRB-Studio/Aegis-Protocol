using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class EnemySpawner : MonoBehaviour, IResettable
{
    public static EnemySpawner Instance;

    [Header("Spawning")]
    [SerializeField] bool enableSpawning = true;
    [SerializeField] bool proceduralWave = true;
    [SerializeField] List<GameObject> enemyPrefabs = new List<GameObject>();
    [SerializeField] float timeBetweenWaves = 5f;
    public int currentWaveIndex = 0;
    [SerializeField] List<EnemyWave> waves = new List<EnemyWave>();
    [SerializeField] float swarmSpawnRadius = 0.5f;

    [Header("Spawn Areas")]
    [SerializeField] Collider2D topArea;
    [SerializeField] Collider2D bottomArea;

    [Header("Runtime")]
    public List<Enemy> instantiatedEnemies = new List<Enemy>();

    bool waveIsRunning;
    public int DisplayWave => Mathf.Max(1, currentWaveIndex +
        ((waveIsRunning || instantiatedEnemies.Count == 0) ? 1 : 0));
    Transform spawnParent;
    Dictionary<Enemy.eEnemyType, GameObject> prefabByType;

    // --- INIT SNAPSHOT ---
    bool initenableSpawning, initproceduralWave, initwaveIsRunning;
    float inittimeBetweenWaves, initswarmSpawnRadius;
    int initcurrentWaveIndex;


    void Awake() => Instance = this;

    public void Init()
    {
        currentWaveIndex = 0;
        waveIsRunning = false;

        var p = GameObject.Find("EnemyParent");
        spawnParent = p ? p.transform : transform;

        BuildPrefabCache();
    }

    public void UpdateNormal()
    {
        UpdateInstantiatedEnemies();
        if (!waveIsRunning && instantiatedEnemies.Count == 0 &&
            SaveGameManager.Instance.HasWaveCheckpoint)
        {
            SaveGameManager.Instance.ClearWaveCheckpoint();
            SaveGameManager.Instance.Save();
        }
        StartNextWaveIfReady();
    }

    public void UpdateGameOver()
    {
        UpdateInstantiatedEnemies();
    }

    void UpdateInstantiatedEnemies()
    {
        for (int i = instantiatedEnemies.Count - 1; i >= 0; i--)
        {
            var e = instantiatedEnemies[i];
            if (!e) { instantiatedEnemies.RemoveAt(i); continue; }
            e.UpdateNormal();
        }
    }

    void StartNextWaveIfReady()
    {
        if (waveIsRunning || instantiatedEnemies.Count > 0 || !enableSpawning) return;

        StartCoroutine(SpawnWave());
    }

    IEnumerator SpawnWave()
    {
        waveIsRunning = true;

        yield return new WaitForSeconds(timeBetweenWaves);

        if (!proceduralWave && currentWaveIndex >= waves.Count)
        {
            enableSpawning = false;
            waveIsRunning = false;
            yield break;
        }

        SaveGameManager.Instance.CaptureWaveCheckpoint();
        if (currentWaveIndex > 0)
            Stats.Instance.wavesCompleted++;

        EnemyWave wave = proceduralWave ? GenerateProceduralWave(currentWaveIndex) : waves[currentWaveIndex];

        foreach (var instr in wave.enemies)
        {
            int amount = Random.Range(instr.amount.x, instr.amount.y + 1);

            for (int i = 0; i < amount; i++)
            {
                if (!prefabByType.TryGetValue(instr.type, out var prefab) || !prefab)
                {
                    Debug.LogWarning($"Missing enemy prefab for type {instr.type}");
                    continue;
                }

                Vector2 basePos = GetRandomPointInArea();
                int groupSize = Random.Range(instr.swarmGroupSize.x, instr.swarmGroupSize.y + 1);

                for (int s = 0; s < groupSize; s++)
                {
                    Vector2 pos = basePos + Random.insideUnitCircle * swarmSpawnRadius;

                    var go = Instantiate(prefab, pos, Quaternion.identity, spawnParent);
                    var enemy = go.GetComponent<Enemy>();
                    enemy.InitWithLevel(currentWaveIndex);

                    instantiatedEnemies.Add(enemy);
                    Stats.Instance.enemiesSpawned++;
                }

                yield return new WaitForSeconds(Random.Range(instr.delayBetweenSpawns.x, instr.delayBetweenSpawns.y));
            }

            yield return new WaitForSeconds(Random.Range(wave.delayBetweenSpawnsTypes.x, wave.delayBetweenSpawnsTypes.y));
        }

        currentWaveIndex++;
        waveIsRunning = false;
    }

    void BuildPrefabCache()
    {
        prefabByType = new Dictionary<Enemy.eEnemyType, GameObject>();

        foreach (var p in enemyPrefabs)
        {
            if (!p) continue;
            var e = p.GetComponent<Enemy>();
            if (!e) continue;

            prefabByType[e.enemyType] = p;
        }
    }

    public static void RemoveEnemy(Enemy enemy, Stats.eDeadBy deadBy, float delay = 0)
    {
        if (!Instance || !enemy) return;

        if (Instance.instantiatedEnemies.Contains(enemy))
        {
            if (deadBy != Stats.eDeadBy.None)
                Stats.Instance.RegisterKill(enemy.enemyType, deadBy);
            Instance.instantiatedEnemies.Remove(enemy);
            Destroy(enemy.gameObject, delay);
        }
    }

    public static void RemoveAllEnemies()
    {
        if (!Instance) return;

        foreach (Enemy e in Instance.instantiatedEnemies.ToArray())
            RemoveEnemy(e, Stats.eDeadBy.None);
    }

    Vector2 GetRandomPointInArea()
    {
        Collider2D area = (Random.value < 0.5f ? topArea : bottomArea) ?? topArea ?? bottomArea;
        if (!area) return Vector2.zero;

        Bounds b = area.bounds;

        for (int i = 0; i < 100; i++)
        {
            Vector2 p = new Vector2(Random.Range(b.min.x, b.max.x), Random.Range(b.min.y, b.max.y));
            if (area.OverlapPoint(p)) return p;
        }

        return area.bounds.center;
    }

    EnemyWave GenerateProceduralWave(int waveIndex)
    {
        int remaining = Mathf.Clamp(2 + waveIndex * 2, 2, 50);
        bool bossWave = waveIndex >= 19 && (waveIndex + 1) % 10 == 0;
        if (bossWave) remaining--;

        var wave = new EnemyWave
        {
            enemies = new List<SpawnInstruction>(),
            delayBetweenSpawnsTypes = new Vector2(0.1f, 0.2f)
        };

        while (remaining > 0)
        {
            var type = GetEnemyTypeByWave(waveIndex);
            int groupSize = type == Enemy.eEnemyType.Swarm ? Mathf.Min(3 + waveIndex / 10, 5, remaining) : 1;
            int amount = Mathf.Min(Random.Range(1, 4), remaining / groupSize);

            wave.enemies.Add(new SpawnInstruction
            {
                type = type,
                amount = new Vector2Int(amount, amount),
                delayBetweenSpawns = new Vector2(0.15f, 0.3f),
                swarmGroupSize = new Vector2Int(groupSize, groupSize)
            });
            remaining -= amount * groupSize;
        }

        if (bossWave)
            wave.enemies.Add(new SpawnInstruction { type = Enemy.eEnemyType.Boss, amount = Vector2Int.one,
                delayBetweenSpawns = Vector2.zero, swarmGroupSize = Vector2Int.one });

        return wave;
    }

    Enemy.eEnemyType GetEnemyTypeByWave(int waveIndex)
    {
        float roll = Random.value;

        if (waveIndex > 13 && roll < 0.25f) return Enemy.eEnemyType.Swarm;
        if (waveIndex > 9 && roll < 0.4f) return Enemy.eEnemyType.Ranged;
        if (waveIndex > 7 && roll < 0.5f) return Enemy.eEnemyType.Tank;
        if (waveIndex > 4 && roll < 0.7f) return Enemy.eEnemyType.Fast;

        return Enemy.eEnemyType.Normal;
    }


    public void StoreInit()
    {
        initenableSpawning = enableSpawning;
        initproceduralWave = proceduralWave;
        inittimeBetweenWaves = timeBetweenWaves;
        initcurrentWaveIndex = currentWaveIndex;
        initswarmSpawnRadius = swarmSpawnRadius;
        initwaveIsRunning = waveIsRunning;
    }

    public void ResetScript()
    {
        StopAllCoroutines();
        RemoveAllEnemies();
        instantiatedEnemies.Clear();

        enableSpawning = initenableSpawning;
        proceduralWave = initproceduralWave;
        timeBetweenWaves = inittimeBetweenWaves;
        currentWaveIndex = initcurrentWaveIndex;
        swarmSpawnRadius = initswarmSpawnRadius;
        waveIsRunning = initwaveIsRunning;

        BuildPrefabCache();
    }
}

[System.Serializable]
public class EnemyWave
{
    public List<SpawnInstruction> enemies;
    public Vector2 delayBetweenSpawnsTypes = new Vector2(0.5f, 1f);
}

[System.Serializable]
public class SpawnInstruction
{
    public Enemy.eEnemyType type;
    public Vector2Int amount = new Vector2Int(1, 2);
    public Vector2 delayBetweenSpawns = new Vector2(0.5f, 1f);
    public Vector2Int swarmGroupSize = new Vector2Int(1, 1);
}
