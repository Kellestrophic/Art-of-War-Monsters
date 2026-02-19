using UnityEngine;
using UnityEngine.UI;

public class WalletUIManager : MonoBehaviour
{
    public Button connectWalletButton;
    public Button addWalletButton;

    void Start()
    {
        connectWalletButton.onClick.AddListener(OnConnectWalletClicked);

    }

    void OnConnectWalletClicked()
    {
        Debug.Log("Connect Wallet clicked");
    }

    void OnAddWalletClicked()
    {
        Debug.Log("Add Wallet clicked");
    }
}