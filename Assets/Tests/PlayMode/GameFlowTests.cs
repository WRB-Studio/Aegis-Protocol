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
        SaveGameManager.Instance.Save();

        yield return ReloadMainScene();

        Assert.That(ResourceManager.Instance.curMaterials, Is.EqualTo(137));
        Assert.That(StationModule.GetModuleByType(StationModule.eModuleType.Core).currentHP,
            Is.EqualTo(damagedCoreHP));
        var restoredModule = StationModule.GetModuleByType(StationModule.eModuleType.Drone);
        Assert.That(restoredModule.isBuilt, Is.True);
        Assert.That(restoredModule.currentHP, Is.EqualTo(damagedDroneModuleHP));
        Assert.That(DroneManager.Instance.allDrones, Has.Count.EqualTo(1));
        Assert.That(DroneManager.Instance.allDrones[0].currentHP, Is.EqualTo(2));
        Assert.That(Stats.Instance.dronesBuilt, Is.EqualTo(7));
        Assert.That(Stats.Instance.resourcesSpawned, Is.EqualTo(19));
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
    }
}
#endif
