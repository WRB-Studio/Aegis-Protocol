using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StationModule : MonoBehaviour, IResettable
{
    public enum eModuleType
    {
        None,
        Core,
        Extractor,
        Shield,
        Drone,
        Radar,
        AmmoFabricator,
        CommandUnit,
        TemporalModulator,
    }

    public eModuleType moduleType = eModuleType.None;
    public string description;

    public int maxHP = 10;
    public int currentHP;
    public int cost;
    public bool isBuilt = false;

    public UpgradeSet upgradeSet;

    [HideInInspector] public Transform linePoint;

    public static List<StationModule> allModules = new List<StationModule>();

    // --- INIT SNAPSHOT ---
    private int initmaxHP;
    private int initcurrentHP;
    private int initcost;
    private bool initisBuilt;


    void Awake()
    {
        allModules.Add(this);
    }

    public void Init()
    {
        currentHP = maxHP;

        linePoint = transform.Find("LinePoint");

        if (!isBuilt) gameObject.SetActive(false);
    }

    public void TakeDamage(int damage)
    {
        SoundManager.Instance.PlayStationHitSound();
        Stats.Instance.modulesDamageTaken += damage;

        currentHP -= damage;
        if (currentHP < 0) currentHP = 0;

        if (UIManager.Instance.stationUI.activeSelf)
        {
            UpgradeUI.Instance.Refresh();
            ModulesUI.Instance.RefreshPanel();
        }

        SaveGameManager.Instance.Save();

        if (currentHP <= 0) Die();

    }

    public void Die()
    {
        if (moduleType == eModuleType.Core)
        {
            foreach (var module in allModules.ToArray())
            {
                if (module == null || module.moduleType == eModuleType.Core) continue;

                if (module.isBuilt)
                {
                    ExplosionManager.Instance.CreateModuleExplosion(module.transform.position);
                    Stats.Instance.modulesDestroyed++;
                }

                module.isBuilt = false;
                module.gameObject.SetActive(false);
            }

            ExplosionManager.Instance.CreateCoreExplosion(transform.position);
            Stats.Instance.modulesDestroyed++;
            SaveGameManager.Instance.Save();

            GameManager.Instance.GameOver();
            Tower.Instance.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }
        else
        {
            ExplosionManager.Instance.CreateModuleExplosion(transform.position);

            Stats.Instance.modulesDestroyed++;
            isBuilt = false;

            switch (moduleType)
            {
                case eModuleType.None:
                    break;
                case eModuleType.Core:
                    break;
                case eModuleType.Extractor:
                    ResourceManager.Instance.disableAutoCollecting();
                    break;
                case eModuleType.Shield:
                    Shield.Instance.deactivateShield();
                    break;
                case eModuleType.Drone:
                    DroneManager.Instance.AfterModuleOff();
                    break;
                case eModuleType.Radar:
                    //handled in Tower.cs
                    break;
                case eModuleType.AmmoFabricator:
                    //handled in Tower.cs
                    break;
                case eModuleType.CommandUnit:
                    //Maybe later!?
                    //decrease all modules HP?
                    //decrease tower rotation speed?
                    break;
                case eModuleType.TemporalModulator:
                    UIManager.Instance.RefreshTimeModulator();
                    break;
                default:
                    break;
            }

            SaveGameManager.Instance.Save();

            UpgradeUI.Instance.Refresh();
            ModulesUI.Instance.RefreshPanel();

            gameObject.SetActive(false);
        }

        ImpactFX.Instance.PlayImpactEffect();
    }

    public static StationModule GetModuleByType(eModuleType type)
    {
        return allModules.FirstOrDefault(m => m.moduleType == type);
    }

    public int GetModuleRepairCost()
    {
        return (maxHP - currentHP) * 5;
    }

    public void StoreInit()
    {
        initmaxHP = maxHP;
        initcurrentHP = currentHP;
        initcost = cost;
        initisBuilt = isBuilt;
    }

    public void ResetScript()
    {
        maxHP = initmaxHP;
        currentHP = initcurrentHP;
        cost = initcost;
        isBuilt = initisBuilt;

        // state sync
        gameObject.SetActive(isBuilt);
    }

}
