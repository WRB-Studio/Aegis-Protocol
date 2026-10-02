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

        sb.AppendLine($"<align=center><color=#9BACB0>Play time</color> 00:00:00</align>");
        sb.AppendLine();

        sb.AppendLine($"<color=#9BACB0>Time</color>  <b>999</b>");
        sb.AppendLine($"<color=#9BACB0>Waves</color>  <b>999</b>");
        sb.AppendLine($"<color=#9BACB0>Kills</color>  <b>999</b>");
        sb.AppendLine($"<color=#9BACB0>Upgrades</color>  <b>999</b>");
        sb.AppendLine($"<color=#9BACB0>Resources</color>  <b>999</b>");

        // modules penalty in red, as negative
        sb.AppendLine($"<color=#FF4D4D>Modules: -999</color>");
        sb.AppendLine("");
        sb.AppendLine($"<size=120%><color=#94ECF4><b>SCORE</b></color>  <b>999999</b></size>");
        sb.AppendLine($"<color=#9BACB0>Best</color>  <b>999999</b>");

        txtGameOverStats.text = sb.ToString();


        sb = new System.Text.StringBuilder(256);

        sb.AppendLine($"<align=center><color=#9BACB0>Play time</color> 00:00:00</align>");
        sb.AppendLine();

        sb.AppendLine($"<color=#94ECF4><b>Waves</b></color>  999");
        sb.AppendLine();

        sb.AppendLine($"<color=#94ECF4><b>Enemies</b></color>");
        sb.AppendLine($"<color=#9BACB0>Spawned</color>  <b>999</b>");
        sb.AppendLine($"<color=#9BACB0>Killed</color>  <b>999</b>");
        sb.AppendLine();

        sb.AppendLine($"<color=#94ECF4><b>Projectiles</b></color>");
        sb.AppendLine($"<color=#9BACB0>Tower shots</color>  <b>999</b>");
        sb.AppendLine($"<color=#9BACB0>Tower hits</color>  <b>999</b>");
        sb.AppendLine($"<color=#9BACB0>Drone shots</color>  <b>999</b>");
        sb.AppendLine($"<color=#9BACB0>Drone hits</color>  <b>999</b>");
        sb.AppendLine($"<color=#9BACB0>Enemy shots</color>  <b>999</b>");
        sb.AppendLine($"<color=#9BACB0>Enemy hits</color>  <b>999</b>");
        sb.AppendLine();

        sb.AppendLine($"<color=#94ECF4><b>Modules</b></color>");
        sb.AppendLine($"<color=#9BACB0>Built</color>  <b>999</b>");
        sb.AppendLine($"<color=#9BACB0>Lost</color>  <b>999</b>");
        sb.AppendLine($"<color=#9BACB0>Damage</color>  <b>999</b>");
        sb.AppendLine();

        sb.AppendLine($"<color=#94ECF4><b>Shield</b></color>");
        sb.AppendLine($"<color=#9BACB0>Damage</color>  <b>999</b>");
        sb.AppendLine($"<color=#9BACB0>Deflected</color>  <b>999</b>");
        sb.AppendLine($"<color=#9BACB0>Deflected hits</color>  <b>999</b>");
        sb.AppendLine();

        sb.AppendLine($"<color=#94ECF4><b>Upgrading</b></color>");
        sb.AppendLine($"<color=#9BACB0>Bought</color>  <b>999</b>");
        sb.AppendLine($"<color=#9BACB0>Spent</color>  <b>999 M</b>");
        sb.AppendLine();

        sb.AppendLine($"<color=#94ECF4><b>Resources</b></color>");
        sb.AppendLine($"<color=#9BACB0>Dropped</color>  <b>999</b>");
        sb.AppendLine($"<color=#9BACB0>Collected</color>  <b>999 M</b>");
        sb.AppendLine($"<color=#9BACB0>Auto-collected</color>  <b>999 M</b>");
        sb.AppendLine($"<color=#9BACB0>Total</color>  <b>999 M</b>");

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
        ResourceManager.Instance.ClearShootingStar();

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

        sb.AppendLine($"<align=center><color=#9BACB0>Play time</color> {FormatTimeSmart(b.playTimeSeconds)}</align>");
        sb.AppendLine();

        sb.AppendLine($"<color=#9BACB0>Time</color>  <b>{b.timeScore}</b>");
        sb.AppendLine($"<color=#9BACB0>Waves</color>  <b>{b.wavesScore}</b>");
        sb.AppendLine($"<color=#9BACB0>Kills</color>  <b>{b.killsScore}</b>");
        sb.AppendLine($"<color=#9BACB0>Upgrades</color>  <b>{b.upgradesScore}</b>");
        sb.AppendLine($"<color=#9BACB0>Resources</color>  <b>{b.resourcesScore}</b>");

        // modules penalty in red, as negative
        sb.AppendLine($"<color=#FF4D4D>Modules: -{b.modulesPenalty}</color>");
        sb.AppendLine("");
        sb.AppendLine($"<size=120%><color=#94ECF4><b>SCORE</b></color>  <b>{b.totalScore}</b></size>");
        if (SaveGameManager.Instance.bestSaveGame.score != 0 && SaveGameManager.Instance.bestSaveGame.score > b.totalScore)
            sb.AppendLine($"<color=#9BACB0>Best</color>  <b>{SaveGameManager.Instance.bestSaveGame.score}</b>");

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

        sb.AppendLine($"<align=center><color=#9BACB0>Play time</color> {FormatTimeSmart(s.playTime)}</align>");
        sb.AppendLine();

        sb.AppendLine($"<color=#94ECF4><b>Waves</b></color>  {s.wavesCompleted}");
        sb.AppendLine();

        sb.AppendLine($"<color=#94ECF4><b>Enemies</b></color>");
        sb.AppendLine($"<color=#9BACB0>Spawned</color>  <b>{s.enemiesSpawned}</b>");
        sb.AppendLine($"<color=#9BACB0>Killed</color>  <b>{s.GetTotalKills()}</b>");
        sb.AppendLine();

        sb.AppendLine($"<color=#94ECF4><b>Projectiles</b></color>");
        sb.AppendLine($"<color=#9BACB0>Tower shots</color>  <b>{s.towerProjectilesFired}</b>");
        sb.AppendLine($"<color=#9BACB0>Tower hits</color>  <b>{s.towerProjectilesHit}</b>");
        sb.AppendLine($"<color=#9BACB0>Drone shots</color>  <b>{s.droneProjectilesFired}</b>");
        sb.AppendLine($"<color=#9BACB0>Drone hits</color>  <b>{s.droneProjectilesHit}</b>");
        sb.AppendLine($"<color=#9BACB0>Enemy shots</color>  <b>{s.enemyProjectilesFired}</b>");
        sb.AppendLine($"<color=#9BACB0>Enemy hits</color>  <b>{s.enemyProjectilesHit}</b>");
        sb.AppendLine();

        sb.AppendLine($"<color=#94ECF4><b>Modules</b></color>");
        sb.AppendLine($"<color=#9BACB0>Built</color>  <b>{s.modulesBuilt}</b>");
        sb.AppendLine($"<color=#9BACB0>Lost</color>  <b>{s.modulesDestroyed}</b>");
        sb.AppendLine($"<color=#9BACB0>Damage</color>  <b>{s.modulesDamageTaken}</b>");
        sb.AppendLine();

        sb.AppendLine($"<color=#94ECF4><b>Shield</b></color>");
        sb.AppendLine($"<color=#9BACB0>Damage</color>  <b>{s.shieldDamageTaken}</b>");
        sb.AppendLine($"<color=#9BACB0>Deflected</color>  <b>{s.deflectedProjectilesFired}</b>");
        sb.AppendLine($"<color=#9BACB0>Deflected hits</color>  <b>{s.deflectedProjectilesHit}</b>");
        sb.AppendLine();

        sb.AppendLine($"<color=#94ECF4><b>Upgrading</b></color>");
        sb.AppendLine($"<color=#9BACB0>Bought</color>  <b>{s.boughtUpgrades}</b>");
        sb.AppendLine($"<color=#9BACB0>Spent</color>  <b>{s.totalUpgradeCosts} M</b>");
        sb.AppendLine();

        sb.AppendLine($"<color=#94ECF4><b>Resources</b></color>");
        sb.AppendLine($"<color=#9BACB0>Dropped</color>  <b>{s.resourcesSpawned}</b>");
        sb.AppendLine($"<color=#9BACB0>Collected</color>  <b>{s.resourcesCollectedManually} M</b>");
        sb.AppendLine($"<color=#9BACB0>Auto-collected</color>  <b>{s.resourcesCollectedAutomatically} M</b>");
        sb.AppendLine($"<color=#9BACB0>Total</color>  <b>{s.resourcesCollectedManually + s.resourcesCollectedAutomatically} M</b>");

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
