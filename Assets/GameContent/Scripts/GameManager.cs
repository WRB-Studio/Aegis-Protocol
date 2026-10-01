using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public static bool isInit = false;
    public static bool gameOver = false;

    public GameObject gameOverPanel;
    public TextMeshProUGUI txtGameOverStats;
    public GameObject DetailPanel;
    public TextMeshProUGUI txtDetails;
    public Button btnDetails;
    public Button btnCloseDetails;
    public Button btnReplay;


    private readonly List<IResettable> allResettable = new();
    private MatchReporter matchReporter;
    public string matchEndReason = "game_over";

#if UNITY_EDITOR
    private void OnValidate()
    {
        var sb = new System.Text.StringBuilder(256);

        sb.AppendLine($"<align=center>Play time 00:00:00</align>");
        sb.AppendLine();

        sb.AppendLine($"Time: 999");
        sb.AppendLine($"Waves: 999");
        sb.AppendLine($"Kills: 999");
        sb.AppendLine($"Upgrades: 999");
        sb.AppendLine($"Resources: 999");

        // modules penalty in red, as negative
        sb.AppendLine($"<color=#FF4D4D>Modules: -999</color>");
        sb.AppendLine("");
        sb.AppendLine($"<size=120%><b>Score</b>: 999999</size>");
        sb.AppendLine($"Best: 999999");

        txtGameOverStats.text = sb.ToString();


        sb = new System.Text.StringBuilder(256);

        sb.AppendLine($"<align=center>Play time: 00:00:00</align>");
        sb.AppendLine();

        sb.AppendLine($"<b>Waves:</b> 999");
        sb.AppendLine();

        sb.AppendLine($"<b>Enemies</b>");
        sb.AppendLine($"Spawned: 999");
        sb.AppendLine($"Killed: 999");
        sb.AppendLine();

        sb.AppendLine($"<b>Projectiles</b>");
        sb.AppendLine($"Tower shots: 999");
        sb.AppendLine($"Tower hits: 999");
        sb.AppendLine($"Drone shots: 999");
        sb.AppendLine($"Drone hits: 999");
        sb.AppendLine($"Enemy shots: 999");
        sb.AppendLine($"Enemy hits: 999");
        sb.AppendLine();

        sb.AppendLine($"<b>Modules</b>");
        sb.AppendLine($"Built: 999");
        sb.AppendLine($"Lost: 999");
        sb.AppendLine($"Damage: 999");
        sb.AppendLine();

        sb.AppendLine($"<b>Shield</b>");
        sb.AppendLine($"Damage: 999");
        sb.AppendLine($"Deflected: 999");
        sb.AppendLine($"Deflected hits: 999");
        sb.AppendLine();

        sb.AppendLine($"<b>Upgrading</b>");
        sb.AppendLine($"Bought: 999");
        sb.AppendLine($"Spent: 999 M");
        sb.AppendLine();

        sb.AppendLine($"<b>Resources</b>");
        sb.AppendLine($"Dropped: 999");
        sb.AppendLine($"Collected: 999 M");
        sb.AppendLine($"Auto-collected: 999 M");
        sb.AppendLine($"Total: 999 M");

        txtDetails.text = sb.ToString();
    }
#endif
    private void Awake()
    {
        Instance = this;
        isInit = false;
        gameOver = false;

        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;

        foreach (var r in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (r is IResettable i) allResettable.Add(i);
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        matchReporter?.Dispose();
        Instance = null;
        isInit = false;
        gameOver = false;
    }

    private void Start()
    {
        btnReplay.onClick.AddListener(() => { Replay(); });
        btnDetails.onClick.AddListener(() =>
        { DetailPanel.gameObject.SetActive(!DetailPanel.activeSelf); });
        btnCloseDetails.onClick.AddListener(() => { DetailPanel.SetActive(false); });

        gameOverPanel.SetActive(false);
        DetailPanel.gameObject.SetActive(false);

        InitScripts();

        foreach (var r in allResettable) r.StoreInit();
        UpgradeAttribute.StoreAllInits();

        SaveGameManager.Instance.Load();
        StartMatchReport(SaveGameManager.Instance.LoadedExistingSave);

        isInit = true;
    }

    public void ResetAll()
    {
        foreach (var r in allResettable) r.ResetScript();
        UpgradeAttribute.ResetAll();
    }

    private void InitScripts()
    {
        foreach (var module in StationModule.allModules)
            module.Init();

        Shield.Instance.Init();
        DroneManager.Instance.Init();
        EnemySpawner.Instance.Init();
        ProjectileManager.Instance.Init();
        Tower.Instance.Init();
        UpgradeManager.Instance.Init();
        ResourceManager.Instance.Init();

        UIManager.Instance.Init();
        ModulesUI.Instance.Init();
        UIToWorldLine.Instance.Init();
        UpgradeUI.Instance.Init();
        UpgradeAttribute.ApplyAllUpgradeEffect();
        TimeController.Instance.Init();

        SoundManager.Instance.Init();
    }

    private void Update()
    {
        if (gameOver && isInit)
        {
            EnemySpawner.Instance.UpdateGameOver();
            return;
        }

        if (gameOver || !isInit)
            return;

        Stats.Instance.playTime += Time.deltaTime;

        Shield.Instance.UpdateNormal();
        Tower.Instance.UpdateNormal();
        DroneManager.Instance.UpdateNormal();
        EnemySpawner.Instance.UpdateNormal();
        ProjectileManager.Instance.UpdateNormal();
        ResourceManager.Instance.UpdateNormal();

        UIManager.Instance.UpdateNormal();
        UIToWorldLine.Instance.UpdateNormal();
        matchReporter?.Tick();

    }

    public void GameOver()
    {
        if (gameOver) return;
        gameOver = true;

        Time.timeScale = 1f;

        SoundManager.Instance.PlayGameOverMusic();

        UIManager.Instance.Show(false);

        gameOverPanel.SetActive(true);
        ShowGameOverStats();

        txtDetails.text = BuildAllStatsText();

        SaveGameManager.Instance.TrySaveBestScore(ScoreManager.Instance.GetBreakdown(Stats.Instance).totalScore);
        SaveGameManager.Instance.DeleteSaveData();
    }

    private void ShowGameOverStats()
    {
        var b = FindAnyObjectByType<ScoreManager>().GetBreakdown(Stats.Instance);
        var sb = new System.Text.StringBuilder(256);

        sb.AppendLine($"<align=center>Play time {FormatTimeSmart(b.playTimeSeconds)}</align>");
        sb.AppendLine();

        sb.AppendLine($"Time: {b.timeScore}");
        sb.AppendLine($"Waves: {b.wavesScore}");
        sb.AppendLine($"Kills: {b.killsScore}");
        sb.AppendLine($"Upgrades: {b.upgradesScore}");
        sb.AppendLine($"Resources: {b.resourcesScore}");

        // modules penalty in red, as negative
        sb.AppendLine($"<color=#FF4D4D>Modules: -{b.modulesPenalty}</color>");
        sb.AppendLine("");
        sb.AppendLine($"<size=120%><b>Score</b>: {b.totalScore}</size>");
        if (SaveGameManager.Instance.bestSaveGame.score != 0 && SaveGameManager.Instance.bestSaveGame.score > b.totalScore)
            sb.AppendLine($"Best: {SaveGameManager.Instance.bestSaveGame.score}");

        txtGameOverStats.text = sb.ToString();

        StartCoroutine(DelayedLayoutRebuild());
    }

    IEnumerator DelayedLayoutRebuild()
    {
        yield return null;

        var layoutRoot = gameOverPanel.GetComponentInChildren<VerticalLayoutGroup>()?.transform as RectTransform;
        if (layoutRoot != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
    }

    private string BuildAllStatsText()
    {
        var s = Stats.Instance;
        var sb = new System.Text.StringBuilder(512);

        sb.AppendLine($"<align=center>Play time: {FormatTimeSmart(s.playTime)}</align>");
        sb.AppendLine();

        sb.AppendLine($"<b>Waves:</b> {s.wavesCompleted}");
        sb.AppendLine();

        sb.AppendLine($"<b>Enemies</b>");
        sb.AppendLine($"Spawned: {s.enemiesSpawned}");
        sb.AppendLine($"Killed: {s.GetTotalKills()}");
        sb.AppendLine();

        sb.AppendLine($"<b>Projectiles</b>");
        sb.AppendLine($"Tower shots: {s.towerProjectilesFired}");
        sb.AppendLine($"Tower hits: {s.towerProjectilesHit}");
        sb.AppendLine($"Drone shots: {s.droneProjectilesFired}");
        sb.AppendLine($"Drone hits: {s.droneProjectilesHit}");
        sb.AppendLine($"Enemy shots: {s.enemyProjectilesFired}");
        sb.AppendLine($"Enemy hits: {s.enemyProjectilesHit}");
        sb.AppendLine();

        sb.AppendLine($"<b>Modules</b>");
        sb.AppendLine($"Built: {s.modulesBuilt}");
        sb.AppendLine($"Lost: {s.modulesDestroyed}");
        sb.AppendLine($"Damage: {s.modulesDamageTaken}");
        sb.AppendLine();

        sb.AppendLine($"<b>Shield</b>");
        sb.AppendLine($"Damage: {s.shieldDamageTaken}");
        sb.AppendLine($"Deflected: {s.deflectedProjectilesFired}");
        sb.AppendLine($"Deflected hits: {s.deflectedProjectilesHit}");
        sb.AppendLine();

        sb.AppendLine($"<b>Upgrading</b>");
        sb.AppendLine($"Bought: {s.boughtUpgrades}");
        sb.AppendLine($"Spent: {s.totalUpgradeCosts} M");
        sb.AppendLine();

        sb.AppendLine($"<b>Resources</b>");
        sb.AppendLine($"Dropped: {s.resourcesSpawned}");
        sb.AppendLine($"Collected: {s.resourcesCollectedManually} M");
        sb.AppendLine($"Auto-collected: {s.resourcesCollectedAutomatically} M");
        sb.AppendLine($"Total: {s.resourcesCollectedManually + s.resourcesCollectedAutomatically} M");

        return sb.ToString();
    }

    private string FormatTimeSmart(float seconds)
    {
        int sec = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int h = sec / 3600;
        int m = (sec % 3600) / 60;
        int s = sec % 60;

        return h > 0 ? $"{h:00}:{m:00}:{s:00}" : $"{m:00}:{s:00}";
    }

    public void Replay()
    {
        matchReporter?.Finish(gameOver ? matchEndReason : "restarted");
        matchReporter?.Dispose();
        StopAllCoroutines();
        Time.timeScale = 1f;

        gameOver = false;

        gameOverPanel.SetActive(false);
        DetailPanel.gameObject.SetActive(false);

        ResetAll();
        UpgradeAttribute.ApplyAllUpgradeEffect();

        TimeController.Instance.RefreshPanel();
        ModulesUI.Instance.ResetModulePanel();
        ModulesUI.Instance.RefreshPanel();
        UpgradeUI.Instance.Refresh();
        DroneManager.Instance.CheckDroneCanBuild();
        ResourceManager.Instance.RefreshUI();

        SoundManager.Instance.PlayMainMusic();
        isInit = true;
        SaveGameManager.Instance.ClearWaveCheckpoint();
        SaveGameManager.Instance.Save();
        StartMatchReport(false);
    }

    private void StartMatchReport(bool resumed)
    {
        matchEndReason = "game_over";
        string root = Application.persistentDataPath;
#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(SaveGameManager.SaveDirectoryOverride)) root = SaveGameManager.SaveDirectoryOverride;
#endif
        matchReporter = new MatchReporter(System.IO.Path.Combine(root, "MatchReports"), resumed);
        matchReporter.Checkpoint();
    }

    private void LateUpdate()
    {
        // Finish after collision callbacks have counted the final hit/removal.
        if (gameOver) matchReporter?.Finish(matchEndReason);
    }

    private void OnApplicationPause(bool paused)
    {
        MatchReporter.Event(paused ? "app_paused" : "app_resumed");
        if (paused) matchReporter?.Checkpoint();
    }

    private void OnApplicationQuit() => matchReporter?.Finish(gameOver ? matchEndReason : "app_quit");


}
