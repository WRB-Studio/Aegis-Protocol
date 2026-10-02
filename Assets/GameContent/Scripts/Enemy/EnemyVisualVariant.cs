using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class EnemyVisualVariant : MonoBehaviour
{
    [SerializeField] Sprite[] variants;
    [SerializeField] int variant3FirstWave = 10;
    [SerializeField] int variant4FirstWave = 20;
    [SerializeField] float variant3Scale = 1f;
    [SerializeField] float variant4Scale = 1f;

    void Awake()
    {
        if (variants == null || variants.Length == 0) return;

        int availableVariants = variants.Length;
        int wave = EnemySpawner.Instance ? EnemySpawner.Instance.DisplayWave : 1;

        if (availableVariants > 3 && wave < variant4FirstWave) availableVariants = 3;
        if (availableVariants > 2 && wave < variant3FirstWave) availableVariants = 2;

        int selectedVariant = Random.Range(0, availableVariants);
        if (GetComponent<Enemy>().enemyType == Enemy.eEnemyType.Boss)
            selectedVariant = Mathf.Max(0, wave / 5 - 1) % variants.Length;
        GetComponent<SpriteRenderer>().sprite = variants[selectedVariant];

        if (selectedVariant == 2) transform.localScale *= variant3Scale;
        if (selectedVariant >= 3) transform.localScale *= variant4Scale;
    }
}
