using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ModulesUI : MonoBehaviour, IResettable
{
    public static ModulesUI Instance;

    [Header("Colors")]
    [SerializeField] Color buildAbleColor;
    [SerializeField] Color selectedColor;
    [SerializeField] Color builtColor;

    [Header("Buttons")]
    [SerializeField] Button btnCore;
    [SerializeField] Button btnExtractor;
    [SerializeField] Button btnShield;
    [SerializeField] Button btnDrone;
    [SerializeField] Button btnRadar;
    [SerializeField] Button btnAmmoFabricator;
    [SerializeField] Button btnCommandUnit;
    [SerializeField] Button btnTemporalModulator;
    [SerializeField] Button btnSelfDestruct;

    public StationModule currentSelectedModule { get; private set; }
    public Button currentSelectedBtnModule { get; private set; }

    readonly Dictionary<StationModule.eModuleType, Button> btnByType = new();
    readonly Dictionary<StationModule.eModuleType, Slider> hpSliderByType = new();

    // --- INIT SNAPSHOT ---
    Color initBuildAbleColor;
    Color initSelectedColor;
    Color initBuiltColor;

    bool confirmSelfDestruct = false;

    void Awake() => Instance = this;

    public void Init()
    {
        BuildButtonMaps();
        HookButtonClicks();
        CacheHpSliders();

        DisableAllSelectionMarkers();

        UIManager.Instance.btnRepair.onClick.AddListener(RepairSelectedModule);
        UIManager.Instance.btnRepair.gameObject.SetActive(false);

        btnSelfDestruct.onClick.AddListener(OnClickSelfDestruct);
        btnSelfDestruct.gameObject.SetActive(false);

        ResetSelectionState();
        RefreshPanel();
    }

    void BuildButtonMaps()
    {
        btnByType.Clear();
        btnByType[StationModule.eModuleType.Core] = btnCore;
        btnByType[StationModule.eModuleType.Extractor] = btnExtractor;
        btnByType[StationModule.eModuleType.Shield] = btnShield;
        btnByType[StationModule.eModuleType.Drone] = btnDrone;
        btnByType[StationModule.eModuleType.Radar] = btnRadar;
        btnByType[StationModule.eModuleType.AmmoFabricator] = btnAmmoFabricator;
        btnByType[StationModule.eModuleType.CommandUnit] = btnCommandUnit;
        btnByType[StationModule.eModuleType.TemporalModulator] = btnTemporalModulator;
    }

    void HookButtonClicks()
    {
        btnCore.onClick.AddListener(() => SelectModule(StationModule.eModuleType.Core));
        btnExtractor.onClick.AddListener(() => SelectModule(StationModule.eModuleType.Extractor));
        btnShield.onClick.AddListener(() => SelectModule(StationModule.eModuleType.Shield));
        btnDrone.onClick.AddListener(() => SelectModule(StationModule.eModuleType.Drone));
        btnRadar.onClick.AddListener(() => SelectModule(StationModule.eModuleType.Radar));
        btnAmmoFabricator.onClick.AddListener(() => SelectModule(StationModule.eModuleType.AmmoFabricator));
        btnCommandUnit.onClick.AddListener(() => SelectModule(StationModule.eModuleType.CommandUnit));
        btnTemporalModulator.onClick.AddListener(() => SelectModule(StationModule.eModuleType.TemporalModulator));
    }

    void CacheHpSliders()
    {
        hpSliderByType.Clear();
        hpSliderByType[StationModule.eModuleType.Core] = FindHpSlider(btnCore);
        hpSliderByType[StationModule.eModuleType.Extractor] = FindHpSlider(btnExtractor);
        hpSliderByType[StationModule.eModuleType.Shield] = FindHpSlider(btnShield);
        hpSliderByType[StationModule.eModuleType.Drone] = FindHpSlider(btnDrone);
        hpSliderByType[StationModule.eModuleType.Radar] = FindHpSlider(btnRadar);
        hpSliderByType[StationModule.eModuleType.AmmoFabricator] = FindHpSlider(btnAmmoFabricator);
        hpSliderByType[StationModule.eModuleType.CommandUnit] = FindHpSlider(btnCommandUnit);
        hpSliderByType[StationModule.eModuleType.TemporalModulator] = FindHpSlider(btnTemporalModulator);
    }

    Slider FindHpSlider(Button btn)
    {
        var t = btn.transform.Find("SliderHP");
        return t ? t.GetComponent<Slider>() : null;
    }

    void DisableAllSelectionMarkers()
    {
        foreach (var kv in btnByType)
        {
            var marker = kv.Value.transform.Find("SelectionMarker")?.GetComponent<Image>();
            if (marker) marker.enabled = false;
        }
    }

    void SetSelectionMarker(Button btn, bool enabled)
    {
        var marker = btn.transform.Find("SelectionMarker")?.GetComponent<Image>();
        if (marker) marker.enabled = enabled;
    }

    public void SelectModule(StationModule.eModuleType type)
    {
        // same module -> no-op
        if (currentSelectedModule != null && currentSelectedModule.moduleType == type)
            return;
        
        btnSelfDestruct.gameObject.SetActive(false);
        ResetSelfDestructButton();

        UpgradeUI.Instance.Hide();
        ResetSelectionVisualsOnly();

        currentSelectedModule = StationModule.GetModuleByType(type);
        currentSelectedBtnModule = btnByType[type];

        if (currentSelectedModule == null || currentSelectedBtnModule == null)
            return;

        currentSelectedModule.gameObject.SetActive(true);
        SetSelectionMarker(currentSelectedBtnModule, true);

        ApplySelectionState();

        if (currentSelectedModule.moduleType == StationModule.eModuleType.Core)
            btnSelfDestruct.gameObject.SetActive(true);
        else
            btnSelfDestruct.gameObject.SetActive(false);
    }

    void ApplySelectionState()
    {
        if (currentSelectedModule == null) return;

        if (currentSelectedModule.isBuilt)
            ApplyBuiltSelectedState();
        else
            ApplyUnbuiltSelectedState();
    }

    void ApplyBuiltSelectedState()
    {
        currentSelectedModule.GetComponent<SpriteRenderer>().color = selectedColor;

        UIManager.Instance.ShowInfoPanel(
            currentSelectedModule.moduleType.ToString(),
            currentSelectedModule.description,
            0,
            false
        );

        ShowSelectedModuleLine();
        ShowUpgradeSet();
        RefreshRepairButton();
        UIManager.Instance.btnBuy.gameObject.SetActive(false);
    }

    void ApplyUnbuiltSelectedState()
    {
        currentSelectedModule.GetComponent<SpriteRenderer>().color = buildAbleColor;

        UIManager.Instance.ShowInfoPanel(
            currentSelectedModule.moduleType.ToString(),
            currentSelectedModule.description,
            currentSelectedModule.cost,
            true
        );

        RefreshBuyButton();
        UIManager.Instance.btnRepair.gameObject.SetActive(false);
    }

    public void ResetModulePanel()
    {
        // full reset (modules + UI + selection)
        ResetAllModulesVisuals();
        UIManager.Instance.UIManagerInfoPanel.SetActive(false);

        if (currentSelectedBtnModule != null)
            SetSelectionMarker(currentSelectedBtnModule, false);

        UIToWorldLine.Instance.HideLine();
        ResetSelectionState();
    }

    void ResetSelectionVisualsOnly()
    {
        // modules + UI line + markers, but keep selection objects to reapply
        ResetAllModulesVisuals();
        UIManager.Instance.UIManagerInfoPanel.SetActive(false);

        if (currentSelectedBtnModule != null)
            SetSelectionMarker(currentSelectedBtnModule, false);

        UIToWorldLine.Instance.HideLine();
    }

    void ResetAllModulesVisuals()
    {
        for (int i = StationModule.allModules.Count - 1; i >= 0; i--)
        {
            var module = StationModule.allModules[i];
            if (module == null) continue;

            var m = module.GetComponent<StationModule>();
            if (m == null) continue;

            if (m.isBuilt)
            {
                module.GetComponent<SpriteRenderer>().color = builtColor;
                module.gameObject.SetActive(true);
            }
            else
            {
                module.GetComponent<SpriteRenderer>().color = buildAbleColor;
                module.gameObject.SetActive(false);
            }
        }
    }

    void ResetSelectionState()
    {
        currentSelectedModule = null;
        currentSelectedBtnModule = null;
    }

    public void RefreshPanel()
    {
        RefreshHpSliders();

        // re-show selection if one exists
        if (currentSelectedBtnModule != null)
            SetSelectionMarker(currentSelectedBtnModule, true);

        if (currentSelectedModule == null) return;

        ApplySelectionState();

        // if unbuilt, hide HP slider for that button
        if (!currentSelectedModule.isBuilt &&
            hpSliderByType.TryGetValue(currentSelectedModule.moduleType, out var slider) &&
            slider != null)
        {
            slider.gameObject.SetActive(false);
        }
    }

    void RefreshHpSliders()
    {
        for (int i = StationModule.allModules.Count - 1; i >= 0; i--)
        {
            var module = StationModule.allModules[i];
            if (module == null) continue;

            var stationModule = module.GetComponent<StationModule>();
            if (stationModule == null) continue;

            if (!hpSliderByType.TryGetValue(stationModule.moduleType, out var slider) || slider == null)
                continue;

            if (stationModule.isBuilt)
            {
                slider.gameObject.SetActive(true);
                slider.maxValue = stationModule.maxHP;
                slider.value = stationModule.currentHP;
            }
            else
            {
                slider.gameObject.SetActive(false);
            }
        }
    }

    void RefreshBuyButton()
    {
        var btnBuy = UIManager.Instance.btnBuy;
        bool canBuy = ResourceManager.Instance.curMaterials >= currentSelectedModule.cost;

        btnBuy.gameObject.SetActive(true);
        btnBuy.interactable = canBuy;

        btnBuy.onClick.RemoveAllListeners();
        btnBuy.onClick.AddListener(BuySelectedModule);

        if (canBuy)
        {
            btnBuy.image.color = UIManager.Instance.btnBuyOriginalColor;
        }
        else
        {
            var c = btnBuy.colors.disabledColor;
            c.a = 1f;
            btnBuy.image.color = c;
        }
    }

    public void RefreshRepairButton()
    {
        var btnRepair = UIManager.Instance.btnRepair;

        bool canRepair = currentSelectedModule != null
                         && currentSelectedModule.isBuilt
                         && currentSelectedModule.currentHP < currentSelectedModule.maxHP;

        if (!canRepair)
        {
            btnRepair.gameObject.SetActive(false);
            return;
        }

        int repairCost = currentSelectedModule.GetModuleRepairCost();
        btnRepair.GetComponentInChildren<TextMeshProUGUI>().text = $"Repair\n{repairCost} $";
        btnRepair.interactable = ResourceManager.Instance.curMaterials >= repairCost;
        btnRepair.gameObject.SetActive(true);
    }

    public void RepairSelectedModule()
    {
        if (currentSelectedModule == null) return;

        int cost = currentSelectedModule.GetModuleRepairCost();
        if (ResourceManager.Instance.curMaterials < cost) return;

        ResourceManager.Instance.SpendMaterial(cost);
        currentSelectedModule.currentHP = currentSelectedModule.maxHP;

        RefreshPanel();
    }

    public void BuySelectedModule()
    {
        if (currentSelectedModule == null) return;

        SoundManager.Instance.PlayInstallationSound();

        Stats.Instance.modulesBuilt++;
        Stats.Instance.modulesCost += currentSelectedModule.cost;

        ResourceManager.Instance.SpendMaterial(currentSelectedModule.cost);

        currentSelectedModule.GetComponent<SpriteRenderer>().color = builtColor;
        currentSelectedModule.isBuilt = true;

        if (currentSelectedModule.moduleType == StationModule.eModuleType.Shield)
        {
            Shield.Instance.activateShield();
            Shield.Instance.sliderShieldPoints.gameObject.SetActive(true);
        }
        else if (currentSelectedModule.moduleType == StationModule.eModuleType.Drone)
        {
            DroneManager.Instance.AfterModulInit();
        }

        RefreshPanel();
        UpgradeUI.Instance.Refresh();
        TimeController.Instance.RefreshPanel();

        SaveGameManager.Instance.Save();
    }

    void ShowSelectedModuleLine()
    {
        if (currentSelectedModule == null) return;

        var btn = btnByType[currentSelectedModule.moduleType];
        UIToWorldLine.Instance.showLine(btn.GetComponent<RectTransform>(), currentSelectedModule.linePoint);
    }

    void ShowUpgradeSet()
    {
        if (currentSelectedModule == null) return;
        UpgradeUI.Instance.Show(currentSelectedModule.upgradeSet);
    }

    public void Hide()
    {
        btnSelfDestruct.gameObject.SetActive(false);
        ResetSelfDestructButton();
        btnSelfDestruct.gameObject.SetActive(false);

        ResetModulePanel();
        RefreshPanel();
        ResetSelectionState();
    }

    public void OnClickSelfDestruct()
    {
        var label = btnSelfDestruct.transform
            .GetChild(0)
            .GetComponent<TMP_Text>();

        if (!confirmSelfDestruct)
        {
            confirmSelfDestruct = true;
            label.text = "CONFIRM\nSELF-DESTRUCT";
            return;
        }

        UIManager.Instance.Show(false);

        StationModule.GetModuleByType(StationModule.eModuleType.Core).Die();
    }

    void ResetSelfDestructButton()
    {
        confirmSelfDestruct = false;

        var label = btnSelfDestruct.transform
            .GetChild(0)
            .GetComponent<TMP_Text>();

        label.text = "Self-Destruct";
    }

    public void StoreInit()
    {
        initBuildAbleColor = buildAbleColor;
        initSelectedColor = selectedColor;
        initBuiltColor = builtColor;
    }

    public void ResetScript()
    {
        buildAbleColor = initBuildAbleColor;
        selectedColor = initSelectedColor;
        builtColor = initBuiltColor;

        ResetModulePanel();
        RefreshPanel();

        DisableAllSelectionMarkers();
        ResetSelectionState();
    }
}
