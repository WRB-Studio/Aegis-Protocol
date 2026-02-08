using UnityEngine;

public class CollectEffect : MonoBehaviour
{
    public Transform target;
    public float duration = 1f;
    public float arcHeight = 1f;
    public float sideOffsetAmount = 1f;

    public int material = 0;

    private Vector3 startPos;
    private float elapsed = 0f;
    private Vector3 sideOffsetDir;

    private Vector3 originalScale;
    public float nonAutoCollectScale = 1f;
    public float lífeTime = 6f;

    [HideInInspector] public bool flyToStation = false;

    [HideInInspector] public bool collectedManually = false;


    private void Awake()
    {
        Stats.Instance.resourcesSpawned++;
    }

    public void init(Transform target, int material, bool autoCollecting)
    {
        this.target = target;
        this.material = material;
        flyToStation = autoCollecting;

        startPos = transform.position;

        // Richtung zum Ziel
        Vector2 toTarget = (target.position - startPos).normalized;

        // 90° gedrehter Vektor (perpendicular)
        Vector2 perpendicular = new Vector2(-toTarget.y, toTarget.x);

        // Zufällige seitliche Richtung
        sideOffsetDir = perpendicular * Random.Range(-sideOffsetAmount, sideOffsetAmount);

        // Optional: Bogenhöhe variieren
        arcHeight = Random.Range(-arcHeight, arcHeight);

        //Random Color for collect effect
        Color color = new Color(Random.Range(0.2f, 1f), Random.Range(0.2f, 1f), Random.Range(0.2f, 1f), 1f);
        transform.GetChild(0).GetComponent<SpriteRenderer>().color = color;
        transform.GetChild(0).GetComponent<TrailRenderer>().startColor = color;
        transform.GetChild(0).GetComponent<TrailRenderer>().endColor = color;

        if (flyToStation)
            originalScale = transform.localScale;
        else
            transform.localScale *= nonAutoCollectScale;
    }

    public void setOriginScale()
    {
        transform.localScale = originalScale;
    }

    public void UpdateNormal()
    {
        if (!ResourceManager.Instance.autoCollecting)
        {
            lífeTime -= Time.deltaTime;
            if (lífeTime <= 0f || GameManager.gameOver)
                ResourceManager.RemoveCollectEffect(this);
        }

        if (!flyToStation)
            return;

        if (target == null)
        {
            ResourceManager.RemoveCollectEffect(this);
            return;
        }

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);

        Vector3 flatTarget = new Vector3(target.position.x, target.position.y, startPos.z);
        Vector3 currentPos = Vector3.Lerp(startPos, flatTarget, t);

        // Vertikaler Bogen
        float arc = Mathf.Sin(t * Mathf.PI) * arcHeight;
        currentPos.y += arc;

        // Seitliche Abweichung (ebenfalls bogenförmig)
        currentPos += sideOffsetDir * Mathf.Sin(t * Mathf.PI);

        transform.position = currentPos;

        if (t >= 1f)
        {
            if (collectedManually)
                ResourceManager.Instance.AddMaterial(material, Stats.eCollectBy.manual);
            else
                ResourceManager.Instance.AddMaterial(material, Stats.eCollectBy.automatic);

            ResourceManager.RemoveCollectEffect(this);
        }
    }

}
