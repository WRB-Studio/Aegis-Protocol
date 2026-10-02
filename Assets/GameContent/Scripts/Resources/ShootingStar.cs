using UnityEngine;

public sealed class ShootingStar : MonoBehaviour
{
    [SerializeField] SpriteRenderer head;
    [SerializeField] LineRenderer tail;
    [SerializeField, Min(1f)] float tapRadiusPixels = 44f;
    Vector3 start, end;
    float duration, elapsed;
    int material;
    bool collected;

    public void Init(Vector3 from, Vector3 to, float flightDuration, int reward)
    {
        start = from;
        end = to;
        duration = Mathf.Max(1f, flightDuration);
        material = Mathf.Max(1, reward);
        elapsed = 0f;
        collected = false;
        transform.position = from;
        Vector3 direction = to - from;
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        UpdateVisual(0f);
    }

    public bool Tick(float deltaTime)
    {
        elapsed += Mathf.Max(0f, deltaTime);
        float progress = Mathf.Clamp01(elapsed / duration);
        transform.position = Vector3.Lerp(start, end, progress);
        UpdateVisual(progress);
        return collected || progress >= 1f;
    }

    void UpdateVisual(float progress)
    {
        float alpha = Mathf.Min(Mathf.Clamp01(progress / 0.08f), Mathf.Clamp01((1f - progress) / 0.12f));
        head.color = new Color(0.8f, 1f, 1f, alpha);
        tail.startColor = new Color(0.4f, 0.9f, 1f, alpha * 0.7f);
        tail.endColor = new Color(0.2f, 0.65f, 0.8f, 0f);
    }

    public bool TryCollect(Vector2 screenPosition)
    {
        if (collected || elapsed <= 0f || elapsed >= duration || GameManager.gameOver ||
            Time.timeScale <= 0f || Utils.IsPointerOverUI() || !Camera.main) return false;
        Vector3 screen = Camera.main.WorldToScreenPoint(transform.position);
        if (screen.x < 0f || screen.x > Screen.width || screen.y < 0f || screen.y > Screen.height) return false;
        float radius = Mathf.Max(tapRadiusPixels, Screen.height * 0.025f);
        if (Vector2.Distance(screenPosition, screen) > radius) return false;

        collected = true;
        ResourceManager.Instance.SpawnMaterial(material, transform.position, true);
        MatchReporter.Event("shooting_star_collected", value: material, position: transform.position);
        Destroy(gameObject);
        return true;
    }
}
