using UnityEngine;

public static class AuthTokenStore
{
    private const string KEY = "MSS_JWT_TOKEN";

    public static string Jwt { get; private set; }

    public static bool HasToken => !string.IsNullOrWhiteSpace(Jwt);

    // Call when login/auth succeeds
    public static void Set(string token)
    {
        Jwt = token;

        PlayerPrefs.SetString(KEY, token);
        PlayerPrefs.Save();

        Debug.Log("🔐 AuthTokenStore: JWT saved.");
    }

    // Call on app boot
    public static void Load()
    {
        Jwt = PlayerPrefs.GetString(KEY, "");

        if (!string.IsNullOrWhiteSpace(Jwt))
            Debug.Log("🔐 AuthTokenStore: JWT loaded from PlayerPrefs.");
    }

    // Call on logout
    public static void Clear()
    {
        Jwt = null;

        PlayerPrefs.DeleteKey(KEY);
        PlayerPrefs.Save();

        Debug.Log("🔐 AuthTokenStore: JWT cleared.");
    }
}
