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
    [SerializeField] GameObject infoPanel;

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
    readonly Dictionary<UpgradeButtonView, ButtonRefs> refsByView = new();
    UpgradeButtonView[] preparedButtons;
    bool layoutRebuildPending;
    bool testSlidersVisible;
    BossTestControls TestControls => EnemySpawner.Instance ? EnemySpawner.Instance.GetComponent<BossTestControls>() : null;
    bool ShowTestSliders => TestControls && TestControls.enableBossTesting &&
        EnemySpawner.Instance.IsBossTestActive && !GameManager.gameOver;
    bool IsCommandSet => currentUpgradeSet && currentUpgradeSet.moduleType == StationModule.eModuleType.CommandUnit;
    static readonly Color CommandCyan = new Color32(148, 236, 244, 255);

    // --- INIT SNAPSHOT ---
    eUpgradeName initSelectedUpgrade;

    void Awake()
    {
        Instance = this;
        preparedButtons = contentContainer.GetComponentsInChildren<UpgradeButtonView>(true);
        foreach (var view in preparedButtons)
        {
            var capturedView = view;
            view.button.onClick.AddListener(() => OnPreparedButtonClicked(capturedView));
        }
        ClearButtons();
        if (infoPanel) infoPanel.SetActive(false);
        if (panel) panel.SetActive(false);
    }

    public void Init()
    {
    }

    void Update()
    {
        if (currentUpgradeSet && testSlidersVisible != ShowTestSliders) RefreshAll(true);
        foreach (var ui in uiByUpgrade.Values)
            if (ui.testSlider) ui.testSlider.interactable = GameManager.isInit && Time.timeScale > 0f;
        if (!IsCommandSet || !panel.activeInHierarchy) return;
        float glowAlpha = 0.12f + 0.08f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f));
        foreach (var entry in uiByUpgrade)
        {
            var ui = entry.Value;
            if (ui.glow && ui.glow.enabled)
                ui.glow.effectColor = new Color(CommandCyan.r, CommandCyan.g, CommandCyan.b, glowAlpha);
        }
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
    }

    void RebuildButtons()
    {
        ClearButtons();

        foreach (var view in preparedButtons)
        {
            if (view.moduleType != currentUpgradeSet.moduleType ||
                !currentUpgradeSet.upgradeAttributes.Exists(upgrade => upgrade.upgradeName == view.upgradeName))
                continue;
            view.gameObject.SetActive(true);
            uiByUpgrade[view.upgradeName] = BuildRefs(view);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)contentContainer);
    }

    void OnPreparedButtonClicked(UpgradeButtonView view)
    {
        if (!currentUpgradeSet || !view.gameObject.activeInHierarchy ||
            view.moduleType != currentUpgradeSet.moduleType) return;
        var upgrade = currentUpgradeSet.upgradeAttributes.Find(item => item.upgradeName == view.upgradeName);
        if (upgrade != null) OnUpgradeClicked(upgrade);
    }

    void ClearButtons()
    {
        StopAllCoroutines();
        layoutRebuildPending = false;

        foreach (var view in preparedButtons)
            view.gameObject.SetActive(false);
        uiByUpgrade.Clear();
    }

    void RefreshAll(bool holdSelection)
    {
        bool changedTestLayout = testSlidersVisible != ShowTestSliders;
        testSlidersVisible = ShowTestSliders;
        // refresh button content + state
        foreach (var upgrade in currentUpgradeSet.upgradeAttributes)
        {
            if (!uiByUpgrade.TryGetValue(upgrade.upgradeName, out var ui))
                continue;

            UpdateButtonUI(ui, upgrade);
            RefreshTestSlider(ui, upgrade);
            bool selected = holdSelection && currentSelectedUpgrade == upgrade.upgradeName;
            bool affordable = upgrade.level < upgrade.maxLevel &&
                Mathf.RoundToInt(upgrade.cost) <= ResourceManager.Instance.curMaterials;
            if (upgrade.upgradeName == eUpgradeName.TargetPriority && upgrade.level > 0)
                affordable = upgrade.ownerModule && upgrade.ownerModule.isBuilt;
            bool maxed = upgrade.level >= upgrade.maxLevel && upgrade.upgradeName != eUpgradeName.TargetPriority;
            ui.marker.enabled = true;
            ui.marker.color = maxed ? new Color32(174, 193, 195, 255) : affordable ? Color.white : colorBtnFrameCantBuy;
            ui.infoText.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
            if (IsCommandSet)
            {
                ui.marker.color = maxed ? new Color32(174, 193, 195, 255) : !affordable ? colorBtnFrameCantBuy : selected ? CommandCyan : Color.white;
                ui.infoText.color = selected ? CommandCyan : Color.white;
                ui.infoText.fontStyle = FontStyles.Bold;
                ui.costText.color = maxed ? new Color32(174, 193, 195, 255) : !affordable ? colorBtnFrameCantBuy : Color.white;
                ui.symbol.color = selected ? CommandCyan : Color.white;
                ui.glow.enabled = selected;
            }
        }

        if (changedTestLayout || testSlidersVisible) LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)contentContainer);

        if (currentSelectedUpgrade != eUpgradeName.None)
        {
            var selected = currentUpgradeSet.upgradeAttributes.Find(
                upgrade => upgrade.upgradeName == currentSelectedUpgrade);
            if (selected != null) RefreshInfoPanel(selected);
        }

    }

    void RefreshInfoPanel(UpgradeAttribute upgrade)
    {
        if (!infoPanel) return;

        infoPanel.SetActive(true);
        UIManager.ConstrainInfoPanel(infoPanel);

        var title = infoPanel.transform.Find(infoTitlePath)?.GetComponent<TextMeshProUGUI>();
        var desc = infoPanel.transform.Find(infoDescPath)?.GetComponent<TextMeshProUGUI>();

        if (title)
        {
            title.text = GetUpgradeTitle(upgrade.upgradeName);
            title.color = new Color32(148, 236, 244, 255);
            title.fontStyle = FontStyles.Bold;
        }

        if (desc)
        {
            desc.color = Color.white;
            string current = GetValueWithUnit(upgrade, upgrade.currentValue);
            string note = upgrade.IsPermanent
                ? "\n<size=75%><color=#9BACB0>Permanent upgrade</color></size>" : "";
            if (upgrade.upgradeName == eUpgradeName.TargetPriority && upgrade.level > 0)
            {
                string focus = Tower.Instance.SelectedPriority == Tower.TargetPriority.ArtilleryFirst
                    ? "Artillery first" : Tower.Instance.SelectedPriority == Tower.TargetPriority.Strongest
                    ? "Strongest enemy" : "Nearest enemy";
                desc.text = "<b>" + focus + "</b>\n" +
                    "<size=85%><color=#94ECF4>Switch target  |  Free</color></size>";
            }
            else if (upgrade.level >= upgrade.maxLevel)
                desc.text = "<b>" + current + "</b>\n<size=85%><color=#9BACB0>Fully upgraded</color></size>" + note;
            else
            {
                int price = Mathf.RoundToInt(upgrade.cost);
                string next = GetValueWithUnit(upgrade, upgrade.CalculateValue(upgrade.level + 1));
                string action = price <= ResourceManager.Instance.curMaterials
                    ? "<color=#94ECF4>Upgrade  " + price.ToString("N0") + " M</color>"
                    : "<color=#FF7777>" + price.ToString("N0") + " M  (missing " +
                        (price - ResourceManager.Instance.curMaterials).ToString("N0") + " M)</color>";
                int unitStart = next.LastIndexOf(' ');
                if (unitStart > 0 && current.EndsWith(next.Substring(unitStart), System.StringComparison.Ordinal))
                    current = current.Substring(0, current.Length - (next.Length - unitStart));
                string values = upgrade.upgradeName == eUpgradeName.TargetPriority
                    ? "Unlock target selection"
                    : "<b>" + current + "</b>  <color=#9BACB0>></color>  <color=#A8E6AD><b>" + next + "</b></color>";
                desc.text = values + "\n<size=85%>" + action + "</size>" + note;
            }
        }

        if (!layoutRebuildPending)
        {
            layoutRebuildPending = true;
            StartCoroutine(DelayedLayoutRebuild());
        }
    }

    string GetUpgradeTitle(eUpgradeName name)
    {
        switch (name)
        {
            case eUpgradeName.StructuralIntegrity: return "Structural Integrity";
            case eUpgradeName.TargetPriority: return "Target Priority";
            case eUpgradeName.DroneCount: return "Drone Slots";
            case eUpgradeName.DroneHP: return "Drone Health";
            case eUpgradeName.DroneBuildTime: return "Drone Build Speed";
            case eUpgradeName.ShieldCapacity: return "Shield Strength";
            case eUpgradeName.RechargeTime: return "Shield Recharge";
            case eUpgradeName.DeflectionChance: return "Shot Reflection";
            case eUpgradeName.AutoCollecting: return "Auto Collection";
            case eUpgradeName.CollectingEfficiency: return "Material Yield";
            case eUpgradeName.TimeMultiplier: return "Game Speed";
            default: return Regex.Replace(name.ToString(), "([a-z])([A-Z])", "$1 $2");
        }
    }

    void UpdateButtonUI(ButtonRefs ui, UpgradeAttribute upgrade)
    {
        ui.infoText.text = GetValueWithUnit(upgrade, upgrade.currentValue);
        if (IsCommandSet && upgrade.upgradeName == eUpgradeName.RotationSpeed)
            ui.infoText.text = (upgrade.currentValue * 100f).ToString("0.#") + "\n<size=60%>deg/s</size>";

        if (upgrade.upgradeName == eUpgradeName.TargetPriority && upgrade.level > 0)
        {
            ui.costText.text = "Switch";
            ui.button.interactable = true;
            return;
        }

        if (upgrade.level >= upgrade.maxLevel)
        {
            ui.costText.text = "MAX";
            ui.button.interactable = true;
            return;
        }

        int cost = Mathf.RoundToInt(upgrade.cost);
        ui.costText.text = Utils.FormatNumber(cost) + " M";
        ui.button.interactable = true;
    }

    void OnUpgradeClicked(UpgradeAttribute upgrade)
    {
        if (currentSelectedUpgrade != upgrade.upgradeName)
        {
            currentSelectedUpgrade = upgrade.upgradeName;
            RefreshAll(true);
            return;
        }

        if (!upgrade.ownerModule ||
            !upgrade.ownerModule.isBuilt || currentUpgradeSet == null ||
            !currentUpgradeSet.upgradeAttributes.Contains(upgrade)) return;

        if (upgrade.upgradeName == eUpgradeName.TargetPriority && upgrade.level > 0)
        {
            var next = (Tower.TargetPriority)(((int)Tower.Instance.SelectedPriority + 1) % 3);
            Tower.Instance.SetTargetPriority(next);
            SoundManager.Instance.PlayUpgradeSound();
            RefreshAll(true);
            return;
        }
        if (upgrade.level >= upgrade.maxLevel) return;

        int cost = Mathf.RoundToInt(upgrade.cost);
        if (!ResourceManager.Instance.SpendMaterial(cost, false)) return;

        SoundManager.Instance.PlayUpgradeSound();
        Stats.Instance.AddUpgrade(cost);

        upgrade.Upgrade();
        if (upgrade.upgradeName == eUpgradeName.TargetPriority)
            Tower.Instance.SetTargetPriority(Tower.TargetPriority.ArtilleryFirst);
        MatchReporter.Event("upgrade_bought", upgrade.ownerModule.moduleType.ToString(), upgrade.upgradeName.ToString(),
            cost, upgrade.level, "value=" + upgrade.currentValue);

        RefreshAll(true);

        TimeController.Instance.RefreshPanel();
        DroneManager.Instance.CheckDroneCanBuild();
        DroneManager.Instance.RefreshUIDroneCount();
    }

    IEnumerator DelayedLayoutRebuild()
    {
        yield return null;
        layoutRebuildPending = false;

        var layoutRoot = infoPanel.GetComponentInChildren<VerticalLayoutGroup>()?.transform as RectTransform;
        if (layoutRoot != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
    }

    ButtonRefs BuildRefs(UpgradeButtonView view)
    {
        if (refsByView.TryGetValue(view, out var cached)) return cached;
        var refs = new ButtonRefs
        {
            view = view,
            button = view.button,
            infoText = view.valueText,
            costText = view.priceText,
            symbol = view.symbol,
            marker = view.marker,
            glow = view.selectionGlow,
        };
        refsByView.Add(view, refs);
        return refs;
    }

    void RefreshTestSlider(ButtonRefs ui, UpgradeAttribute upgrade)
    {
        var layout = ui.view.GetComponent<LayoutElement>();
        if (!ui.testSlider && testSlidersVisible)
        {
            ui.normalWidth = layout.preferredWidth;
            ui.normalMinWidth = layout.minWidth;
            ui.normalButtonPosition = ((RectTransform)ui.button.transform).anchoredPosition;
            var root = CreateSliderRect("Test Level Slider", ui.view.transform, new Vector2(24f, 136f));
            root.anchorMin = root.anchorMax = new Vector2(1f, 0.5f);
            root.anchoredPosition = new Vector2(-16f, 10f);
            var hitArea = root.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;
            var track = CreateSliderRect("Track", root, new Vector2(6f, 136f)).gameObject.AddComponent<Image>();
            track.color = new Color32(47, 65, 69, 255);
            track.raycastTarget = false;
            var fill = CreateSliderRect("Fill", root, new Vector2(6f, 0f)).gameObject.AddComponent<Image>();
            fill.color = CommandCyan;
            fill.raycastTarget = false;
            var handle = CreateSliderRect("Handle", root, new Vector2(22f, 14f)).gameObject.AddComponent<Image>();
            handle.color = Color.white;
            ui.testSlider = root.gameObject.AddComponent<Slider>();
            ui.testSlider.direction = Slider.Direction.BottomToTop;
            ui.testSlider.wholeNumbers = true;
            ui.testSlider.fillRect = fill.rectTransform;
            ui.testSlider.handleRect = handle.rectTransform;
            ui.testSlider.targetGraphic = handle;
            var textRect = CreateSliderRect("Level", root, new Vector2(44f, 24f));
            textRect.anchoredPosition = new Vector2(0f, -86f);
            ui.testLevel = textRect.gameObject.AddComponent<TextMeshProUGUI>();
            ui.testLevel.font = ui.infoText.font;
            ui.testLevel.fontSize = 14f;
            ui.testLevel.alignment = TextAlignmentOptions.Center;
            ui.testLevel.color = Color.white;
            ui.testLevel.raycastTarget = false;
            ui.testSlider.onValueChanged.AddListener(value => OnTestLevelChanged(ui.view, value));
        }
        if (!ui.testSlider) return;
        ui.testSlider.gameObject.SetActive(testSlidersVisible);
        layout.preferredWidth = testSlidersVisible ? ui.normalWidth + 40f : ui.normalWidth;
        layout.minWidth = testSlidersVisible ? ui.normalMinWidth + 40f : ui.normalMinWidth;
        ((RectTransform)ui.button.transform).anchoredPosition = ui.normalButtonPosition + (testSlidersVisible ? Vector2.left * 20f : Vector2.zero);
        ui.testSlider.minValue = 0;
        ui.testSlider.maxValue = upgrade.maxLevel;
        ui.testSlider.SetValueWithoutNotify(upgrade.level);
        ui.testLevel.text = upgrade.level.ToString();
    }

    static RectTransform CreateSliderRect(string name, Transform parent, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = size;
        return rect;
    }

    void OnTestLevelChanged(UpgradeButtonView view, float value)
    {
        if (!currentUpgradeSet || !view.gameObject.activeInHierarchy || view.moduleType != currentUpgradeSet.moduleType) return;
        var upgrade = currentUpgradeSet.upgradeAttributes.Find(item => item.upgradeName == view.upgradeName);
        TestControls?.SetUpgradeLevel(upgrade, Mathf.RoundToInt(value));
    }

    string GetValueWithUnit(UpgradeAttribute upgrade, float value)
    {

        if (ShouldRound(upgrade.upgradeName))
            value = Mathf.RoundToInt(value);

        switch (upgrade.upgradeName)
        {
            case eUpgradeName.TargetPriority:
                if (upgrade.level == 0) return value > 0 ? "Artillery" : "Locked";
                switch (Tower.Instance.SelectedPriority)
                {
                    case Tower.TargetPriority.ArtilleryFirst: return "Artillery";
                    case Tower.TargetPriority.Strongest: return "Strongest";
                    default: return "Nearest";
                }
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
            case eUpgradeName.RotationSpeed: return (value * 100f).ToString("0.#") + " deg/s";
            case eUpgradeName.StructuralIntegrity:
                var core = StationModule.GetModuleByType(StationModule.eModuleType.Core);
                int coreHP = core.BaseHP + Mathf.RoundToInt(value) - Mathf.RoundToInt(upgrade.baseValue);
                return Mathf.Max(1, coreHP) + " HP";
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

        ClearButtons();

        if (panel) panel.SetActive(false);
    }

    class ButtonRefs
    {
        public UpgradeButtonView view;
        public Slider testSlider;
        public TextMeshProUGUI testLevel;
        public float normalWidth, normalMinWidth;
        public Vector2 normalButtonPosition;
        public Button button;
        public TextMeshProUGUI infoText;
        public TextMeshProUGUI costText;
        public Image symbol;
        public Image marker;
        public Outline glow;
    }
}
