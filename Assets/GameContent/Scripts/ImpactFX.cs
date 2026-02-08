using System.Collections;
using UnityEngine;

public class ImpactFX : MonoBehaviour
{
    public static ImpactFX Instance;

    private Camera cam;
    private Vector3 originPosition;

    void Awake()
    {
        Instance = this;

        cam = Camera.main;
        originPosition = cam.transform.position;
    }

    public void PlayImpactEffect()
    {
        StartCoroutine(FreezeShake(0f, 0.4f, 0.5f));
    }

    private IEnumerator FreezeShake(
        float freezeTime = 0.08f,
        float shakeTime = 0.25f,
        float shakeStrength = 0.25f,
        float shakeHz = 35f
    )
    {
        float prevScale = Time.timeScale;
        Time.timeScale = 0f;

        for (float t = 0f; t < freezeTime; t += Time.unscaledDeltaTime)
            yield return null;

        Time.timeScale = prevScale;

        for (float st = 0f; st < shakeTime; st += Time.unscaledDeltaTime)
        {
            float k = 1f - Mathf.Clamp01(st / shakeTime);
            float s = shakeStrength * k;

            float x = (Mathf.PerlinNoise(Time.unscaledTime * shakeHz, 0f) - 0.5f) * 2f * s;
            float y = (Mathf.PerlinNoise(0f, Time.unscaledTime * shakeHz) - 0.5f) * 2f * s;

            cam.transform.position = new Vector3(x, y, originPosition.z);
            yield return null;
        }

        cam.transform.position = originPosition;
    }

}
