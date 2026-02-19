using UnityEngine;

public class PayoutToProfileBridge : MonoBehaviour
{
    private bool applied;

    private void Update()
{
    if (applied) return;
    if (!MatchRewardsNet.LocalPayoutReady) return;

    applied = true;
    ApplyPayout();
}

private async void ApplyPayout()
{
    var payout = MatchRewardsNet.LocalLastPayout;

    var profile =
        ActiveProfileStore.Instance != null
            ? ActiveProfileStore.Instance.CurrentProfile
            : null;

    if (profile == null)
    {
        Debug.LogWarning("[PayoutBridge] No active profile.");
        return;
    }

    if (payout.isWin)
        profile.multiplayerWins = payout.newWins;
    else
        profile.multiplayerLosses = payout.newLosses;

    profile.mssBanked = payout.newMCC;

    await ProfileUploader.UpdateMultiplayerWins(profile.wallet, profile.multiplayerWins);
    await ProfileUploader.UpdateMultiplayerLosses(profile.wallet, profile.multiplayerLosses);
    await ProfileUploader.UpdatemssBanked(profile.wallet, profile.mssBanked);

    Debug.Log($"[PayoutBridge] Applied totals: W {profile.multiplayerWins} / L {profile.multiplayerLosses} / MCC {profile.mssBanked}");
}
}
