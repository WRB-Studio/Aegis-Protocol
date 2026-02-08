using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DroneManager : MonoBehaviour, IResettable
{
    public static DroneManager Instance;

    public GameObject dronePrefab;
    public List<DroneSlot> droneSlots;
    public int currentDroneSlots;
    public float droneBuildTime;
    [HideInInspector] public float droneBuildCountdown;

    [HideInInspector] public int droneInitialHP;
    [HideInInspector] public int droneInitialDamage;

    public TextMeshProUGUI txtDroneCount;
    public TextMeshProUGUI txtDroneBuildTime;

    [HideInInspector] public List<Drone> allDrones = new List<Drone>();
    private Transform spawnParent;


    // --- INIT SNAPSHOT ---
    private int initcurrentDroneSlots;
    private float initdroneBuildTime;
    private float initdroneBuildCountdown;

    private int initdroneInitialHP;
    private int initdroneInitialDamage;


    private void Awake()
    {
        Instance = this;
    }

    public void Init()
    {
        droneBuildCountdown = droneBuildTime;
        spawnParent = GameObject.Find("DroneParent").transform;

        txtDroneCount.gameObject.SetActive(false);
        txtDroneBuildTime.gameObject.SetActive(false);
    }

    public void UpdateNormal()
    {
        if (!StationModule.GetModuleByType(StationModule.eModuleType.Drone).isBuilt) return;

        foreach (var drone in allDrones.ToArray())
        {
            if (drone == null) continue;
            drone.UpdateNormal();
        }

        if (allDrones.Count < currentDroneSlots)
        {
            droneBuildCountdown -= Time.deltaTime;
            txtDroneBuildTime.text = droneBuildCountdown.ToString("F0");

            if (droneBuildCountdown <= 0)
            {
                droneBuildCountdown = droneBuildTime;
                SpawnDrone();
            }
        }
    }

    public void AfterModulInit()
    {
        droneBuildCountdown = droneBuildTime;
        txtDroneCount.gameObject.SetActive(true);
        txtDroneCount.text = allDrones.Count.ToString() + "/" + currentDroneSlots.ToString();
    }

    public void AfterModuleOff()
    {
        txtDroneCount.gameObject.SetActive(false);
        txtDroneBuildTime.gameObject.SetActive(false);

        foreach (var drone in allDrones.ToArray())
        {
            if (drone == null) continue;
            RemoveDrone(drone, Stats.eDeadBy.None);
        }
    }

    public void RefreshUIDroneCount()
    {
        txtDroneCount.text = allDrones.Count.ToString() + "/" + currentDroneSlots.ToString();
    }

    public void CheckDroneCanBuild()
    {
        if (allDrones.Count < currentDroneSlots)
            txtDroneBuildTime.gameObject.SetActive(true);
        else
            txtDroneBuildTime.gameObject.SetActive(false);
    }

    public GameObject SpawnDrone()
    {
        foreach (DroneSlot slot in droneSlots)
        {
            if (slot.occupiedDrone == null)
            {
                Stats.Instance.dronesBuilt++;
                GameObject drone = Instantiate(dronePrefab, slot.transform.position, slot.transform.rotation, spawnParent);
                allDrones.Add(drone.GetComponent<Drone>());
                slot.occupiedDrone = drone.GetComponent<Drone>();
                SaveGameManager.Instance.Save();
                CheckDroneCanBuild();
                RefreshUIDroneCount();
                return drone;
            }
        }

        RefreshUIDroneCount();
        CheckDroneCanBuild();
        return null;
    }

    public void RemoveDrone(Drone drone, Stats.eDeadBy deadBy)
    {
        allDrones.Remove(drone);
        Destroy(drone.gameObject);
        RefreshUIDroneCount();
        CheckDroneCanBuild();
    }

    public void RemoveAllDrones() 
    { 
        foreach (var drone in allDrones.ToArray())
        {
            if (drone == null) continue;
            RemoveDrone(drone, Stats.eDeadBy.None);
        }
        allDrones.Clear();
    }

    public void StoreInit()
    {
        initcurrentDroneSlots = currentDroneSlots;
        initdroneBuildTime = droneBuildTime;
        initdroneBuildCountdown = droneBuildCountdown;

        initdroneInitialHP = droneInitialHP;
        initdroneInitialDamage = droneInitialDamage;
    }

    public void ResetScript()
    {
        // runtime clear
        RemoveAllDrones();

        currentDroneSlots = initcurrentDroneSlots;
        droneBuildTime = initdroneBuildTime;
        droneBuildCountdown = initdroneBuildCountdown;

        droneInitialHP = initdroneInitialHP;
        droneInitialDamage = initdroneInitialDamage;

        // UI sync (keine init-vars nötig)
        txtDroneCount.gameObject.SetActive(false);
        txtDroneBuildTime.gameObject.SetActive(false);
        txtDroneBuildTime.text = droneBuildCountdown.ToString("F0");
    }
}
