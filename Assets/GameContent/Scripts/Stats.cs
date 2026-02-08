using System.Collections.Generic;
using UnityEngine;

public class Stats : MonoBehaviour, IResettable
{
    public static Stats Instance;

    public enum eDeadBy
    {
        None,
        towerProjectile,
        droneProjectile,
        deflectedProjectile,
        shieldCollision,
        stationCollision,
        enemyCollision,
        enemyProjectile,
        droneCollision,
    }

    public enum eCollectBy
    {
        None,
        manual,
        automatic
    }

    public float playTime;

    public int wavesCompleted;
    public int enemiesSpawned;

    public int towerProjectilesFired;
    public int towerProjectilesHit;

    public int droneProjectilesFired;
    public int droneProjectilesHit;

    public int deflectedProjectilesFired;
    public int deflectedProjectilesHit;

    public int enemyProjectilesFired;
    public int enemyProjectilesHit;

    public int resourcesSpawned;
    public int resourcesCollectedManually;
    public int resourcesCollectedAutomatically;

    public int modulesBuilt;
    public int modulesCost;
    public int modulesDestroyed;
    public int modulesDamageTaken;
    public int shieldDamageTaken;

    public int boughtUpgrades;
    public int totalUpgradeCosts;

    public int dronesBuilt;
    public int dronesDestroyed;

    public Dictionary<(Enemy.eEnemyType, eDeadBy), int> killsByTypeAndCause = new();


    // --- INIT SNAPSHOT ---
    private float initplayTime;

    private int initwavesCompleted;
    private int initenemiesSpawned;

    private int inittowerProjectilesFired;
    private int inittowerProjectilesHit;

    private int initdroneProjectilesFired;
    private int initdroneProjectilesHit;

    private int initdeflectedProjectilesFired;
    private int initdeflectedProjectilesHit;

    private int initenemyProjectilesFired;
    private int initenemyProjectilesHit;

    private int initresourcesSpawned;
    private int initresourcesCollectedManually;
    private int initresourcesCollectedAutomatically;

    private int initmodulesBuilt;
    private int initmodulesCost;
    private int initmodulesDestroyed;
    private int initmodulesDamageTaken;
    private int initshieldDamageTaken;

    private int initboughtUpgrades;
    private int inittotalUpgradeCosts;

    private int initdronesBuilt;
    private int initdronesDestroyed;



    void Awake()
    {
        Instance = this;
    }

    public void RegisterKill(Enemy.eEnemyType type, eDeadBy cause)
    {
        var key = (type, cause);
        if (!killsByTypeAndCause.ContainsKey(key))
            killsByTypeAndCause[key] = 0;

        killsByTypeAndCause[key]++;
    }

    public void AddUpgrade(int cost)
    {
        boughtUpgrades++;
        totalUpgradeCosts += cost;
    }

    public void AddCollectResource(int amount, eCollectBy collectBy)
    {
        if (collectBy == eCollectBy.manual)
            resourcesCollectedManually += amount;
        else
            resourcesCollectedAutomatically += amount;
    }

    public string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60);
        int secs = Mathf.FloorToInt(seconds % 60);
        return $"{minutes:D2}:{secs:D2}";
    }

    public int GetTotalKills()
    {
        int sum = 0;
        foreach (var entry in Instance.killsByTypeAndCause)
            sum += entry.Value;
        return sum;
    }

    public void ResetStats()
    {
        wavesCompleted = 0;
        playTime = 0;
        enemiesSpawned = 0;

        towerProjectilesFired = 0;
        towerProjectilesHit = 0;
        droneProjectilesFired = 0;
        droneProjectilesHit = 0;
        enemyProjectilesFired = 0;
        enemyProjectilesHit = 0;
        deflectedProjectilesFired = 0;
        deflectedProjectilesHit = 0;

        resourcesCollectedManually = 0;
        resourcesCollectedAutomatically = 0;

        modulesBuilt = 0;
        modulesDestroyed = 0;
        modulesDamageTaken = 0;
        shieldDamageTaken = 0;

        boughtUpgrades = 0;
        totalUpgradeCosts = 0;

        dronesBuilt = 0;
        dronesDestroyed = 0;

        killsByTypeAndCause.Clear();
    }


    // in Stats.cs (innerhalb class Stats)
    public StatsData GetStatsData()
    {
        return new StatsData
        {
            playTime = playTime,
            wavesCompleted = wavesCompleted,
            enemiesSpawned = enemiesSpawned,
            towerProjectilesFired = towerProjectilesFired,
            towerProjectilesHit = towerProjectilesHit,
            droneProjectilesFired = droneProjectilesFired,
            droneProjectilesHit = droneProjectilesHit,
            enemyProjectilesFired = enemyProjectilesFired,
            enemyProjectilesHit = enemyProjectilesHit,
            deflectedProjectilesFired = deflectedProjectilesFired,
            deflectedProjectilesHit = deflectedProjectilesHit,
            resourcesCollectedManually = resourcesCollectedManually,
            resourcesCollectedAutomatically = resourcesCollectedAutomatically,
            modulesBuilt = modulesBuilt,
            modulesDestroyed = modulesDestroyed,
            modulesDamageTaken = modulesDamageTaken,
            shieldDamageTaken = shieldDamageTaken,
            boughtUpgrades = boughtUpgrades,
            totalUpgradeCosts = totalUpgradeCosts,
            dronesBuilt = dronesBuilt,
            dronesDestroyed = dronesDestroyed
        };
    }

    public void ApplyStatsData(StatsData d)
    {
        if (d == null) { ResetStats(); return; }

        playTime = d.playTime;
        wavesCompleted = d.wavesCompleted;
        enemiesSpawned = d.enemiesSpawned;
        towerProjectilesFired = d.towerProjectilesFired;
        towerProjectilesHit = d.towerProjectilesHit;
        droneProjectilesFired = d.droneProjectilesFired;
        droneProjectilesHit = d.droneProjectilesHit;
        enemyProjectilesFired = d.enemyProjectilesFired;
        enemyProjectilesHit = d.enemyProjectilesHit;
        deflectedProjectilesFired = d.deflectedProjectilesFired;
        deflectedProjectilesHit = d.deflectedProjectilesHit;
        resourcesCollectedManually = d.resourcesCollectedManually;
        resourcesCollectedAutomatically = d.resourcesCollectedAutomatically;
        modulesBuilt = d.modulesBuilt;
        modulesDestroyed = d.modulesDestroyed;
        modulesDamageTaken = d.modulesDamageTaken;
        shieldDamageTaken = d.shieldDamageTaken;
        boughtUpgrades = d.boughtUpgrades;
        totalUpgradeCosts = d.totalUpgradeCosts;
        dronesBuilt = d.dronesBuilt;
        dronesDestroyed = d.dronesDestroyed;

        // Dictionary ist nicht JsonUtility-serialisierbar -> runtime-only
        killsByTypeAndCause.Clear();
    }

    public void StoreInit()
    {
        initplayTime = playTime;

        initwavesCompleted = wavesCompleted;
        initenemiesSpawned = enemiesSpawned;

        inittowerProjectilesFired = towerProjectilesFired;
        inittowerProjectilesHit = towerProjectilesHit;

        initdroneProjectilesFired = droneProjectilesFired;
        initdroneProjectilesHit = droneProjectilesHit;

        initdeflectedProjectilesFired = deflectedProjectilesFired;
        initdeflectedProjectilesHit = deflectedProjectilesHit;

        initenemyProjectilesFired = enemyProjectilesFired;
        initenemyProjectilesHit = enemyProjectilesHit;

        initresourcesSpawned = resourcesSpawned;
        initresourcesCollectedManually = resourcesCollectedManually;
        initresourcesCollectedAutomatically = resourcesCollectedAutomatically;

        initmodulesBuilt = modulesBuilt;
        initmodulesCost = modulesCost;
        initmodulesDestroyed = modulesDestroyed;
        initmodulesDamageTaken = modulesDamageTaken;
        initshieldDamageTaken = shieldDamageTaken;

        initboughtUpgrades = boughtUpgrades;
        inittotalUpgradeCosts = totalUpgradeCosts;

        initdronesBuilt = dronesBuilt;
        initdronesDestroyed = dronesDestroyed;
    }

    public void ResetScript()
    {
        playTime = initplayTime;

        wavesCompleted = initwavesCompleted;
        enemiesSpawned = initenemiesSpawned;

        towerProjectilesFired = inittowerProjectilesFired;
        towerProjectilesHit = inittowerProjectilesHit;

        droneProjectilesFired = initdroneProjectilesFired;
        droneProjectilesHit = initdroneProjectilesHit;

        deflectedProjectilesFired = initdeflectedProjectilesFired;
        deflectedProjectilesHit = initdeflectedProjectilesHit;

        enemyProjectilesFired = initenemyProjectilesFired;
        enemyProjectilesHit = initenemyProjectilesHit;

        resourcesSpawned = initresourcesSpawned;
        resourcesCollectedManually = initresourcesCollectedManually;
        resourcesCollectedAutomatically = initresourcesCollectedAutomatically;

        modulesBuilt = initmodulesBuilt;
        modulesCost = initmodulesCost;
        modulesDestroyed = initmodulesDestroyed;
        modulesDamageTaken = initmodulesDamageTaken;
        shieldDamageTaken = initshieldDamageTaken;

        boughtUpgrades = initboughtUpgrades;
        totalUpgradeCosts = inittotalUpgradeCosts;

        dronesBuilt = initdronesBuilt;
        dronesDestroyed = initdronesDestroyed;

        killsByTypeAndCause.Clear(); // runtime-only
    }
}

[System.Serializable]
public class StatsData
{
    public float playTime;
    public int wavesCompleted;
    public int enemiesSpawned;
    public int towerProjectilesFired;
    public int towerProjectilesHit;
    public int droneProjectilesFired;
    public int droneProjectilesHit;
    public int enemyProjectilesFired;
    public int enemyProjectilesHit;
    public int deflectedProjectilesFired;
    public int deflectedProjectilesHit;
    public int resourcesCollectedManually;
    public int resourcesCollectedAutomatically;
    public int modulesBuilt;
    public int modulesDestroyed;
    public int modulesDamageTaken;
    public int shieldDamageTaken;
    public int boughtUpgrades;
    public int totalUpgradeCosts;
    public int dronesBuilt;
    public int dronesDestroyed;
}