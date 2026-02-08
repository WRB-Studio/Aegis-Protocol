using UnityEngine;

public class ExplosionManager : MonoBehaviour
{
    public static ExplosionManager Instance;

    [Header("Explosion prefabs")]
    public GameObject[] shipExplosionPrefabs;
    public GameObject[] droneExplosionPrefabs;
    public GameObject[] moduleExplosionPrefabs;
    public GameObject coreExplosionPrefab;

    [Header("Explosion scales")]
    public Vector2 shipExplosionScale;
    public Vector2 droneExplosionScale;
    public Vector2 moduleExplosionScale;

    [Header("Explosion colors")]
    public Color[] shipExplosionColors;
    public Color[] droneExplosionColors;
    public Color[] moduleExplosionColors;
    public Color coreExplosionColor;

    private Transform explosionParent;


    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        explosionParent = GameObject.Find("ExplosionParent").transform;
    }

    public void CreateShipExplosion(Vector3 position)
    {
        CreateExplosion(position, shipExplosionScale, shipExplosionColors[Random.Range(0, shipExplosionColors.Length)], shipExplosionPrefabs[Random.Range(0, shipExplosionPrefabs.Length)]);
        SoundManager.Instance.PlayShipExplosion();
    }

    public void CreateDroneExplosion(Vector3 position)
    {
        CreateExplosion(position, droneExplosionScale, droneExplosionColors[Random.Range(0, droneExplosionColors.Length)], droneExplosionPrefabs[Random.Range(0, droneExplosionPrefabs.Length)]);
        SoundManager.Instance.PlayShipExplosion();
    }

    public void CreateModuleExplosion(Vector3 position)
    {
        CreateExplosion(position, moduleExplosionScale, moduleExplosionColors[Random.Range(0, moduleExplosionColors.Length)], moduleExplosionPrefabs[Random.Range(0, moduleExplosionPrefabs.Length)]);
        SoundManager.Instance.PlayModuleExplosion();
    }

    public void CreateCoreExplosion(Vector3 position)
    {
        CreateExplosion(position, Vector2.one, coreExplosionColor, coreExplosionPrefab);
        SoundManager.Instance.PlayCoreExplosion();
    }

    private void CreateExplosion(Vector3 position, Vector2 scaleRange, Color color, GameObject explosionPrefab)
    {
        GameObject newExplosion = Instantiate(explosionPrefab, position, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)), explosionParent);
        float scale = Random.Range(scaleRange.x, scaleRange.y);
        newExplosion.transform.localScale = new Vector3(scale, scale, scale);
        newExplosion.GetComponent<SpriteRenderer>().color = color;

        float lifetime = 1f;

        Animator animator = newExplosion.GetComponent<Animator>();
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
            if (clips.Length > 0)
                lifetime = clips[0].length;
        }

        Destroy(newExplosion, lifetime);
    }
}
