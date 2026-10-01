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
        RefreshUI();
    }

    public void UpdateNormal()
    {
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

        if (!autoCollecting && Utils.TryGetPointerDown(out var screenPosition) && !Utils.IsPointerOverUI())
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
        string hint = displayedHint == 0 ? "\nTap station to build" :
            displayedHint == 1 ? "\nTap material to collect" : "";
        txtMaterial.text = $"Wave {displayedWave}  |  Core {displayedCoreHP}/{displayedCoreMaxHP}\nMaterial {Utils.FormatNumber(curMaterials)}{hint}";
        if (waveBonusDisplayTime > 0f)
            txtMaterial.text += $"\nWave complete +{lastWaveBonus} M";
    }

    int TutorialHint()
    {
        if (Stats.Instance.modulesBuilt == 0) return 0;
        return !autoCollecting && Stats.Instance.resourcesCollectedManually == 0 ? 1 : 2;
    }

    public void SpawnMaterial(int amount, Vector3 spawnPosition)
    {
        GameObject effect = Instantiate(collectEffectPrefab, spawnPosition, Quaternion.identity, spawnParent);
        effect.transform.GetComponent<CollectEffect>()
            .init(StationModule.GetModuleByType(StationModule.eModuleType.Core).transform,
                                                amount,
                                                autoCollecting);

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

    public void ResetScript()
    {
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
