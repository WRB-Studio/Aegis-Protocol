using System.Buffers.Text;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public enum eEnemyType
    {
        None,
        Normal,
        Fast,
        Tank,
        Swarm,
        Ranged,
        Boss
    }

    public eEnemyType enemyType = eEnemyType.None;

    public int maxHP = 3;
    private int currentHP;

    public float speed = 2f;
    public int damage = 1;

    public float fireRange;
    public float fireRate;
    private float fireCooldown;

    private Vector3 target;
    private bool gameOverTargetSet = false;

    public GameObject projectilePrefab;
    public Transform firePoint;



    void Start()
    {
        currentHP = maxHP;

        Transform station = StationModule.GetModuleByType(StationModule.eModuleType.Core).transform;

        Vector2 randomOffset = Random.insideUnitCircle * 0.25f;
        target = (Vector2)station.transform.position + randomOffset;
    }

    public void InitWithLevel(int level)
    {
        float factor = 1.005f + level / 10f;

        maxHP = Mathf.RoundToInt(maxHP * factor);
        currentHP = maxHP;

        speed = enemyType == eEnemyType.Fast
            ? Mathf.Min(speed * factor, 5f)
            : Mathf.Clamp(speed * factor / 3f, speed, 3f);

        damage = Mathf.RoundToInt(damage * factor);
        fireRate = Mathf.Min(fireRate * factor, 6f);
    }


    public void UpdateNormal()
    {
        movementHandling();

        fireHandling();

        if (gameOverTargetSet && Vector2.Distance(transform.position, target) < 0.1f)
            EnemySpawner.RemoveEnemy(this, Stats.eDeadBy.None);
    }

    private void movementHandling()
    {
        if (enemyType == eEnemyType.Ranged && TargetInFireRange())
            return;

        Vector3 direction;

        //move direction if game is over (no station as target exists)
        if (GameManager.gameOver && !gameOverTargetSet)
        {
            Vector2 baseDirection = transform.up;

            float angleOffset = Random.Range(-15f, 15f);
            direction = Quaternion.Euler(0, 0, angleOffset) * baseDirection;
            target = transform.position + direction.normalized * 10f;

            gameOverTargetSet = true;
        }

        //normal moving to target
        direction = (target - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        //rotate to target
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
    }

    private void fireHandling()
    {
        if (enemyType != eEnemyType.Ranged)
            return;

        if (fireCooldown <= 0f && TargetInFireRange())
        {
            Fire();
            fireCooldown = 1f / fireRate;
        }

        fireCooldown -= Time.deltaTime;
    }

    private bool TargetInFireRange()
    {
        return Vector2.Distance(transform.position, target) <= fireRange && !Utils.IsOutOfView(transform.position);
    }

    public void Fire()
    {
        Projectile newProjectile = ProjectileManager.Instance.spawnProjectile(projectilePrefab, firePoint.position);
        newProjectile.transform.rotation = transform.rotation;
        newProjectile.GetComponent<Projectile>().damage = damage;
    }

    public void TakeDamage(int amount, Stats.eDeadBy deadBy)
    {
        currentHP -= amount;

        if (currentHP <= 0)
        {
            Die(deadBy);
        }
    }

    void Die(Stats.eDeadBy deadBy)
    {
        ExplosionManager.Instance.CreateShipExplosion(transform.position);

        ResourceManager.Instance.SpawnMaterial(1, transform.position);

        EnemySpawner.RemoveEnemy(this, deadBy);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Station"))
        {
            other.GetComponent<StationModule>().TakeDamage(damage);
            ExplosionManager.Instance.CreateShipExplosion(other.ClosestPoint(transform.position));
            EnemySpawner.RemoveEnemy(this, Stats.eDeadBy.stationCollision);
        }
        else if (other.CompareTag("Drone"))
        {
            other.GetComponent<Drone>().TakeDamage(1, Stats.eDeadBy.enemyCollision);
            TakeDamage(1, Stats.eDeadBy.droneCollision);
        }
    }

}
