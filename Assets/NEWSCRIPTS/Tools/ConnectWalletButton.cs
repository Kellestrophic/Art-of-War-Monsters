using UnityEngine;

public class ConnectWalletButton : MonoBehaviour
{
    public void OnClick()
    {
        WalletLoginManager mgr = WalletLoginManager.Instance;

        if (mgr == null)
        {
            Debug.LogError("[ConnectWalletButton] WalletLoginManager.Instance is NULL!");
            return;
        }

        mgr.BeginWalletLogin();
    }
}
