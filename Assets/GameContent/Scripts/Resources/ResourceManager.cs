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


    void Awake()
    {
        Instance = this;
    }

    public void Init()
    {
        spawnParent = GameObject.Find("ResourceParent").transform;
        RefreshUI();
    }

    public void UpdateNormal()
    {
        if (!autoCollecting && Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Vector2 touchPos = Camera.main.ScreenToWorldPoint(Input.GetTouch(0).position);
            RaycastHit2D hit = Physics2D.Raycast(touchPos, Vector2.zero);

            if (hit.collider != null && hit.collider.CompareTag("Material"))
            {
                CollectEffect effect = hit.collider.GetComponent<CollectEffect>();

                effect.flyToStation = true;
                effect.setOriginScale();
                effect.collectedManually = true;                
            }
        }

        foreach (CollectEffect effect in collectEffects.ToArray())
        {
            if (effect == null) continue;
            effect.UpdateNormal();
        }
    }

    public void enableAutoCollecting()
    {
        autoCollecting = true;

        foreach (CollectEffect effect in collectEffects.ToArray())
        {
            if (effect == null) continue;
            effect.flyToStation = true;
            effect.setOriginScale();
        }
    }

    public void disableAutoCollecting()
    {
        autoCollecting = false;
    }

    public void RefreshUI()
    {
        txtMaterial.text = $"Material: {Utils.FormatNumber(curMaterials)} $";
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
            UpgradeUI.Instance.Refresh();
            ModulesUI.Instance.RefreshPanel();
        }

        RefreshUI();

        SaveGameManager.Instance.Save();
    }

    public bool SpendMaterial(int amount)
    {
        if (curMaterials >= amount)
        {
            curMaterials -= amount;
            RefreshUI();

            SaveGameManager.Instance.Save();

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
