using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public static class FirebaseNameValidator
{
    private static readonly string serverBase = "https://mss-payout.onrender.com";

    public static async Task<bool> NameExists(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        if (!AuthTokenStore.HasToken)
        {
            Debug.LogWarning("[NameValidator] No JWT token.");
            return false;
        }

        string url = serverBase.TrimEnd('/') + "/profile/check-name";

        string payload = JsonUtility.ToJson(new NameReq
        {
            playerName = name
        });

        using (var req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
            req.downloadHandler = new DownloadHandlerBuffer();

            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", "Bearer " + AuthTokenStore.Jwt);

            await req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[NameValidator] HTTP Error: " + req.responseCode);
                return false;
            }

            var resp = JsonUtility.FromJson<NameResp>(req.downloadHandler.text);

            if (resp == null)
                return false;

            return resp.exists;
        }
    }

    [System.Serializable]
    private class NameReq
    {
        public string playerName;
    }

    [System.Serializable]
    private class NameResp
    {
        public bool ok;
        public bool exists;
    }
}
