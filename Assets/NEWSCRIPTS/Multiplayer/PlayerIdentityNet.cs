// Assets/NEWSCRIPTS/Multiplayer/PlayerIdentityNet.cs
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class PlayerIdentityNet : NetworkBehaviour
{
    [Header("Owner fallbacks (used only if owner never submits)")]
    [SerializeField] private string fallbackName    = "Player";
    [SerializeField] private string fallbackTitle   = "Adventurer";
    [SerializeField] private int    fallbackLevel   = 1;
    [SerializeField] private string fallbackIconId  = "default";
    [SerializeField] private string fallbackFrameId = "";
    [SerializeField] private string fallbackFirebaseUid = "";
    [SerializeField] private string fallbackWallet       = "";

    // Identity data (server writes)
    private readonly NetworkVariable<FixedString64Bytes> _displayName =
        new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<FixedString64Bytes> _title =
        new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<int> _level =
        new(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<FixedString64Bytes> _iconId =
        new("default", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<FixedString64Bytes> _frameId =
        new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<FixedString128Bytes> _firebaseUid =
        new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<FixedString64Bytes> _wallet =
        new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Readiness flag used by MatchLoadingGate
    public readonly NetworkVariable<bool> IdentityReady =
        new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Expose convenient getters
    public string DisplayNameString => _displayName.Value.ToString();
    public string TitleString       => _title.Value.ToString();
    public int    LevelValue        => _level.Value;
    public string IconIdString      => _iconId.Value.ToString();
    public string FrameIdString     => _frameId.Value.ToString();
    public string FirebaseUidString => _firebaseUid.Value.ToString();
    public string WalletString      => _wallet.Value.ToString();

    // If other code still uses these NVs directly:
    public NetworkVariable<FixedString64Bytes> DisplayName => _displayName;
    public NetworkVariable<FixedString64Bytes> TitleKey    => _title;
    public NetworkVariable<int>                Level       => _level;
    public NetworkVariable<FixedString64Bytes> IconKey     => _iconId;
    public NetworkVariable<FixedString64Bytes> FrameKey    => _frameId;
    public NetworkVariable<FixedString128Bytes> FirebaseUid=> _firebaseUid;
    public NetworkVariable<FixedString64Bytes> Wallet      => _wallet;

    public static System.Action<PlayerIdentityNet> OnChanged;

    public override void OnNetworkSpawn()
    {
        _displayName.OnValueChanged += (_, __) => FireChanged();
        _title      .OnValueChanged += (_, __) => FireChanged();
        _level      .OnValueChanged += (_, __) => FireChanged();
        _iconId     .OnValueChanged += (_, __) => FireChanged();
        _frameId    .OnValueChanged += (_, __) => FireChanged();
        _firebaseUid.OnValueChanged += (_, __) => FireChanged();
        _wallet     .OnValueChanged += (_, __) => FireChanged();

        if (IsServer && string.IsNullOrEmpty(DisplayNameString))
        {
            ApplyProfile(
                fallbackName, fallbackTitle, Mathf.Max(1, fallbackLevel),
                fallbackIconId, fallbackFrameId, fallbackFirebaseUid, fallbackWallet);
        }
    }

   [ServerRpc(RequireOwnership=false)]
public void SubmitProfileFullServerRpc(string name, string title, int level,
                                       string iconId, string frameId,
                                       string firebaseUid, string wallet)
{
    Debug.Log($"[ID] {OwnerClientId} -> {name}/{title}/{iconId}/{frameId}");
    ApplyProfile(name, title, level, iconId, frameId, firebaseUid, wallet);
}


    // Used by MatchLoadingGate
    [ServerRpc(RequireOwnership = false)]
    public void SetReadyServerRpc(bool ready) => IdentityReady.Value = ready;

    private void ApplyProfile(
        string name, string title, int level,
        string iconId, string frameId,
        string firebaseUid, string wallet)
    {
        _displayName.Value = new FixedString64Bytes(string.IsNullOrWhiteSpace(name) ? "Player" : name);
        _title.Value = new FixedString64Bytes(title ?? "");
        _level.Value = Mathf.Max(1, level);
        _iconId.Value = new FixedString64Bytes(string.IsNullOrWhiteSpace(iconId) ? "default" : iconId);
        if (frameId != null) _frameId.Value = new FixedString64Bytes(frameId);
        if (firebaseUid != null) _firebaseUid.Value = new FixedString128Bytes(firebaseUid);
        if (wallet != null) _wallet.Value = new FixedString64Bytes(wallet);
        FireChanged();
        
    }
    

    private void FireChanged() => OnChanged?.Invoke(this);
}
