using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Text;
using System.Threading.Tasks;

public class WalletLoginManager : MonoBehaviour
{
    public static WalletLoginManager Instance { get; private set; }

    [Header("Scene Names")]
    [SerializeField] private string mainMenuScene = "Main_Menu";
    [SerializeField] private string createProfileScene = "Create_Profile_Screen";

    [Header("Auth Server")]
    [SerializeField] private string serverBase = "https://mss-payout.onrender.com";

    private bool isHandlingWallet = false;

    // 🔐 Auth state
    private bool _authSignatureReady = false;
    private string _lastSignature = null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ------------------------------------------------------------
    // BUTTON ENTRY POINT
    // ------------------------------------------------------------
    public void BeginWalletLogin()
    {
        Debug.Log("[WalletLoginManager] BeginWalletLogin triggered.");

#if UNITY_WEBGL && !UNITY_EDITOR
        var bridge = FindFirstObjectByType<PhantomWalletJSBridge>();
        if (bridge)
        {
            Debug.Log("[WalletLoginManager] Calling ConnectWalletFromUnity()");
            bridge.ConnectWalletFromUnity();
        }
        else
        {
            Debug.LogError("[WalletLoginManager] PhantomWalletJSBridge not found!");
        }
#else
        string stored = PlayerPrefs.GetString("walletAddress", "");
        if (!string.IsNullOrWhiteSpace(stored))
        {
            HandleWalletConnection(stored);
            return;
        }

        HandleWalletConnection("EDITOR_FAKE_WALLET_1111111111111");
#endif
    }

    // ------------------------------------------------------------
    // WALLET CONNECTED → AUTH FLOW
    // ------------------------------------------------------------
    public async void HandleWalletConnection(string wallet)
    {
        if (string.IsNullOrWhiteSpace(wallet))
        {
            Debug.LogError("Wallet empty.");
            return;
        }

        if (isHandlingWallet)
            return;

        isHandlingWallet = true;

        try
        {
            Debug.Log("🔑 Wallet connected: " + wallet);

            PlayerPrefs.SetString("walletAddress", wallet);
            PlayerPrefs.Save();

            // =========================
            // 1️⃣ Request Nonce
            // =========================
            string nonce = await RequestNonce(wallet);
            if (string.IsNullOrEmpty(nonce))
                throw new System.Exception("Nonce failed.");

            // =========================
            // 2️⃣ Ask Phantom to sign
            // =========================
            string message =
                $"ArtOfWarMonsters Login\nWallet: {wallet}\nNonce: {nonce}";

            var bridge = FindFirstObjectByType<PhantomWalletJSBridge>();
            if (!bridge)
                throw new System.Exception("PhantomWalletJSBridge missing.");

            bridge.SignMessageFromUnity(message);

            // Wait for JS callback
            while (!_authSignatureReady)
                await Task.Yield();

            _authSignatureReady = false;

            // =========================
            // 3️⃣ Verify Signature
            // =========================
            string jwt = await VerifySignature(wallet, nonce, _lastSignature);
            if (string.IsNullOrEmpty(jwt))
                throw new System.Exception("JWT verify failed.");

            AuthTokenStore.Set(jwt);

            Debug.Log("✅ Authenticated with JWT");

            // =========================
            // 4️⃣ Load Profile
            // =========================
            NewProfileData loaded =
                await ProfileDataLoader.LoadOrCreateProfile(wallet);

            if (loaded != null &&
                !string.IsNullOrWhiteSpace(loaded.playerName))
            {
                RuntimeProfileHolder.SetProfile(loaded);
                ActiveProfileStore.Instance?.SetProfile(loaded);

                await Task.Yield();
                SceneManager.LoadScene(mainMenuScene);
                return;
            }

            await Task.Yield();
            SceneManager.LoadScene(createProfileScene);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("🔥 Auth flow failed: " + ex);
        }
        finally
        {
            isHandlingWallet = false;
        }
    }

    // ------------------------------------------------------------
    // JS CALLBACK → MESSAGE SIGNED
    // ------------------------------------------------------------
    public void OnMessageSignedFromJS(string signature)
    {
        Debug.Log("✍️ Message signed: " + signature);
        _lastSignature = signature;
        _authSignatureReady = true;
    }

    // ------------------------------------------------------------
    // SERVER CALLS
    // ------------------------------------------------------------
    private async Task<string> RequestNonce(string wallet)
    {
        string url = serverBase.TrimEnd('/') + "/auth/nonce";
        string payload = JsonUtility.ToJson(new NonceReq { wallet = wallet });

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        await req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Nonce failed: " + req.downloadHandler.text);
            return null;
        }

        var resp = JsonUtility.FromJson<NonceResp>(req.downloadHandler.text);
        return resp?.nonce;
    }

    private async Task<string> VerifySignature(string wallet, string nonce, string sig)
    {
        string url = serverBase.TrimEnd('/') + "/auth/verify";

        var payload = JsonUtility.ToJson(new VerifyReq
        {
            wallet = wallet,
            nonce = nonce,
            signatureBase58 = sig
        });

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        await req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Verify failed: " + req.downloadHandler.text);
            return null;
        }

        var resp = JsonUtility.FromJson<VerifyResp>(req.downloadHandler.text);
        return resp?.token;
    }

    // ------------------------------------------------------------
    // DTOs
    // ------------------------------------------------------------
    [System.Serializable]
    private class NonceReq { public string wallet; }

    [System.Serializable]
    private class NonceResp { public bool ok; public string nonce; }

    [System.Serializable]
    private class VerifyReq
    {
        public string wallet;
        public string nonce;
        public string signatureBase58;
    }

    [System.Serializable]
    private class VerifyResp
    {
        public bool ok;
        public string token;
    }
}
