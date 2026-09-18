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

        Stats.Instance.AddCollectResource(amountCollected, collectBy);

        if (UIManager.Instance.stationUI.activeSelf)
        {
            ModulesUI.Instance.RefreshPanel();
            UpgradeUI.Instance.Refresh();
        }

        RefreshUI();

        SaveGameManager.Instance.RequestSave();
    }

    public bool SpendMaterial(int amount, bool save = true)
    {
        if (amount >= 0 && curMaterials >= amount)
        {
            curMaterials -= amount;
            RefreshUI();

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
        // runtime clear
        for (int i = collectEffects.Count - 1; i >= 0; i--)
            if (collectEffects[i]) Destroy(collectEffects[i].gameObject);
        collectEffects.Clear();

        curMaterials = initcurMaterials;
        collectingEffeciency = initcollectingEffeciency;
        autoCollecting = initautoCollecting;

        RefreshUI();
    }

}
