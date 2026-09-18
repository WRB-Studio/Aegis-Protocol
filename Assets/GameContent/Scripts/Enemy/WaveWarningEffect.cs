using UnityEngine;

// Visual cue shown only while the next wave is waiting to start.
public sealed class WaveWarningEffect
{
    const int RingPoints = 48;
    const int SweepPoints = 12;

    readonly SpriteRenderer[] gridTiles;
    readonly Color[] gridColors;
    readonly Material[] gridMaterials;
    readonly MaterialPropertyBlock[] gridProperties;
    readonly Transform ringRoot;
    readonly LineRenderer ring;
    readonly LineRenderer sweep;
    readonly Material lineMaterial;
    readonly Material gridMaterial;
    bool gridMaterialApplied;

    static readonly int DistortionId = Shader.PropertyToID("_Distortion");
    static readonly int PhaseId = Shader.PropertyToID("_Phase");
    static readonly int TileSeedId = Shader.PropertyToID("_TileSeed");

    public WaveWarningEffect(Transform parent, Transform backgroundGrid, Shader distortionShader)
    {
        gridTiles = backgroundGrid ? backgroundGrid.GetComponentsInChildren<SpriteRenderer>() :
            System.Array.Empty<SpriteRenderer>();
        gridColors = new Color[gridTiles.Length];
        gridMaterials = new Material[gridTiles.Length];
        gridProperties = new MaterialPropertyBlock[gridTiles.Length];
        for (int i = 0; i < gridTiles.Length; i++)
        {
            gridColors[i] = gridTiles[i].color;
            gridMaterials[i] = gridTiles[i].sharedMaterial;
            gridProperties[i] = new MaterialPropertyBlock();
        }
        if (distortionShader) gridMaterial = new Material(distortionShader);

        var root = new GameObject("Wave Warning Ring");
        ringRoot = root.transform;
        ringRoot.SetParent(parent, false);

        lineMaterial = new Material(Shader.Find("Sprites/Default"));
        ring = CreateLine("Radar Ring", RingPoints, true);
        sweep = CreateLine("Radar Sweep", SweepPoints, false);
        root.SetActive(false);
    }

    LineRenderer CreateLine(string name, int points, bool loop)
    {
        var line = new GameObject(name).AddComponent<LineRenderer>();
        line.transform.SetParent(ringRoot, false);
        line.useWorldSpace = false;
        line.sharedMaterial = lineMaterial;
        line.sortingOrder = 8;
        line.loop = loop;
        line.positionCount = points;
        line.numCapVertices = 2;
        return line;
    }

    public void Show(float progress, float visibility = 1f)
    {
        float radius = Mathf.Lerp(2.15f, 1.3f, progress);
        Render(progress, visibility, visibility, radius);
    }

    public void ShowExit(float elapsed, float initialVisibility)
    {
        const float outwardTime = 0.7f;
        const float fadeTime = 2f;

        float blast = Mathf.Clamp01(elapsed / outwardTime);
        float radius = Mathf.Lerp(1.3f, 3.7f, 1f - Mathf.Pow(1f - blast, 3f));
        float ringVisibility = initialVisibility * (1f - Mathf.SmoothStep(0f, 1f, blast));
        float gridVisibility = initialVisibility *
            (1f - Mathf.SmoothStep(0f, 1f, elapsed / fadeTime));

        Render(1f + 0.25f * elapsed, gridVisibility, ringVisibility, radius);
    }

    void Render(float motion, float gridVisibility, float ringVisibility, float radius)
    {
        if (!ringRoot) return;

        var core = StationModule.GetModuleByType(StationModule.eModuleType.Core);
        if (core) ringRoot.position = core.transform.position;
        ringRoot.gameObject.SetActive(true);

        float intensity = Mathf.Clamp01(motion);
        float beat = 0.5f + 0.5f * Mathf.Cos(motion * Mathf.PI * 6f);

        var color = new Color(1f, 0.12f, 0.1f,
            (0.14f + 0.6f * intensity) * (0.7f + 0.3f * beat) * Mathf.Clamp01(ringVisibility));
        ring.startColor = ring.endColor = color;
        ring.widthMultiplier = 0.025f + 0.02f * intensity * beat;
        sweep.startColor = sweep.endColor = new Color(1f, 0.3f, 0.28f, color.a);
        sweep.widthMultiplier = 0.04f + 0.02f * intensity * beat;

        for (int i = 0; i < RingPoints; i++)
        {
            float angle = i * Mathf.PI * 2f / RingPoints;
            ring.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
        }

        for (int i = 0; i < SweepPoints; i++)
        {
            float angle = motion * Mathf.PI * 2f + i * Mathf.PI * 0.4f / (SweepPoints - 1);
            sweep.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
        }

        if (gridMaterial && !gridMaterialApplied)
        {
            foreach (var tile in gridTiles)
                if (tile) tile.sharedMaterial = gridMaterial;
            gridMaterialApplied = true;
        }

        for (int i = 0; i < gridTiles.Length; i++)
        {
            if (!gridTiles[i]) continue;
            Color tint = gridColors[i];
            tint.a = Mathf.Min(0.2f, tint.a + (0.015f + 0.055f * intensity) * gridVisibility);
            gridTiles[i].color = tint;
            if (!gridMaterial) continue;
            var properties = gridProperties[i];
            properties.SetFloat(DistortionId, Mathf.Lerp(0.5f, 4f, intensity) * gridVisibility);
            properties.SetFloat(PhaseId, motion);
            properties.SetFloat(TileSeedId, i * 1.37f);
            gridTiles[i].SetPropertyBlock(properties);
        }
    }

    public void Hide()
    {
        if (ringRoot) ringRoot.gameObject.SetActive(false);
        for (int i = 0; i < gridTiles.Length; i++)
        {
            if (!gridTiles[i]) continue;
            gridTiles[i].color = gridColors[i];
            if (!gridMaterialApplied) continue;
            gridTiles[i].SetPropertyBlock(null);
            gridTiles[i].sharedMaterial = gridMaterials[i];
        }
        gridMaterialApplied = false;
    }

    public void Dispose()
    {
        Hide();
        if (ringRoot) Object.Destroy(ringRoot.gameObject);
        if (lineMaterial) Object.Destroy(lineMaterial);
        if (gridMaterial) Object.Destroy(gridMaterial);
    }
}
