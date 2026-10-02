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
    [SerializeField] float timeAfterBossWave = 10f;
    [SerializeField] float bossArrivalDelay = 8f;
    [SerializeField] Transform backgroundGrid;
    [SerializeField] Shader gridDistortionShader;
    public int currentWaveIndex = 0;
    [SerializeField] List<EnemyWave> waves = new List<EnemyWave>();
    [SerializeField] float swarmSpawnRadius = 0.5f;

    [Header("Spawn Areas")]
    [Tooltip("Clearance between the camera edge and the entire ship, in world units.")]
    [SerializeField, Min(0f)] float spawnEdgeMargin = 0.2f;
    [Tooltip("Depth of the random spawn strip beyond that clearance.")]
    [SerializeField, Min(0f)] float spawnStripDepth = 0.25f;

    [Header("Runtime")]
    public List<Enemy> instantiatedEnemies = new List<Enemy>();

    bool waveIsRunning;
    bool bossTestActive;
    public bool IsBossTestActive => bossTestActive;
    public int DisplayWave => Mathf.Max(1, currentWaveIndex +
        ((waveIsRunning || instantiatedEnemies.Count == 0) ? 1 : 0));
    Transform spawnParent;
    Dictionary<Enemy.eEnemyType, GameObject> prefabByType;
    WaveWarningEffect waveWarning;

    // --- INIT SNAPSHOT ---
    bool initenableSpawning, initproceduralWave, initwaveIsRunning;
    float inittimeBetweenWaves, initswarmSpawnRadius;
    float inittimeAfterBossWave, initbossArrivalDelay;
    int initcurrentWaveIndex;


    void Awake() => Instance = this;

    void OnDestroy() => waveWarning?.Dispose();

    public void Init()
    {
        currentWaveIndex = 0;
        waveIsRunning = false;
        bossTestActive = false;

        var p = GameObject.Find("EnemyParent");
        spawnParent = p ? p.transform : transform;
        waveWarning ??= new WaveWarningEffect(transform, backgroundGrid, gridDistortionShader);

        BuildPrefabCache();
    }

    public void UpdateNormal()
    {
        UpdateInstantiatedEnemies();
        if (!waveIsRunning && instantiatedEnemies.Count == 0 &&
            !GameManager.gameOver && SaveGameManager.Instance.HasWaveCheckpoint)
        {
            SaveGameManager.Instance.ClearWaveCheckpoint();
            Stats.Instance.wavesCompleted++;
            ResourceManager.Instance.AwardWaveBonus(currentWaveIndex);
            MatchReporter.Current?.CompleteWave();
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
        if (waveIsRunning || instantiatedEnemies.Count > 0 || !enableSpawning || bossTestActive) return;

        StartCoroutine(SpawnWave());
    }

    IEnumerator SpawnWave()
    {
        waveIsRunning = true;

        float delay = currentWaveIndex > 0 && currentWaveIndex % 5 == 0 ? timeAfterBossWave : timeBetweenWaves;
        if (delay > 0f)
        {
            const float fadeInDuration = 2f;
            waveWarning?.Show(0f, 0f);
            float elapsed = 0f;
            while (elapsed < delay)
            {
                yield return null;
                if (GameManager.gameOver)
                {
                    waveWarning?.Hide();
                    waveIsRunning = false;
                    yield break;
                }
                elapsed += Time.deltaTime;
                float fade = Mathf.SmoothStep(0f, 1f, elapsed / fadeInDuration);
                waveWarning?.Show(Mathf.Clamp01(elapsed / delay), fade);
            }
            float finalVisibility = Mathf.SmoothStep(0f, 1f, elapsed / fadeInDuration);
            StartCoroutine(FadeOutWarning(finalVisibility));
        }
        else
        {
            yield return new WaitForSeconds(0f);
        }

        bool hasAuthoredWave = currentWaveIndex < waves.Count;
        if (!hasAuthoredWave && !proceduralWave)
        {
            enableSpawning = false;
            waveIsRunning = false;
            yield break;
        }

        if (!Camera.main || !Camera.main.orthographic)
        {
            Debug.LogError("Enemy spawning requires an orthographic Main Camera.");
            enableSpawning = false;
            waveIsRunning = false;
            yield break;
        }

        SaveGameManager.Instance.CaptureWaveCheckpoint();
        EnemyWave wave = hasAuthoredWave ? waves[currentWaveIndex] : GenerateProceduralWave(currentWaveIndex);
        MatchReporter.Current?.BeginWave(currentWaveIndex + 1, hasAuthoredWave);

        foreach (var instr in wave.enemies)
        {
            // Bosses are always appended after the regular wave, including authored waves.
            if (instr.type == Enemy.eEnemyType.Boss) continue;
            int amount = Random.Range(instr.amount.x, instr.amount.y + 1);

            for (int i = 0; i < amount; i++)
            {
                if (GameManager.gameOver)
                {
                    waveIsRunning = false;
                    yield break;
                }
                if (!prefabByType.TryGetValue(instr.type, out var prefab) || !prefab)
                {
                    Debug.LogWarning($"Missing enemy prefab for type {instr.type}");
                    continue;
                }

                int groupSize = Random.Range(instr.swarmGroupSize.x, instr.swarmGroupSize.y + 1);
                bool fromTop = Random.value < 0.5f;
                Bounds view = GetCameraBounds(Camera.main);
                Vector2 basePos = new Vector2(Random.Range(view.min.x, view.max.x),
                    fromTop ? view.max.y : view.min.y);
                float groupRadius = groupSize > 1 ? Mathf.Max(0f, swarmSpawnRadius) : 0f;
                float stripOffset = Random.Range(0f, Mathf.Max(0f, spawnStripDepth));

                for (int s = 0; s < groupSize; s++)
                {
                    Vector2 offset = groupSize > 1 ? Random.insideUnitCircle * groupRadius : Vector2.zero;
                    var go = Instantiate(prefab, basePos, Quaternion.identity, spawnParent);
                    var enemy = go.GetComponent<Enemy>();
                    // Awake has selected the actual sprite and variant scale before measuring it.
                    PlaceOutsideView(enemy, view, basePos.x, fromTop, offset, groupRadius, stripOffset);
                    enemy.InitWithLevel(currentWaveIndex);

                    RegisterEnemy(enemy);
                }

                yield return new WaitForSeconds(Random.Range(instr.delayBetweenSpawns.x, instr.delayBetweenSpawns.y));
            }

            yield return new WaitForSeconds(Random.Range(wave.delayBetweenSpawnsTypes.x, wave.delayBetweenSpawnsTypes.y));
        }

        int waveNumber = currentWaveIndex + 1;
        if (GameManager.gameOver)
        {
            waveIsRunning = false;
            yield break;
        }
        if (waveNumber % 5 == 0 && prefabByType.ContainsKey(Enemy.eEnemyType.Boss))
        {
            float elapsed = 0f;
            while (elapsed < bossArrivalDelay)
            {
                yield return null;
                if (GameManager.gameOver)
                {
                    waveIsRunning = false;
                    yield break;
                }
                elapsed += Time.deltaTime;
            }
            SpawnBosses(waveNumber);
        }

        currentWaveIndex++;
        waveIsRunning = false;
    }

    void SpawnBosses(int waveNumber)
    {
        var bossPrefab = prefabByType[Enemy.eEnemyType.Boss];
        int bossCount = waveNumber >= 25 ? 2 : 1;
        for (int i = 0; i < bossCount; i++)
        {
            Bounds view = GetCameraBounds(Camera.main);
            bool fromTop = bossCount == 1 ? Random.value < 0.5f : i == 0;
            var boss = Instantiate(bossPrefab, view.center, Quaternion.identity, spawnParent).GetComponent<Enemy>();
            PlaceOutsideView(boss, view, Tower.Instance.transform.position.x, fromTop, Vector2.zero, 0f, 0f);
            boss.InitWithLevel(waveNumber - 1);
            RegisterEnemy(boss);
        }
    }

    public void StartBossTest(int waveNumber, int materials)
    {
        var controls = GetComponent<BossTestControls>();
        if (!controls || !controls.enableBossTesting || !GameManager.isInit || GameManager.gameOver ||
            Time.timeScale <= 0f || !Camera.main || !Camera.main.orthographic ||
            !prefabByType.ContainsKey(Enemy.eEnemyType.Boss)) return;

        SaveGameManager.Instance.BeginBossTestSession();
        StopAllCoroutines();
        waveWarning?.Hide();
        RemoveAllEnemies();
        ProjectileManager.Instance.RemoveAllProjectiles();
        currentWaveIndex = Mathf.Max(5, Mathf.CeilToInt(waveNumber / 5f) * 5) - 1;
        if (!bossTestActive)
        {
            bossTestActive = true;
            waveIsRunning = false;
            ResourceManager.Instance.curMaterials = Mathf.Max(ResourceManager.Instance.curMaterials, materials);
            SaveGameManager.Instance.Save();
            ResourceManager.Instance.RefreshUI();
            return;
        }
        bossTestActive = true;
        waveIsRunning = true;
        if (UIManager.Instance.stationUI.activeSelf) UIManager.Instance.Show(false);
        SaveGameManager.Instance.CaptureWaveCheckpoint();
        MatchReporter.Current?.BeginWave(currentWaveIndex + 1, false);
        SpawnBosses(currentWaveIndex + 1);
        currentWaveIndex++;
        waveIsRunning = false;
        ResourceManager.Instance.RefreshUI();
    }

    IEnumerator FadeOutWarning(float initialVisibility)
    {
        const float duration = 2.2f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            yield return null;
            if (GameManager.gameOver) break;
            elapsed += Time.deltaTime;
            waveWarning?.ShowExit(elapsed, initialVisibility);
        }
        waveWarning?.Hide();
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

    void RegisterEnemy(Enemy enemy)
    {
        instantiatedEnemies.Add(enemy);
        Stats.Instance.enemiesSpawned++;
        MatchReporter.Event("enemy_spawned", enemy.GetInstanceID().ToString(), enemy.enemyType.ToString(),
            enemy.maxHP, enemy.speed, $"damage={enemy.damage}; fireRate={enemy.fireRate}; fireRange={enemy.fireRange}", enemy.transform.position);
    }

    public Enemy SpawnCarrierSwarm(Vector2 position, int waveNumber)
    {
        if (GameManager.gameOver || !prefabByType.TryGetValue(Enemy.eEnemyType.Swarm, out var prefab)) return null;
        var enemy = Instantiate(prefab, position, Quaternion.identity, spawnParent).GetComponent<Enemy>();
        enemy.InitWithLevel(waveNumber - 1);
        RegisterEnemy(enemy);
        return enemy;
    }

    public static void RemoveEnemy(Enemy enemy, Stats.eDeadBy deadBy, float delay = 0)
    {
        if (!Instance || !enemy) return;

        if (Instance.instantiatedEnemies.Contains(enemy))
        {
            MatchReporter.Event("enemy_removed", enemy.GetInstanceID().ToString(), enemy.enemyType.ToString(),
                detail: deadBy.ToString(), position: enemy.transform.position);
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

    static Bounds GetCameraBounds(Camera camera)
    {
        Vector3 min = camera.ViewportToWorldPoint(new Vector3(0f, 0f, camera.nearClipPlane));
        Vector3 max = camera.ViewportToWorldPoint(new Vector3(1f, 1f, camera.nearClipPlane));
        return new Bounds((min + max) * 0.5f, new Vector3(max.x - min.x, max.y - min.y, 0f));
    }

    void PlaceOutsideView(Enemy enemy, Bounds view, float entryX, bool fromTop,
        Vector2 groupOffset, float groupRadius, float stripOffset)
    {
        float radius = 0.1f;
        foreach (var renderer in enemy.GetComponentsInChildren<SpriteRenderer>())
        {
            Bounds bounds = renderer.bounds;
            radius = Mathf.Max(radius, Vector2.Distance(bounds.center, enemy.transform.position) +
                ((Vector2)bounds.extents).magnitude);
        }
        foreach (var collider in enemy.GetComponentsInChildren<Collider2D>())
        {
            Bounds bounds = collider.bounds;
            radius = Mathf.Max(radius, Vector2.Distance(bounds.center, enemy.transform.position) +
                ((Vector2)bounds.extents).magnitude);
        }

        // An enclosing radius keeps every variant off-screen even while it turns towards the core.
        float halfWidth = Mathf.Max(0f, view.extents.x - radius);
        float x = Mathf.Clamp(entryX + groupOffset.x, view.center.x - halfWidth, view.center.x + halfWidth);
        float distance = view.extents.y + radius + Mathf.Max(0f, spawnEdgeMargin) +
            groupRadius + stripOffset;
        float y = view.center.y + (fromTop ? distance : -distance) + groupOffset.y;
        enemy.transform.position = new Vector3(x, y, 0f);

        var core = StationModule.GetModuleByType(StationModule.eModuleType.Core);
        Vector2 direction = (core ? (Vector2)core.transform.position : (Vector2)view.center) - new Vector2(x, y);
        enemy.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
    }

    void OnDrawGizmos()
    {
        var camera = Camera.main;
        if (!camera || !camera.orthographic) return;
        Bounds view = GetCameraBounds(camera);
        float depth = Mathf.Max(0.01f, spawnStripDepth);
        Gizmos.color = new Color(1f, 0.35f, 0.2f, 0.8f);
        for (int side = -1; side <= 1; side += 2)
        {
            Vector3 center = new Vector3(view.center.x,
                view.center.y + side * (view.extents.y + Mathf.Max(0f, spawnEdgeMargin) + depth * 0.5f), 0f);
            Gizmos.DrawWireCube(center, new Vector3(view.size.x, depth, 0f));
        }
    }

    EnemyWave GenerateProceduralWave(int waveIndex)
    {
        int remaining = Mathf.Clamp(2 + waveIndex * 2, 2, 50);
        if ((waveIndex + 1) % 5 == 0) remaining -= waveIndex >= 24 ? 2 : 1;

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

        if ((waveIndex + 1) % 5 == 0)
        {
            int count = waveIndex >= 24 ? 2 : 1;
            wave.enemies.Add(new SpawnInstruction { type = Enemy.eEnemyType.Boss,
                amount = new Vector2Int(count, count), delayBetweenSpawns = Vector2.zero,
                swarmGroupSize = Vector2Int.one });
        }

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
        inittimeAfterBossWave = timeAfterBossWave;
        initbossArrivalDelay = bossArrivalDelay;
        initcurrentWaveIndex = currentWaveIndex;
        initswarmSpawnRadius = swarmSpawnRadius;
        initwaveIsRunning = waveIsRunning;
    }

    public void ResetScript()
    {
        bossTestActive = false;
        StopAllCoroutines();
        waveWarning?.Hide();
        RemoveAllEnemies();
        instantiatedEnemies.Clear();

        enableSpawning = initenableSpawning;
        proceduralWave = initproceduralWave;
        timeBetweenWaves = inittimeBetweenWaves;
        timeAfterBossWave = inittimeAfterBossWave;
        bossArrivalDelay = initbossArrivalDelay;
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
