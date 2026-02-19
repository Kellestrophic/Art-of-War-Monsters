using UnityEngine;
using System.IO;

public static class FirebaseConfig
{
    private static string _webApiKey;

    public static string WebApiKey
    {
        get
        {
            if (string.IsNullOrEmpty(_webApiKey))
                LoadConfig();
            return _webApiKey;
        }
    }

    private static void LoadConfig()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "firebase-config.json");

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            FirebaseLocalConfig config = JsonUtility.FromJson<FirebaseLocalConfig>(json);
            _webApiKey = config.webApiKey;
        }
        else
        {
            Debug.LogError("firebase-config.json missing!");
        }
    }
}

[System.Serializable]
public class FirebaseLocalConfig
{
    public string webApiKey;
}
