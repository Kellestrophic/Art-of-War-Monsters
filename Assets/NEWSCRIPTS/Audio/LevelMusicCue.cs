using UnityEngine;

public class LevelMusicCue : MonoBehaviour
{
    [SerializeField] private AudioClip music;                 // assign this scene’s track
    [SerializeField] private float fadeSeconds = 0.8f;
    [SerializeField, Range(0f,1f)] private float targetVolume = 1f;

    void Start()
    {
        if (MusicManager.Instance != null && music != null)
            MusicManager.Instance.CrossfadeTo(music, fadeSeconds, targetVolume);
    }
}
