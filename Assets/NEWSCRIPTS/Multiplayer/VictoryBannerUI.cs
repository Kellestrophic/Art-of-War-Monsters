using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
#if UNITY_NETCODE
using Unity.Netcode;
#endif

public class VictoryBannerUI : MonoBehaviour
{
    // ---- Inspector ----

    [Header("Panel root (disable at start)")]
    [SerializeField] private GameObject root;

    [Header("MATCH YOUR NAMES (these are the CENTER banner widgets)")]
    [SerializeField] private TMP_Text playerNameText;   // VictoryOverlay/PlayerName_profile
    [SerializeField] private TMP_Text titleText;        // VictoryOverlay/Title_profile
    [SerializeField] private Image iconImage;           // VictoryOverlay/ProfileIcon_profile
    [SerializeField] private Image frameImage;          // VictoryOverlay/ProfileFrame_profile

    [Header("Libraries (optional)")]
    [SerializeField] private CosmeticLibrary cosmeticLibrary;

    [Header("Optional")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Sprite fallbackIcon;
    [SerializeField] private Sprite fallbackFrame;
    [SerializeField] private TMP_Text rewardsText;
    [SerializeField] private float payoutCheckHz = 5f;
    [SerializeField] private TMP_Text totalsText;

    [Header("Scenes")]
    [SerializeField] private string fallbackLobbySceneName = "Main_Menu";

    [Header("Winner HUD Sources (drag the HUD parents)")]
    [SerializeField] private Transform playerUIRoot;   // e.g. the GameObject named "PlayerUI"
    [SerializeField] private Transform aiUIRoot;       // e.g. the GameObject named "AIUI"

    [Header("Profile Fallbacks (used only if HUD is missing values)")]
    [SerializeField] private string playerFallbackName = "Player";
    [SerializeField] private string playerFallbackTitle = "";
    [SerializeField] private Sprite playerFallbackIcon;
    [SerializeField] private Sprite playerFallbackFrame;

    [SerializeField] private string aiFallbackName = "AI";
    [SerializeField] private string aiFallbackTitle = "Artificial Butt Kicker";
    [SerializeField] private Sprite aiFallbackIcon;
    [SerializeField] private Sprite aiFallbackFrame;

    [Header("Behaviour")]
    [Tooltip("Pause the game while the banner is visible.")]
    [SerializeField] private bool pauseOnShow = true;

    [Tooltip("If ON, the Continue button is enabled only after MatchRewardsNet says payout is ready (with timeout). If OFF, it enables immediately.")]
    [SerializeField] private bool gateContinueByPayout = false;

    [Tooltip("If gating by payout, enable Continue anyway after this many real-time seconds.")]
    [SerializeField] private float gateTimeoutSeconds = 3f;

    [Tooltip("Small delay before enabling Continue (UX polish).")]
    [SerializeField] private float minShowSeconds = 0.25f;

    // ---- constants for HUD child names ----
    private const string HUD_NAME  = "PlayerName_profile";
    private const string HUD_TITLE = "Title_profile";
    private const string HUD_ICON  = "ProfileIcon_profile";
    private const string HUD_FRAME = "ProfileFrame_profile";

    // ---- Lifecycle ----
    private void Awake()
    {
        if (root) root.SetActive(false);
        if (continueButton) continueButton.interactable = false;
    }

#if UNITY_NETCODE
    private void OnEnable()
    {
        if (MatchStateNet.Instance != null)
            MatchStateNet.Instance.Phase.OnValueChanged += OnPhaseChanged;
    }

    private void OnDisable()
    {
        if (MatchStateNet.Instance != null)
            MatchStateNet.Instance.Phase.OnValueChanged -= OnPhaseChanged;
    }

    private void OnPhaseChanged(MatchPhase oldP, MatchPhase newP)
    {
        if (newP == MatchPhase.Ended)
            ShowBannerFromWinnerId(MatchStateNet.Instance.WinnerClientId.Value);
        else if (newP == MatchPhase.Playing)
            HideBanner();
    }
#endif

    // ---------------------------------------------------------
    // PUBLIC API
    // ---------------------------------------------------------

#if UNITY_NETCODE
    private void ShowBannerFromWinnerId(ulong winnerId)
    {
        // Fallback winner logic: player alive & enemy dead -> player
        Damagable player = null, enemy = null;
        var ds = FindObjectsByType<Damagable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var d in ds)
        {
            if (!d) continue;
            if (d.isEnemy) enemy = d; else player = d;
        }
        bool playerWon = (player && player.IsAlive) && (!enemy || !enemy.IsAlive);

        ShowBannerFromHud(playerWon);
    }
#endif

    /// <summary>Build the banner by copying from PlayerUI (playerWon=true) or AIUI (false).</summary>
    public void ShowBannerFromHud(bool playerWon)
    {
        // Pick HUD roots; if not assigned, auto-find by name
        Transform pHud = playerUIRoot ? playerUIRoot : FindHud("PlayerUI");
        Transform aHud = aiUIRoot     ? aiUIRoot     : FindHud("AIUI");

        bool ok = false;
        if (playerWon)
            ok = ApplyFromHudRobust(pHud, playerFallbackName, playerFallbackTitle, playerFallbackIcon, playerFallbackFrame);
        else
            ok = ApplyFromHudRobust(aHud, aiFallbackName, aiFallbackTitle, aiFallbackIcon, aiFallbackFrame);

        if (!ok)
        {
            // Last-resort: just show fallbacks
            if (playerWon) ApplyFallback(playerFallbackName, playerFallbackTitle, playerFallbackIcon, playerFallbackFrame);
            else           ApplyFallback(aiFallbackName,     aiFallbackTitle,     aiFallbackIcon,     aiFallbackFrame);
        }

        if (root) root.SetActive(true);
        if (rewardsText) rewardsText.text = "Calculating rewards...";
        if (continueButton) continueButton.interactable = false;

        if (pauseOnShow) Time.timeScale = 0f;

        StopAllCoroutines();
        StartCoroutine(EnableContinueRoutine());
    }

    // Back-compat for existing callers (e.g., Damagable)
    public void ShowLocalWin(string _displayName)  => ShowBannerFromHud(true);
    public void ShowLocalLoss(string _displayName) => ShowBannerFromHud(false);

    public void OnClickContinue()
    {
        Time.timeScale = 1f;
        if (continueButton) continueButton.interactable = false;
        if (root) root.SetActive(false);

        if (!string.IsNullOrEmpty(fallbackLobbySceneName))
            SceneManager.LoadScene(fallbackLobbySceneName);
    }

    // ---------------------------------------------------------
    // Internals
    // ---------------------------------------------------------

    private IEnumerator EnableContinueRoutine()
    {
        // Minimum display time (UX)
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, minShowSeconds));

        if (!gateContinueByPayout)
        {
            if (continueButton) continueButton.interactable = true;
            yield break;
        }

        // Gate on payout ready (with timeout)
        float checkDt = 1f / Mathf.Max(1f, payoutCheckHz);
        float t = 0f;

        while (t < Mathf.Max(0.25f, gateTimeoutSeconds))
        {
            if (MatchRewardsNet.LocalPayoutReady) break;
            yield return new WaitForSecondsRealtime(checkDt);
            t += checkDt;
        }

        // Update texts if available
        if (MatchRewardsNet.LocalPayoutReady)
        {
            var p = MatchRewardsNet.LocalLastPayout;
            if (rewardsText) rewardsText.text =
                $"XP +{p.xp}  |  MCC +{p.mcc}  {(p.isWin ? "(WIN)" : "(LOSS)")}";
            if (totalsText) totalsText.text =
                $"Totals:  W {p.newWins}  |  L {p.newLosses}  |  XP {p.newXP}  |  MCC {p.newMCC}";
        }
        else
        {
            if (rewardsText) rewardsText.text = "Rewards pending...";
        }

        if (continueButton) continueButton.interactable = true;
    }

    private void HideBanner()
    {
        if (root) root.SetActive(false);
    }

    private Transform FindHud(string name)
    {
        var go = GameObject.Find(name);
        return go ? go.transform : null;
    }

    private void ApplyFallback(string name, string title, Sprite icon, Sprite frame)
    {
        if (playerNameText) playerNameText.text = $"{name} Wins!";
        if (titleText)      titleText.text      = title;

        if (frameImage)
        {
            frameImage.sprite = frame ? frame : fallbackFrame;
            frameImage.preserveAspect = true;
            frameImage.enabled = frameImage.sprite != null;
            frameImage.color = Color.white;
        }
        if (iconImage)
        {
            iconImage.sprite = icon ? icon : fallbackIcon;
            iconImage.preserveAspect = true;
            iconImage.enabled = iconImage.sprite != null;
            iconImage.color = Color.white;
        }
    }

    // Copy winner name/title/icon/frame from a HUD group into the center banner.
    // Looks anywhere in the subtree (inactive included). Returns true if we applied anything.
    private bool ApplyFromHudRobust(Transform hudRoot, string fbName, string fbTitle, Sprite fbIcon, Sprite fbFrame)
    {
        string nameText  = fbName;
        string titleStr  = fbTitle;
        Sprite iconSpr   = fbIcon   ? fbIcon  : fallbackIcon;
        Sprite frameSpr  = fbFrame  ? fbFrame : fallbackFrame;

        if (!hudRoot) return false;

        TMP_Text[] texts = hudRoot.GetComponentsInChildren<TMP_Text>(true);
        Image[]    imgs  = hudRoot.GetComponentsInChildren<Image>(true);

        TMP_Text name = null, title = null;
        Image icon = null, frame = null;

        foreach (var t in texts)
        {
            if (!t) continue;
            if (!name  && t.gameObject.name == HUD_NAME)  name  = t;
            if (!title && t.gameObject.name == HUD_TITLE) title = t;
            if (name && title) break;
        }
        foreach (var im in imgs)
        {
            if (!im) continue;
            if (!icon  && im.gameObject.name == HUD_ICON)  icon  = im;
            if (!frame && im.gameObject.name == HUD_FRAME) frame = im;
            if (icon && frame) break;
        }

        if (name  && !string.IsNullOrWhiteSpace(name.text))   nameText = name.text;
        if (title && !string.IsNullOrWhiteSpace(title.text))  titleStr = title.text;
        if (icon  && icon.sprite)                             iconSpr  = icon.sprite;
        if (frame && frame.sprite)                            frameSpr = frame.sprite;

        if (playerNameText) playerNameText.text = $"{nameText} Wins!";
        if (titleText)      titleText.text      = titleStr;

        if (frameImage)
        {
            frameImage.sprite = frameSpr;
            frameImage.preserveAspect = true;
            frameImage.enabled = frameImage.sprite != null;
            frameImage.color = Color.white;
        }
        if (iconImage)
        {
            iconImage.sprite = iconSpr;
            iconImage.preserveAspect = true;
            iconImage.enabled = iconImage.sprite != null; // hide the white square if none
            iconImage.color = Color.white;
        }

        return (iconSpr != null) || (frameSpr != null) || !string.IsNullOrEmpty(nameText) || !string.IsNullOrEmpty(titleStr);
    }
}
