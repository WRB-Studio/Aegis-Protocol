using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 1;
    [HideInInspector] public bool isDeflected = false;
    private bool hitProcessed;
    Collider2D projectileCollider;
    ContactFilter2D collisionFilter;
    readonly List<RaycastHit2D> pathHits = new();


    private void Awake()
    {
        projectileCollider = GetComponent<Collider2D>();
        collisionFilter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        collisionFilter.useTriggers = true;
        switch (tag)
        {
            case "TowerProjectile":
                Stats.Instance.towerProjectilesFired++;
                break;
            case "DroneProjectile":
                Stats.Instance.droneProjectilesFired++;
                break;
            case "EnemyProjectile":
                Stats.Instance.enemyProjectilesFired++;
                break;
        }

        SoundManager.Instance.PlayShootSound();
        MatchReporter.Event("projectile_fired", tag, GetInstanceID().ToString(), position: transform.position);
    }

    public void UpdateNormal()
    {
        if (hitProcessed) return;
        float distance = speed * Time.deltaTime;
        if (distance <= 0f) return;

        Vector3 direction = transform.up;
        Vector3 start = transform.position;
        if (projectileCollider)
        {
            pathHits.Clear();
            projectileCollider.Cast(direction, collisionFilter, pathHits, distance);
            RaycastHit2D nearest = default;
            float nearestDistance = float.PositiveInfinity;
            foreach (var hit in pathHits)
            {
                if (hit.distance >= nearestDistance || !CanHit(hit.collider)) continue;
                nearest = hit;
                nearestDistance = hit.distance;
            }
            if (nearest.collider)
            {
                transform.position = start + direction * nearestDistance;
                OnTriggerEnter2D(nearest.collider);
                return;
            }
        }

        transform.position = start + direction * distance;

        if (Utils.IsOutOfView(transform.position))
            ProjectileManager.Instance.RemoveProjectile(this, 0.02f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hitProcessed || !CanHit(other)) return;

        if ((CompareTag("TowerProjectile") || CompareTag("DroneProjectile")) && other.CompareTag("Enemy"))
        {
            hitProcessed = true;
            MatchReporter.Event("projectile_hit", tag + ":" + GetInstanceID(), other.GetComponent<Enemy>().enemyType + ":" + other.GetComponent<Enemy>().GetInstanceID(),
                damage, detail: isDeflected ? "deflected" : "", position: transform.position);
            if (CompareTag("TowerProjectile"))
            {
                if (isDeflected)
                {
                    other.GetComponent<Enemy>().TakeDamage(damage, Stats.eDeadBy.deflectedProjectile);
                    Stats.Instance.deflectedProjectilesHit++;
                }
                else
                {
                    other.GetComponent<Enemy>().TakeDamage(damage, Stats.eDeadBy.towerProjectile);
                    Stats.Instance.towerProjectilesHit++;
                }
            }
            else if (CompareTag("DroneProjectile"))
            {
                other.GetComponent<Enemy>().TakeDamage(damage, Stats.eDeadBy.droneProjectile);
                Stats.Instance.droneProjectilesHit++;
            }

            ProjectileManager.Instance.RemoveProjectile(this);

            return;
        }

        if (CompareTag("EnemyProjectile"))
        {
            if (other.CompareTag("Shield"))
            {
                MatchReporter.Event("projectile_hit", tag + ":" + GetInstanceID(), "Shield", damage, position: transform.position);
                Stats.Instance.enemyProjectilesHit++;

                Shield shield = other.GetComponent<Shield>();
                float deflectionChance = shield.deflectionChance;

                if (Random.Range(0f, 100f) < deflectionChance)
                {
                    SoundManager.Instance.PlayDeflectionHitSound();
                    transform.tag = "TowerProjectile";
                    isDeflected = true;
                    Stats.Instance.deflectedProjectilesFired++;
                    MatchReporter.Event("projectile_deflected", "Shield", GetInstanceID().ToString(), position: transform.position);
                    transform.rotation *= Quaternion.Euler(0f, 0f, 180f);
                    shield.TakeDamage(damage / 2, "deflectedProjectile:" + GetInstanceID());
                    return;
                }

                SoundManager.Instance.PlayStationHitSound();
                hitProcessed = true;
                shield.TakeDamage(damage, "enemyProjectile:" + GetInstanceID());
                ProjectileManager.Instance.RemoveProjectile(this);
                return;
            }

            if (other.CompareTag("Station"))
            {
                if (Shield.Instance.shieldIsActive)
                    return;

                Stats.Instance.enemyProjectilesHit++;

                hitProcessed = true;
                other.GetComponent<StationModule>().TakeDamage(damage, "enemyProjectile:" + GetInstanceID());
                MatchReporter.Event("projectile_hit", tag + ":" + GetInstanceID(), other.GetComponent<StationModule>().moduleType.ToString(), damage, position: transform.position);
                ProjectileManager.Instance.RemoveProjectile(this);
                return;
            }

            if (other.CompareTag("Drone"))
            {
                Stats.Instance.enemyProjectilesHit++;

                hitProcessed = true;
                other.GetComponent<Drone>().TakeDamage(damage, Stats.eDeadBy.enemyProjectile);
                MatchReporter.Event("projectile_hit", tag + ":" + GetInstanceID(), "Drone:" + other.GetComponent<Drone>().GetInstanceID(), damage, position: transform.position);
                ProjectileManager.Instance.RemoveProjectile(this);
                return;
            }
        }
    }

    bool CanHit(Collider2D other)
    {
        if (!other || !other.enabled || !other.gameObject.activeInHierarchy) return false;
        if (CompareTag("TowerProjectile") || CompareTag("DroneProjectile"))
            return other.CompareTag("Enemy") && other.GetComponent<Enemy>() &&
                EnemySpawner.Instance.instantiatedEnemies.Contains(other.GetComponent<Enemy>());

        if (!CompareTag("EnemyProjectile")) return false;
        if (other.CompareTag("Shield"))
            return other.GetComponent<Shield>() && Shield.Instance.shieldIsActive;
        if (other.CompareTag("Station"))
            return !Shield.Instance.shieldIsActive && other.GetComponent<StationModule>() &&
                other.GetComponent<StationModule>().isBuilt;
        return other.CompareTag("Drone") && other.GetComponent<Drone>() &&
            DroneManager.Instance.allDrones.Contains(other.GetComponent<Drone>());
    }


}
