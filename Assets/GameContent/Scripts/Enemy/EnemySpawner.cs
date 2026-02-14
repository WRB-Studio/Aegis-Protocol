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

    [Header("Spawn Areas")]
    public Collider2D topArea;
    public Collider2D bottomArea;

    [HideInInspector] public List<Enemy> instantiatedEnemies = new List<Enemy>();

    bool waveIsRunning;
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

        // Parent für Ordnung in der Hierarchie
        var p = GameObject.Find("EnemyParent");
        spawnParent = p ? p.transform : transform;

        // Cache: EnemyType -> Prefab (spart Find/GetComponent im Spawn-Loop)
        BuildPrefabCache();
    }

    public void UpdateNormal()
    {
        // Enemies updaten + Nulls aus Liste entfernen
        UpdateInstantiatedEnemies();

        // Neue Welle starten, wenn nichts mehr lebt
        StartNextWaveIfReady();
    }

    public void UpdateGameOver()
    {
        // Verhalten bleibt gleich, nur kein neues Spawning
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
        // keine neue Wave, wenn: schon läuft / noch Gegner da / Spawning aus
        if (waveIsRunning || instantiatedEnemies.Count > 0 || !enableSpawning) return;

        // completed zählt erst ab Wave 1 (nach der ersten)
        if (currentWaveIndex > 0)
            Stats.Instance.wavesCompleted++;

        StartCoroutine(SpawnWave());
    }

    IEnumerator SpawnWave()
    {
        waveIsRunning = true;

        // Pause zwischen Wellen
        yield return new WaitForSeconds(timeBetweenWaves);

        // Non-procedural: wenn Liste durch ist -> Ende
        if (!proceduralWave && currentWaveIndex >= waves.Count)
        {
            enableSpawning = false;
            waveIsRunning = false;
            yield break;
        }

        // Wave holen/generieren
        EnemyWave wave = proceduralWave ? GenerateProceduralWave(currentWaveIndex) : waves[currentWaveIndex];

        // repeats ist (min..max) inklusiv gedacht -> +1 wegen int Range exklusiv
        int repeatWaves = Random.Range(wave.repeats.x, wave.repeats.y + 1);

        for (int waveRepeat = 0; waveRepeat < repeatWaves; waveRepeat++)
        {
            foreach (var instr in wave.enemies)
            {
                // wie viele "Pakete" dieses Typs in dieser Wave
                int amount = Random.Range(instr.amount.x, instr.amount.y + 1);

                for (int i = 0; i < amount; i++)
                {
                    // Prefab aus Cache holen
                    if (!prefabByType.TryGetValue(instr.type, out var prefab) || !prefab)
                    {
                        Debug.LogWarning($"Missing enemy prefab for type {instr.type}");
                        continue;
                    }

                    // Basispunkt für den Spawn (Area)
                    Vector2 basePos = GetRandomPointInArea();

                    // Swarm: wie viele Einheiten auf einmal (inkl.)
                    int groupSize = Random.Range(instr.swarmGroupSize.x, instr.swarmGroupSize.y + 1);

                    for (int s = 0; s < groupSize; s++)
                    {
                        // Variation immer, damit nicht alle exakt stacken
                        Vector2 pos = basePos + Random.insideUnitCircle * swarmSpawnRadius;

                        var go = Instantiate(prefab, pos, Quaternion.identity, spawnParent);
                        var enemy = go.GetComponent<Enemy>(); // ok: nur 1x pro Spawn
                        enemy.InitWithLevel(currentWaveIndex);

                        instantiatedEnemies.Add(enemy);
                        Stats.Instance.enemiesSpawned++;
                    }

                    // Delay zwischen Spawns dieses Typs
                    yield return new WaitForSeconds(Random.Range(instr.delayBetweenSpawns.x, instr.delayBetweenSpawns.y));
                }

                // Delay zwischen unterschiedlichen Typen
                yield return new WaitForSeconds(Random.Range(wave.delayBetweenSpawnsTypes.x, wave.delayBetweenSpawnsTypes.y));
            }
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

            prefabByType[e.enemyType] = p; // letzter gewinnt, ist ok
        }
    }

    public static void RemoveEnemy(Enemy enemy, Stats.eDeadBy deadBy, float delay = 0)
    {
        if (!Instance || !enemy) return;

        if (Instance.instantiatedEnemies.Contains(enemy))
        {
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
        // Fallbacks, falls Areas nicht gesetzt sind
        Collider2D area = (Random.value < 0.5f ? topArea : bottomArea) ?? topArea ?? bottomArea;
        if (!area) return Vector2.zero;

        Bounds b = area.bounds;

        // bis zu 100 Versuche: zufälliger Punkt innerhalb Collider-Form
        for (int i = 0; i < 100; i++)
        {
            Vector2 p = new Vector2(Random.Range(b.min.x, b.max.x), Random.Range(b.min.y, b.max.y));
            if (area.OverlapPoint(p)) return p;
        }

        // Notfall: Center
        return area.bounds.center;
    }

    EnemyWave GenerateProceduralWave(int waveIndex)
    {
        // repeats: alle ~7 Wellen steigt max um 1
        int maxRepeats = Mathf.RoundToInt(1 + waveIndex / 7f);

        // Anzahl SpawnInstructions in der Wave (Cap 50)
        int enemyCount = Mathf.Min(Mathf.RoundToInt(2 + waveIndex * 1.2f), 50);

        // Delay zwischen Typen / innerhalb Typ
        Vector2 typeDelay = new Vector2(0.4f, 0.8f);
        Vector2 spawnDelay = new Vector2(0.5f, 1f);

        // pro Instruction: 1..(2+wave)
        Vector2Int amountRange = new Vector2Int(1, 2 + waveIndex);

        var wave = new EnemyWave
        {
            enemies = new List<SpawnInstruction>(enemyCount),
            repeats = new Vector2Int(1, maxRepeats),
            delayBetweenSpawnsTypes = typeDelay
        };

        for (int i = 0; i < enemyCount; i++)
        {
            var type = GetEnemyTypeByWave(waveIndex);
            bool isSwarm = type == Enemy.eEnemyType.Swarm;

            // Swarm skaliert langsam (du willst ggf. später noch cap setzen)
            Vector2Int swarmSize = isSwarm
                ? new Vector2Int(3 + waveIndex / 4, 5 + waveIndex / 4)
                : Vector2Int.one;

            wave.enemies.Add(new SpawnInstruction
            {
                type = type,
                amount = amountRange,
                delayBetweenSpawns = spawnDelay,
                swarmGroupSize = swarmSize
            });
        }

        return wave;
    }

    Enemy.eEnemyType GetEnemyTypeByWave(int waveIndex)
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

        // Cache nach Reset neu bauen (falls Prefab-Liste verändert wurde)
        BuildPrefabCache();
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
    public Enemy.eEnemyType type;
    public Vector2Int amount = new Vector2Int(1, 2);
    public Vector2 delayBetweenSpawns = new Vector2(0.5f, 1f);
    public Vector2Int swarmGroupSize = new Vector2Int(1, 1);
}
