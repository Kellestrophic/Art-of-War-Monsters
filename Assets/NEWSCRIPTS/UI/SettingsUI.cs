using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("Mixer")]
    [SerializeField] private AudioMixer mixer; // GameMixer asset

    [Header("Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Optional Controls")]
    [SerializeField] private Toggle muteToggle; // OPTIONAL – can be null

    private const string P_Master = "vol_master";
    private const string P_Music  = "vol_music";
    private const string P_SFX    = "vol_sfx";
    private const string P_Muted  = "vol_muted"; // used only if toggle exists

    private void Start()
    {
        // Load saved values (defaults 0.8)
        float master = PlayerPrefs.GetFloat(P_Master, 0.8f);
        float music  = PlayerPrefs.GetFloat(P_Music,  0.8f);
        float sfx    = PlayerPrefs.GetFloat(P_SFX,    0.8f);
        bool  muted  = (muteToggle != null) && PlayerPrefs.GetInt(P_Muted, 0) == 1;

        // Push to UI without firing events
        masterSlider.SetValueWithoutNotify(master);
        musicSlider .SetValueWithoutNotify(music);
        sfxSlider   .SetValueWithoutNotify(sfx);
        if (muteToggle != null) muteToggle.SetIsOnWithoutNotify(muted);

        // Apply to mixer once
        ApplyAll(muted, master, music, sfx);

        // Listeners
        masterSlider.onValueChanged.AddListener(_ => ApplyFromUI());
        musicSlider .onValueChanged.AddListener(_ => ApplyFromUI());
        sfxSlider   .onValueChanged.AddListener(_ => ApplyFromUI());
        if (muteToggle != null)
            muteToggle.onValueChanged.AddListener(_ => ApplyFromUI());
    }

    private void ApplyFromUI()
    {
        bool muted = muteToggle != null && muteToggle.isOn;
        ApplyAll(muted, masterSlider.value, musicSlider.value, sfxSlider.value);

        // Save
        PlayerPrefs.SetFloat(P_Master, masterSlider.value);
        PlayerPrefs.SetFloat(P_Music,  musicSlider.value);
        PlayerPrefs.SetFloat(P_SFX,    sfxSlider.value);
        if (muteToggle != null) PlayerPrefs.SetInt(P_Muted, muted ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplyAll(bool muted, float master, float music, float sfx)
    {
        if (muted)
        {
            // Global mute if toggle exists and is ON
            mixer.SetFloat("MasterVol", -80f);
            return;
        }

        SetDB("MasterVol", master);
        SetDB("MusicVol",  music);
        SetDB("SFXVol",    sfx);
    }

    private void SetDB(string exposedParam, float linear01)
    {
        float lin = Mathf.Clamp(linear01, 0.0001f, 1f);
        float db  = Mathf.Log10(lin) * 20f; // linear→dB
        mixer.SetFloat(exposedParam, db);
    }
}

