using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Enemy))]
public sealed class BossCarrier : MonoBehaviour
{
    public enum Phase { Approach, Launching, Attack }

    [SerializeField, Min(1)] int firstSwarmGroups = 3;
    [SerializeField, Min(1)] int swarmGroupSize = 3;
    [SerializeField, Min(0.1f)] float launchInterval = 3f;
    [SerializeField, Range(0f, 2f)] float shieldHealthRatio = 0.6f;
    [SerializeField] Sprite shieldSprite;
    [SerializeField, Min(1f)] float rotationSpeed = 30f;
    [SerializeField, Min(0.1f)] float movementSmoothTime = 1f;
    [SerializeField, Min(0.1f)] float rotationSmoothTime = 0.9f;
    [SerializeField, Min(0.1f)] float turnBeforeArrivalDistance = 0.9f;

    public Phase CurrentPhase { get; private set; }
    public int ShieldPoints { get; private set; }
    public float HoldDistance { get; private set; }

    Enemy enemy;
    int waveNumber, groupsRemaining, maxShieldPoints, currentSwarmGroupSize;
    float side, launchCountdown, fireCountdown, hitFlash, desiredHoldDistance;
    Vector3 movementVelocity;
    float angularVelocity;
    bool turningBroadside;
    int groupsLaunched;
    bool initialized, firstBoss, launchingSwarm;
    readonly List<Enemy> swarms = new();
    SpriteRenderer ship;
    SpriteRenderer shieldVisual, shieldHitVisual;
    LineRenderer healthBar, shieldBar;
    Material visualMaterial;
    Mesh haloMesh;
    GameObject halo;

    public void Initialize(int wave)
    {
        enemy = GetComponent<Enemy>();
        ship = GetComponent<SpriteRenderer>();
        waveNumber = wave;
        firstBoss = wave == 5;
        int encounter = Mathf.Max(1, wave / 5);
        float fraction = encounter == 1 ? 0.05f : encounter == 2 ? 0.2f :
            encounter == 3 ? 0.5f : encounter == 4 ? 0.9f : 1f;
        var tower = Tower.Instance;
        var range = UpgradeAttribute.GetUpgradeByName(UpgradeAttribute.eUpgradeName.FireRange);
        float maximum = range != null ? range.CalculateValue(range.maxLevel) : tower.initialFireRange;
        desiredHoldDistance = Mathf.Lerp(tower.initialFireRange, Mathf.Max(tower.initialFireRange, maximum), fraction);
        side = transform.position.y >= tower.transform.position.y ? 1f : -1f;
        LimitHoldDistanceToView();
        groupsRemaining = firstSwarmGroups + Mathf.Min(encounter - 1, 4);
        currentSwarmGroupSize = swarmGroupSize + (encounter - 1) * 3;
        launchCountdown = 1f;
        maxShieldPoints = firstBoss ? 0 : Mathf.Max(1, Mathf.RoundToInt(enemy.maxHP * shieldHealthRatio));
        ShieldPoints = maxShieldPoints;
        CurrentPhase = Phase.Approach;
        initialized = true;
        CreateVisuals();
        RefreshVisuals();
    }

    public void Tick()
    {
        if (!initialized || !Tower.Instance || GameManager.gameOver) return;
        CheckHealthTransition();
        LimitHoldDistanceToView();
        Vector3 center = Tower.Instance.transform.position;
        float distance = CurrentPhase == Phase.Attack
            ? Mathf.Min(enemy.fireRange, Tower.Instance.EffectiveFireRange * 0.96f) : HoldDistance;
        Vector3 destination = center + Vector3.up * (side * distance);
        float remainingDistance = Vector3.Distance(transform.position, destination);
        if (CurrentPhase != Phase.Attack && remainingDistance <= turnBeforeArrivalDistance) turningBroadside = true;
        float facingCore = side > 0f ? 180f : 0f;
        float desiredAngle = CurrentPhase == Phase.Attack || !turningBroadside
            ? facingCore : side > 0f ? 90f : -90f;
        float angle = Mathf.SmoothDampAngle(transform.eulerAngles.z, desiredAngle, ref angularVelocity,
            rotationSmoothTime, rotationSpeed, Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        float turnProgress = 1f - Mathf.Clamp01(Mathf.Abs(Mathf.DeltaAngle(angle, facingCore)) / 90f);
        float movementFactor = CurrentPhase == Phase.Attack ? Mathf.Lerp(0.15f, 1f, Mathf.SmoothStep(0f, 1f, turnProgress)) : 1f;
        transform.position = Vector3.SmoothDamp(transform.position, destination, ref movementVelocity,
            movementSmoothTime, enemy.speed * movementFactor, Time.deltaTime);
        bool arrived = Vector3.Distance(transform.position, destination) < 0.01f;
        Quaternion rotation = Quaternion.Euler(0f, 0f, desiredAngle);

        if (CurrentPhase == Phase.Approach && arrived) CurrentPhase = Phase.Launching;
        if (CurrentPhase == Phase.Launching)
        {
            launchCountdown -= Time.deltaTime;
            if (groupsRemaining > 0 && !launchingSwarm && launchCountdown <= 0f)
            {
                groupsRemaining--;
                StartCoroutine(LaunchSwarm());
                launchCountdown = launchInterval;
            }
            swarms.RemoveAll(swarm => !swarm || !EnemySpawner.Instance.instantiatedEnemies.Contains(swarm));
            if (groupsRemaining == 0 && !launchingSwarm && swarms.Count == 0) BeginAttack();
        }
        else if (CurrentPhase == Phase.Attack)
        {
            fireCountdown -= Time.deltaTime;
            if (arrived && Quaternion.Angle(transform.rotation, rotation) < 3f && fireCountdown <= 0f &&
                !Utils.IsOutOfView(transform.position))
            {
                enemy.Fire();
                fireCountdown = 1f / Mathf.Max(0.1f, enemy.fireRate);
            }
        }
        hitFlash = Mathf.Max(0f, hitFlash - Time.deltaTime);
        RefreshVisuals();
    }

    void LimitHoldDistanceToView()
    {
        HoldDistance = desiredHoldDistance;
        var camera = Camera.main;
        if (!camera || !camera.orthographic) return;
        Vector2 size = Vector2.Scale(ship.sprite.bounds.extents, transform.lossyScale);
        // Allow room for the whole hull during rotation, its bars and the outward launch arc.
        float margin = size.magnitude + 0.65f;
        float edge = camera.transform.position.y + side * camera.orthographicSize;
        float visibleDistance = (edge - Tower.Instance.transform.position.y) * side - margin;
        HoldDistance = Mathf.Min(HoldDistance, Mathf.Max(0f, visibleDistance));
    }

    IEnumerator LaunchSwarm()
    {
        launchingSwarm = true;
        int launched = 0;
        int group = groupsLaunched++;
        for (int i = 0; i < currentSwarmGroupSize; i++)
        {
            if (i > 0) yield return new WaitForSeconds(Random.Range(0.08f, 0.2f));
            if (CurrentPhase != Phase.Launching || GameManager.gameOver) break;
            Bounds hull = ship.bounds;
            Vector2 offset = Random.insideUnitCircle;
            Vector2 position = (Vector2)hull.center + new Vector2(offset.x * 0.22f, offset.y * 0.16f);
            var swarm = EnemySpawner.Instance.SpawnCarrierSwarm(position, waveNumber);
            if (swarm)
            {
                swarm.BeginCarrierLaunch(transform, hull, side, i, group);
                swarms.Add(swarm);
                launched++;
            }
        }
        launchingSwarm = false;
        MatchReporter.Event("boss_swarm_launched", enemy.GetInstanceID().ToString(), value: launched,
            remaining: groupsRemaining, position: transform.position);
    }

    public int AbsorbDamage(int amount)
    {
        if (!initialized || ShieldPoints <= 0) return amount;
        int absorbed = Mathf.Min(amount, ShieldPoints);
        ShieldPoints -= absorbed;
        hitFlash = 1f / 6f;
        MatchReporter.Event("boss_shield_damage", enemy.GetInstanceID().ToString(), value: absorbed,
            remaining: ShieldPoints, position: transform.position);
        if (ShieldPoints == 0) BeginAttack();
        RefreshVisuals();
        return amount - absorbed;
    }

    public void CheckHealthTransition()
    {
        if (initialized && firstBoss && enemy.CurrentHP <= enemy.maxHP * 0.5f) BeginAttack();
    }

    void BeginAttack()
    {
        if (CurrentPhase == Phase.Attack) return;
        CurrentPhase = Phase.Attack;
        groupsRemaining = 0;
        fireCountdown = 1f;
        MatchReporter.Event("boss_attack_phase", enemy.GetInstanceID().ToString(),
            remaining: ShieldPoints, position: transform.position);
    }

    void CreateVisuals()
    {
        visualMaterial = new Material(Shader.Find("Sprites/Default"));
        CreateHalo();
        shieldVisual = CreateShieldSprite("Carrier Shield", ship.sortingOrder - 1);
        shieldHitVisual = CreateShieldSprite("Carrier Shield Hit", ship.sortingOrder + 1);
        healthBar = CreateLine("Carrier HP", 0.07f, new Color(1f, 0.35f, 0.25f));
        shieldBar = CreateLine("Carrier Shield Points", 0.06f, new Color(1f, 0.3f, 0.2f));
    }

    SpriteRenderer CreateShieldSprite(string name, int sortingOrder)
    {
        var child = new GameObject(name, typeof(SpriteRenderer));
        child.transform.SetParent(transform, false);
        child.transform.localPosition = ship.sprite.bounds.center;
        float diameter = (ship.sprite.bounds.extents.magnitude + 0.1f) * 2f;
        child.transform.localScale = Vector3.one * (diameter / shieldSprite.bounds.size.x);
        var renderer = child.GetComponent<SpriteRenderer>();
        renderer.sprite = shieldSprite;
        renderer.sharedMaterial = visualMaterial;
        renderer.sortingLayerID = ship.sortingLayerID;
        renderer.sortingOrder = sortingOrder;
        renderer.color = new Color(1f, 0.2f, 0.12f, 1f);
        return renderer;
    }

    LineRenderer CreateLine(string label, float width, Color color)
    {
        var child = new GameObject(label);
        child.transform.SetParent(transform, false);
        var line = child.AddComponent<LineRenderer>();
        line.sharedMaterial = visualMaterial;
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.startWidth = line.endWidth = width;
        line.startColor = line.endColor = color;
        line.sortingLayerID = ship.sortingLayerID;
        line.sortingOrder = ship.sortingOrder + 1;
        return line;
    }

    void CreateHalo()
    {
        const int segments = 48;
        float[] radii = { 0f, 0.65f, 1.05f, 1.4f, 1.85f };
        float[] opacity = { 0.04f, 0.12f, 0.28f, 0.08f, 0f };
        var vertices = new Vector3[segments * radii.Length];
        var colors = new Color[vertices.Length];
        var triangles = new int[(radii.Length - 1) * segments * 6];
        Vector2 size = ship.sprite.bounds.extents;
        int triangle = 0;
        for (int ring = 0; ring < radii.Length; ring++)
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            int index = ring * segments + i;
            vertices[index] = ship.sprite.bounds.center +
                new Vector3(Mathf.Cos(angle) * size.x, Mathf.Sin(angle) * size.y) * radii[ring];
            colors[index] = new Color(1f, 0.025f, 0.015f, opacity[ring]);
            if (ring == 0) continue;
            int next = ring * segments + (i + 1) % segments;
            triangles[triangle++] = index - segments;
            triangles[triangle++] = index;
            triangles[triangle++] = next;
            triangles[triangle++] = index - segments;
            triangles[triangle++] = next;
            triangles[triangle++] = next - segments;
        }
        haloMesh = new Mesh { name = "Carrier Red Halo" };
        haloMesh.vertices = vertices;
        haloMesh.colors = colors;
        haloMesh.triangles = triangles;
        haloMesh.RecalculateBounds();
        halo = new GameObject("Carrier Red Halo", typeof(MeshFilter), typeof(MeshRenderer));
        halo.transform.SetParent(transform, false);
        halo.GetComponent<MeshFilter>().sharedMesh = haloMesh;
        var renderer = halo.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = visualMaterial;
        renderer.sortingLayerID = ship.sortingLayerID;
        renderer.sortingOrder = ship.sortingOrder - 2;
    }

    void RefreshVisuals()
    {
        if (!shieldVisual) return;
        shieldVisual.enabled = ShieldPoints > 0;
        shieldHitVisual.enabled = ShieldPoints > 0 && hitFlash > 0f;
        float elapsed = 1f / 6f - hitFlash;
        float flashAlpha = elapsed < 1f / 15f
            ? Mathf.SmoothStep(0f, 1f, elapsed * 15f)
            : Mathf.SmoothStep(1f, 0f, (elapsed - 1f / 15f) * 10f);
        shieldHitVisual.color = new Color(1f, 0.35f, 0.25f, flashAlpha);
        float barY = side > 0f ? ship.bounds.max.y + 0.15f : ship.bounds.min.y - 0.15f;
        Vector3 barCenter = new Vector3(ship.bounds.center.x, barY, transform.position.z);
        UpdateBar(healthBar, barCenter, Mathf.Clamp01((float)enemy.CurrentHP / enemy.maxHP));
        shieldBar.enabled = ShieldPoints > 0;
        UpdateBar(shieldBar, barCenter + Vector3.up * (side * 0.12f),
            maxShieldPoints > 0 ? (float)ShieldPoints / maxShieldPoints : 0f);
    }

    static void UpdateBar(LineRenderer bar, Vector3 center, float fraction)
    {
        bar.SetPosition(0, center + Vector3.left * 0.5f);
        bar.SetPosition(1, center + Vector3.left * 0.5f + Vector3.right * fraction);
    }

    public void HideVisuals()
    {
        if (!shieldVisual) return;
        shieldVisual.enabled = shieldHitVisual.enabled = healthBar.enabled = shieldBar.enabled = false;
        if (halo) halo.SetActive(false);
    }

    void OnDestroy()
    {
        if (visualMaterial) Destroy(visualMaterial);
        if (haloMesh) Destroy(haloMesh);
    }
}
