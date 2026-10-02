using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ResourceManager : MonoBehaviour, IResettable
{
    public static ResourceManager Instance;

    public GameObject collectEffectPrefab;
    private Transform spawnParent;

    public int curMaterials = 0;
    [HideInInspector] public float collectingEffeciency = 1;
    public TextMeshProUGUI txtMaterial;

    [Header("Shooting stars (real seconds while playing)")]
    [SerializeField] ShootingStar shootingStarPrefab;
    [SerializeField] Vector2 firstStarDelay = new Vector2(18f, 25f);
    [SerializeField] Vector2 starInterval = new Vector2(35f, 55f);
    [SerializeField, Min(1f)] float starFlightDuration = 7f;
    [SerializeField] Vector2Int starReward = new Vector2Int(8, 12);
    ShootingStar shootingStar;
    float starCountdown;
    int bonusTapFrame = -1;
    public bool ConsumedBonusTap => bonusTapFrame == Time.frameCount;

    [HideInInspector] public bool autoCollecting = false;

    [HideInInspector] public List<CollectEffect> collectEffects = new List<CollectEffect>();

    // --- INIT SNAPSHOT ---
    private int initcurMaterials;
    private float initcollectingEffeciency;
    private bool initautoCollecting;
    private int displayedWave = -1;
    private int displayedCoreHP = -1;
    private int displayedCoreMaxHP = -1;
    private int displayedMaterials = -1;
    private int displayedHint = -1;
    float waveBonusDisplayTime;
    int lastWaveBonus;
    readonly List<GameObject> incomeLabels = new();


    void Awake()
    {
        Instance = this;
    }

    public void Init()
    {
        var p = GameObject.Find("ResourceParent");
        spawnParent = p ? p.transform : transform;
        starCountdown = Random.Range(firstStarDelay.x, firstStarDelay.y);
        RefreshUI();
    }

    public void UpdateNormal()
    {
        UpdateShootingStar();
        if (waveBonusDisplayTime > 0f)
        {
            waveBonusDisplayTime -= Time.unscaledDeltaTime;
            if (waveBonusDisplayTime <= 0f) RefreshUI();
        }
        var core = StationModule.GetModuleByType(StationModule.eModuleType.Core);
        if (core && (displayedWave != EnemySpawner.Instance.DisplayWave ||
            displayedCoreHP != core.currentHP || displayedCoreMaxHP != core.maxHP ||
            displayedMaterials != curMaterials || displayedHint != TutorialHint()))
            RefreshUI();

        bool pointerDown = Utils.TryGetPointerDown(out var screenPosition) && !Utils.IsPointerOverUI();
        if (pointerDown && shootingStar && shootingStar.TryCollect(screenPosition))
        {
            bonusTapFrame = Time.frameCount;
            shootingStar = null;
        }
        else if (!autoCollecting && pointerDown)
        {
            Vector2 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
            foreach (var collider in Physics2D.OverlapPointAll(worldPosition))
            {
                if (!collider.CompareTag("Material")) continue;
                CollectEffect effect = collider.GetComponent<CollectEffect>();
                if (!effect || effect.flyToStation) break;
                effect.flyToStation = true;
                effect.setOriginScale();
                effect.collectedManually = true;
                break;
            }
        }

        for (int i = collectEffects.Count - 1; i >= 0; i--)
        {
            var e = collectEffects[i];
            if (!e) { collectEffects.RemoveAt(i); continue; }
            e.UpdateNormal();
        }
    }

    public void enableAutoCollecting()
    {
        autoCollecting = true;

        for (int i = collectEffects.Count - 1; i >= 0; i--)
        {
            var e = collectEffects[i];
            if (!e) { collectEffects.RemoveAt(i); continue; }
            e.flyToStation = true;
            e.setOriginScale();
        }
    }

    public void disableAutoCollecting()
    {
        autoCollecting = false;
    }

    public void RefreshUI()
    {
        var core = StationModule.GetModuleByType(StationModule.eModuleType.Core);
        displayedWave = EnemySpawner.Instance ? EnemySpawner.Instance.DisplayWave : 1;
        displayedCoreHP = core ? core.currentHP : 0;
        displayedCoreMaxHP = core ? core.maxHP : 0;
        displayedMaterials = curMaterials;
        displayedHint = TutorialHint();
        string hint = displayedHint == 0 ? "\n<size=85%><color=#9BACB0>Tap station to build</color></size>" :
            displayedHint == 1 ? "\n<size=85%><color=#9BACB0>Tap drops to collect</color></size>" : "";
        string hpColor = displayedCoreHP <= 1 ? "#FF7777" : "#FFFFFF";
        txtMaterial.color = Color.white;
        txtMaterial.text = $"<color=#94ECF4><b>WAVE {displayedWave}</b></color>\n" +
            $"Core <color={hpColor}><b>{displayedCoreHP}/{displayedCoreMaxHP}</b></color>  |  <b>{Utils.FormatNumber(curMaterials)} M</b>{hint}";
        if (waveBonusDisplayTime > 0f)
            txtMaterial.text += $"\n<size=85%><color=#A8E6AD>Wave bonus <b>+{Utils.FormatNumber(lastWaveBonus)} M</b></color></size>";
    }

    int TutorialHint()
    {
        if (Stats.Instance.modulesBuilt == 0) return 0;
        return !autoCollecting && Stats.Instance.resourcesCollectedManually == 0 ? 1 : 2;
    }

    void UpdateShootingStar()
    {
        if (Time.timeScale <= 0f || GameManager.gameOver || !shootingStarPrefab) return;
        if (shootingStar)
        {
            if (shootingStar.Tick(Time.unscaledDeltaTime))
            {
                Destroy(shootingStar.gameObject);
                shootingStar = null;
            }
            return;
        }
        starCountdown -= Time.unscaledDeltaTime;
        if (starCountdown > 0f || !Camera.main) return;

        ChooseStarPath(out Vector2 start, out Vector2 end);
        float depth = -Camera.main.transform.position.z;
        Vector3 from = Camera.main.ViewportToWorldPoint(new Vector3(start.x, start.y, depth));
        Vector3 to = Camera.main.ViewportToWorldPoint(new Vector3(end.x, end.y, depth));
        shootingStar = Instantiate(shootingStarPrefab, from, Quaternion.identity, spawnParent);
        shootingStar.Init(from, to, starFlightDuration, Random.Range(Mathf.Max(1, starReward.x), Mathf.Max(1, starReward.x, starReward.y) + 1));
        starCountdown = Random.Range(Mathf.Max(1f, starInterval.x), Mathf.Max(1f, starInterval.x, starInterval.y));
        MatchReporter.Event("shooting_star_spawned", position: from);
    }

    static void ChooseStarPath(out Vector2 from, out Vector2 to)
    {
        int entry = Random.Range(0, 4);
        for (int attempt = 0; attempt < 32; attempt++)
        {
            int exit = (entry + Random.Range(1, 4)) % 4;
            from = StarEdgePoint(entry, Random.Range(0.1f, 0.9f));
            to = StarEdgePoint(exit, Random.Range(0.1f, 0.9f));
            Vector2 middle = (from + to) * 0.5f;
            // Skip brief corner clips: the star should cross a useful, tappable part of the screen.
            if (middle.x >= 0.15f && middle.x <= 0.85f && middle.y >= 0.15f && middle.y <= 0.85f &&
                Vector2.Distance(from, to) >= 0.8f) return;
        }
        float position = Random.Range(0.2f, 0.8f);
        from = StarEdgePoint(entry, position);
        to = StarEdgePoint(entry ^ 1, 1f - position);
    }

    static Vector2 StarEdgePoint(int edge, float position)
    {
        switch (edge)
        {
            case 0: return new Vector2(-0.15f, position);
            case 1: return new Vector2(1.15f, position);
            case 2: return new Vector2(position, -0.15f);
            default: return new Vector2(position, 1.15f);
        }
    }

    public void SpawnMaterial(int amount, Vector3 spawnPosition, bool collectedManually = false)
    {
        GameObject effect = Instantiate(collectEffectPrefab, spawnPosition, Quaternion.identity, spawnParent);
        effect.transform.GetComponent<CollectEffect>()
            .init(StationModule.GetModuleByType(StationModule.eModuleType.Core).transform,
                                                amount,
                                                autoCollecting || collectedManually);
        effect.GetComponent<CollectEffect>().collectedManually = collectedManually;

        collectEffects.Add(effect.GetComponent<CollectEffect>());
        MatchReporter.Event("material_spawned", value: amount, position: spawnPosition);
    }

    public static void RemoveCollectEffect(CollectEffect effect)
    {
        Instance.collectEffects.Remove(effect);
        Destroy(effect.gameObject);
    }

    public void AddMaterial(int amount, Stats.eCollectBy collectBy)
    {
        float collectingEffeciency = this.collectingEffeciency;
        if (StationModule.GetModuleByType(StationModule.eModuleType.Extractor).isBuilt == false)
            collectingEffeciency = 1;

        int amountCollected = Mathf.RoundToInt(amount * collectingEffeciency);
        curMaterials += amountCollected;
        var core = StationModule.GetModuleByType(StationModule.eModuleType.Core);
        ShowIncome(amountCollected, core.transform.position);

        Stats.Instance.AddCollectResource(amountCollected, collectBy);
        MatchReporter.Event("material_collected", collectBy.ToString(), value: amountCollected, remaining: curMaterials);

        if (UIManager.Instance.stationUI.activeSelf)
        {
            ModulesUI.Instance.RefreshPanel();
            UpgradeUI.Instance.Refresh();
        }

        RefreshUI();

        SaveGameManager.Instance.RequestSave();
    }

    public void AwardWaveBonus(int completedWave)
    {
        lastWaveBonus = 15 + 3 * Mathf.Max(0, completedWave - 1);
        curMaterials += lastWaveBonus;
        MatchReporter.Event("wave_bonus", value: lastWaveBonus, remaining: curMaterials);
        ShowIncome(lastWaveBonus, StationModule.GetModuleByType(StationModule.eModuleType.Core).transform.position);
        waveBonusDisplayTime = 3f;
        RefreshUI();
        ModulesUI.Instance.RefreshPanel();
        UpgradeUI.Instance.Refresh();
    }

    void ShowIncome(int amount, Vector3 position)
    {
        if (amount <= 0 || !txtMaterial || !txtMaterial.canvas || !Camera.main) return;
        StartCoroutine(AnimateIncome(amount, position));
    }

    IEnumerator AnimateIncome(int amount, Vector3 position)
    {
        var canvas = txtMaterial.canvas.rootCanvas;
        var canvasRect = (RectTransform)canvas.transform;
        var worldCamera = Camera.main;
        var labelObject = new GameObject("Income", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(canvasRect, false);
        incomeLabels.Add(labelObject);

        var label = labelObject.GetComponent<TextMeshProUGUI>();
        label.font = txtMaterial.font;
        label.fontStyle = FontStyles.Bold;
        label.fontMaterial.EnableKeyword("OUTLINE_ON");
        label.outlineColor = new Color32(8, 12, 16, 255);
        label.outlineWidth = 0.25f;
        label.UpdateMeshPadding();
        label.fontSize = Mathf.Clamp(txtMaterial.fontSize, 28f, 42f);
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.65f, 1f, 0.8f);
        label.raycastTarget = false;
        label.text = $"+{Utils.FormatNumber(amount)} M";
        var rect = label.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(240f, 60f);

        const float duration = 1.4f;
        float elapsed = 0f;
        while (elapsed < duration && label && worldCamera)
        {
            var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector3 screenPosition = worldCamera.WorldToScreenPoint(position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, uiCamera, out var localPosition);
            float progress = elapsed / duration;
            rect.localPosition = localPosition + Vector2.up * (70f + progress * 65f);
            label.alpha = 1f - Mathf.SmoothStep(0f, 1f, progress);
            elapsed += Time.deltaTime;
            yield return null;
        }

        incomeLabels.Remove(labelObject);
        if (labelObject) Destroy(labelObject);
    }

    public bool SpendMaterial(int amount, bool save = true)
    {
        if (amount >= 0 && curMaterials >= amount)
        {
            curMaterials -= amount;
            MatchReporter.Event("material_spent", value: amount, remaining: curMaterials);
            RefreshUI();
            UpgradeUI.Instance.Refresh();

            if (save) SaveGameManager.Instance.Save();

            return true;
        }
        return false;
    }


    public void StoreInit()
    {
        initcurMaterials = curMaterials;
        initcollectingEffeciency = collectingEffeciency;
        initautoCollecting = autoCollecting;
    }

    public void ClearShootingStar()
    {
        if (shootingStar) Destroy(shootingStar.gameObject);
        shootingStar = null;
    }

    public void ResetScript()
    {
        ClearShootingStar();
        bonusTapFrame = -1;
        starCountdown = Random.Range(firstStarDelay.x, firstStarDelay.y);
        StopAllCoroutines();
        foreach (var label in incomeLabels)
            if (label) Destroy(label);
        incomeLabels.Clear();
        // runtime clear
        for (int i = collectEffects.Count - 1; i >= 0; i--)
            if (collectEffects[i]) Destroy(collectEffects[i].gameObject);
        collectEffects.Clear();

        curMaterials = initcurMaterials;
        waveBonusDisplayTime = 0f;
        collectingEffeciency = initcollectingEffeciency;
        autoCollecting = initautoCollecting;

        RefreshUI();
    }

}
