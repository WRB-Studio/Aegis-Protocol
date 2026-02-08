using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour, IResettable
{
    public static EnemySpawner Instance;

    [Header("Spawning")]
    public bool enableSpawning = true;
    public bool proceduralWave = true;
    public List<GameObject> enemyPrefabs = new List<GameObject>();
    public float timeBetweenWaves = 5f;
    [HideInInspector] public int currentWaveIndex = 0;
    public List<EnemyWave> waves = new List<EnemyWave>();
    public float swarmSpawnRadius = 0.5f;
    private bool waveIsRunning = false;
    private Transform spawnParent;

    [Header("Spawn Areas")]
    public Collider2D topArea;
    public Collider2D bottomArea;

    [HideInInspector] public List<Enemy> instantiatedEnemies = new List<Enemy>();

    // --- INIT SNAPSHOT ---
    private bool initenableSpawning;
    private bool initproceduralWave;
    private float inittimeBetweenWaves;
    private int initcurrentWaveIndex;
    private float initswarmSpawnRadius;
    private bool initwaveIsRunning;


    private void Awake()
    {
        Instance = this;
    }

    public void Init()
    {
        currentWaveIndex = 0;
        waveIsRunning = false;
        spawnParent = GameObject.Find("EnemyParent").transform;
    }

    public void UpdateNormal()
    {
        for (int i = instantiatedEnemies.Count - 1; i >= 0; i--)
        {
            var e = instantiatedEnemies[i];
            if (!e) { instantiatedEnemies.RemoveAt(i); continue; }
            e.UpdateNormal();
        }

        startNextWave();
    }


    public void UpdateGameOver()
    {
        foreach (Enemy enemy in instantiatedEnemies.ToArray())
        {
            if (enemy == null) continue;
            enemy.UpdateNormal();
        }
    }

    private void startNextWave()
    {
        if (waveIsRunning || instantiatedEnemies.Count > 0 || !enableSpawning)
            return;

        if (currentWaveIndex > 0)
            Stats.Instance.wavesCompleted++;

        StartCoroutine(SpawnWave());
    }

    private IEnumerator SpawnWave()
    {
        waveIsRunning = true;

        yield return new WaitForSeconds(timeBetweenWaves);

        if (!proceduralWave && currentWaveIndex >= waves.Count)
        {
            enableSpawning = false;
            yield break;
        }

        EnemyWave wave;
        if (proceduralWave)
            wave = GenerateProceduralWave(currentWaveIndex);
        else
            wave = waves[currentWaveIndex];

        int repeatWaves = Random.Range(wave.repeats.x, wave.repeats.y);

        for (int waveRepeat = 0; waveRepeat < repeatWaves; waveRepeat++)
        {
            foreach (var waveInstruction in wave.enemies)
            {
                for (int i = 0; i < Random.Range(waveInstruction.amount.x, waveInstruction.amount.y); i++)
                {
                    GameObject prefab = enemyPrefabs.Find(p => p && p.GetComponent<Enemy>() && 
                    p.GetComponent<Enemy>().enemyType == waveInstruction.type);

                    if (!prefab)
                    {
                        Debug.LogWarning($"Missing enemy prefab for type {waveInstruction.type}");
                        continue;
                    }

                    Vector2 spawnPos = GetRandomPointInArea();

                    // spawn swarm group
                    for (int swarmGroupSize = 0; swarmGroupSize < Random.Range(waveInstruction.swarmGroupSize.x, waveInstruction.swarmGroupSize.y); swarmGroupSize++)
                    {
                        Vector2 spawnPosVariation = spawnPos;
                        if (swarmGroupSize > 1)
                        {
                            spawnPosVariation = spawnPos + Random.insideUnitCircle * swarmSpawnRadius;
                        }

                        GameObject newEnemy = Instantiate(prefab, spawnPosVariation, Quaternion.identity, spawnParent);
                        newEnemy.GetComponent<Enemy>().InitWithLevel(currentWaveIndex);
                        instantiatedEnemies.Add(newEnemy.GetComponent<Enemy>());
                        Stats.Instance.enemiesSpawned++;
                    }

                    yield return new WaitForSeconds(Random.Range(waveInstruction.delayBetweenSpawns.x, waveInstruction.delayBetweenSpawns.y));
                }

                yield return new WaitForSeconds(Random.Range(wave.delayBetweenSpawnsTypes.x, wave.delayBetweenSpawnsTypes.y));

            }
        }

        currentWaveIndex++;

        waveIsRunning = false;
    }

    public static void RemoveEnemy(Enemy enemy, Stats.eDeadBy deadBy, float delay = 0)
    {
        if (Instance.instantiatedEnemies.Contains(enemy))
        {
            Stats.Instance.RegisterKill(enemy.enemyType, deadBy);

            Instance.instantiatedEnemies.Remove(enemy);
            Destroy(enemy.gameObject, delay);
        }
    }

    public static void RemoveAllEnemies()
    {
        foreach (Enemy enemy in Instance.instantiatedEnemies.ToArray())
        {
            RemoveEnemy(enemy, Stats.eDeadBy.None);
        }
    }

    private Vector2 GetRandomPointInArea()
    {
        Collider2D area = Random.value < 0.5f ? topArea : bottomArea;

        Bounds bounds = area.bounds;

        for (int i = 0; i < 100; i++) // maximal 100 Versuche
        {
            Vector2 point = new Vector2(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y)
            );

            if (area.OverlapPoint(point))
                return point;
        }

        return area.bounds.center;
    }

    private EnemyWave GenerateProceduralWave(int waveIndex)
    {
        EnemyWave wave = new EnemyWave();
        wave.enemies = new List<SpawnInstruction>();
        wave.repeats = new Vector2Int(1, Mathf.RoundToInt(1 + waveIndex / 7));
        wave.delayBetweenSpawnsTypes = new Vector2(0.4f, 0.8f);

        int enemyCount = Mathf.Min(Mathf.RoundToInt(2 + waveIndex * 1.2f), 50);

        for (int i = 0; i < enemyCount; i++)
        {
            var type = GetEnemyTypeByWave(waveIndex);
            var inst = new SpawnInstruction
            {
                type = type,
                amount = new Vector2Int(1, 2 + waveIndex),
                delayBetweenSpawns = new Vector2(0.5f, 1f),
                swarmGroupSize = type == Enemy.eEnemyType.Swarm
                    ? new Vector2Int(3 + waveIndex / 4, 5 + waveIndex / 4)
                    : new Vector2Int(1, 1)
            };
            wave.enemies.Add(inst);
        }

        return wave;
    }

    private Enemy.eEnemyType GetEnemyTypeByWave(int waveIndex)
    {
        float roll = Random.value;

        if (waveIndex > 15 && roll < 0.1f) return Enemy.eEnemyType.Boss;
        if (waveIndex > 12 && roll < 0.25f) return Enemy.eEnemyType.Ranged;
        if (waveIndex > 8 && roll < 0.4f) return Enemy.eEnemyType.Swarm;
        if (waveIndex > 6 && roll < 0.5f) return Enemy.eEnemyType.Tank;
        if (waveIndex > 3 && roll < 0.7f) return Enemy.eEnemyType.Fast;

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
        // runtime clear
        StopAllCoroutines();
        RemoveAllEnemies();
        instantiatedEnemies.Clear();

        enableSpawning = initenableSpawning;
        proceduralWave = initproceduralWave;
        timeBetweenWaves = inittimeBetweenWaves;
        currentWaveIndex = initcurrentWaveIndex;
        swarmSpawnRadius = initswarmSpawnRadius;
        waveIsRunning = initwaveIsRunning;
    }

}

[System.Serializable]
public class EnemyWave
{
    public List<SpawnInstruction> enemies;
    public Vector2 delayBetweenSpawnsTypes = new Vector2(0.5f, 1f);
    public Vector2Int repeats = new Vector2Int(2, 4);
}

[System.Serializable]
public class SpawnInstruction
{
    public Enemy.eEnemyType type;   // Enum: Normal, Fast, Tank, etc.
    public Vector2Int amount = new Vector2Int(1, 2);
    public Vector2 delayBetweenSpawns = new Vector2(0.5f, 1f);
    public Vector2Int swarmGroupSize = new Vector2Int(1, 1);
}