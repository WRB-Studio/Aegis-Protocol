using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UpgradeAttribute;

public class UpgradeUI : MonoBehaviour, IResettable
{
    public static UpgradeUI Instance;

    [Header("UI")]
    [SerializeField] GameObject panel;
    [SerializeField] Transform contentContainer;
    [SerializeField] GameObject buttonPrefab;
    [SerializeField] GameObject infoPanel;
    [SerializeField] GameObject fireRangeCircle;

    [Header("Info Panel Paths")]
    [SerializeField] string infoTitlePath = "TextGrp/txtTitle";
    [SerializeField] string infoDescPath = "TextGrp/txtInfo";

    [Header("Colors")]
    public Color colorBtnFrameSelected;
    public Color colorBtnFrameUnselected;
    public Color colorBtnFrameCantBuy;

    UpgradeSet currentUpgradeSet;

    [HideInInspector] public eUpgradeName currentSelectedUpgrade;

    // Runtime UI cache
    readonly Dictionary<eUpgradeName, ButtonRefs> uiByUpgrade = new();
    readonly List<GameObject> spawnedButtons = new();

    // --- INIT SNAPSHOT ---
    eUpgradeName initSelectedUpgrade;

    const string PATH_INFO_TEXT = "Info/txtInfo";
    const string PATH_COST_TEXT = "Cost/txtCost";
    const string PATH_SYMBOL_IMG = "ImgSymbol";
    const string PATH_MARKER_IMG = "SelectionMarker";


    void Awake()
    {
        Instance = this;
        if (infoPanel) infoPanel.SetActive(false);
        if (panel) panel.SetActive(false);
    }

    public void Init()
    {
        if (fireRangeCircle) fireRangeCircle.SetActive(false);
    }

    public void Show(UpgradeSet upgradeSet, bool holdSelection = false)
    {
        if (upgradeSet == null)
        {
            Hide();
            return;
        }

        bool setChanged = currentUpgradeSet != upgradeSet;
        currentUpgradeSet = upgradeSet;

        panel.SetActive(true);

        if (setChanged)
            RebuildButtons();

        RefreshAll(holdSelection);
    }

    public void Refresh()
    {
        if (currentUpgradeSet == null) return;
        RefreshAll(true);
    }

    public void Hide()
    {
        currentSelectedUpgrade = eUpgradeName.None;
        currentUpgradeSet = null;

        if (infoPanel) infoPanel.SetActive(false);
        if (panel) panel.SetActive(false);

        ClearButtons();
        UpdateFireRangePreview();
    }

    void RebuildButtons()
    {
        ClearButtons();

        foreach (var upgrade in currentUpgradeSet.upgradeAttributes)
        {
            var go = Instantiate(buttonPrefab, contentContainer);
            spawnedButtons.Add(go);

            var refs = BuildRefs(go.transform);
            uiByUpgrade[upgrade.upgradeName] = refs;

            refs.symbol.sprite = Utils.GetSymbolByName(upgrade.upgradeName);

            var capturedUpgrade = upgrade;
            refs.button.onClick.AddListener(() => OnUpgradeClicked(capturedUpgrade));
        }
    }

    void ClearButtons()
    {
        StopAllCoroutines();

        foreach (var go in spawnedButtons)
            if (go) Destroy(go);

        spawnedButtons.Clear();
        uiByUpgrade.Clear();
    }

    void RefreshAll(bool holdSelection)
    {
        // refresh button content + state
        foreach (var upgrade in currentUpgradeSet.upgradeAttributes)
        {
            if (!uiByUpgrade.TryGetValue(upgrade.upgradeName, out var ui))
                continue;

            UpdateButtonUI(ui, upgrade);
            ui.marker.enabled = holdSelection && currentSelectedUpgrade == upgrade.upgradeName;

            if (ui.marker.enabled)
            {
                if(upgrade.level < upgrade.maxLevel && Mathf.RoundToInt(upgrade.cost) <= ResourceManager.Instance.curMaterials)
                    ui.marker.color = colorBtnFrameSelected;
                else
                    ui.marker.color = colorBtnFrameCantBuy;
            }
            else
            {
                ui.marker.color = colorBtnFrameUnselected;
            }
        }

        if (currentSelectedUpgrade != eUpgradeName.None)
        {
            var selected = currentUpgradeSet.upgradeAttributes.Find(
                upgrade => upgrade.upgradeName == currentSelectedUpgrade);
            if (selected != null) RefreshInfoPanel(selected);
        }

        UpdateFireRangePreview();
    }

    void RefreshInfoPanel(UpgradeAttribute upgrade)
    {
        if (!infoPanel) return;

        infoPanel.SetActive(true);

        var title = infoPanel.transform.Find(infoTitlePath)?.GetComponent<TextMeshProUGUI>();
        var desc = infoPanel.transform.Find(infoDescPath)?.GetComponent<TextMeshProUGUI>();

        if (title)
        {
            string formatted = Regex.Replace(upgrade.upgradeName.ToString(), "([a-z])([A-Z])", "$1 $2");
            title.text = char.ToUpper(formatted[0]) + formatted.Substring(1);
        }

        if (desc)
        {
            string current = GetValueWithUnit(upgrade, upgrade.currentValue);
            if (upgrade.level >= upgrade.maxLevel)
                desc.text = upgrade.description + "\n" + current + " (MAX)";
            else
            {
                int price = Mathf.RoundToInt(upgrade.cost);
                string next = GetValueWithUnit(upgrade, upgrade.CalculateValue(upgrade.level + 1));
                string action = price <= ResourceManager.Instance.curMaterials
                    ? "Tap again to buy for " + price + " M"
                    : "Need " + price + " M";
                desc.text = upgrade.description + "\n" + current + " -> " + next + "\n" + action;
            }
        }

        StartCoroutine(DelayedLayoutRebuild());
    }

    void UpdateButtonUI(ButtonRefs ui, UpgradeAttribute upgrade)
    {
        ui.infoText.text = GetValueWithUnit(upgrade, upgrade.currentValue);

        if (upgrade.level >= upgrade.maxLevel)
        {
            ui.costText.text = "MAX";
            ui.button.interactable = true;
            var c = ui.button.colors.disabledColor;
            c.a = 1f;
            ui.button.image.color = c;
            return;
        }

        int cost = Mathf.RoundToInt(upgrade.cost);
        bool canBuy = cost <= ResourceManager.Instance.curMaterials;
        ui.costText.text = cost + " M";
        ui.button.interactable = true;
        ui.button.image.color = canBuy ? ui.button.colors.normalColor : ui.button.colors.disabledColor;
    }

    void OnUpgradeClicked(UpgradeAttribute upgrade)
    {
        if (currentSelectedUpgrade != upgrade.upgradeName)
        {
            currentSelectedUpgrade = upgrade.upgradeName;
            RefreshAll(true);
            return;
        }

        if (upgrade.level >= upgrade.maxLevel || !upgrade.ownerModule ||
            !upgrade.ownerModule.isBuilt || currentUpgradeSet == null ||
            !currentUpgradeSet.upgradeAttributes.Contains(upgrade)) return;

        int cost = Mathf.RoundToInt(upgrade.cost);
        if (!ResourceManager.Instance.SpendMaterial(cost, false)) return;

        SoundManager.Instance.PlayUpgradeSound();
        Stats.Instance.AddUpgrade(cost);

        upgrade.Upgrade();

        RefreshAll(true);

        TimeController.Instance.RefreshPanel();
        DroneManager.Instance.CheckDroneCanBuild();
        DroneManager.Instance.RefreshUIDroneCount();
    }

    void UpdateFireRangePreview()
    {
        if (!fireRangeCircle || currentUpgradeSet == null)
        {
            if (fireRangeCircle) fireRangeCircle.SetActive(false);
            return;
        }

        if (currentSelectedUpgrade != eUpgradeName.FireRange)
        {
            fireRangeCircle.SetActive(false);
            return;
        }

        float fireRange = currentUpgradeSet.upgradeAttributes[0].currentValue;
        if (StationModule.GetModuleByType(StationModule.eModuleType.Radar).isBuilt == false)
            fireRange = Tower.Instance.initialFireRange;

        fireRangeCircle.SetActive(true);
        float scale = fireRange * 2f;
        fireRangeCircle.transform.localScale = new Vector3(scale, scale, scale);
    }

    IEnumerator DelayedLayoutRebuild()
    {
        yield return null;

        var layoutRoot = infoPanel.GetComponentInChildren<VerticalLayoutGroup>()?.transform as RectTransform;
        if (layoutRoot != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
    }

    ButtonRefs BuildRefs(Transform root)
    {
        return new ButtonRefs
        {
            button = root.GetComponent<Button>(),
            infoText = root.Find(PATH_INFO_TEXT).GetComponent<TextMeshProUGUI>(),
            costText = root.Find(PATH_COST_TEXT).GetComponent<TextMeshProUGUI>(),
            symbol = root.Find(PATH_SYMBOL_IMG).GetComponent<Image>(),
            marker = root.Find(PATH_MARKER_IMG).GetComponent<Image>(),
        };
    }

    string GetValueWithUnit(UpgradeAttribute upgrade, float value)
    {

        if (ShouldRound(upgrade.upgradeName))
            value = Mathf.RoundToInt(value);

        switch (upgrade.upgradeName)
        {
            case eUpgradeName.FireRate: return value.ToString("0.##") + " rps";
            case eUpgradeName.Damage: return value.ToString("0.##") + " dmg";
            case eUpgradeName.DroneCount: return value.ToString("0.##") + " pcs";
            case eUpgradeName.DroneHP: return value.ToString("0.##") + " HP";
            case eUpgradeName.DroneBuildTime: return value.ToString("0.##") + " s";
            case eUpgradeName.DroneDamage: return value.ToString("0.##") + " dmg";
            case eUpgradeName.ShieldCapacity: return value.ToString("0.##") + " HP";
            case eUpgradeName.RechargeTime: return value.ToString("0.##") + " s";
            case eUpgradeName.DeflectionChance: return value.ToString("0.##") + " %";
            case eUpgradeName.FireRange: return value.ToString("0.##") + " m";
            case eUpgradeName.RotationSpeed: return value.ToString("0.##") + " deg/s";
            case eUpgradeName.StructuralIntegrity: return value.ToString("0.##") + " HP";
            case eUpgradeName.AutoCollecting: return value > 0f ? "On" : "Off";
            case eUpgradeName.CollectingEfficiency: return "x" + value.ToString("0.##");
            default: return value.ToString("0.##");
        }
    }

    bool ShouldRound(eUpgradeName upgradeName)
    {
        switch (upgradeName)
        {
            case eUpgradeName.Damage:
            case eUpgradeName.DroneCount:
            case eUpgradeName.DroneHP:
            case eUpgradeName.DroneDamage:
            case eUpgradeName.CollectingEfficiency:
            case eUpgradeName.ShieldCapacity:
            case eUpgradeName.DeflectionChance:
            case eUpgradeName.StructuralIntegrity:
                return true;
            default:
                return false;
        }
    }

    public void StoreInit()
    {
        initSelectedUpgrade = currentSelectedUpgrade;
    }

    public void ResetScript()
    {
        StopAllCoroutines();

        currentSelectedUpgrade = initSelectedUpgrade;
        currentUpgradeSet = null;

        if (infoPanel) infoPanel.SetActive(false);
        if (fireRangeCircle) fireRangeCircle.SetActive(false);

        ClearButtons();

        if (panel) panel.SetActive(false);
    }

    class ButtonRefs
    {
        public Button button;
        public TextMeshProUGUI infoText;
        public TextMeshProUGUI costText;
        public Image symbol;
        public Image marker;
    }
}
