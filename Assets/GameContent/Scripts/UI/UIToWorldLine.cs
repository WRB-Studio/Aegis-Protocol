using UnityEngine;

public class UIToWorldLine : MonoBehaviour, IResettable
{
    public static UIToWorldLine Instance;

    public RectTransform uiElement;     // Das UI-Symbol im Canvas
    public Transform worldTarget;       // Ziel-Modul im World Space

    private LineRenderer line;
    private Camera cam;


    // --- INIT SNAPSHOT ---
    private bool initLineEnabled;


    private void Awake()
    {
        Instance = this;
    }

    public void Init()
    {
        line = GetComponent<LineRenderer>();
        line.enabled = false;
        cam = Camera.main;
    }

    public void UpdateNormal()
    {
        if (!line.enabled) return;

        Vector3 screenPos = uiElement.position; // direkt Screen-Space
        Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 1f)); // Z = Entfernung zur Kamera

        line.SetPosition(0, worldPos);
        line.SetPosition(1, worldTarget.position);
    }

    public void showLine(RectTransform uiElement, Transform worldTarget)
    {
        this.uiElement = uiElement;
        this.worldTarget = worldTarget;

        if (uiElement && worldTarget)
            line.enabled = true;
    }

    public void HideLine()
    {
        line.enabled = false;

        uiElement = null;
        worldTarget = null;
    }


    public void StoreInit()
    {
        initLineEnabled = line != null && line.enabled;
    }

    public void ResetScript()
    {
        if (!line) line = GetComponent<LineRenderer>();

        line.enabled = false;
        uiElement = null;
        worldTarget = null;
    }
}
