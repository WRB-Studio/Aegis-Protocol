using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

public class Tower : MonoBehaviour, IResettable
{
    public static Tower Instance;

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
        if (currentTarget == null)
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
        float fireRange = this.fireRange;
        if (StationModule.GetModuleByType(StationModule.eModuleType.Radar).isBuilt == false)
            fireRange = initialFireRange;

        float shortestDistance = fireRange;
        Transform nearest = null;

        foreach (var enemy in EnemySpawner.Instance.instantiatedEnemies.ToArray())
        {
            if (enemy == null) continue;
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < shortestDistance)
            {
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
    }
}
