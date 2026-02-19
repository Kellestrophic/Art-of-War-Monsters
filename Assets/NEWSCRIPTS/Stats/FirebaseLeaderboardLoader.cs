using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

public static class FirebaseLeaderboardLoader
{
    private static readonly string serverBase = "https://mss-payout.onrender.com";

    public static async Task<List<LeaderboardRowData>> LoadLeaderboardRowsAsync()
    {
        string url = serverBase.TrimEnd('/') + "/leaderboard";

        UnityWebRequest req = UnityWebRequest.Get(url);

        var op = req.SendWebRequest();
        while (!op.isDone)
            await Task.Yield();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[Leaderboard] Server error: " + req.responseCode);
            Debug.LogError(req.downloadHandler.text);
            return new List<LeaderboardRowData>();
        }

        string json = req.downloadHandler.text;

        LeaderboardResponse resp =
            JsonUtility.FromJson<LeaderboardResponse>(json);

        if (resp == null || !resp.ok || resp.rows == null)
        {
            Debug.LogError("[Leaderboard] Invalid response");
            return new List<LeaderboardRowData>();
        }

        return resp.rows;
    }

    // =============================
    // SERVER RESPONSE DTO
    // =============================
    [System.Serializable]
    private class LeaderboardResponse
    {
        public bool ok;
        public List<LeaderboardRowData> rows;
    }
}
