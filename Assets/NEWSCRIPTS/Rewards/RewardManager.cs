// Assets/NEWSCRIPTS/Rewards/RewardManager.cs
using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class RewardManager : MonoBehaviour
{
    [Header("Server")]
    [Tooltip("Set to your payout server endpoint (https://xxxx.onrender.com/withdraw or ngrok /payout)")]
    public string ngrokEndpoint = "https://a6900b2ba6af.ngrok-free.app";

    [Header("Refs")]
    public RewardCalculator calculator;

    [Header("Wallet")]
    [Tooltip("Set from your Phantom bridge at login")]
    public string connectedWallet;

    // ✅ Track if the last payout request succeeded
    public bool LastRequestSuccess { get; private set; }
    public string LastResponseText { get; private set; }

    public IEnumerator SendMSSReward(int amount)
    {
        LastRequestSuccess = false;
        LastResponseText = "";

        if (string.IsNullOrWhiteSpace(connectedWallet))
        {
            Debug.LogError("[MSS] ❌ No wallet connected!");
            yield break;
        }

        if (!ngrokEndpoint.StartsWith("http"))
        {
            Debug.LogError("[MSS] ❌ Invalid endpoint: " + ngrokEndpoint);
            yield break;
        }

        // ✅ Optional minimum enforcement
        if (amount < 100)
        {
            Debug.Log($"[MSS] Minimum threshold not met: {amount} < 100 MSS");
            yield break;
        }

        var payload = JsonUtility.ToJson(new RewardPayload { wallet = connectedWallet, amount = amount });
        using (var req = new UnityWebRequest(ngrokEndpoint, UnityWebRequest.kHttpVerbPOST))
        {
            var bodyRaw = System.Text.Encoding.UTF8.GetBytes(payload);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            Debug.Log($"[MSS] Sending payout request → {ngrokEndpoint}");
            yield return req.SendWebRequest();

            LastResponseText = req.downloadHandler.text;

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[MSS] ✅ Payout request ok: " + LastResponseText);
                LastRequestSuccess = true;
            }
            else
            {
                Debug.LogError("[MSS] ❌ Payout failed: " + req.error + "\n" + LastResponseText);
                LastRequestSuccess = false;
            }
        }
    }

    [System.Serializable]
    public class RewardPayload { public string wallet; public int amount; }
}
