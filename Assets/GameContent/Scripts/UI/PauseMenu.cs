using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] Button pauseButton;
    [SerializeField] GameObject overlay;
    [SerializeField] Button resumeButton;
    [SerializeField] Button musicButton;
    [SerializeField] Button effectsButton;
    [SerializeField] Button exitButton;
    [SerializeField] Image musicIcon;
    [SerializeField] Image effectsIcon;
    [SerializeField] Sprite musicOnIcon;
    [SerializeField] Sprite musicOffIcon;
    [SerializeField] Sprite effectsOnIcon;
    [SerializeField] Sprite effectsOffIcon;
    [SerializeField] Button privacyPolicyButton;
    const string PrivacyPolicyUrl = "https://www.freeprivacypolicy.com/live/686cb10e-b576-4646-916b-9ee178e1fa7e";

    public void Init()
    {
        pauseButton.onClick.AddListener(TogglePause);
        resumeButton.onClick.AddListener(TogglePause);
        musicButton.onClick.AddListener(ToggleMusic);
        effectsButton.onClick.AddListener(ToggleEffects);
        exitButton.onClick.AddListener(ExitGame);
        privacyPolicyButton.onClick.AddListener(OpenPrivacyPolicy);
        RefreshAudioIcons();
        overlay.SetActive(false);
    }

    void Update()
    {
        if (!GameManager.isInit) return;
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

    void TogglePause()
    {
        if (!GameManager.isInit || GameManager.gameOver) return;
        bool paused = !overlay.activeSelf;
        if (paused && UIManager.Instance.stationUI.activeSelf)
            UIManager.Instance.Show(false);
        overlay.SetActive(paused);
        TimeController.Instance.SetPaused(paused);
    }

    void OpenPrivacyPolicy()
    {
        Application.OpenURL(PrivacyPolicyUrl);
    }

    void ToggleMusic()
    {
        SoundManager.Instance.ToggleMusic(PlayerPrefs.GetInt("Aegis.MusicEnabled", 1) == 0);
        RefreshAudioIcons();
    }

    void ToggleEffects()
    {
        SoundManager.Instance.ToggleEffects(PlayerPrefs.GetInt("Aegis.EffectsEnabled", 1) == 0);
        RefreshAudioIcons();
    }

    void RefreshAudioIcons()
    {
        musicIcon.sprite = PlayerPrefs.GetInt("Aegis.MusicEnabled", 1) != 0 ? musicOnIcon : musicOffIcon;
        effectsIcon.sprite = PlayerPrefs.GetInt("Aegis.EffectsEnabled", 1) != 0 ? effectsOnIcon : effectsOffIcon;
    }

    void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
