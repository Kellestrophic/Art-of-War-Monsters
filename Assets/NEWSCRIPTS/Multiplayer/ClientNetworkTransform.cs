// Assets/NEWSCRIPTS/Multiplayer/ClientNetworkTransform.cs
using Unity.Netcode;
using Unity.Netcode.Components;   // <-- this is required for NetworkTransform

public class ClientNetworkTransform : NetworkTransform
{
    // Make the player owner-authoritative (clients can move themselves)
    protected override bool OnIsServerAuthoritative() => false;
}
