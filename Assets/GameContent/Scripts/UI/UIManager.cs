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
