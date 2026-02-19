using UnityEngine;

public class RuntimeProfileHolder : MonoBehaviour
{
    // === Singleton ===
    public static RuntimeProfileHolder Instance { get; private set; }

    // === The current active profile for the entire game ===
public NewProfileData ActiveProfile
{
    get
    {
        return ActiveProfileStore.Instance != null
            ? ActiveProfileStore.Instance.CurrentProfile
            : pendingProfile;
    }
}


    // === Used if a profile loads BEFORE this object exists (common in WebGL) ===
    private static NewProfileData pendingProfile = null;

  private void Awake()
{
    // Singleton
    if (Instance != null && Instance != this)
    {
        Destroy(gameObject);
        return;
    }

    Instance = this;
    DontDestroyOnLoad(gameObject);

    // If we had a profile loaded before this object existed,
    // forward it to ActiveProfileStore now
    if (pendingProfile != null && ActiveProfileStore.Instance != null)
    {
        ActiveProfileStore.Instance.SetProfile(pendingProfile);
        pendingProfile = null;
        Debug.Log("[RuntimeProfileHolder] Pending profile forwarded on Awake.");
    }
}


private void SaveActiveToPrefs()
{
    if (ActiveProfile == null) return;
    string json = JsonUtility.ToJson(ActiveProfile);
    PlayerPrefs.SetString("ACTIVE_PROFILE_JSON", json);
    PlayerPrefs.Save();
}

public static void ClearPending()
{
    pendingProfile = null;
    PlayerPrefs.DeleteKey("PENDING_PROFILE_JSON");
    PlayerPrefs.Save();
    Debug.Log("[RuntimeProfileHolder] Pending profile cleared.");
}

    // ============================================================
    //  PUBLIC API
    // ============================================================

    /// <summary>
    /// Fully sets the active profile used by all menus + scenes.
    /// Safe to call before or after this singleton exists.
    /// </summary>
   public static void SetProfile(NewProfileData profile)
{
    if (profile == null)
    {
        Debug.LogWarning("[RuntimeProfileHolder] Tried to SetProfile(NULL). Ignored.");
        return;
    }

    if (ActiveProfileStore.Instance != null)
    {
        ActiveProfileStore.Instance.SetProfile(profile);
        Debug.Log("[RuntimeProfileHolder] Forwarded profile to ActiveProfileStore.");
    }
    else
    {
        pendingProfile = profile;
        Debug.Log("[RuntimeProfileHolder] Stored profile as pending.");
    }
}


    /// <summary>
    /// Convenience: Returns current profile safely (avoids null errors).
    /// </summary>
    public static NewProfileData GetProfile()
    {
        if (Instance != null && Instance.ActiveProfile != null)
            return Instance.ActiveProfile;

        if (pendingProfile != null)
            return pendingProfile;

        Debug.LogWarning("[RuntimeProfileHolder] No ActiveProfile or pending profile available.");
        return null;
    }

    /// <summary>
    /// Updates a single field (icon, frame, name, title, etc.)
    /// Then propagates to UI & Firebase.
    /// </summary>
    public static void UpdateActiveProfileField()
    {
        if (Instance != null)
        {
            // This is your new flow: UI auto-updates after any field change
            // FirebaseRestManager calls will be made from ProfileUIRenderer or CreateProfileManager
            Debug.Log("[RuntimeProfileHolder] Profile updated → UI should refresh now.");
        }
    }
}
