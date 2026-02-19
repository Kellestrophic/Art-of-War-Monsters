using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using System.Collections;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Mixer Routing")]
    [SerializeField] private AudioMixer mixer;                 // your GameMixer (optional; kept for saved volume)
    [SerializeField] private AudioMixerGroup musicGroup;       // route music here (recommended)

    [Header("Clips")]
    [SerializeField] private AudioClip mainMenuMusic;          // assign your Main Menu song

    [Header("Defaults")]
    [SerializeField] private float defaultFadeSeconds = 0.75f;
    [SerializeField, Range(0f,1f)] private float defaultTargetVolume = 1f;

    private AudioSource src;
    private Coroutine fadeRoutine;

    void Awake()
{
    if (Instance != null && Instance != this)
    {
        Destroy(gameObject);
        return;
    }

    Instance = this;
    DontDestroyOnLoad(gameObject);

    if (src == null)
        src = gameObject.AddComponent<AudioSource>();

    src.loop = true;
    src.playOnAwake = false;
    src.volume = defaultTargetVolume;
    if (musicGroup) src.outputAudioMixerGroup = musicGroup;

    if (PlayerPrefs.HasKey("vol_music"))
        mixer?.SetFloat("MusicVol",
            Mathf.Log10(Mathf.Clamp(PlayerPrefs.GetFloat("vol_music"), 0.0001f, 1f)) * 20f);

    SceneManager.sceneLoaded += OnSceneLoaded;

    if (mainMenuMusic && SceneManager.GetActiveScene().name == "Main_Menu")
        Play(mainMenuMusic);
}

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Safety: whenever Main Menu loads, ensure we’re back on menu track
        if (scene.name == "Main_Menu" && mainMenuMusic != null)
            CrossfadeTo(mainMenuMusic, defaultFadeSeconds, defaultTargetVolume);
    }

    public void Play(AudioClip clip)
    {
        if (!clip) return;
        src.Stop();
        src.clip = clip;
        src.time = 0f;
        src.Play();
    }

    // Convenience
    public void PlayMainMenuMusic() => CrossfadeTo(mainMenuMusic, defaultFadeSeconds, defaultTargetVolume);

    // Overloads so both 2-arg and 3-arg calls work
    public void CrossfadeTo(AudioClip clip, float fadeSeconds) =>
        CrossfadeTo(clip, fadeSeconds, defaultTargetVolume);

    public void CrossfadeTo(AudioClip clip, float fadeSeconds, float targetVolume)
    {
        if (src == null)
{
    Debug.LogWarning("[MusicManager] AudioSource missing — recreating.");
    src = gameObject.AddComponent<AudioSource>();
    src.loop = true;
}

        if (!clip) return;
        if (src.clip == clip && src.isPlaying) return;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeCoroutine(clip, fadeSeconds, targetVolume));
    }

    private IEnumerator FadeCoroutine(AudioClip newClip, float fadeSeconds, float targetVolume)
    {
        float startVol = src.volume;
        float t = 0f;

        // fade out
        while (t < fadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            src.volume = Mathf.Lerp(startVol, 0f, t / fadeSeconds);
            yield return null;
        }

        // swap
        src.Stop();
        src.clip = newClip;
        src.time = 0f;
        src.Play();

        // fade in
        t = 0f;
        while (t < fadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            src.volume = Mathf.Lerp(0f, targetVolume, t / fadeSeconds);
            yield return null;
        }
        src.volume = targetVolume;

        fadeRoutine = null;
    }
}
