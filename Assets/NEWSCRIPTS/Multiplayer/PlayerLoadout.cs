using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerLoadout : NetworkBehaviour
{
    // Small fixed string to sync efficiently
    public NetworkVariable<FixedString64Bytes> SelectedCharacter = new(
        new FixedString64Bytes("dracula"),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        if (IsOwner && IsClient)
        {
            // Send the locally chosen key up to the server
            SubmitCharacterServerRpc(MatchContext.LocalSelectedCharacter);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitCharacterServerRpc(string key, ServerRpcParams rpcParams = default)
    {
        // Optional: validate key against a whitelist/server library
        SelectedCharacter.Value = new FixedString64Bytes(key ?? "dracula");
    }
}
