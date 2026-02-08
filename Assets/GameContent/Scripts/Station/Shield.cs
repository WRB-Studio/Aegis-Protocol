using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Shield : MonoBehaviour, IResettable
{
    public static Shield Instance;

    [HideInInspector] public bool shieldIsActive;
    public float maxShieldPoints;
    public float currentShieldPoints;
    public float rechargeTime;
    [HideInInspector] public float rechargeCountdown;
    public float deflectionChance;

    public GameObject shieldObject;
    public Animator shieldEffectAnimator;
    public Slider sliderShieldPoints;
    public TextMeshProUGUI txtShieldRecharge;

    public float regenerationPercentPerTime = 2f;
    public float damageRegenDelay = 3;
    private float regenCountdown = 0;

    // --- INIT SNAPSHOT (runtime state) ---
    private bool initshieldIsActive;
    private float initmaxShieldPoints;
    private float initcurrentShieldPoints;
    private float initrechargeTime;
    private float initrechargeCountdown;
    private float initdeflectionChance;
    private float initregenerationPercentPerTime;
    private float initdamageRegenDelay;
    private float initregenCountdown;

    private void Awake()
    {
        Instance = this;
    }

    public void Init()
    {
        shieldIsActive = false;

        currentShieldPoints = maxShieldPoints;

        shieldObject.SetActive(false);
        GetComponent<Collider2D>().enabled = false;

        refreshShieldPointSlider();

        sliderShieldPoints.gameObject.SetActive(false);
        txtShieldRecharge.gameObject.SetActive(false);
    }

    public void StoreInit()
    {
        initshieldIsActive = shieldIsActive;
        initmaxShieldPoints = maxShieldPoints;
        initcurrentShieldPoints = currentShieldPoints;
        initrechargeTime = rechargeTime;
        initrechargeCountdown = rechargeCountdown;
        initdeflectionChance = deflectionChance;
        initregenerationPercentPerTime = regenerationPercentPerTime;
        initdamageRegenDelay = damageRegenDelay;
        initregenCountdown = regenCountdown;
    }

    public void ResetScript()
    {
        shieldIsActive = initshieldIsActive;
        maxShieldPoints = initmaxShieldPoints;
        currentShieldPoints = initcurrentShieldPoints;
        rechargeTime = initrechargeTime;
        rechargeCountdown = initrechargeCountdown;
        deflectionChance = initdeflectionChance;
        regenerationPercentPerTime = initregenerationPercentPerTime;
        damageRegenDelay = initdamageRegenDelay;
        regenCountdown = initregenCountdown;

        // visuals/state sync (keine init vars nötig)
        if (shieldIsActive) activateShield();
        else deactivateShield();

        refreshShieldPointSlider();

        txtShieldRecharge.gameObject.SetActive(rechargeCountdown > 0f);
        if (rechargeCountdown > 0f)
            txtShieldRecharge.text = Mathf.Ceil(rechargeCountdown).ToString();
    }

    public void activateShield()
    {
        shieldObject.SetActive(true);
        GetComponent<Collider2D>().enabled = true;
        sliderShieldPoints.gameObject.SetActive(true);
        refreshShieldPointSlider();
        txtShieldRecharge.gameObject.SetActive(false);
        shieldIsActive = true;
    }

    public void deactivateShield()
    {
        shieldObject.SetActive(false);
        GetComponent<Collider2D>().enabled = false;
        sliderShieldPoints.gameObject.SetActive(false);
        txtShieldRecharge.gameObject.SetActive(false);
        shieldIsActive = false;
    }

    public void refreshShieldPointSlider()
    {
        sliderShieldPoints.maxValue = maxShieldPoints;
        sliderShieldPoints.value = currentShieldPoints;
    }

    public void UpdateNormal()
    {
        RechargeHandling();

        RegenerationHandling();
    }

    private void RechargeHandling()
    {
        if (rechargeCountdown > 0)
        {
            rechargeCountdown -= Time.deltaTime;
            txtShieldRecharge.text = Mathf.Ceil(rechargeCountdown).ToString();
            if (rechargeCountdown <= 0f)
            {
                currentShieldPoints = maxShieldPoints;
                regenCountdown = 0;
                activateShield();
            }
        }
    }

    private void RegenerationHandling()
    {
        if (!shieldIsActive) return;

        if (regenCountdown > 0f)
        {
            regenCountdown -= Time.deltaTime;
            return;
        }

        if (currentShieldPoints < maxShieldPoints)
        {
            float regenPerSecond = maxShieldPoints * (regenerationPercentPerTime / 100f);
            currentShieldPoints += regenPerSecond * Time.deltaTime;
            currentShieldPoints = Mathf.Min(currentShieldPoints, maxShieldPoints);
            refreshShieldPointSlider();
        }
    }


    public void TakeDamage(int damage)
    {
        Stats.Instance.shieldDamageTaken += damage;
        currentShieldPoints -= damage;
        if (currentShieldPoints < 0) currentShieldPoints = 0;

        refreshShieldPointSlider();
        PlayDamageEffect();

        if (currentShieldPoints == 0)
        {
            shieldObject.SetActive(false);
            GetComponent<Collider2D>().enabled = false;
            rechargeCountdown = rechargeTime;
            txtShieldRecharge.gameObject.SetActive(true);
            shieldIsActive = false;
        }

        regenCountdown = damageRegenDelay;
    }

    public void PlayDamageEffect()
    {
        if (shieldEffectAnimator != null)
        {
            shieldEffectAnimator.Play("ShieldHit");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Enemy")
        {
            SoundManager.Instance.PlayStationHitSound();
            TakeDamage(1);
            EnemySpawner.RemoveEnemy(collision.gameObject.GetComponent<Enemy>(), Stats.eDeadBy.shieldCollision);
        }
    }

}
