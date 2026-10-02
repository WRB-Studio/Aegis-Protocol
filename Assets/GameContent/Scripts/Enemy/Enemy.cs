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
    public int CurrentHP => currentHP;
    BossCarrier carrier;
    CarrierSwarmFlight carrierFlight;
    bool initialized;
    private int materialReward = 1;

    public float speed = 2f;
    public int damage = 1;

    public float fireRange;
    public float fireRate;
    private float fireCooldown;

    private Vector3 target;
    private bool gameOverTargetSet = false;

    public GameObject projectilePrefab;
    public Transform firePoint;



    void Awake() => carrier = GetComponent<BossCarrier>();

    void Start()
    {
        if (!initialized) currentHP = maxHP;

        Transform station = StationModule.GetModuleByType(StationModule.eModuleType.Core).transform;

        Vector2 randomOffset = Random.insideUnitCircle * 0.25f;
        target = (Vector2)station.transform.position + randomOffset;
    }

    public void InitWithLevel(int level)
    {
        float factor = 1.005f + level / 10f;
        materialReward = Mathf.Max(1, level + 1);
        if (enemyType == eEnemyType.Boss) materialReward *= 3;

        maxHP = Mathf.RoundToInt(maxHP * factor);
        currentHP = maxHP;

        speed = enemyType == eEnemyType.Fast
            ? Mathf.Min(speed * factor, 5f)
            : Mathf.Clamp(speed * factor / 3f, speed, 3f);
        if (enemyType == eEnemyType.Fast && level < 3)
            speed *= 0.8f;
        if (enemyType == eEnemyType.Boss) speed = Mathf.Min(speed, 0.5f);

        damage = enemyType == eEnemyType.Ranged
            ? Mathf.Max(1, Mathf.RoundToInt(damage * factor * 0.5f))
            : Mathf.RoundToInt(damage * factor);
        fireRate = Mathf.Min(fireRate * factor, 6f);
        initialized = true;
        if (carrier) carrier.Initialize(level + 1);
    }


    public void UpdateNormal()
    {
        if (carrier)
        {
            if (!GameManager.gameOver)
            {
                carrier.Tick();
                return;
            }
            carrier.HideVisuals();
        }
        movementHandling();

        fireHandling();

        if (gameOverTargetSet && Vector2.Distance(transform.position, target) < 0.1f)
            EnemySpawner.RemoveEnemy(this, Stats.eDeadBy.None);
    }

    private void movementHandling()
    {
        if (carrierFlight && !GameManager.gameOver)
        {
            carrierFlight.Tick(target, speed);
            return;
        }
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
        var tower = Tower.Instance;
        if (!tower || GameManager.gameOver) return false;

        // Stay inside the turret's range, including after radar loss. The target has a random offset.
        return Vector2.Distance(transform.position, target) <= fireRange &&
            Vector3.Distance(transform.position, tower.transform.position) < tower.EffectiveFireRange * 0.9f &&
            !Utils.IsOutOfView(transform.position);
    }

    public void Fire()
    {
        Projectile newProjectile = ProjectileManager.Instance.spawnProjectile(projectilePrefab, firePoint.position);
        newProjectile.transform.rotation = transform.rotation;
        newProjectile.GetComponent<Projectile>().damage = damage;
        MatchReporter.Event("enemy_fired", enemyType + ":" + GetInstanceID(), newProjectile.GetInstanceID().ToString(),
            damage, position: firePoint.position);
    }

    public void BeginCarrierLaunch(Transform source, Bounds hull, float side, int index, int group)
    {
        carrierFlight = gameObject.AddComponent<CarrierSwarmFlight>();
        carrierFlight.Initialize(source, hull, side, index, group);
    }

    public void TakeDamage(int amount, Stats.eDeadBy deadBy)
    {
        if (carrier) amount = carrier.AbsorbDamage(Mathf.Max(0, amount));
        MatchReporter.Event("enemy_damage", deadBy.ToString(), enemyType + ":" + GetInstanceID(),
            Mathf.Min(Mathf.Max(0, amount), Mathf.Max(0, currentHP)), Mathf.Max(0, currentHP - amount),
            "requested=" + amount, transform.position);
        currentHP -= amount;
        if (carrier) carrier.CheckHealthTransition();

        if (currentHP <= 0)
        {
            Die(deadBy);
        }
    }

    void Die(Stats.eDeadBy deadBy)
    {
        ExplosionManager.Instance.CreateShipExplosion(transform.position);

        ResourceManager.Instance.SpawnMaterial(materialReward, transform.position);

        EnemySpawner.RemoveEnemy(this, deadBy);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Station"))
        {
            other.GetComponent<StationModule>().TakeDamage(damage, enemyType + ":" + GetInstanceID());
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
