using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;

public struct PayoutResult : INetworkSerializable
{
    public bool isWin;
    public int xp;
    public int mcc;
    public int newWins;
    public int newLosses;
    public int newXP;
    public int newMCC;

    public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
    {
        s.SerializeValue(ref isWin);
        s.SerializeValue(ref xp);
        s.SerializeValue(ref mcc);
        s.SerializeValue(ref newWins);
        s.SerializeValue(ref newLosses);
        s.SerializeValue(ref newXP);
        s.SerializeValue(ref newMCC);
    }
}

public class MatchRewardsNet : NetworkBehaviour
{
    public static bool LocalPayoutReady { get; private set; }
    public static PayoutResult LocalLastPayout;

    private void OnEnable()
    {
        if (MatchStateNet.Instance != null)
            MatchStateNet.Instance.Phase.OnValueChanged += OnPhaseChanged;
    }

    private void OnDisable()
    {
        if (MatchStateNet.Instance != null)
            MatchStateNet.Instance.Phase.OnValueChanged -= OnPhaseChanged;
    }

    private void OnPhaseChanged(MatchPhase oldP, MatchPhase newP)
    {
        if (!IsServer) return;
        if (newP != MatchPhase.Ended) return;

        _ = ServerSendPayoutsAsync();
    }

    private async Task ServerSendPayoutsAsync()
    {
        // 1) Collect participants (clientId, wallet)
        var players = new List<(ulong clientId, string wallet)>();
        foreach (var kv in NetworkManager.Singleton.ConnectedClients)
        {
            var po = kv.Value?.PlayerObject;
            var id = po ? po.GetComponent<PlayerIdentityNet>() : null;
            if (!id) continue;

            string wallet = id.WalletString;
            if (string.IsNullOrWhiteSpace(wallet)) wallet = $"unset-{kv.Key}";
            players.Add((kv.Key, wallet));
        }

        // 2) Winner wallet (by clientId)
        string winnerWallet = "";
        {
            ulong wcid = MatchStateNet.Instance.WinnerClientId.Value;
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].clientId == wcid)
                {
                    winnerWallet = players[i].wallet;
                    break;
                }
            }
        }

        // 3) Match id
        string matchId = $"arena-{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        // 4) AFK flag from server watchdog
        bool disableMcc = InactivityManagerNet.Instance != null &&
                          InactivityManagerNet.Instance.MCCDisabledThisMatch;

        // 5) Call backend (HTTPS)
        List<SimpleHttpFunctionsClient.BackendResult> backendResults =
            await SimpleHttpFunctionsClient.CallAwardMatchPayout(
                matchId, winnerWallet, players, disableMcc
            );

        // 6) Fan out per-client results
        foreach (var r in backendResults)
        {
            var send = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { r.clientId } }
            };
            ApplyPayoutClientRpc(r.result, send);
        }
    }

    [ClientRpc]
    private void ApplyPayoutClientRpc(PayoutResult payout, ClientRpcParams send = default)
    {
        LocalLastPayout = payout;
        LocalPayoutReady = true;
    }
}
