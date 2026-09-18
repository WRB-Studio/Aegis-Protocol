#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class GameFlowTests
{
    string saveDirectory;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        GameManager.isInit = false;
        GameManager.gameOver = false;
        Time.timeScale = 1f;

        if (SaveGameManager.Instance)
        {
            UnityEngine.Object.Destroy(SaveGameManager.Instance.gameObject);
            yield return null;
        }

        StationModule.allModules.Clear();
        UpgradeAttribute.allUpgradeAttributes.Clear();
        UpgradeSet.allUpgradeSets.Clear();

        saveDirectory = Path.Combine(Path.GetTempPath(), "aegis-flow-test-" + Guid.NewGuid());
        Directory.CreateDirectory(saveDirectory);
        SaveGameManager.SaveDirectoryOverride = saveDirectory;

        SceneManager.LoadScene("MainScene");
        yield return null;

        Assert.That(GameManager.isInit, Is.True);
        GameManager.isInit = false;
        SaveGameManager.Instance.ClearWaveCheckpoint();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        GameManager.isInit = false;
        GameManager.gameOver = false;
        Time.timeScale = 1f;

        if (SaveGameManager.Instance)
            UnityEngine.Object.Destroy(SaveGameManager.Instance.gameObject);
        yield return null;

        SaveGameManager.SaveDirectoryOverride = null;
        if (Directory.Exists(saveDirectory)) Directory.Delete(saveDirectory, true);
    }

    [UnityTest]
    public IEnumerator LoadingRestoresWorldWithoutCountingDronesAgain()
    {
        var resources = ResourceManager.Instance;
        var core = StationModule.GetModuleByType(StationModule.eModuleType.Core);
        var droneModule = StationModule.GetModuleByType(StationModule.eModuleType.Drone);
        var drones = DroneManager.Instance;
        var command = StationModule.GetModuleByType(StationModule.eModuleType.CommandUnit);
        command.isBuilt = true;
        command.gameObject.SetActive(true);
        var integrity = UpgradeAttribute.GetUpgradeByName(UpgradeAttribute.eUpgradeName.StructuralIntegrity);
        integrity.level = 1;
        integrity.RecalculateFromLevel();
        UpgradeAttribute.ApplyAllUpgradeEffect();
        int upgradedCoreMaxHP = core.maxHP;
        int damagedCoreHP = core.maxHP - 2;
        int damagedDroneModuleHP = droneModule.maxHP - 1;

        resources.curMaterials = 137;
        core.currentHP = damagedCoreHP;
        droneModule.isBuilt = true;
        droneModule.gameObject.SetActive(true);
        droneModule.currentHP = damagedDroneModuleHP;
        drones.AfterModulInit();
        var droneObject = drones.SpawnDrone(true);
        Assert.That(droneObject, Is.Not.Null);
        droneObject.GetComponent<Drone>().currentHP = 2;
        Stats.Instance.dronesBuilt = 7;
        Stats.Instance.resourcesSpawned = 19;
        Stats.Instance.resourcesCollectedManually = 23;
        Stats.Instance.modulesCost = 44;
        Stats.Instance.RegisterKill(Enemy.eEnemyType.Normal, Stats.eDeadBy.towerProjectile);
        float playTime = Stats.Instance.playTime;
        int score = ScoreManager.Instance.GetBreakdown(Stats.Instance).totalScore;
        SaveGameManager.Instance.Save();

        yield return ReloadMainScene();

        Assert.That(ResourceManager.Instance.curMaterials, Is.EqualTo(137));
        Assert.That(StationModule.GetModuleByType(StationModule.eModuleType.Core).currentHP,
            Is.EqualTo(damagedCoreHP));
        Assert.That(StationModule.GetModuleByType(StationModule.eModuleType.Core).maxHP,
            Is.EqualTo(upgradedCoreMaxHP));
        var restoredModule = StationModule.GetModuleByType(StationModule.eModuleType.Drone);
        Assert.That(restoredModule.isBuilt, Is.True);
        Assert.That(restoredModule.currentHP, Is.EqualTo(damagedDroneModuleHP));
        Assert.That(DroneManager.Instance.allDrones, Has.Count.EqualTo(1));
        Assert.That(DroneManager.Instance.allDrones[0].currentHP, Is.EqualTo(2));
        Assert.That(Stats.Instance.dronesBuilt, Is.EqualTo(7));
        Assert.That(Stats.Instance.resourcesSpawned, Is.EqualTo(19));
        Assert.That(Stats.Instance.resourcesCollectedManually, Is.EqualTo(23));
        Assert.That(Stats.Instance.modulesCost, Is.EqualTo(44));
        Assert.That(Stats.Instance.GetTotalKills(), Is.EqualTo(1));
        Assert.That(Stats.Instance.playTime, Is.EqualTo(playTime).Within(0.1f));
        Stats.Instance.playTime = playTime;
        Assert.That(ScoreManager.Instance.GetBreakdown(Stats.Instance).totalScore, Is.EqualTo(score));
    }

    [Test]
    public void ProceduralWaves_RespectActualEnemyBudgetAndExplicitBossWaves()
    {
        var generate = typeof(EnemySpawner).GetMethod("GenerateProceduralWave",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(generate, Is.Not.Null);
        foreach (int waveIndex in new[] { 0, 9, 18, 19, 29, 49, 99 })
        {
            var wave = (EnemyWave)generate.Invoke(EnemySpawner.Instance, new object[] { waveIndex });
            int count = wave.enemies.Sum(instruction => instruction.amount.x * instruction.swarmGroupSize.x);
            Assert.That(count, Is.InRange(2, 50), $"Wave {waveIndex + 1}");
            int bosses = wave.enemies.Where(instruction => instruction.type == Enemy.eEnemyType.Boss)
                .Sum(instruction => instruction.amount.x * instruction.swarmGroupSize.x);
            Assert.That(bosses, Is.EqualTo((waveIndex + 1) % 10 == 0 && waveIndex >= 19 ? 1 : 0));
        }
    }

    [UnityTest]
    public IEnumerator DroneUpgrades_UpdateExistingDronesAndPreserveDamageTaken()
    {
        var module = StationModule.GetModuleByType(StationModule.eModuleType.Drone);
        module.isBuilt = true;
        module.gameObject.SetActive(true);
        var manager = DroneManager.Instance;
        manager.AfterModulInit();
        var drone = manager.SpawnDrone(true).GetComponent<Drone>();
        int originalHP = drone.maxHP;
        drone.currentHP = originalHP - 1;

        var hp = UpgradeAttribute.GetUpgradeByName(UpgradeAttribute.eUpgradeName.DroneHP);
        hp.level = 1;
        hp.RecalculateFromLevel();
        hp.ApplyUpgradeEffect();
        Assert.That(drone.maxHP, Is.EqualTo(manager.droneInitialHP));
        Assert.That(drone.currentHP, Is.EqualTo(drone.maxHP - 1));

        var damage = UpgradeAttribute.GetUpgradeByName(UpgradeAttribute.eUpgradeName.DroneDamage);
        damage.level = 1;
        damage.RecalculateFromLevel();
        damage.ApplyUpgradeEffect();
        Assert.That(drone.damage, Is.EqualTo(manager.droneInitialDamage));
        yield break;
    }

    [UnityTest]
    public IEnumerator HeavyEnemyCollision_ConsumesShieldPointsByEnemyHealth()
    {
        var module = StationModule.GetModuleByType(StationModule.eModuleType.Shield);
        module.isBuilt = true;
        module.gameObject.SetActive(true);
        Shield.Instance.OnModuleBuilt();
        var enemyObject = new GameObject("Heavy enemy test");
        enemyObject.tag = "Enemy";
        var collider = enemyObject.AddComponent<CircleCollider2D>();
        var enemy = enemyObject.AddComponent<Enemy>();
        enemy.maxHP = 3;
        EnemySpawner.Instance.instantiatedEnemies.Add(enemy);
        float before = Shield.Instance.currentShieldPoints;
        var hit = typeof(Shield).GetMethod("OnTriggerEnter2D", BindingFlags.Instance | BindingFlags.NonPublic);
        hit.Invoke(Shield.Instance, new object[] { collider });
        Assert.That(Shield.Instance.currentShieldPoints, Is.EqualTo(Mathf.Max(0f, before - 3f)));
        UnityEngine.Object.Destroy(enemyObject);
        yield break;
    }

    [UnityTest]
    public IEnumerator PauseMenu_StopsAndRestoresGameSpeed()
    {
        GameManager.isInit = true;
        var menu = UnityEngine.Object.FindFirstObjectByType<PauseMenu>();
        Assert.That(menu, Is.Not.Null);
        var toggle = typeof(PauseMenu).GetMethod("TogglePause", BindingFlags.Instance | BindingFlags.NonPublic);
        toggle.Invoke(menu, null);
        Assert.That(Time.timeScale, Is.Zero);
        toggle.Invoke(menu, null);
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        GameManager.isInit = false;
        yield break;
    }

    [UnityTest]
    public IEnumerator UpgradeButtons_RemainReadableWhenUnselectedAndSelected()
    {
        var module = StationModule.GetModuleByType(StationModule.eModuleType.AmmoFabricator);
        module.isBuilt = true;
        module.gameObject.SetActive(true);
        ResourceManager.Instance.curMaterials = 0;
        UpgradeUI.Instance.Show(module.upgradeSet);

        var button = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(
            FindObjectsInactive.Include, FindObjectsSortMode.None)
            .First(item => item.gameObject.name == "btnUpgrade(Clone)");
        Assert.That(button.interactable, Is.True);
        Assert.That(button.image.color.grayscale, Is.LessThan(0.5f));

        button.onClick.Invoke();
        Assert.That(button.image.color.grayscale, Is.LessThan(0.5f));
        yield break;
    }

    [UnityTest]
    public IEnumerator ReplayRestoresInitialRunThreeTimes()
    {
        var resources = ResourceManager.Instance;
        var core = StationModule.GetModuleByType(StationModule.eModuleType.Core);
        var extractor = StationModule.GetModuleByType(StationModule.eModuleType.Extractor);
        var damage = UpgradeAttribute.GetUpgradeByName(UpgradeAttribute.eUpgradeName.Damage);
        int initialMaterials = resources.curMaterials;
        int initialLevel = damage.level;
        int initialCost = extractor.cost;

        for (int i = 0; i < 3; i++)
        {
            resources.curMaterials += 200;
            core.currentHP -= 2;
            extractor.isBuilt = true;
            extractor.cost += 5;
            damage.level++;
            damage.RecalculateFromLevel();
            Stats.Instance.modulesBuilt = 3;
            GameManager.gameOver = true;

            GameManager.Instance.Replay();

            Assert.That(resources.curMaterials, Is.EqualTo(initialMaterials));
            Assert.That(core.currentHP, Is.EqualTo(core.maxHP));
            Assert.That(extractor.isBuilt, Is.False);
            Assert.That(extractor.cost, Is.EqualTo(initialCost));
            Assert.That(UpgradeAttribute.GetUpgradeByName(UpgradeAttribute.eUpgradeName.Damage).level,
                Is.EqualTo(initialLevel));
            Assert.That(Stats.Instance.modulesBuilt, Is.Zero);
            Assert.That(GameManager.gameOver, Is.False);
            GameManager.isInit = false;
        }

        yield break;
    }

    [UnityTest]
    public IEnumerator ExtractorLossAndRebuildRestoreItsEffect()
    {
        var extractor = StationModule.GetModuleByType(StationModule.eModuleType.Extractor);
        var efficiency = UpgradeAttribute.GetUpgradeByName(UpgradeAttribute.eUpgradeName.CollectingEfficiency);
        var autoCollecting = UpgradeAttribute.GetUpgradeByName(UpgradeAttribute.eUpgradeName.AutoCollecting);
        var core = StationModule.GetModuleByType(StationModule.eModuleType.Core);

        extractor.isBuilt = true;
        extractor.gameObject.SetActive(true);
        efficiency.level = Math.Min(1, efficiency.maxLevel);
        efficiency.RecalculateFromLevel();
        efficiency.ApplyUpgradeEffect();
        autoCollecting.level = 1;
        autoCollecting.ApplyUpgradeEffect();
        Assert.That(ResourceManager.Instance.autoCollecting, Is.True);
        float expectedEfficiency = efficiency.currentValue;
        core.currentHP = core.maxHP - 2;

        extractor.Die();
        Assert.That(extractor.isBuilt, Is.False);
        Assert.That(ResourceManager.Instance.autoCollecting, Is.False);

        ResourceManager.Instance.curMaterials = extractor.cost + 100;
        ModulesUI.Instance.SelectModule(StationModule.eModuleType.Extractor);
        ModulesUI.Instance.BuySelectedModule();

        Assert.That(extractor.isBuilt, Is.True);
        Assert.That(ResourceManager.Instance.autoCollecting, Is.True);
        Assert.That(efficiency.currentValue, Is.EqualTo(expectedEfficiency));
        Assert.That(core.currentHP, Is.EqualTo(core.maxHP - 2));
        yield break;
    }

    [UnityTest]
    public IEnumerator ModulePurchaseAndRepairChargeAndSaveOnce()
    {
        var extractor = StationModule.GetModuleByType(StationModule.eModuleType.Extractor);
        var resources = ResourceManager.Instance;
        var stats = Stats.Instance;
        ModulesUI.Instance.SelectModule(StationModule.eModuleType.Extractor);

        resources.curMaterials = extractor.cost - 1;
        ModulesUI.Instance.BuySelectedModule();
        Assert.That(extractor.isBuilt, Is.False);
        Assert.That(resources.curMaterials, Is.EqualTo(extractor.cost - 1));
        Assert.That(stats.modulesBuilt, Is.Zero);

        resources.curMaterials = extractor.cost + 30;
        int price = extractor.cost;
        ModulesUI.Instance.BuySelectedModule();
        Assert.That(extractor.isBuilt, Is.True);
        Assert.That(resources.curMaterials, Is.EqualTo(30));
        Assert.That(stats.modulesBuilt, Is.EqualTo(1));
        Assert.That(stats.modulesCost, Is.EqualTo(price));

        ModulesUI.Instance.BuySelectedModule();
        Assert.That(resources.curMaterials, Is.EqualTo(30));
        Assert.That(stats.modulesBuilt, Is.EqualTo(1));

        extractor.currentHP = extractor.maxHP - 2;
        int repairPrice = extractor.GetModuleRepairCost();
        resources.curMaterials = repairPrice - 1;
        ModulesUI.Instance.RepairSelectedModule();
        Assert.That(extractor.currentHP, Is.EqualTo(extractor.maxHP - 2));
        Assert.That(resources.curMaterials, Is.EqualTo(repairPrice - 1));

        resources.curMaterials = repairPrice + 10;
        ModulesUI.Instance.RepairSelectedModule();
        Assert.That(extractor.currentHP, Is.EqualTo(extractor.maxHP));
        Assert.That(resources.curMaterials, Is.EqualTo(10));

        var saved = JsonUtility.FromJson<SaveGame>(
            File.ReadAllText(Path.Combine(saveDirectory, "savegame.json")));
        Assert.That(saved.material, Is.EqualTo(10));
        Assert.That(saved.modules.Single(m => m.moduleType == "Extractor").currentHP,
            Is.EqualTo(extractor.maxHP));
        yield break;
    }

    [UnityTest]
    public IEnumerator UpgradePurchaseChargesOnlyOnSuccess()
    {
        var upgrade = UpgradeAttribute.GetUpgradeByName(UpgradeAttribute.eUpgradeName.Damage);
        var owner = StationModule.allModules.Single(m =>
            m.upgradeSet && m.upgradeSet.upgradeAttributes.Contains(upgrade));
        var resources = ResourceManager.Instance;
        var stats = Stats.Instance;
        int initialLevel = upgrade.level;
        int price = Mathf.RoundToInt(upgrade.cost);
        Assert.That(upgrade.maxLevel, Is.GreaterThan(initialLevel));

        owner.isBuilt = true;
        owner.gameObject.SetActive(true);
        ModulesUI.Instance.SelectModule(owner.moduleType);
        UpgradeUI.Instance.Show(owner.upgradeSet);
        UpgradeUI.Instance.currentSelectedUpgrade = upgrade.upgradeName;
        var click = typeof(UpgradeUI).GetMethod("OnUpgradeClicked",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(click, Is.Not.Null);

        resources.curMaterials = price - 1;
        click.Invoke(UpgradeUI.Instance, new object[] { upgrade });
        Assert.That(upgrade.level, Is.EqualTo(initialLevel));
        Assert.That(stats.boughtUpgrades, Is.Zero);
        Assert.That(resources.curMaterials, Is.EqualTo(price - 1));

        resources.curMaterials = price + 10;
        click.Invoke(UpgradeUI.Instance, new object[] { upgrade });
        Assert.That(upgrade.level, Is.EqualTo(initialLevel + 1));
        Assert.That(stats.boughtUpgrades, Is.EqualTo(1));
        Assert.That(stats.totalUpgradeCosts, Is.EqualTo(price));
        Assert.That(resources.curMaterials, Is.EqualTo(10));

        var saved = JsonUtility.FromJson<SaveGame>(
            File.ReadAllText(Path.Combine(saveDirectory, "savegame.json")));
        Assert.That(saved.material, Is.EqualTo(10));
        Assert.That(saved.upgrades.Single(u => u.upgradeName == "Damage").level,
            Is.EqualTo(initialLevel + 1));
        yield break;
    }

    [UnityTest]
    public IEnumerator ShortRunSurvivesSaveAndLoad()
    {
        int initialMaterials = ResourceManager.Instance.curMaterials;
        var extractor = StationModule.GetModuleByType(StationModule.eModuleType.Extractor);
        var core = StationModule.GetModuleByType(StationModule.eModuleType.Core);
        var damage = UpgradeAttribute.GetUpgradeByName(UpgradeAttribute.eUpgradeName.Damage);
        var owner = StationModule.allModules.Single(m =>
            m.upgradeSet && m.upgradeSet.upgradeAttributes.Contains(damage));
        int initialLevel = damage.level;

        ResourceManager.Instance.curMaterials = extractor.cost + owner.cost + Mathf.RoundToInt(damage.cost) + 50;
        ModulesUI.Instance.SelectModule(StationModule.eModuleType.Extractor);
        ModulesUI.Instance.BuySelectedModule();
        Assert.That(extractor.isBuilt, Is.True);

        ModulesUI.Instance.SelectModule(owner.moduleType);
        ModulesUI.Instance.BuySelectedModule();
        Assert.That(owner.isBuilt, Is.True);
        UpgradeUI.Instance.Show(owner.upgradeSet);
        UpgradeUI.Instance.currentSelectedUpgrade = damage.upgradeName;
        var click = typeof(UpgradeUI).GetMethod("OnUpgradeClicked",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(click, Is.Not.Null);
        click.Invoke(UpgradeUI.Instance, new object[] { damage });
        Assert.That(damage.level, Is.EqualTo(initialLevel + 1));

        core.TakeDamage(2);
        int materials = ResourceManager.Instance.curMaterials;
        int coreHP = core.currentHP;
        SaveGameManager.Instance.Save();

        yield return ReloadMainScene();

        Assert.That(ResourceManager.Instance.curMaterials, Is.EqualTo(materials));
        Assert.That(StationModule.GetModuleByType(StationModule.eModuleType.Extractor).isBuilt, Is.True);
        Assert.That(StationModule.GetModuleByType(owner.moduleType).isBuilt, Is.True);
        Assert.That(UpgradeAttribute.GetUpgradeByName(UpgradeAttribute.eUpgradeName.Damage).level,
            Is.EqualTo(initialLevel + 1));
        Assert.That(StationModule.GetModuleByType(StationModule.eModuleType.Core).currentHP,
            Is.EqualTo(coreHP));
        Assert.That(Stats.Instance.modulesBuilt, Is.EqualTo(2));
        Assert.That(Stats.Instance.boughtUpgrades, Is.EqualTo(1));
        Assert.That(Stats.Instance.modulesDamageTaken, Is.EqualTo(2));

        var loadedCore = StationModule.GetModuleByType(StationModule.eModuleType.Core);
        loadedCore.TakeDamage(loadedCore.currentHP);
        Assert.That(GameManager.gameOver, Is.True);
        GameManager.Instance.Replay();
        GameManager.isInit = false;
        Assert.That(ResourceManager.Instance.curMaterials, Is.EqualTo(initialMaterials));
        Assert.That(StationModule.GetModuleByType(StationModule.eModuleType.Extractor).isBuilt, Is.False);
        Assert.That(Stats.Instance.boughtUpgrades, Is.Zero);
    }

    [UnityTest]
    public IEnumerator CoreDeathKeepsResultAndReplayStartsCleanThreeTimes()
    {
        int initialMaterials = ResourceManager.Instance.curMaterials;
        int initialCost = StationModule.GetModuleByType(StationModule.eModuleType.Extractor).cost;
        Vector3 initialCameraPosition = Camera.main.transform.position;
        var initialDamage = UpgradeAttribute.GetUpgradeByName(UpgradeAttribute.eUpgradeName.Damage);
        int initialLevel = initialDamage.level;
        float initialUpgradeCost = initialDamage.cost;
        float initialUpgradeValue = initialDamage.currentValue;
        int initialTowerDamage = Tower.Instance.damage;

        for (int i = 0; i < 3; i++)
        {
            var core = StationModule.GetModuleByType(StationModule.eModuleType.Core);
            var extractor = StationModule.GetModuleByType(StationModule.eModuleType.Extractor);
            var damage = UpgradeAttribute.GetUpgradeByName(UpgradeAttribute.eUpgradeName.Damage);
            ResourceManager.Instance.curMaterials += 200;
            extractor.isBuilt = true;
            extractor.gameObject.SetActive(true);
            extractor.cost += 5;
            damage.level++;
            damage.RecalculateFromLevel();
            Stats.Instance.wavesCompleted = i + 3;
            SaveGameManager.Instance.Save();

            var droneModule = StationModule.GetModuleByType(StationModule.eModuleType.Drone);
            droneModule.isBuilt = true;
            droneModule.gameObject.SetActive(true);
            DroneManager.Instance.AfterModulInit();
            Assert.That(DroneManager.Instance.SpawnDrone(true), Is.Not.Null);
            Assert.That(ProjectileManager.Instance.spawnProjectile(
                Tower.Instance.projectilePrefab, Vector2.zero), Is.Not.Null);
            ResourceManager.Instance.SpawnMaterial(5, Vector3.zero);
            var enemy = new GameObject("Replay test enemy").AddComponent<Enemy>();
            enemy.enemyType = Enemy.eEnemyType.Normal;
            EnemySpawner.Instance.instantiatedEnemies.Add(enemy);

            core.TakeDamage(core.currentHP);
            int score = ScoreManager.Instance.GetBreakdown(Stats.Instance).totalScore;

            Assert.That(GameManager.gameOver, Is.True);
            Assert.That(GameManager.Instance.gameOverPanel.activeSelf, Is.True);
            Assert.That(Stats.Instance.wavesCompleted, Is.EqualTo(i + 3));
            Assert.That(GameObject.Find("ExplosionParent").transform.childCount, Is.GreaterThan(0));
            Assert.That(File.Exists(Path.Combine(saveDirectory, "savegame.json")), Is.False);
            Assert.That(score, Is.GreaterThan(0));
            Assert.That(SaveGameManager.Instance.bestSaveGame.score, Is.EqualTo(score));
            var savedBest = JsonUtility.FromJson<SaveGame>(
                File.ReadAllText(Path.Combine(saveDirectory, "bestscore.json")));
            Assert.That(savedBest.score, Is.EqualTo(score));

            GameManager.Instance.Replay();
            GameManager.isInit = false;

            Assert.That(GameManager.gameOver, Is.False);
            Assert.That(GameManager.Instance.gameOverPanel.activeSelf, Is.False);
            Assert.That(core.gameObject.activeSelf, Is.True);
            Assert.That(core.currentHP, Is.EqualTo(core.maxHP));
            Assert.That(ResourceManager.Instance.curMaterials, Is.EqualTo(initialMaterials));
            Assert.That(extractor.isBuilt, Is.False);
            Assert.That(extractor.cost, Is.EqualTo(initialCost));
            var resetDamage = UpgradeAttribute.GetUpgradeByName(UpgradeAttribute.eUpgradeName.Damage);
            Assert.That(resetDamage.level, Is.EqualTo(initialLevel));
            Assert.That(resetDamage.cost, Is.EqualTo(initialUpgradeCost));
            Assert.That(resetDamage.currentValue, Is.EqualTo(initialUpgradeValue));
            Assert.That(Tower.Instance.damage, Is.EqualTo(initialTowerDamage));
            Assert.That(Stats.Instance.wavesCompleted, Is.Zero);
            Assert.That(Stats.Instance.modulesDestroyed, Is.Zero);
            Assert.That(Stats.Instance.GetTotalKills(), Is.Zero);
            Assert.That(DroneManager.Instance.allDrones, Is.Empty);
            Assert.That(ProjectileManager.Instance.allProjectiles, Is.Empty);
            Assert.That(ResourceManager.Instance.collectEffects, Is.Empty);
            Assert.That(EnemySpawner.Instance.instantiatedEnemies, Is.Empty);
            Assert.That(File.Exists(Path.Combine(saveDirectory, "savegame.json")), Is.True);
            var newRunSave = JsonUtility.FromJson<SaveGame>(
                File.ReadAllText(Path.Combine(saveDirectory, "savegame.json")));
            Assert.That(newRunSave.material, Is.EqualTo(initialMaterials));
            Assert.That(newRunSave.upgrades.Single(u => u.upgradeName == "Damage").level,
                Is.EqualTo(initialLevel));
            yield return null;
            Assert.That(enemy == null, Is.True);
            Assert.That(GameObject.Find("ExplosionParent").transform.childCount, Is.Zero);
            Assert.That(Camera.main.transform.position, Is.EqualTo(initialCameraPosition));
        }

        yield break;
    }

    [UnityTest]
    public IEnumerator ExpandedRunDoesNotChangeUpgradeDefaults()
    {
        var defaults = UpgradeAttribute.allUpgradeAttributes.ToDictionary(
            upgrade => upgrade.upgradeName,
            upgrade => (upgrade.level, upgrade.currentValue, upgrade.cost));
        Assert.That(defaults.Count, Is.GreaterThan(10));

        // The Temporal Modulator's template is assigned to Shield (tracked by U19).
        foreach (var module in StationModule.allModules.Where(m => m.upgradeSet &&
                     m.moduleType != StationModule.eModuleType.TemporalModulator))
            Assert.That(module.upgradeSet.moduleType, Is.EqualTo(module.moduleType));

        for (int run = 0; run < 2; run++)
        {
            foreach (var upgrade in UpgradeAttribute.allUpgradeAttributes)
            {
                if (upgrade.level >= upgrade.maxLevel) continue;
                upgrade.level++;
                upgrade.RecalculateFromLevel();
            }
            UpgradeAttribute.ApplyAllUpgradeEffect();
            SaveGameManager.Instance.Save();

            if (run == 0)
            {
                yield return ReloadMainScene();
                foreach (var upgrade in UpgradeAttribute.allUpgradeAttributes)
                    Assert.That(upgrade.level, Is.EqualTo(defaults[upgrade.upgradeName].level + 1));
            }

            GameManager.Instance.GameOver();
            GameManager.Instance.Replay();
            GameManager.isInit = false;

            foreach (var upgrade in UpgradeAttribute.allUpgradeAttributes)
            {
                var expected = defaults[upgrade.upgradeName];
                Assert.That(upgrade.level, Is.EqualTo(expected.level), upgrade.upgradeName.ToString());
                Assert.That(upgrade.currentValue, Is.EqualTo(expected.currentValue).Within(0.001f),
                    upgrade.upgradeName.ToString());
                Assert.That(upgrade.cost, Is.EqualTo(expected.cost).Within(0.001f),
                    upgrade.upgradeName.ToString());
            }

            if (run == 0)
            {
                yield return ReloadMainScene();
                foreach (var upgrade in UpgradeAttribute.allUpgradeAttributes)
                    Assert.That(upgrade.level, Is.EqualTo(defaults[upgrade.upgradeName].level));
            }
        }
    }

    [UnityTest]
    public IEnumerator ReloadingSceneDoesNotKeepOldModulesOrRunFlags()
    {
        int moduleCount = StationModule.allModules.Count;
        int upgradeCount = UpgradeAttribute.allUpgradeAttributes.Count;
        int setCount = UpgradeSet.allUpgradeSets.Count;
        var saveManager = SaveGameManager.Instance;
        Assert.That(moduleCount, Is.GreaterThan(0));
        Assert.That(upgradeCount, Is.GreaterThan(0));
        Assert.That(setCount, Is.GreaterThan(0));
        Assert.That(saveManager.transform.parent, Is.Null);

        for (int i = 0; i < 3; i++)
        {
            if (i == 1) GameManager.Instance.GameOver();

            SceneManager.LoadScene("MainScene");
            yield return null;

            Assert.That(GameManager.isInit, Is.True);
            Assert.That(GameManager.gameOver, Is.False);
            Assert.That(StationModule.allModules, Has.Count.EqualTo(moduleCount));
            Assert.That(StationModule.allModules.All(module => module != null), Is.True);
            Assert.That(StationModule.allModules.Select(module => module.moduleType).Distinct().Count(),
                Is.EqualTo(moduleCount));
            Assert.That(UpgradeAttribute.allUpgradeAttributes, Has.Count.EqualTo(upgradeCount));
            Assert.That(UpgradeAttribute.allUpgradeAttributes.Distinct().Count(), Is.EqualTo(upgradeCount));
            Assert.That(UpgradeSet.allUpgradeSets, Has.Count.EqualTo(setCount));
            Assert.That(UpgradeSet.allUpgradeSets.Distinct().Count(), Is.EqualTo(setCount));
            foreach (var module in StationModule.allModules.Where(m => m.upgradeSet))
            {
                Assert.That(UpgradeSet.allUpgradeSets, Does.Contain(module.upgradeSet));
                foreach (var upgrade in module.upgradeSet.upgradeAttributes)
                    Assert.That(UpgradeAttribute.allUpgradeAttributes, Does.Contain(upgrade));
            }
            Assert.That(SaveGameManager.Instance, Is.SameAs(saveManager));
            Assert.That(UnityEngine.Object.FindObjectsByType<SaveGameManager>(
                FindObjectsInactive.Include, FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(StationModule.GetModuleByType(StationModule.eModuleType.Core).gameObject.activeSelf,
                Is.True);
            GameManager.isInit = false;
        }
    }

    [UnityTest]
    public IEnumerator ModuleLossAndLoadKeepOnlyBuiltModuleEffects()
    {
        var command = StationModule.GetModuleByType(StationModule.eModuleType.CommandUnit);
        var extractor = StationModule.GetModuleByType(StationModule.eModuleType.Extractor);
        var core = StationModule.GetModuleByType(StationModule.eModuleType.Core);
        var integrity = UpgradeAttribute.GetUpgradeByName(UpgradeAttribute.eUpgradeName.StructuralIntegrity);
        var efficiency = UpgradeAttribute.GetUpgradeByName(UpgradeAttribute.eUpgradeName.CollectingEfficiency);
        command.isBuilt = true;
        command.gameObject.SetActive(true);
        extractor.isBuilt = true;
        extractor.gameObject.SetActive(true);
        integrity.level = 1;
        integrity.RecalculateFromLevel();
        efficiency.level = 1;
        efficiency.RecalculateFromLevel();
        UpgradeAttribute.ApplyAllUpgradeEffect();
        int upgradedHP = core.maxHP;
        float upgradedEfficiency = ResourceManager.Instance.collectingEffeciency;
        Assert.That(upgradedHP, Is.GreaterThan(Mathf.RoundToInt(integrity.baseValue)));
        Assert.That(upgradedEfficiency, Is.GreaterThan(efficiency.baseValue));

        command.TakeDamage(command.currentHP);
        Assert.That(core.maxHP, Is.EqualTo(Mathf.RoundToInt(integrity.baseValue)));
        Assert.That(ResourceManager.Instance.collectingEffeciency, Is.EqualTo(upgradedEfficiency));
        Assert.That(integrity.currentValue, Is.GreaterThan(integrity.baseValue));

        yield return ReloadMainScene();

        command = StationModule.GetModuleByType(StationModule.eModuleType.CommandUnit);
        core = StationModule.GetModuleByType(StationModule.eModuleType.Core);
        Assert.That(command.isBuilt, Is.False);
        Assert.That(core.maxHP, Is.EqualTo(Mathf.RoundToInt(integrity.baseValue)));
        Assert.That(ResourceManager.Instance.collectingEffeciency, Is.EqualTo(upgradedEfficiency));

        var radar = StationModule.GetModuleByType(StationModule.eModuleType.Radar);
        radar.isBuilt = false;
        radar.currentHP = 0;
        ResourceManager.Instance.curMaterials = command.cost + 10;
        ModulesUI.Instance.SelectModule(StationModule.eModuleType.CommandUnit);
        ModulesUI.Instance.BuySelectedModule();
        Assert.That(command.isBuilt, Is.True);
        Assert.That(core.maxHP, Is.EqualTo(upgradedHP));
        Assert.That(ResourceManager.Instance.collectingEffeciency, Is.EqualTo(upgradedEfficiency));
        Assert.That(radar.isBuilt, Is.False);
        Assert.That(radar.currentHP, Is.Zero);
    }

    [UnityTest]
    public IEnumerator UnbuiltPreviewCannotTakeDamage()
    {
        var extractor = StationModule.GetModuleByType(StationModule.eModuleType.Extractor);
        ModulesUI.Instance.SelectModule(StationModule.eModuleType.Extractor);
        Assert.That(extractor.gameObject.activeSelf, Is.True);
        Assert.That(extractor.GetComponent<Collider2D>().enabled, Is.False);
        int initialHP = extractor.currentHP;
        int damageTaken = Stats.Instance.modulesDamageTaken;

        extractor.TakeDamage(2);

        Assert.That(extractor.currentHP, Is.EqualTo(initialHP));
        Assert.That(Stats.Instance.modulesDamageTaken, Is.EqualTo(damageTaken));
        Assert.That(extractor.isBuilt, Is.False);
        yield break;
    }

    [UnityTest]
    public IEnumerator LostShieldCannotRechargeBeforeRebuild()
    {
        var module = StationModule.GetModuleByType(StationModule.eModuleType.Shield);
        var shield = Shield.Instance;
        module.isBuilt = true;
        module.gameObject.SetActive(true);
        module.RefreshCollider();
        shield.currentShieldPoints = shield.maxShieldPoints;
        shield.activateShield();
        shield.TakeDamage(Mathf.CeilToInt(shield.maxShieldPoints));
        Assert.That(shield.rechargeCountdown, Is.GreaterThan(0f));

        module.TakeDamage(module.currentHP);
        Assert.That(shield.rechargeCountdown, Is.Zero);
        Assert.That(shield.shieldIsActive, Is.False);
        shield.UpdateNormal();
        Assert.That(shield.shieldIsActive, Is.False);

        ResourceManager.Instance.curMaterials = module.cost + 10;
        ModulesUI.Instance.SelectModule(StationModule.eModuleType.Shield);
        ModulesUI.Instance.BuySelectedModule();
        Assert.That(shield.shieldIsActive, Is.True);
        Assert.That(shield.currentShieldPoints, Is.EqualTo(shield.maxShieldPoints));
        Assert.That(shield.rechargeCountdown, Is.Zero);
        yield break;
    }

    [UnityTest]
    public IEnumerator UpgradeClickAfterModuleLossDoesNotCharge()
    {
        var upgrade = UpgradeAttribute.GetUpgradeByName(UpgradeAttribute.eUpgradeName.Damage);
        var owner = StationModule.allModules.Single(m =>
            m.upgradeSet && m.upgradeSet.upgradeAttributes.Contains(upgrade));
        owner.isBuilt = true;
        owner.gameObject.SetActive(true);
        ModulesUI.Instance.SelectModule(owner.moduleType);
        UpgradeUI.Instance.Show(owner.upgradeSet);
        UpgradeUI.Instance.currentSelectedUpgrade = upgrade.upgradeName;
        owner.TakeDamage(owner.currentHP);

        int level = upgrade.level;
        int materials = Mathf.RoundToInt(upgrade.cost) + 10;
        ResourceManager.Instance.curMaterials = materials;
        var click = typeof(UpgradeUI).GetMethod("OnUpgradeClicked",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(click, Is.Not.Null);
        click.Invoke(UpgradeUI.Instance, new object[] { upgrade });

        Assert.That(upgrade.level, Is.EqualTo(level));
        Assert.That(ResourceManager.Instance.curMaterials, Is.EqualTo(materials));
        Assert.That(Stats.Instance.boughtUpgrades, Is.Zero);
        yield break;
    }

    [UnityTest]
    public IEnumerator TimeSpeedFollowsRealMenuTransitionsAndReplay()
    {
        var module = StationModule.GetModuleByType(StationModule.eModuleType.TemporalModulator);
        var upgrade = UpgradeAttribute.GetUpgradeByName(UpgradeAttribute.eUpgradeName.TimeMultiplier);
        var time = TimeController.Instance;
        module.isBuilt = true;
        module.gameObject.SetActive(true);
        upgrade.level = 2;
        upgrade.RecalculateFromLevel();
        time.RefreshPanel();
        time.btnTimeIncrease.onClick.Invoke();
        time.btnTimeIncrease.onClick.Invoke();
        Assert.That(Time.timeScale, Is.EqualTo(2f));

        UIManager.Instance.Show(true);
        Assert.That(Time.timeScale, Is.EqualTo(time.stationUITimeModulation));
        time.OnStationUIOpen();
        Assert.That(Time.timeScale, Is.EqualTo(time.stationUITimeModulation));
        UIManager.Instance.Show(false);
        Assert.That(Time.timeScale, Is.EqualTo(2f));
        time.OnStationUIClose();
        Assert.That(Time.timeScale, Is.EqualTo(2f));

        module.TakeDamage(module.currentHP);
        Assert.That(Time.timeScale, Is.EqualTo(time.minTimeModulation));
        Assert.That(time.panelTimeModulation.activeSelf, Is.False);

        GameManager.Instance.Replay();
        GameManager.isInit = false;
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        Assert.That(time.panelTimeModulation.activeSelf, Is.False);
        Assert.That(time.btnTimeIncrease.interactable, Is.True);
        yield break;
    }

    [UnityTest]
    public IEnumerator PauseFlushesPendingSaveButGameOverDoesNot()
    {
        var manager = SaveGameManager.Instance;
        string path = Path.Combine(saveDirectory, "savegame.json");
        var pause = typeof(SaveGameManager).GetMethod("OnApplicationPause",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(pause, Is.Not.Null);
        ResourceManager.Instance.curMaterials = 123;
        manager.RequestSave();
        Assert.That(JsonUtility.FromJson<SaveGame>(File.ReadAllText(path)).material, Is.Not.EqualTo(123));

        GameManager.isInit = true;
        pause.Invoke(manager, new object[] { true });
        GameManager.isInit = false;
        Assert.That(JsonUtility.FromJson<SaveGame>(File.ReadAllText(path)).material, Is.EqualTo(123));

        ResourceManager.Instance.curMaterials = 321;
        GameManager.gameOver = true;
        GameManager.isInit = true;
        pause.Invoke(manager, new object[] { true });
        GameManager.isInit = false;
        GameManager.gameOver = false;
        Assert.That(JsonUtility.FromJson<SaveGame>(File.ReadAllText(path)).material, Is.EqualTo(123));
        yield break;
    }

    [UnityTest]
    public IEnumerator CorruptSaveRestoresBackupOrStartsFresh()
    {
        string path = Path.Combine(saveDirectory, "savegame.json");
        int initialMaterials = ResourceManager.Instance.curMaterials;
        ResourceManager.Instance.curMaterials = 41;
        SaveGameManager.Instance.Save();
        ResourceManager.Instance.curMaterials = 52;
        SaveGameManager.Instance.Save();
        File.WriteAllText(path, "{broken json");

        yield return ReloadMainScene();
        Assert.That(ResourceManager.Instance.curMaterials, Is.EqualTo(41));

        File.WriteAllText(path, "{broken json");
        File.WriteAllText(path + ".bak", "{broken backup");
        yield return ReloadMainScene();
        Assert.That(ResourceManager.Instance.curMaterials, Is.EqualTo(initialMaterials));
    }

    [UnityTest]
    public IEnumerator WaveCheckpointKeepsResourcesPurchasesAndStatsTogether()
    {
        var resources = ResourceManager.Instance;
        var stats = Stats.Instance;
        var spawner = EnemySpawner.Instance;
        var extractor = StationModule.GetModuleByType(StationModule.eModuleType.Extractor);
        var command = StationModule.GetModuleByType(StationModule.eModuleType.CommandUnit);
        resources.curMaterials = 80;
        extractor.isBuilt = true;
        extractor.gameObject.SetActive(true);
        stats.modulesBuilt = 1;
        stats.resourcesCollectedManually = 7;
        spawner.currentWaveIndex = 2;
        SaveGameManager.Instance.CaptureWaveCheckpoint();

        resources.curMaterials = 30;
        command.isBuilt = true;
        command.gameObject.SetActive(true);
        stats.modulesBuilt = 2;
        stats.resourcesCollectedManually = 22;
        spawner.currentWaveIndex = 3;
        SaveGameManager.Instance.Save();
        var checkpoint = JsonUtility.FromJson<SaveGame>(
            File.ReadAllText(Path.Combine(saveDirectory, "savegame.json")));
        Assert.That(checkpoint.material, Is.EqualTo(80));
        Assert.That(checkpoint.currentWaveIndex, Is.EqualTo(2));
        Assert.That(checkpoint.stats.modulesBuilt, Is.EqualTo(1));
        Assert.That(checkpoint.stats.resourcesCollectedManually, Is.EqualTo(7));
        Assert.That(checkpoint.modules.Single(m => m.moduleType == "Extractor").isBuilt, Is.True);
        Assert.That(checkpoint.modules.Single(m => m.moduleType == "CommandUnit").isBuilt, Is.False);

        yield return ReloadMainScene();
        Assert.That(ResourceManager.Instance.curMaterials, Is.EqualTo(80));
        Assert.That(EnemySpawner.Instance.currentWaveIndex, Is.EqualTo(2));
        Assert.That(Stats.Instance.modulesBuilt, Is.EqualTo(1));
        Assert.That(Stats.Instance.resourcesCollectedManually, Is.EqualTo(7));
        Assert.That(StationModule.GetModuleByType(StationModule.eModuleType.Extractor).isBuilt, Is.True);
        Assert.That(StationModule.GetModuleByType(StationModule.eModuleType.CommandUnit).isBuilt, Is.False);

        var loadedSpawner = EnemySpawner.Instance;
        loadedSpawner.ResetScript();
        loadedSpawner.currentWaveIndex = 2;
        SaveGameManager.Instance.CaptureWaveCheckpoint();
        ResourceManager.Instance.curMaterials = 55;
        loadedSpawner.currentWaveIndex = 3;
        loadedSpawner.UpdateNormal();
        var completedWaveSave = JsonUtility.FromJson<SaveGame>(
            File.ReadAllText(Path.Combine(saveDirectory, "savegame.json")));
        Assert.That(completedWaveSave.material, Is.EqualTo(55));
        Assert.That(completedWaveSave.currentWaveIndex, Is.EqualTo(3));
    }

    [UnityTest]
    public IEnumerator SpawnerCapturesCheckpointWhenWaveActuallyStarts()
    {
        var spawner = EnemySpawner.Instance;
        spawner.ResetScript();
        var delay = typeof(EnemySpawner).GetField("timeBetweenWaves",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(delay, Is.Not.Null);
        delay.SetValue(spawner, 0f);
        spawner.currentWaveIndex = 2;
        Stats.Instance.wavesCompleted = 1;
        ResourceManager.Instance.curMaterials = 80;
        spawner.UpdateNormal();
        Assert.That(SaveGameManager.Instance.HasWaveCheckpoint, Is.False);

        ResourceManager.Instance.curMaterials = 90;
        yield return null;

        Assert.That(SaveGameManager.Instance.HasWaveCheckpoint, Is.True);
        var checkpoint = JsonUtility.FromJson<SaveGame>(
            File.ReadAllText(Path.Combine(saveDirectory, "savegame.json")));
        Assert.That(checkpoint.material, Is.EqualTo(90));
        Assert.That(checkpoint.currentWaveIndex, Is.EqualTo(2));
        Assert.That(checkpoint.stats.enemiesSpawned, Is.Zero);
        Assert.That(checkpoint.stats.wavesCompleted, Is.EqualTo(1));
        Assert.That(Stats.Instance.wavesCompleted, Is.EqualTo(2));
        spawner.ResetScript();

        yield return ReloadMainScene();
        Assert.That(Stats.Instance.wavesCompleted, Is.EqualTo(1));
        spawner = EnemySpawner.Instance;
        spawner.ResetScript();
        spawner.currentWaveIndex = 2;
        delay.SetValue(spawner, 0f);
        spawner.UpdateNormal();
        yield return null;
        Assert.That(Stats.Instance.wavesCompleted, Is.EqualTo(2));
        spawner.ResetScript();
    }

    IEnumerator ReloadMainScene()
    {
        GameManager.isInit = false;
        UnityEngine.Object.Destroy(SaveGameManager.Instance.gameObject);
        yield return null;
        StationModule.allModules.Clear();
        UpgradeAttribute.allUpgradeAttributes.Clear();
        UpgradeSet.allUpgradeSets.Clear();
        SceneManager.LoadScene("MainScene");
        yield return null;
        Assert.That(GameManager.isInit, Is.True);
        GameManager.isInit = false;
        SaveGameManager.Instance.ClearWaveCheckpoint();
    }
}
#endif
