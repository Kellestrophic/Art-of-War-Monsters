using UnityEngine;
using TMPro;

public class WithdrawOverlayUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject overlayPanel;
    [SerializeField] private TMP_Text messageText;

    [Header("Fee Display")]
    [SerializeField, Range(0f, 0.5f)]
    private float devFeePercent = 0.05f; // 5%

    public static WithdrawOverlayUI Instance;

    private void Awake()
    {
        Instance = this;
        if (overlayPanel != null)
            overlayPanel.SetActive(false);
    }

    private void Start()
    {
        if (overlayPanel != null)
            overlayPanel.SetActive(false);
    }

    // =====================================
    // WITHDRAW FLOW
    // =====================================
    public void OnWithdrawStarted(int amountUi)
    {
        int fee = Mathf.FloorToInt(amountUi * devFeePercent);
        int net = amountUi - fee;

        ShowOverlay(
            $"Withdrawing {amountUi} MCC\n" +
            $"Dev Fee (5%): {fee} MCC\n" +
            $"You Receive: {net} MCC"
        );
    }

    public void OnWithdrawSuccess(string signature)
    {
        Debug.Log("[WithdrawOverlayUI] Success, hiding overlay. Sig: " + signature);
        HideOverlay();
    }

    public void OnWithdrawError(string error)
    {
        Debug.Log("[WithdrawOverlayUI] Error, hiding overlay. Error: " + error);
        HideOverlay();
    }

    // =====================================
    // INTERNAL UI
    // =====================================
    private void ShowOverlay(string msg)
    {
        if (overlayPanel != null)
            overlayPanel.SetActive(true);

        if (messageText != null)
            messageText.text = msg;
    }

    private void HideOverlay()
    {
        if (overlayPanel != null)
            overlayPanel.SetActive(false);
    }
}
