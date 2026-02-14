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

    string PathFile => Path.Combine(Application.persistentDataPath, "savegame.json");
    string PathBestFile => Path.Combine(Application.persistentDataPath, "bestscore.json");

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadBestScore();
    }

    public void Save()
    {
        currentSaveGame = BuildFromWorld();
        File.WriteAllText(PathFile, JsonUtility.ToJson(currentSaveGame, true));
    }

    public void Load()
    {
        if (!File.Exists(PathFile))
        {
            // Es gibt noch keinen Save → aktueller Scene-Start ist der Default
            currentSaveGame = BuildFromWorld();
            Save();
            return;
        }

        currentSaveGame = JsonUtility.FromJson<SaveGame>(File.ReadAllText(PathFile));
        ApplyToWorld(currentSaveGame);

        TimeController.Instance.RefreshPanel();
        ModulesUI.Instance.ResetModulePanel();
        ModulesUI.Instance.RefreshPanel();
        UpgradeUI.Instance.Refresh();
        DroneManager.Instance.CheckDroneCanBuild();
        ResourceManager.Instance.RefreshUI();
    }

    public void DeleteSaveData()
    {
        if (File.Exists(PathFile))
            File.Delete(PathFile);

#if UNITY_EDITOR
        // Im Editor: NUR Datei löschen
        // kein Reset, kein Save
        if (!Application.isPlaying)
            return;
#endif

        // Runtime (Play Mode)
        GameManager.Instance.ResetAll();
        Save(); // neuen Default-Save schreiben
    }

    SaveGame BuildFromWorld()
    {
        var data = new SaveGame();

        data.material = ResourceManager.Instance.curMaterials;
        data.currentWaveIndex = EnemySpawner.Instance.currentWaveIndex;
        data.currentShieldPoints = Shield.Instance.currentShieldPoints;

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
        DroneManager.Instance.droneBuildCountdown = data.droneBuildCountdown;

        // Modules
        foreach (var loadedModule in data.modules)
        {
            var module = StationModule.GetModuleByType(ParseModuleType(loadedModule.moduleType));
            if (!module) continue;

            module.isBuilt = loadedModule.isBuilt;
            module.currentHP = loadedModule.currentHP;
        }

        // Upgrades
        foreach (var loadedUpgradeAttribute in data.upgrades)
        {
            var upgradeAttribute = UpgradeAttribute.GetUpgradeByName(ParseUpgradeName(loadedUpgradeAttribute.upgradeName));
            if (upgradeAttribute == null) continue;
            upgradeAttribute.level = loadedUpgradeAttribute.level;
            upgradeAttribute.RecalculateFromLevel();
        }

        // Drones
        foreach (var loadedDrone in data.drones)
        {
            GameObject droneObj = DroneManager.Instance.SpawnDrone();
            Drone newDrone = droneObj.GetComponent<Drone>();
            newDrone.currentHP = loadedDrone.currentHP;
        }

        // Stats
        if (data.stats != null) Stats.Instance.ApplyStatsData(data.stats);

        UpgradeAttribute.ApplyAllUpgradeEffect();

        Shield.Instance.currentShieldPoints = data.currentShieldPoints;
        if (StationModule.GetModuleByType(StationModule.eModuleType.Shield).isBuilt)
            Shield.Instance.activateShield();
    }


    void LoadBestScore()
    {
        if (!File.Exists(PathBestFile))
        {
            bestSaveGame = new SaveGame();
            bestSaveGame.score = 0;
            SaveBestScoredSaveGame();
            return;
        }

        bestSaveGame = JsonUtility.FromJson<SaveGame>(File.ReadAllText(PathBestFile));
        if (bestSaveGame == null) bestSaveGame = new SaveGame();
    }

    void SaveBestScoredSaveGame()
    {
        if (bestSaveGame == null) bestSaveGame = new SaveGame();
        File.WriteAllText(PathBestFile, JsonUtility.ToJson(bestSaveGame, true));
    }

    public void TrySaveBestScore(int score)
    {
        score = Mathf.Max(0, score);

        if (bestSaveGame != null && score <= bestSaveGame.score)
            return;

        // currentSaveGame ist der neue Best
        currentSaveGame.score = score;

        // Deep Copy erstellen (wichtig!)
        bestSaveGame = JsonUtility.FromJson<SaveGame>(
            JsonUtility.ToJson(currentSaveGame)
        );

        File.WriteAllText(PathBestFile, JsonUtility.ToJson(bestSaveGame, true));
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
