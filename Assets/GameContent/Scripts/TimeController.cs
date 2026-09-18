using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TimeController : MonoBehaviour, IResettable
{
    public static TimeController Instance;

    [Header("UI")]
    public GameObject panelTimeModulation;
    public Button btnTimeIncrease;
    public Button btnTimeDecrease;
    public TextMeshProUGUI txtTime;

    [Header("Config")]
    public float step = 0.5f;
    public float minTimeModulation = 1f;
    public float stationUITimeModulation = 0.5f;

    float current = 1f;
    float initialCurrent;
    bool stationUIOpen;


    void Awake()
    {
        Instance = this;
    }

    public void Init()
    {
        btnTimeIncrease.onClick.AddListener(() => Change(+1));
        btnTimeDecrease.onClick.AddListener(() => Change(-1));
        RefreshPanel();
        Apply();
    }

    public void RefreshPanel()
    {
        bool unlocked = StationModule
            .GetModuleByType(StationModule.eModuleType.TemporalModulator)
            .isBuilt;

        panelTimeModulation.SetActive(unlocked);

        UpdateText();
    }

    public void OnModulDestroy()
    {
        current = minTimeModulation;
        Apply();

        RefreshPanel();
    }

    void Change(int dir)
    {
        float max = UpgradeAttribute.GetUpgradeByName(UpgradeAttribute.eUpgradeName.TimeMultiplier).currentValue;
        current = Mathf.Clamp(current + dir * step, minTimeModulation, max);
        Apply();
        UpdateText();
    }

    void Apply()
    {
        Time.timeScale = stationUIOpen ? stationUITimeModulation : current;
    }

    void UpdateText()
    {
        txtTime.text = $"x{current:0.#}";
    }

    // === Station UI ===
    public void OnStationUIOpen()
    {
        if (stationUIOpen) return;
        stationUIOpen = true;
        btnTimeIncrease.interactable = false;
        btnTimeDecrease.interactable = false;
        Apply();
    }

    public void OnStationUIClose()
    {
        if (!stationUIOpen) return;
        stationUIOpen = false;
        btnTimeIncrease.interactable = true;
        btnTimeDecrease.interactable = true;
        Apply();
    }


    public void StoreInit()
    {
        initialCurrent = current;
    }

    public void ResetScript()
    {
        current = initialCurrent;
        stationUIOpen = false;
        btnTimeIncrease.interactable = true;
        btnTimeDecrease.interactable = true;
        Apply();
        RefreshPanel();
    }
}
