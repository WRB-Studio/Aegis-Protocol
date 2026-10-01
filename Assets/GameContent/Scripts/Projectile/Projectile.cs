using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 1;
    [HideInInspector] public bool isDeflected = false;
    private bool hitProcessed;


    private void Awake()
    {
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
        transform.position += transform.up * speed * Time.deltaTime;

        if (Utils.IsOutOfView(transform.position))
            ProjectileManager.Instance.RemoveProjectile(this, 0.02f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hitProcessed) return;

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


}
