using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    Button pauseButton;
    GameObject overlay;
    TextMeshProUGUI musicLabel;
    TextMeshProUGUI effectsLabel;
    int screenWidth;
    int screenHeight;

    public void Init()
    {
        var canvas = FindFirstObjectByType<Canvas>();
        pauseButton = CreateButton("Pause", canvas.transform, new Vector2(180, 70), TogglePause);
        pauseButton.GetComponent<RectTransform>().pivot = Vector2.one;

        overlay = new GameObject("Pause Overlay", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(canvas.transform, false);
        var overlayRect = (RectTransform)overlay.transform;
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        overlay.GetComponent<Image>().color = new Color(0.01f, 0.04f, 0.08f, 0.9f);

        CreateButton("Resume", overlay.transform, new Vector2(320, 80), TogglePause)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 110);
        var music = CreateButton("Music", overlay.transform, new Vector2(320, 80), ToggleMusic);
        music.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        musicLabel = music.GetComponentInChildren<TextMeshProUGUI>();
        var effects = CreateButton("Effects", overlay.transform, new Vector2(320, 80), ToggleEffects);
        effects.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -110);
        effectsLabel = effects.GetComponentInChildren<TextMeshProUGUI>();
        RefreshAudioLabels();
        overlay.SetActive(false);
        PositionPauseButton();
    }

    void Update()
    {
        if (!GameManager.isInit) return;
        if (screenWidth != Screen.width || screenHeight != Screen.height)
            PositionPauseButton();
        if (pauseButton.gameObject.activeSelf == GameManager.gameOver)
            pauseButton.gameObject.SetActive(!GameManager.gameOver);

        if (Input.GetKeyDown(KeyCode.Escape) && !GameManager.gameOver)
        {
            if (UIManager.Instance.stationUI.activeSelf && !overlay.activeSelf)
                UIManager.Instance.Show(false);
            else
                TogglePause();
        }
    }

    void PositionPauseButton()
    {
        screenWidth = Screen.width;
        screenHeight = Screen.height;
        var safe = Screen.safeArea;
        var rect = pauseButton.GetComponent<RectTransform>();
        var anchor = new Vector2(safe.xMax / screenWidth, safe.yMax / screenHeight);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = new Vector2(-20, -20);
    }

    void TogglePause()
    {
        if (!GameManager.isInit || GameManager.gameOver) return;
        bool paused = !overlay.activeSelf;
        if (paused && UIManager.Instance.stationUI.activeSelf)
            UIManager.Instance.Show(false);
        overlay.SetActive(paused);
        TimeController.Instance.SetPaused(paused);
    }

    void ToggleMusic()
    {
        SoundManager.Instance.ToggleMusic(PlayerPrefs.GetInt("Aegis.MusicEnabled", 1) == 0);
        RefreshAudioLabels();
    }

    void ToggleEffects()
    {
        SoundManager.Instance.ToggleEffects(PlayerPrefs.GetInt("Aegis.EffectsEnabled", 1) == 0);
        RefreshAudioLabels();
    }

    void RefreshAudioLabels()
    {
        musicLabel.text = "Music: " + (PlayerPrefs.GetInt("Aegis.MusicEnabled", 1) != 0 ? "On" : "Off");
        effectsLabel.text = "Effects: " + (PlayerPrefs.GetInt("Aegis.EffectsEnabled", 1) != 0 ? "On" : "Off");
    }

    static Button CreateButton(string title, Transform parent, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        var root = new GameObject(title, typeof(RectTransform), typeof(Image), typeof(Button));
        root.transform.SetParent(parent, false);
        var rect = root.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        root.GetComponent<Image>().color = new Color(0.03f, 0.15f, 0.2f, 0.95f);
        var button = root.GetComponent<Button>();
        button.onClick.AddListener(action);

        var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI))
            .GetComponent<TextMeshProUGUI>();
        label.transform.SetParent(root.transform, false);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        label.font = ResourceManager.Instance.txtMaterial.font;
        label.fontSize = 30;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.7f, 1f, 1f);
        label.raycastTarget = false;
        label.text = title;
        return button;
    }
}
