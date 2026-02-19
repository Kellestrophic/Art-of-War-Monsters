using UnityEngine;
using UnityEngine.Audio;

public class SFXBus : MonoBehaviour
{
    public static SFXBus Instance { get; private set; }

    [Header("Routing")]
    [SerializeField] private AudioMixerGroup sfxGroup; // assign GameMixer/SFX in Inspector

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // 3D one-shot at world position
    public void PlayAt(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
    {
        if (!clip || sfxGroup == null) return;

        var go = new GameObject("OneShotSFX");
        go.transform.position = position;

        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = Mathf.Clamp01(volume);
        src.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
        src.outputAudioMixerGroup = sfxGroup;
        src.spatialBlend = 1f;      // 3D
        src.minDistance = 1f;
        src.maxDistance = 500f;
        src.rolloffMode = AudioRolloffMode.Logarithmic;

        src.Play();
        Destroy(go, clip.length / Mathf.Max(0.01f, src.pitch) + 0.05f);
    }

    // 2D one-shot (UI, menus)
    public void Play2D(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (!clip || sfxGroup == null) return;

        var go = new GameObject("OneShotSFX_2D");
        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = Mathf.Clamp01(volume);
        src.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
        src.outputAudioMixerGroup = sfxGroup;
        src.spatialBlend = 0f;  // 2D

        src.Play();
        Destroy(go, clip.length / Mathf.Max(0.01f, src.pitch) + 0.05f);
    }

    // Handy random pitch helper for variation (e.g., 0.95–1.05)
    public void PlayVariedAt(AudioClip clip, Vector3 position, float volume = 1f, float pitchVar = 0.05f)
    {
        float p = 1f + Random.Range(-pitchVar, pitchVar);
        PlayAt(clip, position, volume, p);
    }
}
