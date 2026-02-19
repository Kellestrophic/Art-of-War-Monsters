using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Central UI updater for name, title, icon, frame, level, coins.
/// Refreshes whenever ActiveProfileStore broadcasts changes.
/// </summary>
public class ProfileUIRenderer : MonoBehaviour
{
    // 🔹 Global access for other scripts (ProfileUIFromRuntime, IconCosmeticButton, etc.)
    public static ProfileUIRenderer Instance { get; private set; }

    [System.Serializable]
    public class DisplayTarget
    {
        public Image iconImage;
        public Image frameImage;
        public TMP_Text playerNameText;
        public TMP_Text playerTitleText;
        public TMP_Text playerLevelText;
        public TMP_Text mssBankedText;
    }

    [Header("Display Targets")]
    [SerializeField] private List<DisplayTarget> targets = new();

    private CosmeticLibrary cosmeticLibrary;
    private FrameLibrary frameLibrary;

    // ---------------------------------------------------------
    // LIFECYCLE
    // ---------------------------------------------------------
    private void Awake()
    {
        // Singleton-style instance for convenience
        Instance = this;

        cosmeticLibrary = Resources.Load<CosmeticLibrary>("CosmeticLibrary");
        frameLibrary    = Resources.Load<FrameLibrary>("FrameLibrary");

        if (cosmeticLibrary == null)
            Debug.LogError("[ProfileUIRenderer] Missing CosmeticLibrary in Resources!");

        if (frameLibrary == null)
            Debug.LogError("[ProfileUIRenderer] Missing FrameLibrary in Resources!");
    }
private void HandleStatsChanged()
{
    RefreshUI();   // Forces MSS to update live
}

   private void OnEnable()
{
    if (ActiveProfileStore.Instance != null)
        ActiveProfileStore.Instance.OnProfileChanged += Refresh;

    if (StatsTracker.Instance != null)
        StatsTracker.Instance.OnStatsChanged += HandleStatsChanged;

    Refresh(ActiveProfileStore.Instance?.CurrentProfile);
}

private void OnDisable()
{
    if (ActiveProfileStore.Instance != null)
        ActiveProfileStore.Instance.OnProfileChanged -= Refresh;

    if (StatsTracker.Instance != null)
        StatsTracker.Instance.OnStatsChanged -= HandleStatsChanged;
}

    // ---------------------------------------------------------
    // PUBLIC WRAPPER – for scripts that only know about the store
    // ---------------------------------------------------------
    public void RefreshUI()
    {
        if (ActiveProfileStore.Instance?.CurrentProfile == null)
    return;

        var profile = ActiveProfileStore.Instance?.CurrentProfile;
        Refresh(profile);
    }

    // ---------------------------------------------------------
    // MAIN REFRESH (NOW PUBLIC)
    // ---------------------------------------------------------
    public void Refresh(NewProfileData p)
    {
        if (p == null) return;

        string name  = p.playerName ?? "";
        string title = GetTitleDisplay(p);
        Sprite icon  = GetIconSprite(p);
        Sprite frame = GetFrameSprite(p);

        foreach (var t in targets)
        {
            if (t.playerNameText)  t.playerNameText.text  = name;
            if (t.playerTitleText) t.playerTitleText.text = title;
            if (t.playerLevelText) t.playerLevelText.text = "Lv " + p.level;
            if (t.mssBankedText)    t.mssBankedText.text       = p.mssBanked.ToString();

            if (t.iconImage)  t.iconImage.sprite  = icon;
            if (t.frameImage) t.frameImage.sprite = frame;
        }

        Debug.Log("[ProfileUIRenderer] UI refreshed.");
    }

    // ---------------------------------------------------------
    // TITLE DISPLAY (FROM CosmeticLibrary)
    // ---------------------------------------------------------
    private string GetTitleDisplay(NewProfileData p)
    {
        if (cosmeticLibrary == null) 
            return p.activeTitle;

        if (cosmeticLibrary.TryGetItem(p.activeTitle, out var item))
            return item.displayName;

        return p.activeTitle;
    }

    // ---------------------------------------------------------
    // ICON DISPLAY
    // ---------------------------------------------------------
    private Sprite GetIconSprite(NewProfileData p)
    {
        if (cosmeticLibrary == null) 
            return null;

        if (cosmeticLibrary.TryGetItem(p.activeIcon, out var item))
            return item.sprite;

        return null;
    }

    // ---------------------------------------------------------
    // FRAME DISPLAY (FROM FrameLibrary)
    // ---------------------------------------------------------
    private Sprite GetFrameSprite(NewProfileData p)
    {
        if (frameLibrary == null) 
            return null;

        return frameLibrary.GetByKey(p.activeFrame);
    }
}
