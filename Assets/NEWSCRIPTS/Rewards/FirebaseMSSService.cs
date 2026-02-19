// Assets/NEWSCRIPTS/Rewards/FirebaseMSSService.cs

using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class FirebaseMSSService : MonoBehaviour
{
    public static FirebaseMSSService Instance { get; private set; }

    private static readonly string serverBase = "https://mss-payout.onrender.com";

    private void Awake()
    {
        Instance = this;
    }

    // ==========================================================
    // 🔐 INTERNAL AUTHED POST
    // ==========================================================
    private IEnumerator PostAuthed(string path, string jsonBody, Action<string> onSuccess = null)
    {
        if (!AuthTokenStore.HasToken)
        {
            Debug.LogWarning("[MSSService] No JWT token available.");
            yield break;
        }

        string url = serverBase.TrimEnd('/') + path;

        using (var req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
            req.downloadHandler = new DownloadHandlerBuffer();

            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", "Bearer " + AuthTokenStore.Jwt);

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[MSSService] HTTP ERROR: " + req.responseCode);
                Debug.LogError(req.downloadHandler.text);
                yield break;
            }

            onSuccess?.Invoke(req.downloadHandler.text);
        }
    }

    // ==========================================================
    // READ BANKED (SERVER VERSION)
    // ==========================================================
    public IEnumerator GetBanked(string wallet, Action<int> onDone)
    {
        string json = JsonUtility.ToJson(new WalletReq { wallet = wallet });

        yield return PostAuthed("/profile/mss/get", json, (resp) =>
        {
            try
            {
                var parsed = JsonUtility.FromJson<GetResp>(resp);
                onDone?.Invoke(parsed.mssBanked);
            }
            catch
            {
                onDone?.Invoke(0);
            }
        });
    }

    // ==========================================================
    // ADD
    // ==========================================================
    public IEnumerator AddToBank(string wallet, int addAmount, Action<int> onDone = null)
    {
        if (addAmount <= 0)
            yield break;

        string json = JsonUtility.ToJson(new ModifyReq
        {
            wallet = wallet,
            amount = addAmount
        });

        yield return PostAuthed("/profile/mss/add", json, (resp) =>
        {
            var parsed = JsonUtility.FromJson<GetResp>(resp);
            UpdateLocal(wallet, parsed.mssBanked);
            onDone?.Invoke(parsed.mssBanked);
        });
    }

    // ==========================================================
    // SUBTRACT
    // ==========================================================
    public IEnumerator SubtractFromBank(string wallet, int subtractAmount, Action<bool> onDone = null)
    {
        if (subtractAmount <= 0)
        {
            onDone?.Invoke(false);
            yield break;
        }

        string json = JsonUtility.ToJson(new ModifyReq
        {
            wallet = wallet,
            amount = subtractAmount
        });

        yield return PostAuthed("/profile/mss/subtract", json, (resp) =>
        {
            var parsed = JsonUtility.FromJson<GetResp>(resp);

            if (!parsed.ok)
            {
                onDone?.Invoke(false);
                return;
            }

            UpdateLocal(wallet, parsed.mssBanked);
            onDone?.Invoke(true);
        });
    }

    // ==========================================================
    // LOCAL PROFILE UPDATE
    // ==========================================================
    private void UpdateLocal(string wallet, int newValue)
    {
        var prof = ActiveProfileStore.Instance?.CurrentProfile;

        if (prof != null && prof.wallet == wallet)
        {
            prof.mssBanked = newValue;
            ActiveProfileStore.Instance.ForceBroadcast();
        }
    }

    // ==========================================================
    // DTOs
    // ==========================================================
    [Serializable]
    private class WalletReq
    {
        public string wallet;
    }

    [Serializable]
    private class ModifyReq
    {
        public string wallet;
        public int amount;
    }

    [Serializable]
    private class GetResp
    {
        public bool ok;
        public int mssBanked;
    }
}
