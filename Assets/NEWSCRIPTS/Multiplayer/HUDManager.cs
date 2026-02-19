using System.Collections;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    [Header("Local HUD UI")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image    iconImage;
    [SerializeField] private Image    frameImage;
    [SerializeField] private Slider   hpBar;
    [SerializeField] private Slider   strikeBar;

    [Header("Cosmetics")]
    [SerializeField] private FrameLibrary frameLibrary; // assign in Inspector

    private PlayerHudRefs _refs;
    private int _lastLevel = -1;

    private void OnEnable()
    {
        var store = ActiveProfileStore.Instance;
        if (store != null) store.OnProfileChanged += OnProfileChanged;
    }

    private void OnDisable()
    {
        var store = ActiveProfileStore.Instance;
        if (store != null) store.OnProfileChanged -= OnProfileChanged;
    }

    private IEnumerator Start()
    {
        // Wait for Netcode + local player
        while (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) yield return null;
        var nm = NetworkManager.Singleton;
        while (nm.LocalClient == null || nm.LocalClient.PlayerObject == null) yield return null;

        var localPlayer = nm.LocalClient.PlayerObject.gameObject;
        _refs = localPlayer.GetComponent<PlayerHudRefs>();
        if (_refs == null)
        {
            Debug.LogWarning("[HUDManager] PlayerHudRefs not found on local player.");
            yield break;
        }

        // Seed static UI from refs (will be overridden by profile event too)
        if (nameText)   nameText.text    = _refs.PlayerName;
        if (titleText)  titleText.text   = _refs.PlayerTitle;
        if (iconImage)  iconImage.sprite = _refs.PlayerIcon;

        if (hpBar)     hpBar.maxValue     = Mathf.Max(1f, _refs.MaxHP);
        if (strikeBar) strikeBar.maxValue = Mathf.Max(1f, _refs.MaxStrike);

        ApplyFrameForLevel(_refs.Level);

        StartCoroutine(UpdateLoop());
        Debug.Log("[HUDManager] HUD bound to local player: " + localPlayer.name);

        // Draw profile immediately if already loaded
        var p = ActiveProfileStore.Instance?.CurrentProfile;
        if (p != null) OnProfileChanged(p);
    }

    private IEnumerator UpdateLoop()
    {
        var wait = new WaitForSeconds(0.1f); // 10 Hz
        while (_refs != null)
        {
            if (hpBar)     hpBar.value     = Mathf.Clamp(_refs.CurrentHP, 0f, hpBar.maxValue);
            if (strikeBar) strikeBar.value = Mathf.Clamp(_refs.CurrentStrike, 0f, strikeBar.maxValue);

            int lvl = _refs.Level;
            if (lvl != _lastLevel) ApplyFrameForLevel(lvl);

            yield return wait;
        }
    }

    private void ApplyFrameForLevel(int level)
    {
        _lastLevel = level;
        if (frameImage == null) return;

        Sprite frame = null;
        if (frameLibrary != null) frame = frameLibrary.GetForLevel(level);
        if (frame == null) frame = _refs.DefaultFrame;

        frameImage.sprite = frame;
    }

    private void OnProfileChanged(NewProfileData p)
    {
        if (p == null) return;
        if (nameText)  nameText.text  = string.IsNullOrEmpty(p.playerName) ? "Player" : p.playerName;
        if (titleText) titleText.text = p.activeTitle ?? "";
        // icon is handled by PlayerHudRefs in your flow; leave as is unless you want to fetch via IconImageLibrary here.
    }
}
