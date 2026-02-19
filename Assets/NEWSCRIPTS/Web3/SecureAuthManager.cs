using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class SecureAuthManager : MonoBehaviour
{
    public static SecureAuthManager Instance;

    [SerializeField] private string serverBase = "https://mss-payout.onrender.com";

    private string jwtToken;

    private void Awake()
    {
        Instance = this;
    }

    public string GetJwt() => jwtToken;

    // STEP 1 — Request nonce
    public IEnumerator RequestNonce(string wallet, System.Action<string> callback)
    {
        string url = serverBase + "/auth/nonce";

        string json = "{\"wallet\":\"" + wallet + "\"}";
        var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Nonce request failed: " + req.downloadHandler.text);
            callback?.Invoke(null);
            yield break;
        }

        var response = JsonUtility.FromJson<NonceResponse>(req.downloadHandler.text);
        callback?.Invoke(response.nonce);
    }

    // STEP 2 — Verify signed message
    public IEnumerator VerifySignature(string wallet, string nonce, string signatureBase58, System.Action<bool> callback)
    {
        string url = serverBase + "/auth/verify";

        string json =
            "{\"wallet\":\"" + wallet +
            "\",\"nonce\":\"" + nonce +
            "\",\"signatureBase58\":\"" + signatureBase58 + "\"}";

        var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Verify failed: " + req.downloadHandler.text);
            callback?.Invoke(false);
            yield break;
        }

        var response = JsonUtility.FromJson<VerifyResponse>(req.downloadHandler.text);
        jwtToken = response.token;

        PlayerPrefs.SetString("authToken", jwtToken);
        PlayerPrefs.Save();

        callback?.Invoke(true);
    }

    [System.Serializable]
    private class NonceResponse
    {
        public bool ok;
        public string nonce;
    }

    [System.Serializable]
    private class VerifyResponse
    {
        public bool ok;
        public string token;
    }
}
