using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour, IResettable
{
    public static UIManager Instance;

    [Header("Root")]
    public GameObject stationUI;

    [Header("Slow Panel")]
    [SerializeField] private GameObject slowPanel;
    [SerializeField] private float slowPanelChangeDuration = 1f;

    private float originScale = 1f;

    [Header("Info Panel")]
    public GameObject UIManagerInfoPanel;
    [SerializeField] private TextMeshProUGUI txtTitle;
    [SerializeField] private TextMeshProUGUI txtInfo;

    [Header("Buy")]
    public Button btnBuy;
    [SerializeField] private TextMeshProUGUI txtBuyCost;
    [HideInInspector] public Color btnBuyOriginalColor;

    // --- INIT SNAPSHOT ---
    private float initoriginScale;
    private float initslowPanelChangeDuration;


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

    }

    public void UpdateNormal()
    {
        if (!Utils.TryGetPointerDown(out var screenPosition) || Utils.IsPointerOverUI()) return;

        Vector2 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
        RaycastHit2D hit = Physics2D.Raycast(worldPosition, Vector2.zero);

        bool hitStation = hit.collider != null &&
                          (hit.collider.CompareTag("Tower") || hit.collider.CompareTag("Station"));

        if (hitStation && !stationUI.activeSelf)
            Show(true);
        else if (!hitStation && stationUI.activeSelf)
            Show(false);
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

            TimeController.Instance.OnStationUIOpen();
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

            TimeController.Instance.OnStationUIClose();
        }
    }

    public IEnumerator ShowSlowPanel(bool show)
    {
        float duration = slowPanelChangeDuration;

        if (show) duration /= 1.5f;

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

        if (!show) slowPanel.SetActive(false);
    }

    public void ShowInfoPanel(string title, string info, int cost, bool showBtnBuy = false)
    {
        UIManagerInfoPanel.SetActive(true);

        string formatted = Regex.Replace(title, "([a-z])([A-Z])", "$1 $2");
        txtTitle.text = char.ToUpper(formatted[0]) + formatted.Substring(1);

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
      

    public void StoreInit()
    {
        initoriginScale = originScale;
        initslowPanelChangeDuration = slowPanelChangeDuration;
    }

    public void ResetScript()
    {
        StopAllCoroutines();

        originScale = initoriginScale;
        slowPanelChangeDuration = initslowPanelChangeDuration;

        stationUI.SetActive(false);
        slowPanel.transform.localScale = Vector3.zero;
        slowPanel.SetActive(false);
        UIManagerInfoPanel.SetActive(false);
    }

}
