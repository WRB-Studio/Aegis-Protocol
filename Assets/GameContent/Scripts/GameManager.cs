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

#if UNITY_EDITOR
    private void OnValidate()
    {
        var sb = new System.Text.StringBuilder(256);

        sb.AppendLine($"<align=center>Played Time 00:00:00</align>");
        sb.AppendLine();

        sb.AppendLine($"Time\t\t\t999");
        sb.AppendLine($"Waves\t\t999");
        sb.AppendLine($"Kills\t\t\t999");
        sb.AppendLine($"Upgrades\t\t999");
        sb.AppendLine($"Resources\t\t999");

        // modules penalty in red, as negative
        sb.AppendLine($"<color=#FF4D4D>Modules\t\t999</color>");
        sb.AppendLine("__________________");
        sb.AppendLine($"<size=140%><b>Score</b>\t999999</size>");
        sb.AppendLine($"Best\t\t\t999999");

        txtGameOverStats.text = sb.ToString();


        sb = new System.Text.StringBuilder(256);

        sb.AppendLine($"<align=center>Played Time: 00:00:00</align>");
        sb.AppendLine();

        sb.AppendLine($"<b>Waves\t\t\t999");
        sb.AppendLine();

        sb.AppendLine($"<b>Enemies</b>");
        sb.AppendLine($" └ Spawned\t\t999");
        sb.AppendLine($" └ Killed\t\t\t999");
        sb.AppendLine();

        sb.AppendLine($"<b>Projectiles</b>");
        sb.AppendLine($" └ Tower fired\t\t999");
        sb.AppendLine($" └ Tower hits\t\t999");
        sb.AppendLine($" └ Drone fired\t\t999");
        sb.AppendLine($" └ Drone hits\t\t999");
        sb.AppendLine($" └ Enemy fired\t\t999");
        sb.AppendLine($" └ Enemy hits\t\t999");
        sb.AppendLine();

        sb.AppendLine($"<b>Modules</b>");
        sb.AppendLine($" └ Built\t\t\t999");
        sb.AppendLine($" └ Lost\t\t\t999");
        sb.AppendLine($" └ Damage Taken\t999");
        sb.AppendLine();

        sb.AppendLine($"<b>Shield</b>");
        sb.AppendLine($" └ Damage Taken\t999");
        sb.AppendLine($" └ Deflected\t\t999");
        sb.AppendLine($" └ Deflected hits\t999");
        sb.AppendLine();

        sb.AppendLine($"<b>Upgrading</b>");
        sb.AppendLine($" └ Bought\t\t\t999");
        sb.AppendLine($" └ Total costs\t\t999 $");
        sb.AppendLine();

        sb.AppendLine($"<b>Resources</b>");
        sb.AppendLine($" └ spawned raw\t\t999");
        sb.AppendLine($" └ manually\t\t999 $");
        sb.AppendLine($" └ automatically\t\t999 $");
        sb.AppendLine($" └ total\t\t\t999 $");

        txtDetails.text = sb.ToString();
    }
#endif
    private void Awake()
    {
        Instance = this;

        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;

        foreach (var r in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (r is IResettable i) allResettable.Add(i);
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
        UpgradeManager.Instance.Init();
        ResourceManager.Instance.Init();

        UIManager.Instance.Init();
        ModulesUI.Instance.Init();
        UIToWorldLine.Instance.Init();
        UpgradeUI.Instance.Init();
        UpgradeAttribute.ApplyAllUpgradeEffect();
        TimeController.Instance.Init();

        Tower.Instance.Init();

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

    }

    public void GameOver()
    {
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

        sb.AppendLine($"<align=center>Played Time {FormatTimeSmart(b.playTimeSeconds)}</align>");
        sb.AppendLine();

        sb.AppendLine($"Time\t\t\t{b.timeScore}");
        sb.AppendLine($"Waves\t\t{b.wavesScore}");
        sb.AppendLine($"Kills\t\t\t{b.killsScore}");
        sb.AppendLine($"Upgrades\t\t{b.upgradesScore}");
        sb.AppendLine($"Resources\t\t{b.resourcesScore}");

        // modules penalty in red, as negative
        sb.AppendLine($"<color=#FF4D4D>Modules\t\t{b.modulesPenalty}</color>");
        sb.AppendLine("__________________");
        sb.AppendLine($"<size=140%><b>Score</b>\t{b.totalScore}</size>");
        if (SaveGameManager.Instance.bestSaveGame.score != 0 && SaveGameManager.Instance.bestSaveGame.score > b.totalScore)
            sb.AppendLine($"Best\t\t\t{SaveGameManager.Instance.bestSaveGame.score}");

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

        sb.AppendLine($"<align=center>Play Time: {FormatTimeSmart(s.playTime)}</align>");
        sb.AppendLine();

        sb.AppendLine($"<b>Waves:\t\t\t{s.wavesCompleted}");
        sb.AppendLine();

        sb.AppendLine($"<b>Enemies</b>");
        sb.AppendLine($" └ Spawned\t\t{s.enemiesSpawned}");
        sb.AppendLine($" └ Killed\t\t\t{s.GetTotalKills()}");
        sb.AppendLine();

        sb.AppendLine($"<b>Projectiles</b>");
        sb.AppendLine($" └ Tower fired\t\t{s.towerProjectilesFired}");
        sb.AppendLine($" └ Tower hits\t\t{s.towerProjectilesHit}");
        sb.AppendLine($" └ Drone fired\t\t{s.droneProjectilesFired}");
        sb.AppendLine($" └ Drone hits\t\t{s.droneProjectilesHit}");
        sb.AppendLine($" └ Enemy fired\t\t{s.enemyProjectilesFired}");
        sb.AppendLine($" └ Enemy hits\t\t{s.enemyProjectilesHit}");
        sb.AppendLine();

        sb.AppendLine($"<b>Modules</b>");
        sb.AppendLine($" └ Built\t\t\t{s.modulesBuilt}");
        sb.AppendLine($" └ Lost\t\t\t{s.modulesDestroyed}");
        sb.AppendLine($" └ Damage Taken\t{s.modulesDamageTaken}");
        sb.AppendLine();

        sb.AppendLine($"<b>Shield</b>");
        sb.AppendLine($" └ Damage Taken\t{s.shieldDamageTaken}");
        sb.AppendLine($" └ Deflected\t\t{s.deflectedProjectilesFired}");
        sb.AppendLine($" └ Deflected hits\t{s.deflectedProjectilesHit}");
        sb.AppendLine();

        sb.AppendLine($"<b>Upgrading</b>");
        sb.AppendLine($" └ Bought\t\t\t{s.boughtUpgrades}");
        sb.AppendLine($" └ Total costs\t\t{s.totalUpgradeCosts} $");
        sb.AppendLine();

        sb.AppendLine($"<b>Resources</b>");
        sb.AppendLine($" └ spawned raw\t\t{s.resourcesSpawned}");
        sb.AppendLine($" └ manually\t\t{s.resourcesCollectedManually} $");
        sb.AppendLine($" └ automatically\t\t{s.resourcesCollectedAutomatically} $");
        sb.AppendLine($" └ total\t\t\t{s.resourcesCollectedManually + s.resourcesCollectedAutomatically} $");

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
        StopAllCoroutines();
        Time.timeScale = 1f;

        gameOver = false;

        gameOverPanel.SetActive(false);
        DetailPanel.gameObject.SetActive(false);

        foreach (var resettable in allResettable) resettable.ResetScript();

        // UpgradeAttribute.ResetAll(); // <- RAUS, killt dir die frisch neu gebauten costs

        SoundManager.Instance.PlayMainMusic();
        isInit = true;
    }


}
