using UnityEngine;

public static class TotalMssRewarder
{
    public static async void AddEarnedMss(int amount)
    {
        var store = ActiveProfileStore.Instance;

        if (store == null || store.CurrentProfile == null)
        {
            Debug.LogError("❌ TotalMssRewarder: missing profile");
            return;
        }

        var p = store.CurrentProfile;

        p.mssBanked += amount;
        p.totalMssEarned += amount;

        Debug.Log($"💰 Added {amount} MSS → Bank:{p.mssBanked}  Total:{p.totalMssEarned}");

        // 🔥 Save both values safely
        await ProfileUploader.UpdatemssBanked(p.wallet, p.mssBanked);
        await ProfileUploader.UpdatetotalMssEarned(p.wallet, p.totalMssEarned);
    }

}
