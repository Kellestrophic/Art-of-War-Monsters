using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class XPBar : MonoBehaviour
{
    [Header("UI")]
    public Slider xpSlider;
    public TMP_Text xpBarText;
    public TMP_Text levelText;

    private PlayerXP playerXP;
    private bool usingFacadeFallback;

    void Awake()
    {
        if (xpSlider)
        {
            xpSlider.minValue = 0;
            xpSlider.value = 0; // start empty
        }

        // Try to bind to PlayerXP (gameplay scenes)
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player) playerXP = player.GetComponent<PlayerXP>();

        // If no PlayerXP (e.g., Main Menu), we’ll listen to XPFacade directly
        usingFacadeFallback = (playerXP == null);
    }

    void OnEnable()
    {
        // Always listen to the facade so level updates are instant everywhere
        XPFacade.OnXPChanged += OnFacadeXPChanged;
        XPFacade.InitializeFromProfileIfAvailable();

        if (!usingFacadeFallback && playerXP != null)
        {
            playerXP.xpChanged.AddListener(OnPlayerXPChanged);
        }

        // Push current state once on enable
        PushFromTotal(XPFacade.GetTotalXP());
    }

    void OnDisable()
    {
        XPFacade.OnXPChanged -= OnFacadeXPChanged;

        if (!usingFacadeFallback && playerXP != null)
        {
            playerXP.xpChanged.RemoveListener(OnPlayerXPChanged);
        }
    }

    // Legacy path: event from PlayerXP (xpIntoLevel, xpToNext)
    private void OnPlayerXPChanged(int newXP, int xpToNext)
    {
        // Update bar
        if (xpSlider)
        {
            xpSlider.maxValue = xpToNext;
            xpSlider.value = Mathf.Clamp(newXP, 0, xpToNext);
        }
        if (xpBarText) xpBarText.text = $"XP {newXP} / {xpToNext}";

        // 🔥 Recompute level from the canonical totalXP so it updates live on level-up
        int total = XPFacade.GetTotalXP();
        XPLevelCalculator.GetProgressInLevel(
            total,
            out var lvl, out _, out _, out _, out _, out _);

        if (levelText) levelText.text = $"LVL. {lvl}";
    }

    // Facade path: level, xpInto, pct (works in menus and anywhere else)
    private void OnFacadeXPChanged(int level, int xpIntoLevel, float pct)
    {
        PushFromTotal(XPFacade.GetTotalXP());
    }

    private void PushFromTotal(int totalXP)
    {
        XPLevelCalculator.GetProgressInLevel(
            totalXP,
            out var lvl,
            out var _levelStart,
            out var _nextLevelStart,
            out var into,
            out var need,
            out var _pct);

        if (xpSlider)
        {
            xpSlider.maxValue = need;
            xpSlider.value = Mathf.Clamp(into, 0, need);
        }
        if (xpBarText) xpBarText.text = $"XP {into} / {need}";
        if (levelText) levelText.text = $"LVL. {lvl}";
    }
}
