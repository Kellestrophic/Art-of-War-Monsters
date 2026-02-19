using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;

[DefaultExecutionOrder(-500)]
public class PhantomBridgeHandler : MonoBehaviour
{
    public static PhantomBridgeHandler Instance { get; private set; }

    private bool _withdrawInFlight = false;
    private int _pendingAmountUi = 0;

    private const int MinWithdrawAmount = 500;

    // 🔹 ADDED — PURCHASE STATE
    private bool _purchaseInFlight = false;
    private string _pendingPurchaseItemId = null;

    // 🔐 AUTH STATE
    private bool _authInProgress = false;
    private string _authWallet = null;
    private string _authNonce = null;

    [Header("References")]
    [SerializeField] private WalletLoginManager loginManager;
    [SerializeField] private PhantomWalletJSBridge jsBridge;

    [Header("Server")]
    [SerializeField] private string serverBase = "https://mss-payout.onrender.com";

    [Header("Mode")]
    [Tooltip("If ON, use /withdraw-build (Phantom signs). If OFF, use /withdraw-send (server signs).")]
    [SerializeField] private bool useBuildRoute = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!loginManager)
            loginManager = FindFirstObjectByType<WalletLoginManager>(FindObjectsInactive.Include);

        if (!jsBridge)
            jsBridge = FindFirstObjectByType<PhantomWalletJSBridge>(FindObjectsInactive.Include);
    }

    // ============================================================
    // JS → UNITY CALLBACKS
    // ============================================================

    public void OnWalletConnectedFromJS(string walletAddress)
    {
        Debug.Log("🌐 JS wallet address received: " + walletAddress);

        // Start secure auth handshake (nonce → sign message → verify → store JWT)
        BeginWalletAuth(walletAddress);

        // Keep your existing login flow
        loginManager?.HandleWalletConnection(walletAddress);
    }

    // Called by JS after signMessage returns signature (base58)
    public void OnMessageSignedFromJS(string signatureBase58)
    {
        Debug.Log("✍️ Message signed: " + signatureBase58);

        if (!_authInProgress)
        {
            Debug.LogWarning("[Auth] Received message signature but auth is not in progress.");
            return;
        }

        StartCoroutine(VerifySignature(signatureBase58));
    }

    public void OnTxSuccessFromJS(string signature)
    {
        // 🔹 ADDED — PURCHASE SUCCESS
        if (_purchaseInFlight)
        {
            Debug.Log("✅ Purchase TX Success: " + signature);
            StartCoroutine(VerifyPurchase(signature));
            return;
        }

        Debug.Log("✅ Withdraw TX Success: " + signature);

        _withdrawInFlight = false;

        WithdrawOverlayUI.Instance?.OnWithdrawSuccess(signature);

        var ui = FindFirstObjectByType<WithdrawUIController>(FindObjectsInactive.Include);
        ui?.HandleWithdrawSuccess();

        _pendingAmountUi = 0;
    }

    public void OnTxErrorFromJS(string error)
    {
        // 🔹 ADDED — PURCHASE ERROR
        if (_purchaseInFlight)
        {
            HandlePurchaseError(error);
            return;
        }

        Debug.LogError("❌ Withdraw TX Error: " + error);

        _withdrawInFlight = false;

        WithdrawOverlayUI.Instance?.OnWithdrawError(error);

        var ui = FindFirstObjectByType<WithdrawUIController>(FindObjectsInactive.Include);
        ui?.HandleWithdrawError(error);

        _pendingAmountUi = 0;
    }

    // ============================================================
    // 🔐 AUTH FLOW
    // ============================================================

    public void BeginWalletAuth(string wallet)
    {
        if (_authInProgress)
        {
            Debug.LogWarning("[Auth] Already in progress.");
            return;
        }

        if (string.IsNullOrWhiteSpace(wallet))
        {
            Debug.LogError("[Auth] Wallet is empty, cannot auth.");
            return;
        }

        _authInProgress = true;
        _authWallet = wallet;
        _authNonce = null;

        StartCoroutine(RequestNonce(wallet));
    }

    private IEnumerator RequestNonce(string wallet)
    {
        if (!jsBridge)
            jsBridge = FindFirstObjectByType<PhantomWalletJSBridge>(FindObjectsInactive.Include);

        if (!jsBridge)
        {
            Debug.LogError("[Auth] PhantomWalletJSBridge missing.");
            _authInProgress = false;
            yield break;
        }

        string url = serverBase.TrimEnd('/') + "/auth/nonce";
        string payload = JsonUtility.ToJson(new AuthNonceReq { wallet = wallet });

        using (var req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[Auth] Nonce request failed: " + req.responseCode + " :: " + req.downloadHandler.text);
                _authInProgress = false;
                yield break;
            }

            var resp = JsonUtility.FromJson<AuthNonceResp>(req.downloadHandler.text);
            if (resp == null || !resp.ok || string.IsNullOrEmpty(resp.nonce))
            {
                Debug.LogError("[Auth] Nonce response invalid: " + req.downloadHandler.text);
                _authInProgress = false;
                yield break;
            }

            _authNonce = resp.nonce;

            string message =
                $"ArtOfWarMonsters Login\nWallet: {wallet}\nNonce: {_authNonce}";

            Debug.Log("[Auth] Nonce received. Asking Phantom to sign message...");
            jsBridge.SignMessageFromUnity(message);
        }
    }

    private IEnumerator VerifySignature(string signatureBase58)
    {
        string url = serverBase.TrimEnd('/') + "/auth/verify";

        string payload = JsonUtility.ToJson(new AuthVerifyReq
        {
            wallet = _authWallet,
            nonce = _authNonce,
            signatureBase58 = signatureBase58
        });

        using (var req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[Auth] Verify failed: " + req.responseCode + " :: " + req.downloadHandler.text);
                _authInProgress = false;
                yield break;
            }

            var resp = JsonUtility.FromJson<AuthVerifyResp>(req.downloadHandler.text);
            if (resp == null || !resp.ok || string.IsNullOrEmpty(resp.token))
            {
                Debug.LogError("[Auth] Verify rejected: " + req.downloadHandler.text);
                _authInProgress = false;
                yield break;
            }

            AuthTokenStore.Set(resp.token);
            Debug.Log("🔐 [Auth] JWT stored successfully");

            _authInProgress = false;
        }
    }

    // ============================================================
    // WITHDRAW ENTRY POINT
    // ============================================================

    public void StartWithdraw(int amountUi, string toWallet)
    {
        if (_withdrawInFlight)
        {
            Debug.Log("[Withdraw] Already in progress.");
            return;
        }

        if (string.IsNullOrWhiteSpace(toWallet))
        {
            WithdrawOverlayUI.Instance?.OnWithdrawError("No wallet connected.");
            return;
        }

        if (amountUi < MinWithdrawAmount)
        {
            WithdrawOverlayUI.Instance?.OnWithdrawError($"Minimum withdraw is {MinWithdrawAmount} MCC.");
            return;
        }

        var profile = ActiveProfileStore.Instance?.CurrentProfile;
        if (profile != null && amountUi > profile.mssBanked)
        {
            WithdrawOverlayUI.Instance?.OnWithdrawError("Insufficient MCC.");
            return;
        }

        // 🔐 Must be authed now (server requires JWT)
        if (!AuthTokenStore.HasToken)
        {
            WithdrawOverlayUI.Instance?.OnWithdrawError("Not authenticated. Reconnect wallet.");
            Debug.LogWarning("[Withdraw] No JWT token present. You must auth first.");
            return;
        }

        _withdrawInFlight = true;
        _pendingAmountUi = amountUi;

        StartCoroutine(BeginWithdraw(amountUi, toWallet));
    }

    private IEnumerator BeginWithdraw(int amountUi, string wallet)
    {
        float t = 0f;
        const float timeout = 0.5f;

        while (WithdrawOverlayUI.Instance == null && t < timeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (WithdrawOverlayUI.Instance != null)
            WithdrawOverlayUI.Instance.OnWithdrawStarted(amountUi);
        else
            Debug.LogWarning("[Withdraw] Overlay not found; continuing withdraw without overlay.");

        Debug.Log("[Withdraw] Begin build route: /withdraw-build");
        StartCoroutine(BuildThenSign(amountUi, wallet));
    }

    // ============================================================
    // 🔹 ADDED — PURCHASE ENTRY POINT
    // ============================================================

    public void StartPurchaseIcon(string itemId)
    {
        if (_purchaseInFlight)
        {
            Debug.Log("[Purchase] Already in progress.");
            return;
        }

        var profile = ActiveProfileStore.Instance?.CurrentProfile;
        if (profile == null)
        {
            Debug.LogError("[Purchase] No active profile.");
            return;
        }

        // Purchases should also require auth if you lock them later
        // (currently server doesn't require JWT for purchase endpoints)
        _purchaseInFlight = true;
        _pendingPurchaseItemId = itemId;

        StartCoroutine(BuildThenSignPurchase(itemId, profile.wallet));
    }

    // ============================================================
    // /withdraw-build
    // ============================================================

    private IEnumerator BuildThenSign(int amountUi, string playerWallet)
    {
        string url = serverBase.TrimEnd('/') + "/withdraw-build";
        string payload = JsonUtility.ToJson(new BuildReq { player = playerWallet, amount = amountUi });

        using (var req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            // 🔐 REQUIRED: send JWT
            req.SetRequestHeader("Authorization", "Bearer " + AuthTokenStore.Jwt);

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                OnTxErrorFromJS($"HTTP {req.responseCode} :: {req.downloadHandler.text}");
                yield break;
            }


            var resp = JsonUtility.FromJson<BuildResp>(req.downloadHandler.text);
            if (resp == null || !resp.ok || string.IsNullOrEmpty(resp.tx))
            {
                OnTxErrorFromJS(resp?.error ?? "Invalid /withdraw-build response");
                yield break;
            }

            if (!jsBridge)
                jsBridge = FindFirstObjectByType<PhantomWalletJSBridge>(FindObjectsInactive.Include);

            if (!jsBridge)
            {
                OnTxErrorFromJS("PhantomWalletJSBridge missing.");
                yield break;
            }

            Debug.Log("[Withdraw] Calling JS SignAndSendFromUnity(tx)");
            jsBridge.SignAndSendFromUnity(resp.tx);
        }
    }

    // ============================================================
    // 🔹 ADDED — /purchase-build
    // ============================================================

    private IEnumerator BuildThenSignPurchase(string itemId, string playerWallet)
    {
        string url = serverBase.TrimEnd('/') + "/purchase-build";
        string payload = JsonUtility.ToJson(new PurchaseBuildReq
        {
            player = playerWallet,
            itemId = itemId
        });

        using (var req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                HandlePurchaseError($"HTTP {req.responseCode} :: {req.downloadHandler.text}");
                yield break;
            }

            var resp = JsonUtility.FromJson<PurchaseBuildResp>(req.downloadHandler.text);
            if (resp == null || !resp.ok || string.IsNullOrEmpty(resp.tx))
            {
                HandlePurchaseError(resp?.error ?? "Invalid /purchase-build response");
                yield break;
            }

            jsBridge.SignAndSendFromUnity(resp.tx);
        }
    }

    // ============================================================
    // 🔹 ADDED — VERIFY PURCHASE
    // ============================================================

    private IEnumerator VerifyPurchase(string txSignature)
    {
        string url = serverBase.TrimEnd('/') + "/purchase-verify";

        var profile = ActiveProfileStore.Instance.CurrentProfile;

        var payload = JsonUtility.ToJson(new PurchaseVerifyReq
        {
            player = profile.wallet,
            itemId = _pendingPurchaseItemId,
            tx = txSignature
        });

        using (var req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                HandlePurchaseError("Purchase verification failed.");
                yield break;
            }

            var resp = JsonUtility.FromJson<PurchaseVerifyResp>(req.downloadHandler.text);
            if (!resp.ok)
            {
                HandlePurchaseError(resp.error ?? "Verification rejected.");
                yield break;
            }

            if (!profile.unlockedCosmetics.Contains(_pendingPurchaseItemId))
            {
                profile.unlockedCosmetics.Add(_pendingPurchaseItemId);
                _ = ProfileUploader.SaveUnlockedCosmetics(profile.wallet, profile.unlockedCosmetics);
            }

            var buttons = FindObjectsByType<LevelSelectButton>(FindObjectsSortMode.None);
            foreach (var b in buttons)
                b.RefreshState();

            ProfileUIRenderer.Instance?.RefreshUI();

            Debug.Log("[Purchase] Unlocked: " + _pendingPurchaseItemId);

            _purchaseInFlight = false;
            _pendingPurchaseItemId = null;
        }
    }

    // ============================================================
    // 🔹 ADDED — PURCHASE ERROR HANDLER
    // ============================================================

    private void HandlePurchaseError(string msg)
    {
        Debug.LogError("❌ Purchase error: " + msg);
        _purchaseInFlight = false;
        _pendingPurchaseItemId = null;

        PurchaseConfirmUI.Instance?.OnPurchaseError(msg);
    }

    // ============================================================
    // DTOs
    // ============================================================

    [System.Serializable] private class BuildReq { public string player; public int amount; }
    [System.Serializable] private class BuildResp { public bool ok; public string tx; public string error; public string playerAta; }

    [System.Serializable] private class PurchaseBuildReq { public string player; public string itemId; }
    [System.Serializable] private class PurchaseBuildResp { public bool ok; public string tx; public string error; }
    [System.Serializable] private class PurchaseVerifyReq { public string player; public string itemId; public string tx; }
    [System.Serializable] private class PurchaseVerifyResp { public bool ok; public string error; }

    // 🔐 AUTH DTOs
    [System.Serializable] private class AuthNonceReq { public string wallet; }
    [System.Serializable] private class AuthNonceResp { public bool ok; public string nonce; }

    [System.Serializable] private class AuthVerifyReq
    {
        public string wallet;
        public string nonce;
        public string signatureBase58;
    }

    [System.Serializable] private class AuthVerifyResp
    {
        public bool ok;
        public string token;
    }
}
