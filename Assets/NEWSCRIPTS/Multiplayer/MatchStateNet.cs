using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum MatchPhase : byte { Waiting = 0, Playing = 1, Ended = 2 }

public class MatchStateNet : NetworkBehaviour
{
    public static MatchStateNet Instance { get; private set; }

    [Header("Networked state")]
    public NetworkVariable<MatchPhase> Phase = new(writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<ulong> WinnerClientId = new(writePerm: NetworkVariableWritePermission.Server);

    [Header("Return destination")]
    [SerializeField] private string lobbySceneName = "Lobby_Menu";

    private bool _returning;

    private void Awake() => Instance = this;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Phase.Value = MatchPhase.Playing;
            WinnerClientId.Value = ulong.MaxValue;
        }
        Phase.OnValueChanged += OnPhaseChanged;
    }

    private void OnDestroy()
    {
        Phase.OnValueChanged -= OnPhaseChanged;
        // safety so you never get stuck paused
        Time.timeScale = 1f;
        if (Instance == this) Instance = null;
    }

    private void OnPhaseChanged(MatchPhase oldVal, MatchPhase newVal)
    {
        if (newVal == MatchPhase.Ended) Time.timeScale = 0f;
        if (newVal == MatchPhase.Playing) Time.timeScale = 1f;
    }

    // ====== Declare winner (server) ======
    public void ServerDeclareWinner(ulong winnerClientId)
    {
        if (!IsServer || Phase.Value == MatchPhase.Ended) return;

        WinnerClientId.Value = winnerClientId;
        Phase.Value = MatchPhase.Ended;

        ShowVictoryClientRpc(winnerClientId);
    }

    [ClientRpc] private void ShowVictoryClientRpc(ulong winnerId) { /* UI listens to Phase */ }

    // ====== CONTINUE → return to lobby ======
    [ServerRpc(RequireOwnership = false)]
    public void RequestContinueServerRpc(ServerRpcParams rpc = default)
    {
        if (!IsServer) return;
        if (Phase.Value != MatchPhase.Ended || _returning) return;
        ServerReturnToLobby();
    }

    public void ServerReturnToLobby()
    {
        if (!IsServer || _returning) return;
        _returning = true;

        // unpause everyone locally (extra safety)
        UnpauseAllClientsClientRpc();

        // networked scene change – clients auto-follow to Lobby_Menu
        NetworkManager.Singleton.SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
    }

    [ClientRpc] private void UnpauseAllClientsClientRpc() { Time.timeScale = 1f; }
}
