using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class FirebaseHelper : MonoBehaviour
{
    [Header("Firestore REST")]
    [SerializeField] private string firestoreBaseUrl; 
    // Example:
    // https://firestore.googleapis.com/v1/projects/art-of-war-monsters/databases/(default)/documents


    [Header("Auth")]
    [TextArea]
    [SerializeField] private string authToken; 
    // Put your Firebase ID token here at runtime (NOT hardcoded for production)

    // Call this from your login flow after you get a fresh token
    public void SetAuthToken(string token)
    {
        authToken = token;
    }

    public async Task SaveStatsToFirebase()
    {
        // ---- IMPORTANT: This must match YOUR system ----
          var p = ActiveProfileStore.Instance?.CurrentProfile;
        // -----------------------------------------------

        if (p == null)
        {
            Debug.LogError("[FirebaseHelper] ❌ No active profile");
            return;
        }

        if (string.IsNullOrEmpty(firestoreBaseUrl))
        {
            Debug.LogError("[FirebaseHelper] ❌ firestoreBaseUrl is empty. Set it in Inspector.");
            return;
        }

        if (string.IsNullOrEmpty(authToken))
        {
            Debug.LogError("[FirebaseHelper] ❌ authToken is empty. Firestore will reject the request.");
            return;
        }

        // Build Firestore PATCH body
        var sb = new StringBuilder();
        sb.Append("{\"fields\":{");

        sb.Append("\"totalXP\":").Append(FirestoreHelpers.Int(p.totalXP)).Append(",");
        sb.Append("\"level\":").Append(FirestoreHelpers.Int(p.level)).Append(",");
        sb.Append("\"enemyKills\":").Append(FirestoreHelpers.IntMap(p.enemyKills)).Append(",");
        sb.Append("\"bossKills\":").Append(FirestoreHelpers.IntMap(p.bossKills)).Append(",");
        sb.Append("\"aiWins\":").Append(FirestoreHelpers.Int(p.aiWins)).Append(",");
        sb.Append("\"multiplayerWins\":").Append(FirestoreHelpers.Int(p.multiplayerWins)).Append(",");
        sb.Append("\"multiplayerLosses\":").Append(FirestoreHelpers.Int(p.multiplayerLosses));

        sb.Append("}}");

        string json = sb.ToString();

        // Firestore document URL + updateMask
        string url =
            $"{firestoreBaseUrl}/profiles/{p.wallet}" +
            "?updateMask.fieldPaths=totalXP" +
            "&updateMask.fieldPaths=level" +
            "&updateMask.fieldPaths=enemyKills" +
            "&updateMask.fieldPaths=bossKills" +
            "&updateMask.fieldPaths=aiWins" +
            "&updateMask.fieldPaths=multiplayerWins" +
            "&updateMask.fieldPaths=multiplayerLosses";

        Debug.Log("[FirebaseHelper] 📤 PATCH stats -> " + url);
        Debug.Log("[FirebaseHelper] BODY: " + json);

        await PatchRawJson(url, json);
    }

    private async Task PatchRawJson(string url, string json)
    {
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (var req = new UnityWebRequest(url, "PATCH"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", "Bearer " + authToken);

            var op = req.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[FirebaseHelper] ❌ PATCH failed: {req.responseCode}\n{req.error}\n{req.downloadHandler.text}");
                return;
            }

            Debug.Log($"[FirebaseHelper] ✅ PATCH success ({req.responseCode})");
            // Optional: Debug.Log(req.downloadHandler.text);
        }
    }
}
