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
