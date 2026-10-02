#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class GameFlowTests
{
    string saveDirectory;

    [UnityTest]
    public IEnumerator ShootingStarPathsUseEveryEdgeAndCrossThePlayableScreen()
    {
        var choose = typeof(ResourceManager).GetMethod("ChooseStarPath", BindingFlags.Static | BindingFlags.NonPublic);
        var state = UnityEngine.Random.state;
        var entries = new System.Collections.Generic.HashSet<int>();
        var exits = new System.Collections.Generic.HashSet<int>();
        try
        {
            UnityEngine.Random.InitState(73129);
            for (int i = 0; i < 1000; i++)
            {
                var args = new object[] { Vector2.zero, Vector2.zero };
                choose.Invoke(null, args);
                var from = (Vector2)args[0];
                var to = (Vector2)args[1];
                int entry = from.x < 0f ? 0 : from.x > 1f ? 1 : from.y < 0f ? 2 : 3;
                int exit = to.x < 0f ? 0 : to.x > 1f ? 1 : to.y < 0f ? 2 : 3;
                Assert.That(from.x < 0f || from.x > 1f || from.y < 0f || from.y > 1f, Is.True);
                Assert.That(to.x < 0f || to.x > 1f || to.y < 0f || to.y > 1f, Is.True);
                Assert.That(exit, Is.Not.EqualTo(entry));
                Vector2 middle = (from + to) * 0.5f;
                Assert.That(middle.x, Is.InRange(0.149f, 0.851f));
                Assert.That(middle.y, Is.InRange(0.149f, 0.851f));
                Assert.That(Vector2.Distance(from, to), Is.GreaterThanOrEqualTo(0.799f));
                entries.Add(entry);
                exits.Add(exit);
            }
            Assert.That(entries.Count, Is.EqualTo(4));
            Assert.That(exits.Count, Is.EqualTo(4));
        }
        finally { UnityEngine.Random.state = state; }
        yield return null;
    }

    [UnityTest]
    public IEnumerator ShootingStarRequiresTapAndAwardsOnlyOnce()
    {
        var resources = ResourceManager.Instance;
        var prefab = (ShootingStar)typeof(ResourceManager).GetField("shootingStarPrefab",
            BindingFlags.NonPublic | BindingFlags.Instance).GetValue(resources);
        Assert.That(prefab, Is.Not.Null);
        var star = UnityEngine.Object.Instantiate(prefab);
        Vector3 position = Camera.main.ViewportToWorldPoint(new Vector3(0.45f, 0.75f, -Camera.main.transform.position.z));
        star.Init(position, position, 7f, 10);
        int effectsBefore = resources.collectEffects.Count;
        int materialsBefore = resources.curMaterials;
        resources.autoCollecting = true;
        Assert.That(star.Tick(3f), Is.False);
        Assert.That(resources.curMaterials, Is.EqualTo(materialsBefore));
        Assert.That(resources.collectEffects.Count, Is.EqualTo(effectsBefore));
        Assert.That(star.TryCollect(new Vector2(-1000f, -1000f)), Is.False);
        Vector2 screen = Camera.main.WorldToScreenPoint(position);
        Assert.That(star.TryCollect(screen), Is.True);
        Assert.That(star.TryCollect(screen), Is.False);
        Assert.That(resources.collectEffects.Count, Is.EqualTo(effectsBefore + 1));
        var drop = resources.collectEffects.Last();
        Assert.That(drop.flyToStation, Is.True);
        Assert.That(drop.collectedManually, Is.True);
        Assert.That(drop.material, Is.EqualTo(10));
        StationModule.GetModuleByType(StationModule.eModuleType.Extractor).isBuilt = false;
        drop.duration = 0.001f;
        yield return null;
        drop.UpdateNormal();
        Assert.That(resources.curMaterials, Is.EqualTo(materialsBefore + 10));
    }

    [UnityTest]
    public IEnumerator ShootingStarExpiryPauseAndReplayDoNotAwardMaterials()
    {
        var resources = ResourceManager.Instance;
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var prefab = (ShootingStar)typeof(ResourceManager).GetField("shootingStarPrefab", flags).GetValue(resources);
        var star = UnityEngine.Object.Instantiate(prefab);
        Vector3 position = Camera.main.ViewportToWorldPoint(new Vector3(0.45f, 0.75f, -Camera.main.transform.position.z));
        star.Init(position, position + Vector3.right, 7f, 10);
        typeof(ResourceManager).GetField("shootingStar", flags).SetValue(resources, star);
        int materialsBefore = resources.curMaterials;
        int effectsBefore = resources.collectEffects.Count;
        Time.timeScale = 0f;
        resources.UpdateNormal();
        Assert.That(star.transform.position, Is.EqualTo(position));
        Assert.That(star.TryCollect(Camera.main.WorldToScreenPoint(position)), Is.False);
        Time.timeScale = 8f;
        Assert.That(star.Tick(1f), Is.False, "Speed mode must not multiply the supplied real elapsed time.");
        Assert.That(star.Tick(6f), Is.True);
        Assert.That(star.TryCollect(Camera.main.WorldToScreenPoint(star.transform.position)), Is.False);
        Assert.That(resources.curMaterials, Is.EqualTo(materialsBefore));
        Assert.That(resources.collectEffects.Count, Is.EqualTo(effectsBefore));
        resources.ResetScript();
        yield return null;
        Assert.That(star == null, Is.True);
        Assert.That(typeof(ResourceManager).GetField("shootingStar", flags).GetValue(resources), Is.Null);
    }

    [UnityTest]
    public IEnumerator SpawnAreasKeepAllShipVariantsOutsideDifferentViewports()
    {
        var camera = Camera.main;
        var spawner = EnemySpawner.Instance;
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var place = typeof(EnemySpawner).GetMethod("PlaceOutsideView", flags);
        var getBounds = typeof(EnemySpawner).GetMethod("GetCameraBounds", BindingFlags.NonPublic | BindingFlags.Static);
        var prefabs = (System.Collections.Generic.List<GameObject>)typeof(EnemySpawner)
            .GetField("enemyPrefabs", flags).GetValue(spawner);
        float originalAspect = camera.aspect;
        Vector3 originalPosition = camera.transform.position;
        try
        {
            camera.transform.position += new Vector3(1.5f, -0.75f, 0f);
            foreach (float aspect in new[] { 9f / 20f, 9f / 16f, 3f / 4f, 16f / 9f })
            {
                camera.aspect = aspect;
                var view = (Bounds)getBounds.Invoke(null, new object[] { camera });
                Assert.That(view.size.x, Is.EqualTo(camera.orthographicSize * 2f * aspect).Within(0.001f));
                foreach (var prefab in prefabs)
                {
                    var go = UnityEngine.Object.Instantiate(prefab);
                    try
                    {
                        var enemy = go.GetComponent<Enemy>();
                        var variants = (Sprite[])typeof(EnemyVisualVariant).GetField("variants", flags)
                            .GetValue(go.GetComponent<EnemyVisualVariant>());
                        // Exercise future larger designs as well as every existing sprite.
                        go.transform.localScale = prefab.transform.localScale * 1.5f;
                        foreach (var sprite in variants)
                        {
                            go.GetComponent<SpriteRenderer>().sprite = sprite;
                            foreach (bool top in new[] { true, false })
                            foreach (float edge in new[] { -1f, 0f, 1f })
                            foreach (float groupRadius in new[] { 0f, 0.5f })
                            {
                                Vector2 offset = new Vector2(edge * groupRadius, top ? -groupRadius : groupRadius);
                                place.Invoke(spawner, new object[] { enemy, view, view.center.x + edge * view.extents.x,
                                    top, offset, groupRadius, 0f });
                                Assert.That(enemy.transform.position.x, Is.InRange(view.min.x, view.max.x));
                                foreach (var renderer in go.GetComponentsInChildren<SpriteRenderer>())
                                {
                                    if (top) Assert.That(renderer.bounds.min.y, Is.GreaterThanOrEqualTo(view.max.y + 0.199f));
                                    else Assert.That(renderer.bounds.max.y, Is.LessThanOrEqualTo(view.min.y - 0.199f));
                                }
                            }
                        }
                    }
                    finally { UnityEngine.Object.DestroyImmediate(go); }
                }
            }
        }
        finally
        {
            camera.aspect = originalAspect;
            camera.transform.position = originalPosition;
        }
        yield return null;
    }

    [UnityTest]
    public IEnumerator ActualWaveSpawnsOutsideCameraBeforeEnemiesMove()
    {
        var spawner = EnemySpawner.Instance;
        spawner.ResetScript();
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        typeof(EnemySpawner).GetField("firstWaveDelay", flags).SetValue(spawner, 0f);
        var routine = (IEnumerator)typeof(EnemySpawner).GetMethod("SpawnWave", flags).Invoke(spawner, null);
        Assert.That(routine.MoveNext(), Is.True);
        Assert.That(routine.MoveNext(), Is.True);
        Assert.That(spawner.instantiatedEnemies.Count, Is.GreaterThan(0));
        foreach (var enemy in spawner.instantiatedEnemies)
        foreach (var renderer in enemy.GetComponentsInChildren<SpriteRenderer>())
        {
            Vector3 min = Camera.main.WorldToViewportPoint(renderer.bounds.min);
            Vector3 max = Camera.main.WorldToViewportPoint(renderer.bounds.max);
            Assert.That(min.y > 1f || max.y < 0f, Is.True, "A ship appeared inside the camera at spawn.");
        }
        (routine as IDisposable)?.Dispose();
        spawner.ResetScript();
        yield return null;
    }

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

        MatchReporter.Current?.Dispose();
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

    [Test]
    public void AuthoredWaves_FollowCampaignPlan()
    {
        var field = typeof(EnemySpawner).GetField("waves", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);

        var waves = (System.Collections.Generic.List<EnemyWave>)field.GetValue(EnemySpawner.Instance);
        Assert.That(waves, Has.Count.EqualTo(20));
        Assert.That(waves[0].enemies.All(instruction => instruction.type == Enemy.eEnemyType.Normal), Is.True);
        Assert.That(waves[0].enemies.Sum(instruction => instruction.amount.x), Is.GreaterThanOrEqualTo(25));
        Assert.That(waves[1].enemies.Select(instruction => instruction.type),
            Is.EqualTo(new[] { Enemy.eEnemyType.Normal, Enemy.eEnemyType.Fast, Enemy.eEnemyType.Normal,
                Enemy.eEnemyType.Fast, Enemy.eEnemyType.Normal }));
        Assert.That(waves[1].enemies.Where(instruction => instruction.type == Enemy.eEnemyType.Fast)
            .Sum(instruction => instruction.amount.x), Is.EqualTo(2));
        Assert.That(waves[2].enemies.Where(instruction => instruction.type == Enemy.eEnemyType.Fast)
            .All(instruction => instruction.amount.y <= 2 && instruction.delayBetweenSpawns.x >= 0.8f), Is.True);
        Assert.That(waves[3].enemies.Any(instruction => instruction.type == Enemy.eEnemyType.Tank), Is.True);
        Assert.That(waves[5].enemies.Any(instruction => instruction.type == Enemy.eEnemyType.Ranged), Is.True);
        Assert.That(waves[10].enemies.Any(instruction => instruction.type == Enemy.eEnemyType.Swarm), Is.True);

        foreach (int waveIndex in new[] { 9, 19 })
            Assert.That(waves[waveIndex].enemies.Count(instruction => instruction.type == Enemy.eEnemyType.Boss), Is.EqualTo(1));
    }

    [Test]
    public void EnemyPrefabs_HaveFourVisualVariants()
    {
        var prefabsField = typeof(EnemySpawner).GetField("enemyPrefabs",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var variantsField = typeof(EnemyVisualVariant).GetField("variants",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(prefabsField, Is.Not.Null);
        Assert.That(variantsField, Is.Not.Null);

        var prefabs = (System.Collections.Generic.List<GameObject>)prefabsField.GetValue(EnemySpawner.Instance);
        foreach (var prefab in prefabs)
        {
            var variants = prefab.GetComponent<EnemyVisualVariant>();
            Assert.That(variants, Is.Not.Null, prefab.name);
            Assert.That(((Sprite[])variantsField.GetValue(variants)), Has.Length.EqualTo(4), prefab.name);
        }
    }

    [UnityTest]
    public IEnumerator ProjectileSweepHitsThinTargetsOnceAtNormalAndFastSpeed()
    {
        var enemyObject = new GameObject("Thin projectile target");
        enemyObject.tag = "Enemy";
        enemyObject.transform.position = new Vector3(2f, 2f, 0f);
        var collider = enemyObject.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one * 0.05f;
        collider.isTrigger = true;
        var enemy = enemyObject.AddComponent<Enemy>();
        enemy.enemyType = Enemy.eEnemyType.Normal;
        enemy.maxHP = 20;
        enemy.InitWithLevel(0);
        EnemySpawner.Instance.instantiatedEnemies.Add(enemy);
        var hpField = typeof(Enemy).GetField("currentHP", BindingFlags.Instance | BindingFlags.NonPublic);
        var trigger = typeof(Projectile).GetMethod("OnTriggerEnter2D", BindingFlags.Instance | BindingFlags.NonPublic);
        try
        {
            foreach (float scale in new[] { 1f, 4f })
            {
                Time.timeScale = scale;
                yield return null;
                foreach (bool droneShot in new[] { false, true })
                {
                    var prefab = droneShot ? DroneManager.Instance.dronePrefab.GetComponent<Drone>().projectilePrefab
                        : Tower.Instance.projectilePrefab;
                    var projectile = ProjectileManager.Instance.spawnProjectile(prefab, new Vector2(2f, 0f));
                    projectile.speed = 4f / Time.deltaTime;
                    projectile.damage = 1;
                    int hp = (int)hpField.GetValue(enemy);
                    int hits = droneShot ? Stats.Instance.droneProjectilesHit : Stats.Instance.towerProjectilesHit;
                    ProjectileManager.Instance.UpdateNormal();
                    Assert.That((int)hpField.GetValue(enemy), Is.EqualTo(hp - 1), "A target between frame positions must be hit.");
                    Assert.That(projectile.transform.position.y, Is.LessThan(2f));
                    Assert.That(ProjectileManager.Instance.allProjectiles, Has.No.Member(projectile));
                    trigger.Invoke(projectile, new object[] { collider });
                    Assert.That((int)hpField.GetValue(enemy), Is.EqualTo(hp - 1), "The trigger must not duplicate the swept hit.");
                    Assert.That(droneShot ? Stats.Instance.droneProjectilesHit : Stats.Instance.towerProjectilesHit,
                        Is.EqualTo(hits + 1));
                }
            }
        }
        finally
        {
            Time.timeScale = 1f;
            EnemySpawner.Instance.instantiatedEnemies.Remove(enemy);
            UnityEngine.Object.Destroy(enemyObject);
        }
    }

    [UnityTest]
    public IEnumerator SweptShieldReflectionProtectsCoreAndCanHitAnEnemy()
    {
        var shieldModule = StationModule.GetModuleByType(StationModule.eModuleType.Shield);
        shieldModule.isBuilt = true;
        shieldModule.gameObject.SetActive(true);
        var shield = Shield.Instance;
        shield.OnModuleBuilt();
        shield.deflectionChance = 100f;
        Time.timeScale = 4f;
        yield return null;
        Physics2D.SyncTransforms();
        var shieldCollider = shield.GetComponent<Collider2D>();
        var bounds = shieldCollider.bounds;
        var prefabs = (System.Collections.Generic.List<GameObject>)typeof(EnemySpawner)
            .GetField("enemyPrefabs", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(EnemySpawner.Instance);
        var prefab = prefabs.Select(p => p.GetComponent<Enemy>())
            .First(e => e.enemyType == Enemy.eEnemyType.Ranged).projectilePrefab;
        var core = StationModule.GetModuleByType(StationModule.eModuleType.Core);
        int coreHP = core.currentHP;
        int reflected = Stats.Instance.deflectedProjectilesFired;
        var projectile = ProjectileManager.Instance.spawnProjectile(prefab,
            new Vector2(bounds.center.x, bounds.max.y + 1f));
        projectile.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
        projectile.speed = (bounds.size.y + 2f) / Time.deltaTime;
        projectile.damage = 2;
        ProjectileManager.Instance.UpdateNormal();
        Assert.That(projectile.isDeflected, Is.True);
        Assert.That(projectile.CompareTag("TowerProjectile"), Is.True);
        Assert.That(core.currentHP, Is.EqualTo(coreHP));
        Assert.That(Stats.Instance.deflectedProjectilesFired, Is.EqualTo(reflected + 1));
        Assert.That(ProjectileManager.Instance.allProjectiles, Has.Member(projectile));

        var trigger = typeof(Projectile).GetMethod("OnTriggerEnter2D", BindingFlags.Instance | BindingFlags.NonPublic);
        trigger.Invoke(projectile, new object[] { shieldCollider });
        Assert.That(Stats.Instance.deflectedProjectilesFired, Is.EqualTo(reflected + 1));
        var enemyObject = new GameObject("Reflected projectile target");
        enemyObject.tag = "Enemy";
        enemyObject.transform.position = projectile.transform.position + Vector3.up * 0.5f;
        enemyObject.AddComponent<BoxCollider2D>().size = Vector2.one * 0.05f;
        var enemy = enemyObject.AddComponent<Enemy>();
        enemy.enemyType = Enemy.eEnemyType.Normal;
        enemy.maxHP = 20;
        enemy.InitWithLevel(0);
        EnemySpawner.Instance.instantiatedEnemies.Add(enemy);
        try
        {
            int hits = Stats.Instance.deflectedProjectilesHit;
            projectile.speed = 2f / Time.deltaTime;
            ProjectileManager.Instance.UpdateNormal();
            Assert.That(Stats.Instance.deflectedProjectilesHit, Is.EqualTo(hits + 1));
            Assert.That(ProjectileManager.Instance.allProjectiles, Has.No.Member(projectile));
        }
        finally
        {
            Time.timeScale = 1f;
            EnemySpawner.Instance.instantiatedEnemies.Remove(enemy);
            UnityEngine.Object.Destroy(enemyObject);
        }
    }

    [UnityTest]
    public IEnumerator ArtilleryMustEnterActualTowerRangeBeforeFiring()
    {
        var tower = Tower.Instance;
        var radar = StationModule.GetModuleByType(StationModule.eModuleType.Radar);
        float originalBaseRange = tower.initialFireRange;
        float originalRange = tower.fireRange;
        bool originalBuilt = radar.isBuilt;
        var enemyObject = new GameObject("Artillery range test");
        var enemy = enemyObject.AddComponent<Enemy>();
        enemy.enemyType = Enemy.eEnemyType.Ranged;
        enemy.fireRange = 5f;
        typeof(Enemy).GetField("target", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(enemy, tower.transform.position + Vector3.up * 0.25f);
        var inRange = typeof(Enemy).GetMethod("TargetInFireRange", BindingFlags.Instance | BindingFlags.NonPublic);
        try
        {
            tower.initialFireRange = 1f;
            tower.fireRange = 2f;
            foreach (bool built in new[] { false, true })
            {
                radar.isBuilt = built;
                float range = built ? 2f : 1f;
                enemy.transform.position = tower.transform.position + Vector3.up * range * 1.05f;
                Assert.That((bool)inRange.Invoke(enemy, null), Is.False, "Enemy range must not allow firing from outside turret range.");
                enemy.transform.position = tower.transform.position + Vector3.up * range * 0.85f;
                Assert.That((bool)inRange.Invoke(enemy, null), Is.True);
            }
            radar.isBuilt = false;
            Assert.That((bool)inRange.Invoke(enemy, null), Is.False, "Radar loss must force artillery to approach again.");
        }
        finally
        {
            tower.initialFireRange = originalBaseRange;
            tower.fireRange = originalRange;
            radar.isBuilt = originalBuilt;
            UnityEngine.Object.Destroy(enemyObject);
        }
        yield break;
    }

    [UnityTest]
    public IEnumerator DroneModuleBuildGivesOneStarterWithoutDuplicatingOnLoadOrRebuild()
    {
        var module = StationModule.GetModuleByType(StationModule.eModuleType.Drone);
        ResourceManager.Instance.curMaterials = module.cost;
        ModulesUI.Instance.SelectModule(StationModule.eModuleType.Drone);
        ModulesUI.Instance.BuySelectedModule();
        Assert.That(DroneManager.Instance.allDrones, Has.Count.EqualTo(1));
        Assert.That(DroneManager.Instance.currentDroneSlots, Is.EqualTo(1));
        Assert.That(ResourceManager.Instance.curMaterials, Is.Zero);
        Assert.That(Stats.Instance.dronesBuilt, Is.EqualTo(1));
        ModulesUI.Instance.BuySelectedModule();
        Assert.That(DroneManager.Instance.allDrones, Has.Count.EqualTo(1));

        yield return ReloadMainScene();
        module = StationModule.GetModuleByType(StationModule.eModuleType.Drone);
        Assert.That(DroneManager.Instance.allDrones, Has.Count.EqualTo(1));
        Assert.That(Stats.Instance.dronesBuilt, Is.EqualTo(1));
        module.TakeDamage(module.currentHP);
        ResourceManager.Instance.curMaterials = module.cost;
        ModulesUI.Instance.SelectModule(StationModule.eModuleType.Drone);
        ModulesUI.Instance.BuySelectedModule();
        Assert.That(DroneManager.Instance.allDrones, Has.Count.EqualTo(1));
        Assert.That(Stats.Instance.dronesBuilt, Is.EqualTo(1));
        var count = UpgradeAttribute.GetUpgradeByName(UpgradeAttribute.eUpgradeName.DroneCount);
        Assert.That(count.CalculateValue(count.maxLevel), Is.EqualTo(4f));
    }

    [UnityTest]
    public IEnumerator RadarBuildImprovesRangeAndOldLevelsLoadWithinNewLimits()
    {
        var tower = Tower.Instance;
        var radar = StationModule.GetModuleByType(StationModule.eModuleType.Radar);
        Assert.That(tower.initialFireRange, Is.EqualTo(2.5f));
        Assert.That(tower.EffectiveFireRange, Is.EqualTo(2.5f));
        ResourceManager.Instance.curMaterials = radar.cost;
        ModulesUI.Instance.SelectModule(StationModule.eModuleType.Radar);
        ModulesUI.Instance.BuySelectedModule();
        Assert.That(tower.EffectiveFireRange, Is.EqualTo(3f));
        var range = UpgradeAttribute.GetUpgradeByName(UpgradeAttribute.eUpgradeName.FireRange);
        range.Upgrade();
        Assert.That(tower.EffectiveFireRange, Is.EqualTo(3.1f).Within(0.001f));
        radar.TakeDamage(radar.currentHP);
        Assert.That(tower.EffectiveFireRange, Is.EqualTo(2.5f));
        range.level = 84;
        SaveGameManager.Instance.Save();

        yield return ReloadMainScene();
        range = UpgradeAttribute.GetUpgradeByName(UpgradeAttribute.eUpgradeName.FireRange);
        Assert.That(range.level, Is.EqualTo(16));
        Assert.That(range.currentValue, Is.EqualTo(4.6f).Within(0.001f));
        Assert.That(Tower.Instance.EffectiveFireRange, Is.EqualTo(2.5f));
        radar = StationModule.GetModuleByType(StationModule.eModuleType.Radar);
        ResourceManager.Instance.curMaterials = radar.cost;
        ModulesUI.Instance.SelectModule(StationModule.eModuleType.Radar);
        ModulesUI.Instance.BuySelectedModule();
        Assert.That(Tower.Instance.EffectiveFireRange, Is.EqualTo(4.6f).Within(0.001f));
        Assert.That(range.IsPermanent, Is.False);
        Assert.That(UpgradeAttribute.GetUpgradeByName(UpgradeAttribute.eUpgradeName.StructuralIntegrity).IsPermanent, Is.True);
        Assert.That(UpgradeAttribute.GetUpgradeByName(UpgradeAttribute.eUpgradeName.DroneHP).IsPermanent, Is.True);
        Assert.That(UpgradeAttribute.GetUpgradeByName(UpgradeAttribute.eUpgradeName.DroneDamage).IsPermanent, Is.True);
    }

    [UnityTest]
    public IEnumerator DroneUpgrades_UpdateExistingDronesAndPreserveDamageTaken()
    {
        var module = StationModule.GetModuleByType(StationModule.eModuleType.Drone);
        module.isBuilt = true;
        module.gameObject.SetActive(true);
        var manager = DroneManager.Instance;
        manager.AfterModulInit();
        var count = UpgradeAttribute.GetUpgradeByName(UpgradeAttribute.eUpgradeName.DroneCount);
        count.level = 1;
        count.RecalculateFromLevel();
        count.ApplyUpgradeEffect();
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
        int upgradedHP = drone.maxHP;
        int damagedHP = drone.currentHP;
        int upgradedDamage = drone.damage;

        module.TakeDamage(module.currentHP);
        Assert.That(module.isBuilt, Is.False);
        Assert.That(manager.allDrones, Has.Count.EqualTo(1));
        Assert.That(drone.maxHP, Is.EqualTo(upgradedHP));
        Assert.That(drone.currentHP, Is.EqualTo(damagedHP));
        Assert.That(drone.damage, Is.EqualTo(upgradedDamage));
        float countdown = manager.droneBuildCountdown;
        drone.transform.position = StationModule.GetModuleByType(StationModule.eModuleType.Core).transform.position + Vector3.up * 10f;
        Vector3 position = drone.transform.position;
        manager.UpdateNormal();
        Assert.That(drone.transform.position, Is.Not.EqualTo(position), "Surviving drones must remain active.");
        Assert.That(manager.droneBuildCountdown, Is.EqualTo(countdown));
        Assert.That(manager.allDrones, Has.Count.EqualTo(1));

        yield return ReloadMainScene();
        manager = DroneManager.Instance;
        module = StationModule.GetModuleByType(StationModule.eModuleType.Drone);
        Assert.That(module.isBuilt, Is.False);
        Assert.That(manager.allDrones, Has.Count.EqualTo(1));
        drone = manager.allDrones[0];
        Assert.That(drone.maxHP, Is.EqualTo(upgradedHP));
        Assert.That(drone.currentHP, Is.EqualTo(damagedHP));
        Assert.That(drone.damage, Is.EqualTo(upgradedDamage));

        ResourceManager.Instance.curMaterials = module.cost;
        ModulesUI.Instance.SelectModule(StationModule.eModuleType.Drone);
        ModulesUI.Instance.BuySelectedModule();
        Assert.That(module.isBuilt, Is.True);
        Assert.That(manager.allDrones, Has.Count.EqualTo(1));
        Assert.That(drone.currentHP, Is.EqualTo(damagedHP));
        Assert.That(drone.damage, Is.EqualTo(upgradedDamage));
        Assert.That(manager.currentDroneSlots, Is.EqualTo(2));
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
    public IEnumerator PauseMenu_AudioButtonsChangeTheirIcons()
    {
        var menu = UnityEngine.Object.FindFirstObjectByType<PauseMenu>();
        Assert.That(menu, Is.Not.Null);
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var musicButton = (Button)typeof(PauseMenu).GetField("musicButton", flags).GetValue(menu);
        var effectsButton = (Button)typeof(PauseMenu).GetField("effectsButton", flags).GetValue(menu);
        var musicIcon = (Image)typeof(PauseMenu).GetField("musicIcon", flags).GetValue(menu);
        var effectsIcon = (Image)typeof(PauseMenu).GetField("effectsIcon", flags).GetValue(menu);
        var originalMusic = PlayerPrefs.GetInt("Aegis.MusicEnabled", 1);
        var originalEffects = PlayerPrefs.GetInt("Aegis.EffectsEnabled", 1);

        try
        {
            Assert.That(musicIcon.sprite, Is.Not.Null);
            Assert.That(effectsIcon.sprite, Is.Not.Null);
            var firstMusicIcon = musicIcon.sprite;
            var firstEffectsIcon = effectsIcon.sprite;

            musicButton.onClick.Invoke();
            effectsButton.onClick.Invoke();

            Assert.That(musicIcon.sprite, Is.Not.SameAs(firstMusicIcon));
            Assert.That(effectsIcon.sprite, Is.Not.SameAs(firstEffectsIcon));
            Assert.That(PlayerPrefs.GetInt("Aegis.MusicEnabled", 1), Is.Not.EqualTo(originalMusic));
            Assert.That(PlayerPrefs.GetInt("Aegis.EffectsEnabled", 1), Is.Not.EqualTo(originalEffects));
        }
        finally
        {
            SoundManager.Instance.ToggleMusic(originalMusic != 0);
            SoundManager.Instance.ToggleEffects(originalEffects != 0);
        }
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

        var view = UnityEngine.Object.FindObjectsByType<UpgradeButtonView>(
            FindObjectsInactive.Include, FindObjectsSortMode.None)
            .First(item => item.moduleType == module.moduleType && item.gameObject.activeSelf);
        var button = view.button;
        Assert.That(button.interactable, Is.True);
        Assert.That(button.image.color.grayscale, Is.LessThan(0.5f));
        var marker = view.marker;
        Assert.That(marker.enabled, Is.True);
        Assert.That(marker.color, Is.EqualTo(UpgradeUI.Instance.colorBtnFrameCantBuy));

        button.onClick.Invoke();
        Assert.That(button.image.color.grayscale, Is.LessThan(0.5f));
        ResourceManager.Instance.curMaterials = 100000;
        UpgradeUI.Instance.Refresh();
        Assert.That(marker.color, Is.EqualTo(Color.white));
        yield break;
    }

    [UnityTest]
    public IEnumerator PreparedUpgradeButtonsReuseInstancesAndShowOnlySelectedModule()
    {
        var views = UnityEngine.Object.FindObjectsByType<UpgradeButtonView>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert.That(views.Length, Is.EqualTo(16));
        var instanceIds = views.Select(view => view.GetInstanceID()).OrderBy(id => id).ToArray();
        Assert.That(views.All(view => !view.gameObject.activeSelf), Is.True);
        foreach (var module in StationModule.allModules.Where(item => item.upgradeSet))
        {
            module.isBuilt = true;
            UpgradeUI.Instance.Show(module.upgradeSet);
            var visible = views.Where(view => view.gameObject.activeInHierarchy).ToArray();
            Assert.That(visible.Length, Is.EqualTo(module.upgradeSet.upgradeAttributes.Count));
            Assert.That(visible.All(view => view.moduleType == module.moduleType), Is.True);
            foreach (var view in visible)
            {
                Assert.That(((RectTransform)view.transform).rect.height, Is.GreaterThanOrEqualTo(190f), view.name);
                Assert.That(view.symbol.sprite, Is.Not.Null, view.name);
                Assert.That(view.valueText.text, Is.Not.Empty, view.name);
                Assert.That(view.button, Is.InstanceOf<UpgradeButton>(), view.name);
            }
            UpgradeUI.Instance.Hide();
            Assert.That(views.All(view => !view.gameObject.activeSelf), Is.True);
        }
        yield return null;
        Assert.That(UnityEngine.Object.FindObjectsByType<UpgradeButtonView>(
            FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Select(view => view.GetInstanceID()).OrderBy(id => id).ToArray(), Is.EqualTo(instanceIds));

        var command = StationModule.GetModuleByType(StationModule.eModuleType.CommandUnit);
        var rotation = command.upgradeSet.upgradeAttributes.Single(
            upgrade => upgrade.upgradeName == UpgradeAttribute.eUpgradeName.RotationSpeed);
        rotation.level = rotation.maxLevel;
        rotation.RecalculateFromLevel();
        ResourceManager.Instance.curMaterials = 0;
        UpgradeUI.Instance.Show(command.upgradeSet);
        var rotationView = views.Single(view => view.upgradeName == rotation.upgradeName);
        Assert.That(rotationView.priceText.text, Is.EqualTo("MAX"));
        Assert.That(rotationView.marker.color, Is.Not.EqualTo(UpgradeUI.Instance.colorBtnFrameCantBuy));
    }

    [Test]
    public void CompletedWave_AwardsBonusOnceAndPersistsIt()
    {
        var spawner = EnemySpawner.Instance;
        spawner.StopAllCoroutines();
        typeof(EnemySpawner).GetField("enableSpawning", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(spawner, false);
        typeof(EnemySpawner).GetField("waveIsRunning", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(spawner, false);
        EnemySpawner.RemoveAllEnemies();
        spawner.currentWaveIndex = 1;
        ResourceManager.Instance.curMaterials = 100;
        SaveGameManager.Instance.CaptureWaveCheckpoint();
        spawner.UpdateNormal();
        Assert.That(ResourceManager.Instance.curMaterials, Is.EqualTo(115));
        spawner.UpdateNormal();
        Assert.That(ResourceManager.Instance.curMaterials, Is.EqualTo(115));
        ResourceManager.Instance.curMaterials = 0;
        SaveGameManager.Instance.Load();
        Assert.That(ResourceManager.Instance.curMaterials, Is.EqualTo(115));
        spawner.currentWaveIndex = 2;
        SaveGameManager.Instance.CaptureWaveCheckpoint();
        spawner.UpdateNormal();
        Assert.That(ResourceManager.Instance.curMaterials, Is.EqualTo(133));
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

        yield return new WaitForSecondsRealtime(1.1f);
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

        yield return new WaitForSecondsRealtime(1.1f);
        var saved = JsonUtility.FromJson<SaveGame>(
            File.ReadAllText(Path.Combine(saveDirectory, "savegame.json")));
        Assert.That(saved.material, Is.EqualTo(10));
        Assert.That(saved.upgrades.Single(u => u.upgradeName == "Damage").level,
            Is.EqualTo(initialLevel + 1));
        yield break;
    }

    [UnityTest]
    public IEnumerator RapidUpgradeClicksUseCurrentPricesAndCannotOverspend()
    {
        var upgrade = UpgradeAttribute.GetUpgradeByName(UpgradeAttribute.eUpgradeName.Damage);
        var owner = StationModule.allModules.Single(m =>
            m.upgradeSet && m.upgradeSet.upgradeAttributes.Contains(upgrade));
        int initialLevel = upgrade.level;
        Assert.That(upgrade.maxLevel, Is.GreaterThanOrEqualTo(initialLevel + 2));
        int budget = Mathf.RoundToInt(upgrade.CalculateCost(initialLevel)) +
            Mathf.RoundToInt(upgrade.CalculateCost(initialLevel + 1));

        UIManager.Instance.Show(true);
        owner.isBuilt = true;
        owner.gameObject.SetActive(true);
        ModulesUI.Instance.SelectModule(owner.moduleType);
        UpgradeUI.Instance.Show(owner.upgradeSet);
        UpgradeUI.Instance.currentSelectedUpgrade = upgrade.upgradeName;
        ResourceManager.Instance.curMaterials = budget;
        int purchases = Stats.Instance.boughtUpgrades;
        int costs = Stats.Instance.totalUpgradeCosts;
        var container = (Transform)typeof(UpgradeUI).GetField("contentContainer",
            BindingFlags.Instance | BindingFlags.NonPublic).GetValue(UpgradeUI.Instance);
        var button = container.GetComponentsInChildren<UpgradeButtonView>()
            .Single(view => view.upgradeName == upgrade.upgradeName).button as UpgradeButton;
        Assert.That(button, Is.Not.Null, "The prefab must use press-triggered upgrade buttons.");
        var pointer = new PointerEventData(EventSystem.current)
        {
            button = PointerEventData.InputButton.Left
        };

        // Exercise pointer events, including rapid taps and release without duplicate purchases.
        for (int i = 0; i < 10; i++)
        {
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            int expectedLevel = initialLevel + Mathf.Min(i + 1, 2);
            Assert.That(upgrade.level, Is.EqualTo(expectedLevel), "Purchase must happen on press.");
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(upgrade.level, Is.EqualTo(expectedLevel), "Release must not purchase again.");
        }

        Assert.That(upgrade.level, Is.EqualTo(initialLevel + 2));
        Assert.That(ResourceManager.Instance.curMaterials, Is.Zero);
        Assert.That(Stats.Instance.boughtUpgrades, Is.EqualTo(purchases + 2));
        Assert.That(Stats.Instance.totalUpgradeCosts, Is.EqualTo(costs + budget));

        yield return new WaitForSecondsRealtime(1.1f);
        var saved = JsonUtility.FromJson<SaveGame>(
            File.ReadAllText(Path.Combine(saveDirectory, "savegame.json")));
        Assert.That(saved.material, Is.Zero);
        Assert.That(saved.upgrades.Single(u => u.upgradeName == "Damage").level,
            Is.EqualTo(initialLevel + 2));
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
    public IEnumerator CommandUnitLossAndLoadPreservePurchasedHP()
    {
        var command = StationModule.GetModuleByType(StationModule.eModuleType.CommandUnit);
        var extractor = StationModule.GetModuleByType(StationModule.eModuleType.Extractor);
        var core = StationModule.GetModuleByType(StationModule.eModuleType.Core);
        var integrity = UpgradeAttribute.GetUpgradeByName(UpgradeAttribute.eUpgradeName.StructuralIntegrity);
        Assert.That(core.upgradeSet.upgradeAttributes, Does.Contain(integrity));
        Assert.That(command.upgradeSet.upgradeAttributes.Contains(integrity), Is.False);
        var efficiency = UpgradeAttribute.GetUpgradeByName(UpgradeAttribute.eUpgradeName.CollectingEfficiency);
        var rotation = UpgradeAttribute.GetUpgradeByName(UpgradeAttribute.eUpgradeName.RotationSpeed);
        command.isBuilt = true;
        command.gameObject.SetActive(true);
        extractor.isBuilt = true;
        extractor.gameObject.SetActive(true);
        integrity.level = 1;
        integrity.RecalculateFromLevel();
        efficiency.level = 1;
        efficiency.RecalculateFromLevel();
        rotation.level = 1;
        rotation.RecalculateFromLevel();
        UpgradeAttribute.ApplyAllUpgradeEffect();
        int upgradedHP = core.maxHP;
        core.TakeDamage(1);
        int damagedHP = core.currentHP;
        float upgradedEfficiency = ResourceManager.Instance.collectingEffeciency;
        Assert.That(upgradedHP, Is.GreaterThan(Mathf.RoundToInt(integrity.baseValue)));
        Assert.That(upgradedEfficiency, Is.GreaterThan(efficiency.baseValue));

        command.TakeDamage(command.currentHP);
        Assert.That(core.maxHP, Is.EqualTo(upgradedHP));
        Assert.That(core.currentHP, Is.EqualTo(damagedHP));
        Assert.That(Tower.Instance.rotationSpeed, Is.EqualTo(rotation.baseValue));
        Assert.That(ResourceManager.Instance.collectingEffeciency, Is.EqualTo(upgradedEfficiency));
        Assert.That(integrity.currentValue, Is.GreaterThan(integrity.baseValue));

        yield return ReloadMainScene();

        command = StationModule.GetModuleByType(StationModule.eModuleType.CommandUnit);
        core = StationModule.GetModuleByType(StationModule.eModuleType.Core);
        Assert.That(command.isBuilt, Is.False);
        Assert.That(core.maxHP, Is.EqualTo(upgradedHP));
        Assert.That(core.currentHP, Is.EqualTo(damagedHP));
        rotation = UpgradeAttribute.GetUpgradeByName(UpgradeAttribute.eUpgradeName.RotationSpeed);
        Assert.That(Tower.Instance.rotationSpeed, Is.EqualTo(rotation.baseValue));
        Assert.That(ResourceManager.Instance.collectingEffeciency, Is.EqualTo(upgradedEfficiency));

        var radar = StationModule.GetModuleByType(StationModule.eModuleType.Radar);
        radar.isBuilt = false;
        radar.currentHP = 0;
        ResourceManager.Instance.curMaterials = command.cost + 10;
        ModulesUI.Instance.SelectModule(StationModule.eModuleType.CommandUnit);
        ModulesUI.Instance.BuySelectedModule();
        Assert.That(command.isBuilt, Is.True);
        Assert.That(core.maxHP, Is.EqualTo(upgradedHP));
        Assert.That(core.currentHP, Is.EqualTo(damagedHP));
        Assert.That(Tower.Instance.rotationSpeed, Is.EqualTo(rotation.currentValue));
        Assert.That(ResourceManager.Instance.collectingEffeciency, Is.EqualTo(upgradedEfficiency));
        Assert.That(radar.isBuilt, Is.False);
        Assert.That(radar.currentHP, Is.Zero);
    }

    [UnityTest]
    public IEnumerator TargetPriorityChargesOnceAndPersistsThroughModuleLossAndLoad()
    {
        var command = StationModule.GetModuleByType(StationModule.eModuleType.CommandUnit);
        command.isBuilt = true;
        command.gameObject.SetActive(true);
        var upgrade = UpgradeAttribute.GetUpgradeByName(UpgradeAttribute.eUpgradeName.TargetPriority);
        UpgradeUI.Instance.Show(command.upgradeSet);
        UpgradeUI.Instance.currentSelectedUpgrade = upgrade.upgradeName;
        var click = typeof(UpgradeUI).GetMethod("OnUpgradeClicked", BindingFlags.Instance | BindingFlags.NonPublic);
        int price = Mathf.RoundToInt(upgrade.cost);
        ResourceManager.Instance.curMaterials = price - 1;
        click.Invoke(UpgradeUI.Instance, new object[] { upgrade });
        Assert.That(upgrade.level, Is.Zero);
        ResourceManager.Instance.curMaterials = price;
        click.Invoke(UpgradeUI.Instance, new object[] { upgrade });
        Assert.That(upgrade.level, Is.EqualTo(1));
        Assert.That(ResourceManager.Instance.curMaterials, Is.Zero);
        Assert.That(Tower.Instance.EffectivePriority, Is.EqualTo(Tower.TargetPriority.ArtilleryFirst));
        click.Invoke(UpgradeUI.Instance, new object[] { upgrade });
        Assert.That(Tower.Instance.SelectedPriority, Is.EqualTo(Tower.TargetPriority.Strongest));
        click.Invoke(UpgradeUI.Instance, new object[] { upgrade });
        Assert.That(Tower.Instance.SelectedPriority, Is.EqualTo(Tower.TargetPriority.Nearest));
        click.Invoke(UpgradeUI.Instance, new object[] { upgrade });
        Assert.That(Tower.Instance.SelectedPriority, Is.EqualTo(Tower.TargetPriority.ArtilleryFirst));
        Assert.That(ResourceManager.Instance.curMaterials, Is.Zero);
        Assert.That(Stats.Instance.boughtUpgrades, Is.EqualTo(1));

        command.TakeDamage(command.currentHP);
        Assert.That(Tower.Instance.EffectivePriority, Is.EqualTo(Tower.TargetPriority.Nearest));
        yield return ReloadMainScene();
        Assert.That(Tower.Instance.SelectedPriority, Is.EqualTo(Tower.TargetPriority.ArtilleryFirst));
        Assert.That(Tower.Instance.EffectivePriority, Is.EqualTo(Tower.TargetPriority.Nearest));
        command = StationModule.GetModuleByType(StationModule.eModuleType.CommandUnit);
        ResourceManager.Instance.curMaterials = command.cost;
        ModulesUI.Instance.SelectModule(command.moduleType);
        ModulesUI.Instance.BuySelectedModule();
        Assert.That(Tower.Instance.EffectivePriority, Is.EqualTo(Tower.TargetPriority.ArtilleryFirst));
        GameManager.Instance.GameOver();
        GameManager.Instance.Replay();
        GameManager.isInit = false;
        Assert.That(Tower.Instance.SelectedPriority, Is.EqualTo(Tower.TargetPriority.Nearest));
        Assert.That(UpgradeAttribute.GetUpgradeByName(UpgradeAttribute.eUpgradeName.TargetPriority).level, Is.Zero);
    }

    [Test]
    public void TargetPrioritySelectsReachableEnemiesAndFallsBackToNearest()
    {
        var command = StationModule.GetModuleByType(StationModule.eModuleType.CommandUnit);
        command.isBuilt = true;
        var upgrade = UpgradeAttribute.GetUpgradeByName(UpgradeAttribute.eUpgradeName.TargetPriority);
        upgrade.level = 1;
        var tower = Tower.Instance;
        var findTarget = typeof(Tower).GetMethod("FindTarget", BindingFlags.Instance | BindingFlags.NonPublic);
        var currentTarget = typeof(Tower).GetField("currentTarget", BindingFlags.Instance | BindingFlags.NonPublic);
        var enemies = new Enemy[3];
        try
        {
            for (int i = 0; i < enemies.Length; i++)
            {
                enemies[i] = new GameObject("Priority test enemy").AddComponent<Enemy>();
                enemies[i].transform.position = tower.transform.position + Vector3.up * (1f + i * 0.5f);
                enemies[i].maxHP = i == 2 ? 10 : i + 1;
                enemies[i].enemyType = i == 1 ? Enemy.eEnemyType.Ranged : Enemy.eEnemyType.Normal;
                EnemySpawner.Instance.instantiatedEnemies.Add(enemies[i]);
            }
            tower.SetTargetPriority(Tower.TargetPriority.Nearest);
            findTarget.Invoke(tower, null);
            Assert.That(currentTarget.GetValue(tower), Is.EqualTo(enemies[0].transform));
            tower.SetTargetPriority(Tower.TargetPriority.ArtilleryFirst);
            findTarget.Invoke(tower, null);
            Assert.That(currentTarget.GetValue(tower), Is.EqualTo(enemies[1].transform));
            tower.SetTargetPriority(Tower.TargetPriority.Strongest);
            findTarget.Invoke(tower, null);
            Assert.That(currentTarget.GetValue(tower), Is.EqualTo(enemies[2].transform));
            enemies[2].transform.position = tower.transform.position + Vector3.up * (tower.EffectiveFireRange + 1f);
            findTarget.Invoke(tower, null);
            Assert.That(currentTarget.GetValue(tower), Is.EqualTo(enemies[1].transform));
            tower.SetTargetPriority(Tower.TargetPriority.ArtilleryFirst);
            enemies[1].enemyType = Enemy.eEnemyType.Normal;
            findTarget.Invoke(tower, null);
            Assert.That(currentTarget.GetValue(tower), Is.EqualTo(enemies[0].transform));
            enemies[1].enemyType = Enemy.eEnemyType.Ranged;
            command.isBuilt = false;
            findTarget.Invoke(tower, null);
            Assert.That(currentTarget.GetValue(tower), Is.EqualTo(enemies[0].transform));
            tower.RestoreTargetPriority("invalid");
            Assert.That(tower.SelectedPriority, Is.EqualTo(Tower.TargetPriority.Nearest));
            tower.RestoreTargetPriority(null);
            Assert.That(tower.SelectedPriority, Is.EqualTo(Tower.TargetPriority.Nearest));
        }
        finally
        {
            foreach (var enemy in enemies)
            {
                if (!enemy) continue;
                EnemySpawner.Instance.instantiatedEnemies.Remove(enemy);
                UnityEngine.Object.DestroyImmediate(enemy.gameObject);
            }
        }
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
        var earlyDelay = typeof(EnemySpawner).GetField("earlyWaveDelay",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(delay, Is.Not.Null);
        Assert.That(earlyDelay, Is.Not.Null);
        delay.SetValue(spawner, 0f);
        earlyDelay.SetValue(spawner, 0f);
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
        earlyDelay.SetValue(spawner, 0f);
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
