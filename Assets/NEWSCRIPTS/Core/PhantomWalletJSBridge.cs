// Assets/NEWSCRIPTS/Core/PhantomWalletJSBridge.cs
using UnityEngine;
using System.Runtime.InteropServices;

public class PhantomWalletJSBridge : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR

    // CONNECT
    [DllImport("__Internal", EntryPoint = "phantomBridge_connect")]
    private static extern void ConnectPhantomWallet(string gameObjectName, string okMethod, string errMethod);

    // SIGN & SEND TX
    [DllImport("__Internal", EntryPoint = "phantomBridge_signAndSend_v4")]
    private static extern void Phantom_SignAndSendTx(string gameObjectName, string okMethod, string errMethod, string base64Tx);

    // ✅ NEW — SIGN MESSAGE
    [DllImport("__Internal", EntryPoint = "phantomBridge_signMessage")]
    private static extern void Phantom_SignMessage(string gameObjectName, string okMethod, string errMethod, string message);

#else

    // Editor simulation
    private static void ConnectPhantomWallet(string a, string b, string c)
    {
        Debug.Log("🔧 (Editor) Simulated connect");
        var h = FindFirstObjectByType<PhantomBridgeHandler>();
        if (h) h.OnWalletConnectedFromJS("EDITOR_FAKE_WALLET_11111111111111111111111111111111");
    }

    private static void Phantom_SignAndSendTx(string gameObjectName, string okMethod, string errMethod, string base64Tx)
    {
        Debug.Log("🔧 (Editor) Simulated sign+send");
        var h = FindFirstObjectByType<PhantomBridgeHandler>();
        if (h) h.OnTxSuccessFromJS("SIMULATED_SIGNATURE");
    }

    private static void Phantom_SignMessage(string gameObjectName, string okMethod, string errMethod, string message)
    {
        Debug.Log("🔧 (Editor) Simulated signMessage");
        var h = FindFirstObjectByType<PhantomBridgeHandler>();
        if (h) h.OnMessageSignedFromJS("SIMULATED_SIGNATURE_BASE58");
    }

#endif

    // ----------------------------------------------------
    // CONNECT WALLET
    // ----------------------------------------------------
    public void ConnectWalletFromUnity()
    {
        var h = FindFirstObjectByType<PhantomBridgeHandler>();
        var goName = h ? h.gameObject.name : gameObject.name;

        const string okCb  = "OnWalletConnectedFromJS";
        const string errCb = "OnTxErrorFromJS";

        ConnectPhantomWallet(goName, okCb, errCb);
    }

    // ----------------------------------------------------
    // SIGN & SEND TX
    // ----------------------------------------------------
    public void SignAndSendFromUnity(string base64Tx)
    {
        var h = FindFirstObjectByType<PhantomBridgeHandler>();
        var goName = h ? h.gameObject.name : gameObject.name;

        const string okCb  = "OnTxSuccessFromJS";
        const string errCb = "OnTxErrorFromJS";

        Phantom_SignAndSendTx(goName, okCb, errCb, base64Tx);
    }

    // ----------------------------------------------------
    // ✅ NEW — SIGN MESSAGE
    // ----------------------------------------------------
    public void SignMessageFromUnity(string message)
    {
        var h = FindFirstObjectByType<PhantomBridgeHandler>();
        var goName = h ? h.gameObject.name : gameObject.name;

        const string okCb  = "OnMessageSignedFromJS";
        const string errCb = "OnTxErrorFromJS";

        Phantom_SignMessage(goName, okCb, errCb, message);
    }
}
