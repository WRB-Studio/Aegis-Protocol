using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour, IResettable
{
    public static UIManager Instance;

    public GameObject stationUI;

    public GameObject slowPanel;
    public float timeSpeedInUI = 0.5f;
    private float originScale = 1f;
    public float slowPanelChangeDuration = 1f;

    public GameObject UIManagerInfoPanel;
    public TextMeshProUGUI txtTitle;
    public TextMeshProUGUI txtInfo;

    public Button btnRepair;
    public TextMeshProUGUI txtRepairCost;

    public Button btnBuy;
    [HideInInspector] public Color btnBuyOriginalColor;
    public TextMeshProUGUI txtBuyCost;

    [Header("Time Modulation")]
    public GameObject panelTimeModulation;
    public Button btnIncreaseTime;
    public Button btnDecreaseTime;
    public TextMeshProUGUI txtTimeModulation;
    [HideInInspector] public float currentTimeModulation = 1f;


    // --- INIT SNAPSHOT ---
    private float inittimeSpeedInUI;
    private float initoriginScale;
    private float initslowPanelChangeDuration;
    private float initcurrentTimeModulation;



    private void Awake()
    {
        Instance = this;
    }

    public void Init()
    {
        stationUI.gameObject.SetActive(false);
        originScale = slowPanel.transform.localScale.x;
        slowPanel.transform.localScale = Vector3.zero;
        btnBuyOriginalColor = btnBuy.image.color;

        btnIncreaseTime.onClick.AddListener(() => { OnTimeModulationChanged(true); });
        btnDecreaseTime.onClick.AddListener(() => { OnTimeModulationChanged(false); });

        RefreshTimeModulator();
    }

    public void UpdateNormal()
    {
#if UNITY_ANDROID
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            // UI prüfen
            if (Utils.IsPointerOverUI())
                return;

            Vector2 touchPos = Camera.main.ScreenToWorldPoint(Input.GetTouch(0).position);

            RaycastHit2D hit = Physics2D.Raycast(touchPos, Vector2.zero);

            if (hit.collider != null &&
               (hit.collider.CompareTag("Tower") || hit.collider.CompareTag("Station")) &&
               !stationUI.activeSelf)
            {
                Show(true);
            }
            else
            {
                Show(false);
                UIToWorldLine.Instance.HideLine();
                UpgradeUI.Instance.Hide();
            }
        }
#endif

#if UNITY_STANDALONE

        // Linksklick erkannt
        if (Input.GetMouseButtonDown(0))
        {
            // Wenn auf UI geklickt → nichts tun
            if (IsPointerOverUI())
            {
                return;
            }

            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            // Tower angeklickt → UI anzeigen
            if (hit.collider != null && 
               (hit.collider.CompareTag("Tower") || hit.collider.CompareTag("Station")))
            {
                Show(tower, tower.transform.position);
            }
            else
            {
                // Klick ins Leere → UI schließen
                Show(null, Vector3.zero);
            }
        }
#endif
    }

    public void Show(bool show)
    {
        if (show)
        {
            SoundManager.Instance.SetMusicPitch(0.98f);

            stationUI.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(ShowSlowPanel(true));

            ModulesUI.Instance.ResetModulePanel();
            ModulesUI.Instance.RefreshPanel();

            Time.timeScale = timeSpeedInUI;
        }
        else
        {
            SaveGameManager.Instance.Save();

            SoundManager.Instance.SetMusicPitch(1f);

            UpgradeUI.Instance.Hide();
            ModulesUI.Instance.Hide();

            stationUI.SetActive(false);
            StopAllCoroutines();
            StartCoroutine(ShowSlowPanel(false));

            Time.timeScale = currentTimeModulation;
        }
    }

    public IEnumerator ShowSlowPanel(bool show)
    {
        float duration = slowPanelChangeDuration;

        if (show)
            duration /= 1.5f;

        float targetScale;
        float startScale;

        if (show)
        {
            targetScale = originScale;
            startScale = slowPanel.transform.localScale.x;
            slowPanel.SetActive(true);
        }
        else
        {
            targetScale = 0;
            startScale = slowPanel.transform.localScale.x;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            slowPanel.transform.localScale = Vector3.Lerp(
                new Vector3(startScale, startScale, startScale),
                new Vector3(targetScale, targetScale, targetScale),
                t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        slowPanel.transform.localScale = new Vector3(targetScale, targetScale, targetScale);

        if (!show)
            slowPanel.SetActive(false);
    }

    public void ShowInfoPanel(string title, string info, int cost, bool showBtnBuy = false)
    {
        UIManagerInfoPanel.SetActive(true);
        txtTitle.text = title;
        txtInfo.text = info;
        txtBuyCost.text = cost.ToString() + " $";
        btnBuy.gameObject.SetActive(showBtnBuy);
        StartCoroutine(DelayedLayoutRebuild());
    }

    private IEnumerator DelayedLayoutRebuild()
    {
        yield return null;

        var layoutRoot = UIManagerInfoPanel.GetComponentInChildren<VerticalLayoutGroup>()?.transform as RectTransform;
        if (layoutRoot != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
    }


    public void RefreshTimeModulator()
    {
        if (StationModule.GetModuleByType(StationModule.eModuleType.TemporalModulator).isBuilt)
        {
            UpgradeAttribute upgrade = UpgradeAttribute.GetUpgradeByName(UpgradeAttribute.eUpgradeName.TimeMultiplier);

            txtTimeModulation.text = $"x{(currentTimeModulation % 1 == 0 ? currentTimeModulation.ToString("F0") : currentTimeModulation.ToString("F1"))}";

            panelTimeModulation.gameObject.SetActive(true);
        }
        else
        {
            panelTimeModulation.gameObject.SetActive(false);
        }
    }

    private void OnTimeModulationChanged(bool increase)
    {
        if (stationUI.activeSelf) return;

        float steppedValue = increase ? 0.5f : -0.5f;

        float maxMultiplier = UpgradeAttribute.GetUpgradeByName(UpgradeAttribute.eUpgradeName.TimeMultiplier).currentValue;
        currentTimeModulation = Mathf.Clamp(currentTimeModulation + steppedValue, 1f, maxMultiplier);
        Time.timeScale = currentTimeModulation;

        txtTimeModulation.text = $"x{(currentTimeModulation % 1 == 0 ? currentTimeModulation.ToString("F0") : currentTimeModulation.ToString("F1"))}";
    }


    public void StoreInit()
    {
        inittimeSpeedInUI = timeSpeedInUI;
        initoriginScale = originScale;
        initslowPanelChangeDuration = slowPanelChangeDuration;
        initcurrentTimeModulation = currentTimeModulation;
    }

    public void ResetScript()
    {
        StopAllCoroutines();

        timeSpeedInUI = inittimeSpeedInUI;
        originScale = initoriginScale;
        slowPanelChangeDuration = initslowPanelChangeDuration;
        currentTimeModulation = initcurrentTimeModulation;

        stationUI.SetActive(false);
        slowPanel.transform.localScale = Vector3.zero;
        slowPanel.SetActive(false);
        UIManagerInfoPanel.SetActive(false);

        Time.timeScale = currentTimeModulation;
        RefreshTimeModulator();
    }

}
