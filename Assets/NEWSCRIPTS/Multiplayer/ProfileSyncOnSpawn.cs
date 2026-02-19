using System.Collections;
using Unity.Netcode;
using UnityEngine;
#if HAS_FIREBASE
using Firebase.Auth;
#endif

[DefaultExecutionOrder(1000)]
public class ProfileSyncOnSpawn : NetworkBehaviour
{
    [SerializeField] float waitTimeoutSeconds = 6f;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        StartCoroutine(SubmitWhenReady());
    }

    private IEnumerator SubmitWhenReady()
    {
        float deadline = Time.unscaledTime + waitTimeoutSeconds;

        // ✅ FIXED: Get active profile correctly
        NewProfileData profile =
    ActiveProfileStore.Instance != null
        ? ActiveProfileStore.Instance.CurrentProfile
        : null;

while (profile == null && Time.unscaledTime < deadline)
{
    yield return null;

    profile =
        ActiveProfileStore.Instance != null
            ? ActiveProfileStore.Instance.CurrentProfile
            : null;
}


        // Build payload safely
        string display = (profile != null && !string.IsNullOrWhiteSpace(profile.playerName))
            ? profile.playerName
            : $"Player {OwnerClientId}";

        string title = profile?.activeTitle ?? "default_title";
        string icon  = profile?.activeIcon  ?? "default_icon";
        string frame = profile?.activeFrame ?? "bronze_frame";
        int    level = Mathf.Max(1, profile?.level ?? 1);
        string wallet = profile?.wallet ?? $"local-{OwnerClientId}";

        // UID source (Firebase or fallback)
        string uid =
#if HAS_FIREBASE
            FirebaseAuth.DefaultInstance?.CurrentUser?.UserId
#else
            null
#endif
        ;
        if (string.IsNullOrEmpty(uid))
            uid = $"local-{OwnerClientId}";

        // Submit identity to SERVER so both peers see the same values
        var pid = GetComponent<PlayerIdentityNet>();
        if (pid != null)
            pid.SubmitProfileFullServerRpc(display, title, level, icon, frame, uid, wallet);

        // Notify match gate
        var gate = FindFirstObjectByType<FreezeUntilStart>();
        if (gate != null)
            gate.SetReadyServerRpc();
    }
}
