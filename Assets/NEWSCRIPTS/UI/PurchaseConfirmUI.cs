using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PurchaseConfirmUI : MonoBehaviour
{
    public static PurchaseConfirmUI Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject root;
    [SerializeField] private Image iconImage;      // ← ADD THIS
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text errorText;

    private string pendingItemId;

    private void Awake()
    {
        Instance = this;
        root.SetActive(false);
    }

    // ------------------------------------------------------------
    // OPEN CONFIRMATION
    // ------------------------------------------------------------
   public void Open(
    string itemId,
    string displayName,
    float priceUSD,
    Sprite iconSprite = null
)
{
    // ✅ THIS is what the server expects ("dracula_icon", "default_icon", etc.)
    pendingItemId = itemId;

    if (titleText)
        titleText.text = displayName;

    if (priceText)
        priceText.text = $"Price: ${priceUSD:0.00} USD";

    // 🔹 ICON HANDLING
    if (iconImage)
    {
        if (iconSprite != null)
        {
            iconImage.gameObject.SetActive(true);
            iconImage.sprite = iconSprite;
        }
        else
        {
            iconImage.gameObject.SetActive(false);
        }
    }

    if (errorText)
        errorText.text = "";

    root.SetActive(true);
}


    // ------------------------------------------------------------
    // BUTTON CALLBACKS
    // ------------------------------------------------------------
    public void OnConfirmPurchase()
    {
        root.SetActive(false);
        PhantomBridgeHandler.Instance.StartPurchaseIcon(pendingItemId);
    }

    public void OnCancel()
    {
        root.SetActive(false);
    }

    // ------------------------------------------------------------
    // ERROR DISPLAY
    // ------------------------------------------------------------
    public void OnPurchaseError(string msg)
    {
        if (errorText)
            errorText.text = msg;

        root.SetActive(true);
    }
}
