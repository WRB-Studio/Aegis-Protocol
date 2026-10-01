using System.Collections.Generic;
using System.Linq;
using System;
using Unity.Collections;
using UnityEngine;

public class Tower : MonoBehaviour, IResettable
{
    public static Tower Instance;

    public enum TargetPriority { Nearest, ArtilleryFirst, Strongest }
    public TargetPriority SelectedPriority { get; private set; }
    public TargetPriority EffectivePriority
    {
        get
        {
            var command = StationModule.GetModuleByType(StationModule.eModuleType.CommandUnit);
            var upgrade = UpgradeAttribute.GetUpgradeByName(UpgradeAttribute.eUpgradeName.TargetPriority);
            return command && command.isBuilt && upgrade != null && upgrade.level > 0
                ? SelectedPriority : TargetPriority.Nearest;
        }
    }

    public void SetTargetPriority(TargetPriority priority)
    {
        SelectedPriority = priority;
        MatchReporter.Event("target_priority_changed", "CommandUnit", priority.ToString());
        SaveGameManager.Instance?.RequestSave();
    }

    public void RestoreTargetPriority(string priority)
    {
        SelectedPriority = Enum.TryParse(priority, out TargetPriority parsed) && Enum.IsDefined(typeof(TargetPriority), parsed)
            ? parsed : TargetPriority.Nearest;
    }

    [Header("Gun Setup")]
    public Transform gun; // Gun object (child of Tower)
    public Transform firePoint; // Projectile start point
    public GameObject projectilePrefab;

    public float fireRate;
    public float fireRange;
    public float rotationSpeed;
    public int damage;


    public float initialFireRate;
    public float initialFireRange;
    public int initialDamage;

    public float EffectiveFireRange
    {
        get
        {
            var radar = StationModule.GetModuleByType(StationModule.eModuleType.Radar);
            return radar && radar.isBuilt ? fireRange : initialFireRange;
        }
    }

    [Range(0f, 45f)] public float aimToleranceAngle = 5f; // Degree tolerance for firing

    private float fireCooldown = 0f;
    private Transform currentTarget;


    // --- INIT SNAPSHOT ---
    private float initfireRate;
    private float initfireRange;
    private float initrotationSpeed;
    private int initdamage;
    private float initaimToleranceAngle;

    private float initfireCooldown;
    private Transform initcurrentTarget;


    private void Awake()
    {
        Instance = this;
    }

    public void Init()
    {
        initialFireRate = fireRate;
        initialFireRange = fireRange;
        initialDamage = damage;
    }

    public void UpdateNormal()
    {
        FindTarget();

        if (currentTarget != null)
        {
            RotateTowardsTarget();

            if (fireCooldown <= 0f && IsGunAimedAtTarget())
            {
                Fire();

                float fireRate = this.fireRate;
                if (StationModule.GetModuleByType(StationModule.eModuleType.AmmoFabricator).isBuilt == false)
                    fireRate = initialFireRate;

                fireCooldown = 1f / fireRate;
            }
        }

        fireCooldown -= Time.deltaTime;
    }

    void FindTarget()
    {
        float fireRange = EffectiveFireRange;

        float shortestDistance = fireRange;
        int highestPriority = -1;
        var priority = EffectivePriority;
        Transform nearest = null;

        var enemies = EnemySpawner.Instance ? EnemySpawner.Instance.instantiatedEnemies : null;
        if (enemies == null)
        {
            currentTarget = null;
            return;
        }

        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            var enemy = enemies[i];
            if (!enemy) continue;
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist >= fireRange || Utils.IsOutOfView(enemy.transform.position)) continue;
            int rank = priority == TargetPriority.ArtilleryFirst
                ? (enemy.enemyType == Enemy.eEnemyType.Ranged ? 1 : 0)
                : priority == TargetPriority.Strongest ? enemy.maxHP : 0;
            if (rank > highestPriority || (rank == highestPriority && dist < shortestDistance))
            {
                highestPriority = rank;
                shortestDistance = dist;
                nearest = enemy.transform;
            }
        }

        currentTarget = nearest;
    }

    void RotateTowardsTarget()
    {
        Vector3 direction = currentTarget.position - gun.position;
        Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, direction);
        gun.rotation = Quaternion.RotateTowards(gun.rotation, targetRotation, rotationSpeed * Time.deltaTime * 100);
    }

    bool IsGunAimedAtTarget()
    {
        Vector3 toTarget = (currentTarget.position - gun.position).normalized;
        float angle = Vector3.Angle(gun.up, toTarget);
        return angle < aimToleranceAngle && !Utils.IsOutOfView(currentTarget.position);
    }

    void Fire()
    {
        Projectile newProjectile = ProjectileManager.Instance.spawnProjectile(projectilePrefab, firePoint.position);
        newProjectile.transform.rotation = gun.rotation;

        int damage = this.damage;
        if (StationModule.GetModuleByType(StationModule.eModuleType.AmmoFabricator).isBuilt == false)
            damage = initialDamage;

        newProjectile.GetComponent<Projectile>().damage = damage;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, fireRange);
    }

    public void StoreInit()
    {
        initfireRate = fireRate;
        initfireRange = fireRange;
        initrotationSpeed = rotationSpeed;
        initdamage = damage;
        initaimToleranceAngle = aimToleranceAngle;

        initfireCooldown = fireCooldown;
        initcurrentTarget = currentTarget;
    }

    public void ResetScript()
    {
        gameObject.SetActive(true);

        fireRate = initfireRate;
        fireRange = initfireRange;
        rotationSpeed = initrotationSpeed;
        damage = initdamage;
        aimToleranceAngle = initaimToleranceAngle;

        fireCooldown = initfireCooldown;
        currentTarget = initcurrentTarget;
        SelectedPriority = TargetPriority.Nearest;
    }
}
