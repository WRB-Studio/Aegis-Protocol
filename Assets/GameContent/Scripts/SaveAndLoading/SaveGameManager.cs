using System;
using System.Collections;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SaveGameManager : MonoBehaviour
{
    public static SaveGameManager Instance;
    public SaveGame currentSaveGame { get; private set; }

    public SaveGame bestSaveGame { get; private set; }
    bool isLoading;
    SaveGame waveCheckpoint;
    Coroutine pendingSave;
    public bool HasWaveCheckpoint => waveCheckpoint != null;

#if UNITY_EDITOR
    // Allows Play Mode tests to use an isolated save directory.
    public static string SaveDirectoryOverride;
#endif

    string SaveDirectory
    {
        get
        {
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(SaveDirectoryOverride)) return SaveDirectoryOverride;
#endif
            return Application.persistentDataPath;
        }
    }

    string PathFile => Path.Combine(SaveDirectory, "savegame.json");
    string PathBestFile => Path.Combine(SaveDirectory, "bestscore.json");
    string PathBackupFile => PathFile + ".bak";

    void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        LoadBestScore();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void OnApplicationPause(bool pause)
    {
        if (pause && GameManager.isInit && !GameManager.gameOver) Save();
    }

    void OnApplicationQuit()
    {
        if (GameManager.isInit && !GameManager.gameOver) Save();
    }

    public void Save()
    {
        if (isLoading) return;
        if (pendingSave != null)
        {
            StopCoroutine(pendingSave);
            pendingSave = null;
        }

        currentSaveGame = BuildFromWorld();
        WriteFile(PathFile, waveCheckpoint ?? currentSaveGame);
    }

    public void RequestSave()
    {
        if (!isLoading && !GameManager.gameOver && pendingSave == null)
            pendingSave = StartCoroutine(SaveAfterDelay());
    }

    IEnumerator SaveAfterDelay()
    {
        yield return new WaitForSecondsRealtime(1f);
        pendingSave = null;
        Save();
    }

    public void CaptureWaveCheckpoint()
    {
        waveCheckpoint = BuildFromWorld();
        currentSaveGame = waveCheckpoint;
        WriteFile(PathFile, waveCheckpoint);
    }

    public void ClearWaveCheckpoint()
    {
        waveCheckpoint = null;
    }

    public void Load()
    {
        ClearWaveCheckpoint();
        if (!File.Exists(PathFile))
        {
            Save();
            return;
        }

        if (!TryReadSave(PathFile, out var loaded))
        {
            try { File.Move(PathFile, PathFile + ".corrupt-" + DateTime.UtcNow.Ticks); }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not quarantine corrupt save: {exception.Message}");
            }

            if (!TryReadSave(PathBackupFile, out loaded))
            {
                Save();
                return;
            }

            WriteFile(PathFile, loaded);
        }

        currentSaveGame = loaded;
        isLoading = true;
        try
        {
            ApplyToWorld(currentSaveGame);

            TimeController.Instance.RefreshPanel();
            ModulesUI.Instance.ResetModulePanel();
            ModulesUI.Instance.RefreshPanel();
            UpgradeUI.Instance.Refresh();
            DroneManager.Instance.CheckDroneCanBuild();
            ResourceManager.Instance.RefreshUI();
        }
        finally
        {
            isLoading = false;
        }
    }

    public void DeleteSaveData()
    {
        ClearWaveCheckpoint();
        if (File.Exists(PathFile))
            File.Delete(PathFile);
    }

    bool TryReadSave(string path, out SaveGame data)
    {
        data = null;
        if (!File.Exists(path)) return false;

        try
        {
            data = JsonUtility.FromJson<SaveGame>(File.ReadAllText(path));
            if (!IsValidSave(data))
            {
                Debug.LogWarning($"Invalid save data in {path}");
                return false;
            }
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Could not read save {path}: {exception.Message}");
            return false;
        }
    }

    bool IsValidSave(SaveGame data)
    {
        if (data == null || data.version < 1 || data.version > 2 ||
            data.material < 0 || data.currentWaveIndex < 0 ||
            data.modules == null || data.upgrades == null || data.drones == null ||
            !data.modules.Exists(module => module != null && module.moduleType == "Core") ||
            float.IsNaN(data.currentShieldPoints) || float.IsInfinity(data.currentShieldPoints))
            return false;

        foreach (var module in data.modules)
            if (module == null || module.currentHP < 0 ||
                !Enum.TryParse(module.moduleType, out StationModule.eModuleType type) ||
                !Enum.IsDefined(typeof(StationModule.eModuleType), type)) return false;

        foreach (var upgrade in data.upgrades)
        {
            if (upgrade == null ||
                !Enum.TryParse(upgrade.upgradeName, out UpgradeAttribute.eUpgradeName name)) return false;
            var runtimeUpgrade = UpgradeAttribute.GetUpgradeByName(name);
            if (runtimeUpgrade == null || upgrade.level < 0 || upgrade.level > runtimeUpgrade.maxLevel)
                return false;
        }

        return true;
    }

    void WriteFile(string path, SaveGame data)
    {
        string tempPath = path + ".tmp";
        try
        {
            Directory.CreateDirectory(SaveDirectory);
            File.WriteAllText(tempPath, JsonUtility.ToJson(data, true));
            if (File.Exists(path)) File.Replace(tempPath, path, path == PathFile ? PathBackupFile : null);
            else File.Move(tempPath, path);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Could not write save {path}: {exception.Message}");
            try { if (File.Exists(tempPath)) File.Delete(tempPath); }
            catch (IOException) { }
        }
    }

    SaveGame BuildFromWorld()
    {
        var data = new SaveGame();

        data.material = ResourceManager.Instance.curMaterials;
        data.currentWaveIndex = EnemySpawner.Instance.currentWaveIndex;
        data.currentShieldPoints = Shield.Instance.currentShieldPoints;
        data.shieldRechargeCountdown = Shield.Instance.rechargeCountdown;

        // Drones
        data.droneBuildCountdown = DroneManager.Instance.droneBuildCountdown;
        data.drones.Clear();
        foreach (var drone in DroneManager.Instance.allDrones)
            if (drone) data.drones.Add(new SaveGame.DroneState { currentHP = drone.currentHP });

        // Modules
        data.modules.Clear();
        foreach (var module in StationModule.allModules)
            if (module != null) data.modules.Add(new SaveGame.ModuleState
            {
                moduleType = module.moduleType.ToString(),
                isBuilt = module.isBuilt,
                wasDestroyed = module.wasDestroyed,
                currentHP = module.currentHP
            });

        // Upgrades
        data.upgrades.Clear();
        foreach (var upgradeAttribute in UpgradeAttribute.allUpgradeAttributes)
            if (upgradeAttribute != null) data.upgrades.Add(new SaveGame.UpgradeState
            {
                upgradeName = upgradeAttribute.upgradeName.ToString(),
                level = upgradeAttribute.level
            });

        // Stats
        data.stats = Stats.Instance.GetStatsData();

        data.score = ScoreManager.Instance.GetBreakdown(Stats.Instance).totalScore;

        return data;
    }

    void ApplyToWorld(SaveGame data)
    {
        ResourceManager.Instance.curMaterials = data.material;
        EnemySpawner.Instance.currentWaveIndex = data.currentWaveIndex;
        // Modules
        foreach (var loadedModule in data.modules)
        {
            var module = StationModule.GetModuleByType(ParseModuleType(loadedModule.moduleType));
            if (!module) continue;

            module.isBuilt = loadedModule.isBuilt;
            module.wasDestroyed = loadedModule.wasDestroyed;
        }

        // Upgrades
        foreach (var loadedUpgradeAttribute in data.upgrades)
        {
            var upgradeAttribute = UpgradeAttribute.GetUpgradeByName(ParseUpgradeName(loadedUpgradeAttribute.upgradeName));
            if (upgradeAttribute == null) continue;
            upgradeAttribute.level = loadedUpgradeAttribute.level;
            upgradeAttribute.RecalculateFromLevel();
        }

        UpgradeAttribute.ApplyAllUpgradeEffect();

        // Restore current HP only after upgrades have established maximum HP.
        foreach (var loadedModule in data.modules)
        {
            var module = StationModule.GetModuleByType(ParseModuleType(loadedModule.moduleType));
            if (module) module.currentHP = Mathf.Clamp(loadedModule.currentHP, 0, module.maxHP);
        }

        if (data.stats != null) Stats.Instance.ApplyStatsData(data.stats);

        var droneModule = StationModule.GetModuleByType(StationModule.eModuleType.Drone);
        if (droneModule && droneModule.isBuilt)
        {
            foreach (var loadedDrone in data.drones)
            {
                GameObject droneObj = DroneManager.Instance.SpawnDrone(true);
                if (!droneObj) break;
                var drone = droneObj.GetComponent<Drone>();
                drone.currentHP = Mathf.Clamp(loadedDrone.currentHP, 0, drone.maxHP);
            }

            DroneManager.Instance.AfterModulInit();
        }
        DroneManager.Instance.droneBuildCountdown = data.droneBuildCountdown;
        DroneManager.Instance.CheckDroneCanBuild();

        Shield.Instance.currentShieldPoints = Mathf.Clamp(data.currentShieldPoints, 0f, Shield.Instance.maxShieldPoints);
        var shieldModule = StationModule.GetModuleByType(StationModule.eModuleType.Shield);
        if (shieldModule && shieldModule.isBuilt && Shield.Instance.currentShieldPoints > 0f)
        {
            Shield.Instance.rechargeCountdown = 0f;
            Shield.Instance.activateShield();
        }
        else
        {
            Shield.Instance.deactivateShield();
            Shield.Instance.rechargeCountdown = shieldModule && shieldModule.isBuilt
                ? (data.shieldRechargeCountdown > 0f ? data.shieldRechargeCountdown : Shield.Instance.rechargeTime)
                : 0f;
            if (Shield.Instance.rechargeCountdown > 0f)
            {
                Shield.Instance.txtShieldRecharge.gameObject.SetActive(true);
                Shield.Instance.txtShieldRecharge.text = Mathf.CeilToInt(Shield.Instance.rechargeCountdown).ToString();
            }
        }
        Shield.Instance.refreshShieldPointSlider();
    }


    void LoadBestScore()
    {
        try
        {
            if (File.Exists(PathBestFile))
                bestSaveGame = JsonUtility.FromJson<SaveGame>(File.ReadAllText(PathBestFile));
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Could not read best score: {exception.Message}");
        }

        if (bestSaveGame != null && bestSaveGame.score >= 0) return;

        bestSaveGame = new SaveGame();
        SaveBestScoredSaveGame();
    }

    void SaveBestScoredSaveGame()
    {
        if (bestSaveGame == null) bestSaveGame = new SaveGame();
        WriteFile(PathBestFile, bestSaveGame);
    }

    public void TrySaveBestScore(int score)
    {
        score = Mathf.Max(0, score);

        if (bestSaveGame != null && score <= bestSaveGame.score)
            return;

        // currentSaveGame ist der neue Best
        if (currentSaveGame == null) currentSaveGame = BuildFromWorld();
        currentSaveGame.score = score;

        // Deep Copy erstellen (wichtig!)
        bestSaveGame = JsonUtility.FromJson<SaveGame>(
            JsonUtility.ToJson(currentSaveGame)
        );

        WriteFile(PathBestFile, bestSaveGame);
    }


    StationModule.eModuleType ParseModuleType(string v)
    {
        return System.Enum.TryParse(v, out StationModule.eModuleType t) ? t : StationModule.eModuleType.Core;
    }

    UpgradeAttribute.eUpgradeName ParseUpgradeName(string v)
    {
        return System.Enum.TryParse(v, out UpgradeAttribute.eUpgradeName t) ? t : UpgradeAttribute.eUpgradeName.None;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SaveGameManager))]
public class SaveGameManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SaveGameManager mgr = (SaveGameManager)target;

        GUILayout.Space(10);
        if (GUILayout.Button("DELETE SAVEGAME", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog(
                "Delete Savegame",
                "Savegame wirklich löschen?",
                "Ja, löschen",
                "Abbrechen"))
            {
                mgr.DeleteSaveData();
                Debug.Log("Savegame deleted.");
            }
        }
    }
}
#endif
