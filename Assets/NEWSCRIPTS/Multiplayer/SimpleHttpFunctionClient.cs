using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Linq;

// If you don't have Newtonsoft in your project, Unity's "com.unity.nuget.newtonsoft-json" is included in modern Unity.
// If missing, add it via Package Manager first (search "Newtonsoft").
using Newtonsoft.Json;

public static class SimpleHttpFunctionsClient
{
    public static string AwardEndpoint; // set once at boot — MUST start with https://

    public class BackendResult
    {
        public ulong clientId;
        public PayoutResult result;
    }
public static async Task<List<BackendResult>> CallAwardMatchPayout(
    string matchId,
    string winnerWallet,
    List<(ulong clientId, string wallet)> players,
    bool disableMcc // <-- NEW
)

    {
        if (string.IsNullOrEmpty(AwardEndpoint) || !AwardEndpoint.StartsWith("https://"))
            throw new System.Exception("[SimpleHttpFunctionsClient] AwardEndpoint must be set and start with https://");

         var payload = new
    {
        matchId,
        winnerWallet,
        disableMcc, // <-- NEW
        players = players.Select(p => new { clientId = p.clientId, wallet = p.wallet }).ToArray()
    };

        var json = JsonConvert.SerializeObject(payload);
        using var req = new UnityWebRequest(AwardEndpoint, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        var op = req.SendWebRequest();
        while (!op.isDone) await Task.Yield();

        if (req.result != UnityWebRequest.Result.Success)
            throw new System.Exception($"awardMatchPayout HTTP {req.responseCode}: {req.error}\n{req.downloadHandler.text}");

        var list = JsonConvert.DeserializeObject<List<BackendResult>>(req.downloadHandler.text);
        return list ?? new List<BackendResult>();
    }
}
