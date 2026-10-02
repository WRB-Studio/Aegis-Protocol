using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(EnemySpawner))]
public sealed class BossTestControls : MonoBehaviour
{
    [Tooltip("Disable this before making a release build.")]
    public bool enableBossTesting;
    [SerializeField, Min(5)] int bossTestWave = 5;
    [SerializeField, Min(1)] int testMaterials = 1000000;

    Button testButton, nextBossButton, previousBossButton;
    TextMeshProUGUI label;

    void Update()
    {
        if (enableBossTesting && !testButton && GameManager.isInit) CreateButton();
        if (!testButton) return;
        testButton.gameObject.SetActive(enableBossTesting && !GameManager.gameOver);
        nextBossButton.gameObject.SetActive(testButton.gameObject.activeSelf);
        previousBossButton.gameObject.SetActive(testButton.gameObject.activeSelf);
        testButton.interactable = GameManager.isInit && Time.timeScale > 0f;
        nextBossButton.interactable = testButton.interactable;
        previousBossButton.interactable = testButton.interactable && SelectedWave > 5;
        RefreshLabel();
    }

    void RefreshLabel()
    {
        if (!label) return;
        label.text = GetComponent<EnemySpawner>().IsBossTestActive
            ? $"Boss Wave {SelectedWave}\nSpawnen"
            : $"Boss Wave {SelectedWave} · Testmodus\n+{Utils.FormatNumber(testMaterials)} Material";
    }

    int SelectedWave => Mathf.Max(5, Mathf.CeilToInt(bossTestWave / 5f) * 5);

    public void StartBossTest()
    {
        if (!enableBossTesting || !GameManager.isInit || GameManager.gameOver || Time.timeScale <= 0f) return;
        GetComponent<EnemySpawner>().StartBossTest(SelectedWave, testMaterials);
        RefreshLabel();
    }

    public void SelectNextBoss()
    {
        if (!enableBossTesting || !GameManager.isInit || GameManager.gameOver || Time.timeScale <= 0f) return;
        bossTestWave = SelectedWave + 5;
        RefreshLabel();
    }

    public void SelectPreviousBoss()
    {
        if (!enableBossTesting || !GameManager.isInit || GameManager.gameOver || Time.timeScale <= 0f) return;
        bossTestWave = Mathf.Max(5, SelectedWave - 5);
        RefreshLabel();
    }

    public void SetUpgradeLevel(UpgradeAttribute upgrade, int level)
    {
        if (!enableBossTesting || !GameManager.isInit || GameManager.gameOver || Time.timeScale <= 0f ||
            !GetComponent<EnemySpawner>().IsBossTestActive || upgrade == null ||
            !UpgradeAttribute.allUpgradeAttributes.Contains(upgrade)) return;
        upgrade.level = Mathf.Clamp(level, 0, upgrade.maxLevel);
        upgrade.RecalculateFromLevel();
        upgrade.ApplyUpgradeEffect();
        if (upgrade.upgradeName == UpgradeAttribute.eUpgradeName.TargetPriority)
            Tower.Instance.SetTargetPriority(upgrade.level > 0 ? Tower.TargetPriority.ArtilleryFirst : Tower.TargetPriority.Nearest);
        SaveGameManager.Instance.RequestSave();
        UpgradeUI.Instance.Refresh();
        TimeController.Instance.RefreshPanel();
        DroneManager.Instance.CheckDroneCanBuild();
        DroneManager.Instance.RefreshUIDroneCount();
    }

    void CreateButton()
    {
        testButton = CreateTestButton("Boss Test Button", 0f, 340f);
        testButton.onClick.AddListener(StartBossTest);
        label = testButton.GetComponentInChildren<TextMeshProUGUI>();
        nextBossButton = CreateTestButton("Next Boss Button", 218f, 80f);
        nextBossButton.GetComponentInChildren<TextMeshProUGUI>().text = "Weiter";
        nextBossButton.onClick.AddListener(SelectNextBoss);
        previousBossButton = CreateTestButton("Previous Boss Button", -228f, 100f);
        previousBossButton.GetComponentInChildren<TextMeshProUGUI>().text = "Zurück";
        previousBossButton.onClick.AddListener(SelectPreviousBoss);
        previousBossButton.interactable = SelectedWave > 5;
        RefreshLabel();
    }

    Button CreateTestButton(string name, float x, float width)
    {
        var canvas = UIManager.Instance.stationUI.GetComponentInParent<Canvas>().rootCanvas;
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(canvas.transform, false);
        var rect = (RectTransform)buttonObject.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(x, 24f);
        rect.sizeDelta = new Vector2(width, 72f);
        buttonObject.GetComponent<Image>().color = new Color(0.3f, 0.055f, 0.055f, 0.95f);
        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();

        var text = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        text.transform.SetParent(buttonObject.transform, false);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
        text.font = UIManager.Instance.btnBuy.GetComponentInChildren<TextMeshProUGUI>(true).font;
        text.fontSize = 22f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        return button;
    }

    void OnDestroy()
    {
        if (testButton) Destroy(testButton.gameObject);
        if (nextBossButton) Destroy(nextBossButton.gameObject);
        if (previousBossButton) Destroy(previousBossButton.gameObject);
    }
}
