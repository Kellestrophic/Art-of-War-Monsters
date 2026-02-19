// Assets/NEWSCRIPTS/Multiplayer/PlayerCodePanelController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Security.Cryptography;

/// <summary>
/// LOCAL stub flow (UI only):
/// Host:
///   1) Generate Code  → just creates & shows a 12-digit code (NO Ready UI).
///   2) Start Hosting  → confirms host role and shows Ready UI.
/// Client:
///   3) Enter code + Join → validates and shows Ready UI.
/// Both:
///   4) Press "Fight (Ready)" → when both ready, load arena.
/// Back:
///   5) Return to Hub.
/// Swap internals to Relay later; the UI/flow can stay.
/// </summary>
public class PlayerCodePanelController : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────────
    // STEP 0: Panels / Navigation
    // ─────────────────────────────────────────────────────────────────────────────
    [Header("Panels / Navigation")]
    [SerializeField] private MultiplayerHubController hub;   // HubController (recommended)
    [SerializeField] private GameObject panelHub;            // Fallback if 'hub' not set
    [SerializeField] private GameObject panelPlayerCode;

    // ─────────────────────────────────────────────────────────────────────────────
    // STEP 1: Host UI (Generate & Start Hosting)
    // ─────────────────────────────────────────────────────────────────────────────
    [Header("Host UI")]
    [SerializeField] private TMP_Text yourCodeText;          // Shows host's code
    [SerializeField] private Button createCodeButton;        // "Generate Code" (NO Ready toggle)
    [SerializeField] private Button startHostingButton;      // "Start Hosting" (shows Ready UI)
    [SerializeField] private Button copyCodeButton;          // Optional "Copy"
    [SerializeField] private TMP_Text hostStatusText;        // Optional host status

    // ─────────────────────────────────────────────────────────────────────────────
    // STEP 2: Client UI (Join)
    // ─────────────────────────────────────────────────────────────────────────────
    [Header("Client UI")]
    [SerializeField] private TMP_InputField joinCodeInput;   // Enter/paste code here
    [SerializeField] private Button joinButton;              // "Join"
    [SerializeField] private TMP_Text joinStatusText;        // Join status/errors

    // ─────────────────────────────────────────────────────────────────────────────
    // STEP 3: Ready gate (both press "Fight")
    // ─────────────────────────────────────────────────────────────────────────────
    [Header("Ready UI")]
    [SerializeField] private GameObject readyGroup;          // Container we show/hide
    [SerializeField] private Button readyButton;             // "Fight (Ready)"
    [SerializeField] private TMP_Text readyLabel;            // "Fight (Ready)" / "Ready ✓"

    // ─────────────────────────────────────────────────────────────────────────────
    // STEP 4: Back
    // ─────────────────────────────────────────────────────────────────────────────
    [Header("Back")]
    [SerializeField] private Button backButton;              // Back to Hub

    // ─────────────────────────────────────────────────────────────────────────────
    // Local state
    // ─────────────────────────────────────────────────────────────────────────────
    private bool _hostReady = false;
    private bool _clientReady = false;
    private bool _iAmHost = false;
    private bool _iAmClient = false;
    private string _rawCode; // 12 digits, no dashes (UI shows dashed)

    // ─────────────────────────────────────────────────────────────────────────────
    // LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        // Wire buttons
        if (createCodeButton)   createCodeButton.onClick.AddListener(OnGenerateCode);
        if (startHostingButton) startHostingButton.onClick.AddListener(OnStartHosting);
        if (copyCodeButton)     copyCodeButton.onClick.AddListener(CopyToClipboard);

        if (joinButton)         joinButton.onClick.AddListener(OnJoinByCode);
        if (readyButton)        readyButton.onClick.AddListener(OnToggleReady);
        if (backButton)         backButton.onClick.AddListener(OnBackToHub);
    }

    private void OnEnable()
    {
        // RESET VISUAL STATE
        if (readyGroup) readyGroup.SetActive(false); // Hidden until Host starts OR Client joins
        _hostReady = _clientReady = false;
        _iAmHost = _iAmClient = false;
        UpdateReadyLabel(false);
        if (joinStatusText) joinStatusText.text = "";
        if (hostStatusText) hostStatusText.text = "";

        // RESTORE EXISTING CODE (persist across panel swaps)
        if (!string.IsNullOrEmpty(MatchContext.LastJoinCode))
        {
            _rawCode = MatchContext.LastJoinCode;
            if (yourCodeText) yourCodeText.text = FormatCode(_rawCode);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // STEP 1A: HOST — Generate Code (NO Ready toggle here)
    // ─────────────────────────────────────────────────────────────────────────────
    private void OnGenerateCode()
    {
        _rawCode = GenerateNumericCode(12);      // e.g., "584903112776"
        MatchContext.LastJoinCode = _rawCode;    // store globally
        _iAmHost = true;                         // mark role (but not ready)
        _iAmClient = false;

        if (yourCodeText)  yourCodeText.text   = FormatCode(_rawCode);
        if (hostStatusText) hostStatusText.text = "Code generated. Share it with your opponent.";

        // DO NOT show Ready yet. Host must click "Start Hosting".
        if (readyGroup) readyGroup.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // STEP 1B: HOST — Start Hosting (NOW show Ready UI)
    // ─────────────────────────────────────────────────────────────────────────────
    private void OnStartHosting()
    {
        if (string.IsNullOrEmpty(_rawCode))
        {
            if (hostStatusText) hostStatusText.text = "Generate a code first.";
            return;
        }

        _iAmHost = true;   // confirm role
        _iAmClient = false;

        if (hostStatusText) hostStatusText.text = "Hosting. Press Fight when ready.";
        if (readyGroup) readyGroup.SetActive(true);
        UpdateReadyLabel(false);
    }

    private void CopyToClipboard()
    {
        if (string.IsNullOrEmpty(_rawCode)) return;
        GUIUtility.systemCopyBuffer = _rawCode; // raw 12 digits
        if (hostStatusText) hostStatusText.text = "Code copied.";
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // STEP 2: CLIENT — Join by Code (show Ready UI on success)
    // ─────────────────────────────────────────────────────────────────────────────
    private void OnJoinByCode()
    {
        var typed = (joinCodeInput ? joinCodeInput.text.Trim() : "");
        typed = typed.Replace("-", "").Replace(" ", "");

        // 2.1 Validate input
        if (typed.Length != 12 || !IsAllDigits(typed))
        {
            if (joinStatusText) joinStatusText.text = "Enter a 12-digit code.";
            return;
        }
        // 2.2 Ensure a host code exists
        if (string.IsNullOrEmpty(MatchContext.LastJoinCode))
        {
            if (joinStatusText) joinStatusText.text = "No host code yet.";
            return;
        }
        // 2.3 Match input to host code
        if (typed != MatchContext.LastJoinCode)
        {
            if (joinStatusText) joinStatusText.text = "Code not found.";
            return;
        }

        // 2.4 Success → mark as client and reveal Ready UI
        _iAmClient = true;
        _iAmHost = false;

        if (joinStatusText) joinStatusText.text = "Joined! Press Fight when ready.";
        if (readyGroup) readyGroup.SetActive(true);
        UpdateReadyLabel(false);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // STEP 3: READY — Both press "Fight"
    // ─────────────────────────────────────────────────────────────────────────────
    private void OnToggleReady()
    {
        bool nowReady;

        if      (_iAmHost)   { _hostReady   = !_hostReady;   nowReady = _hostReady; }
        else if (_iAmClient) { _clientReady = !_clientReady; nowReady = _clientReady; }
        else
        {
            // Neither role confirmed yet → hint to start the flow properly
            if (joinStatusText) joinStatusText.text = "Join a code or Start Hosting first.";
            if (hostStatusText) hostStatusText.text = "Generate a code, then Start Hosting.";
            return;
        }

        UpdateReadyLabel(nowReady);

        // LOCAL stub: check both flags here.
        // REAL version: move this to server (ReadySync) then NetworkManager.SceneManager.LoadScene(...)
        if (_hostReady && _clientReady)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                MatchContext.ArenaSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }

    private void UpdateReadyLabel(bool isReady)
    {
        if (readyLabel) readyLabel.text = isReady ? "Ready ✓" : "Fight (Ready)";
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // STEP 4: BACK — Return to Hub
    // ─────────────────────────────────────────────────────────────────────────────
    private void OnBackToHub()
    {
        if (hub) { hub.ShowHub(); return; } // preferred

        // Fallback toggles if HubController isn't referenced
        if (panelHub)        panelHub.SetActive(true);
        if (panelPlayerCode) panelPlayerCode.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────────
    private static string GenerateNumericCode(int length)
    {
        var digits = new char[length];
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[length];
        rng.GetBytes(bytes);
        for (int i = 0; i < length; i++)
            digits[i] = (char)('0' + (bytes[i] % 10)); // 0..255 → 0..9
        return new string(digits);
    }

    private static string FormatCode(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        if (raw.Length != 12) return raw;
        return $"{raw.Substring(0,3)}-{raw.Substring(3,3)}-{raw.Substring(6,3)}-{raw.Substring(9,3)}";
    }

    private static bool IsAllDigits(string s)
    {
        for (int i = 0; i < s.Length; i++)
            if (s[i] < '0' || s[i] > '9') return false;
        return true;
    }
}
