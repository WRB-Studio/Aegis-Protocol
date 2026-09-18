using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour, IResettable
{
    public static SoundManager Instance;

    [Header("Music")]
    private AudioSource musicSource;
    public AudioClip mainMusic;
    public AudioClip gameOverMusic;
    [Range(0.5f, 2f)] public float musicPitch = 1f;

    [Header("Ship Explosion")]
    public AudioClip shipExplosionClip;
    public Vector2 shipExplosionPitch = new Vector2(0.9f, 1.1f);

    [Header("Module Explosion")]
    public AudioClip moduleExplosionClip;
    public Vector2 moduleExplosionPitch = new Vector2(0.9f, 1.1f);

    [Header("Core Explosion")]
    public AudioClip coreExplosionClip;
    public Vector2 coreExplosionPitch = new Vector2(0.9f, 1.1f);

    [Header("Ship hit")]
    public AudioClip stationHitClip;
    public Vector2 stationHitPitch = new Vector2(0.9f, 1.1f);

    [Header("Deflection Hit")]
    public AudioClip deflectionHitClip;
    public Vector2 deflectionHitPitch = new Vector2(0.9f, 1.1f);

    [Header("Installation")]
    public AudioClip installationClip;
    public Vector2 installationPitch = new Vector2(0.9f, 1.1f);

    [Header("Upgrade")]
    public AudioClip upgradeClip;
    public Vector2 upgradePitch = new Vector2(0.9f, 1.1f);

    [Header("Shoot")]
    public AudioClip shootClip;
    public Vector2 shootPitch = new Vector2(0.9f, 1.1f);

    public int maxSoundsPerCategory = 6;

    private List<AudioSource> currentExplosionSounds = new List<AudioSource>();
    private List<AudioSource> currentHitSounds = new List<AudioSource>();
    private List<AudioSource> currentShootSounds = new List<AudioSource>();
    private List<AudioSource> currentInstallationSounds = new List<AudioSource>();
    private List<AudioSource> currentUpgradeSounds = new List<AudioSource>();

    private bool musicEnabled = true;
    private bool effectsEnabled = true;
    private Coroutine pitchRoutine;
    private Coroutine fadeRoutine;


    // --- INIT SNAPSHOT ---
    private void Awake()
    {
        Instance = this;
    }

    public void Init()
    {
        musicSource = GetComponent<AudioSource>();
        musicEnabled = PlayerPrefs.GetInt("Aegis.MusicEnabled", 1) != 0;
        effectsEnabled = PlayerPrefs.GetInt("Aegis.EffectsEnabled", 1) != 0;
        musicSource.mute = !musicEnabled;
        PlayMainMusic();
    }

    public void ToggleMusic(bool enabled)
    {
        musicEnabled = enabled;
        musicSource.mute = !enabled;
        PlayerPrefs.SetInt("Aegis.MusicEnabled", enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void ToggleEffects(bool enabled)
    {
        effectsEnabled = enabled;
        PlayerPrefs.SetInt("Aegis.EffectsEnabled", enabled ? 1 : 0);
        PlayerPrefs.Save();
    }


    public void SetMusicPitch(float targetPitch, float duration = 0.75f)
    {
        targetPitch = Mathf.Clamp(targetPitch, 0.5f, 2f);
        if (pitchRoutine != null) StopCoroutine(pitchRoutine);
        pitchRoutine = StartCoroutine(SmoothPitchChange(targetPitch, duration));
    }

    private IEnumerator SmoothPitchChange(float targetPitch, float duration)
    {
        float startPitch = musicSource.pitch;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            musicSource.pitch = Mathf.Lerp(startPitch, targetPitch, time / duration);
            yield return null;
        }

        musicSource.pitch = targetPitch;
        musicPitch = targetPitch;
    }


    public void PlayMainMusic(float fadeDuration = 3f)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        musicSource.clip = mainMusic;
        musicSource.loop = true;
        musicSource.volume = 0f;
        musicSource.pitch = musicPitch;
        musicSource.Play();
        fadeRoutine = StartCoroutine(FadeInMusic(fadeDuration, 0.2f));
    }

    private IEnumerator FadeInMusic(float duration, float startDuration = 0f)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startDuration, 1f, time / duration);
            yield return null;
        }
        musicSource.volume = 1f;
    }

    public void PlayGameOverMusic()
    {
        musicSource.clip = gameOverMusic;
        musicSource.loop = true;
        musicSource.pitch = musicPitch;
        musicSource.Play();
    }



    public void PlaySound(List<AudioSource> soundList, AudioClip sound, Vector2 pitchRange)
    {
        if (!effectsEnabled || !sound || soundList.Count >= maxSoundsPerCategory) return;

        GameObject tempGO = new GameObject("TempAudio");
        tempGO.transform.parent = this.transform;

        AudioSource aSource = tempGO.AddComponent<AudioSource>();
        aSource.clip = sound;
        aSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        aSource.volume = 1f;
        aSource.spatialBlend = 0f;
        aSource.Play();

        soundList.Add(aSource);

        StartCoroutine(DestroySoundAfterPlay(aSource, soundList));

    }

    public void PlayShipExplosion()
    {
        PlaySound(currentExplosionSounds, shipExplosionClip, shipExplosionPitch);
    }

    public void PlayModuleExplosion()
    {
        PlaySound(currentExplosionSounds, moduleExplosionClip, moduleExplosionPitch);
    }

    public void PlayCoreExplosion()
    {
        PlaySound(currentExplosionSounds, coreExplosionClip, coreExplosionPitch);
    }

    public void PlayStationHitSound()
    {
        PlaySound(currentHitSounds, stationHitClip, stationHitPitch);
    }

    public void PlayDeflectionHitSound()
    {
        PlaySound(currentHitSounds, deflectionHitClip, deflectionHitPitch);
    }

    public void PlayShootSound()
    {
        PlaySound(currentShootSounds, shootClip, shootPitch);
    }

    public void PlayInstallationSound()
    {
        PlaySound(currentInstallationSounds, installationClip, installationPitch);
    }

    public void PlayUpgradeSound()
    {
        PlaySound(currentUpgradeSounds, upgradeClip, upgradePitch);
    }


    public float GetAudioClipLength(AudioSource audioSource)
    {
        return audioSource.clip.length / audioSource.pitch;
    }

    private IEnumerator DestroySoundAfterPlay(AudioSource source, List<AudioSource> list)
    {
        while (source && source.isPlaying)
            yield return null;

        list.Remove(source);
        if (source) Destroy(source.gameObject);
    }


    public void StoreInit()
    {
    }

    public void ResetScript()
    {
        StopAllCoroutines();
        pitchRoutine = null;
        fadeRoutine = null;
        foreach (var source in GetComponentsInChildren<AudioSource>())
            if (source != musicSource) Destroy(source.gameObject);
        currentExplosionSounds.Clear();
        currentHitSounds.Clear();
        currentShootSounds.Clear();
        currentInstallationSounds.Clear();
        currentUpgradeSounds.Clear();
        musicPitch = 1f;
        musicSource.pitch = musicPitch;
        musicSource.mute = !musicEnabled;
    }

}
