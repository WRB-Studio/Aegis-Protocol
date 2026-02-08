using System.Linq;
using UnityEngine;

public class Drone : MonoBehaviour
{
    public int maxHP;
    public int currentHP;

    public float moveSpeed;
    public float moveAroundDistance;

    public float fireRate;
    public float range;
    public float rotationSpeed;
    public int damage;

    public Transform firePoint;
    public GameObject projectilePrefab;

    private float fireCooldown = 0f;
    private Transform currentTarget;

    [Range(0f, 45f)] public float aimToleranceAngle = 5f;

    private Transform station;


    private void Start()
    {
        station = StationModule.GetModuleByType(StationModule.eModuleType.Core).transform;

        maxHP = DroneManager.Instance.droneInitialHP;
        currentHP = maxHP;

        damage = DroneManager.Instance.droneInitialDamage;
    }

    public void UpdateNormal()
    {
        if (currentTarget == null)
        {
            FindTarget();
        }

        if (currentTarget != null)
        {
            RotateTowardsTarget();

            if (fireCooldown <= 0f && IsGunAimedAtTarget())
            {
                Fire();
                fireCooldown = 1f / fireRate;
            }
        }
        else
        {
            float distanceToStation = Vector3.Distance(transform.position, station.position);
            float delta = Mathf.Abs(distanceToStation - moveAroundDistance);

            if (delta > 0.1f)
            {
                moveToStationRadius();
            }
            else
            {
                MoveAroundStation();
            }

        }

        fireCooldown -= Time.deltaTime;
    }
    
    private void moveToStationRadius()
    {
        Vector3 dir = (transform.position - station.position).normalized;
        Vector3 targetPos = station.position + dir * moveAroundDistance;

        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
    }

    private void MoveAroundStation()
    {
        if (station == null) return;

        float adjustedSpeed = moveSpeed;

        // Check: ist eine andere Drohne im Rücken?
        foreach (var otherDrone in DroneManager.Instance.allDrones)
        {
            if (otherDrone == null || otherDrone == this) continue;

            Vector3 toOther = otherDrone.transform.position - transform.position;
            float distance = toOther.magnitude;

            // Prüfe, ob andere Drohne im Rücken und nahe
            if (distance < 1.5f && Vector3.Dot(toOther.normalized, -transform.up) > 0.7f)
            {
                adjustedSpeed *= 1.5f; // Boost falls jemand im Nacken sitzt
                break;
            }
        }

        // Kreisbahn
        Vector3 direction = (transform.position - station.position).normalized;
        Vector3 tangent = Vector3.Cross(Vector3.forward, direction);
        Vector3 targetPosition = transform.position + tangent * adjustedSpeed * Time.deltaTime;

        transform.position = Vector3.Lerp(transform.position, targetPosition, 0.5f);

        Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, tangent);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    void RotateTowardsTarget()
    {
        Vector3 direction = currentTarget.position - transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime * 100);
    }

    void FindTarget()
    {
        float shortest = range;
        Transform nearest = null;

        var list = EnemySpawner.Instance.instantiatedEnemies;
        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];
            if (!e) continue;

            float dist = Vector3.Distance(transform.position, e.transform.position);
            if (dist < shortest) { shortest = dist; nearest = e.transform; }
        }

        currentTarget = nearest;
    }


    bool IsGunAimedAtTarget()
    {
        Vector3 toTarget = (currentTarget.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.up, toTarget);
        return angle < aimToleranceAngle && Vector2.Distance(transform.position, currentTarget.position) < range;
    }

    void Fire()
    {
        Projectile newProjectile = ProjectileManager.Instance.spawnProjectile(projectilePrefab, firePoint.position);
        newProjectile.transform.rotation = transform.rotation;
        newProjectile.GetComponent<Projectile>().damage = damage;
    }

    public void TakeDamage(int damage, Stats.eDeadBy deadBy)
    {
        currentHP -= damage;

        if (currentHP <= 0)
        {
            Die(deadBy);
        }
    }

    public void Die(Stats.eDeadBy deadBy)
    {
        ExplosionManager.Instance.CreateDroneExplosion(transform.position);

        Stats.Instance.dronesDestroyed++;
        DroneManager.Instance.RemoveDrone(this, deadBy);
    }


    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, range);
    }

}
