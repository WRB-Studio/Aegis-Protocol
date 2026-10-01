using UnityEngine;

public class FireRangeIndicator : MonoBehaviour
{
    Mesh mesh;
    Material material;
    MeshRenderer ringRenderer;

    void Awake()
    {
        var sprite = GetComponent<SpriteRenderer>();
        if (sprite) sprite.enabled = false;

        const int dashCount = 64;
        const int stepsPerDash = 3;
        var vertices = new Vector3[dashCount * (stepsPerDash + 1) * 2];
        var triangles = new int[dashCount * stepsPerDash * 6];
        var colors = new Color32[vertices.Length];
        var color = new Color32(255, 40, 30, 25);
        int triangle = 0;
        for (int dash = 0; dash < dashCount; dash++)
        {
            int start = dash * (stepsPerDash + 1) * 2;
            for (int step = 0; step <= stepsPerDash; step++)
            {
                float angle = (dash + step / (float)stepsPerDash * 0.65f) * Mathf.PI * 2f / dashCount;
                var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                int index = start + step * 2;
                vertices[index] = direction * 0.996f;
                vertices[index + 1] = direction * 1.004f;
                colors[index] = colors[index + 1] = color;
                if (step == stepsPerDash) continue;
                triangles[triangle++] = index;
                triangles[triangle++] = index + 1;
                triangles[triangle++] = index + 2;
                triangles[triangle++] = index + 1;
                triangles[triangle++] = index + 3;
                triangles[triangle++] = index + 2;
            }
        }

        mesh = new Mesh { name = "Dashed Fire Range" };
        mesh.vertices = vertices;
        mesh.colors32 = colors;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        var ringObject = new GameObject("Dashed Range");
        ringObject.transform.SetParent(transform, false);
        ringObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        material = new Material(Shader.Find("Sprites/Default"));
        ringRenderer = ringObject.AddComponent<MeshRenderer>();
        ringRenderer.sharedMaterial = material;
        ringRenderer.sortingOrder = 1;
    }

    void LateUpdate()
    {
        var tower = Tower.Instance;
        ringRenderer.enabled = tower && !GameManager.gameOver;
        if (!ringRenderer.enabled) return;

        float radius = tower.EffectiveFireRange;
        transform.position = tower.transform.position;
        transform.localScale = Vector3.one * Mathf.Max(0f, radius);
    }

    void OnDestroy()
    {
        if (mesh) Destroy(mesh);
        if (material) Destroy(material);
    }
}
