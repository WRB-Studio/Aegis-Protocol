using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 1;
    [HideInInspector] public bool isDeflected = false;


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
    }

    public void UpdateNormal()
    {
        transform.position += transform.up * speed * Time.deltaTime;

        if (Utils.IsOutOfView(transform.position))
            ProjectileManager.Instance.RemoveProjectile(this, 0.02f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if ((CompareTag("TowerProjectile") || CompareTag("DroneProjectile")) && other.CompareTag("Enemy"))
        {
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
                Stats.Instance.enemyProjectilesHit++;

                Shield shield = other.GetComponent<Shield>();
                float deflectionChance = shield.deflectionChance;

                if (Random.Range(0f, 100f) < deflectionChance)
                {
                    SoundManager.Instance.PlayDeflectionHitSound();
                    transform.tag = "TowerProjectile";
                    Stats.Instance.deflectedProjectilesFired++;
                    transform.rotation *= Quaternion.Euler(0f, 0f, 180f);
                    shield.TakeDamage(damage / 2);
                    return;
                }

                SoundManager.Instance.PlayStationHitSound();
                shield.TakeDamage(damage);
                ProjectileManager.Instance.RemoveProjectile(this);
                return;
            }

            if (other.CompareTag("Station"))
            {
                if (Shield.Instance.shieldIsActive)
                    return;

                Stats.Instance.enemyProjectilesHit++;

                other.GetComponent<StationModule>().TakeDamage(damage);
                ProjectileManager.Instance.RemoveProjectile(this);
                return;
            }

            if (other.CompareTag("Drone"))
            {
                Stats.Instance.enemyProjectilesHit++;

                other.GetComponent<Drone>().TakeDamage(damage, Stats.eDeadBy.enemyProjectile);
                ProjectileManager.Instance.RemoveProjectile(this);
                return;
            }
        }
    }


}
