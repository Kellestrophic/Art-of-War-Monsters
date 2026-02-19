using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IconCosmeticButton : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image lockOverlay;
    [SerializeField] private TMP_Text priceText; // optional (for premium)

    private string iconId;
    private bool isUnlocked;
    private bool isPremium;
    private float priceUSD;

    private System.Action onPremiumClick;

    // ─────────────────────────────────────────────
    // Existing-compatible setup (FREE / UNLOCKED)
    // ─────────────────────────────────────────────
    public void Setup(string id, Sprite iconSprite, bool unlocked)
    {
        iconId = id;
        isUnlocked = unlocked;
        isPremium = false;

        iconImage.sprite = iconSprite;
        lockOverlay.gameObject.SetActive(!unlocked);

        if (priceText) priceText.gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────
    // NEW: Premium locked setup
    // ─────────────────────────────────────────────
    public void SetupPremium(
        string id,
        Sprite iconSprite,
        float usdPrice,
        System.Action onClickPurchase
    )
    {
        iconId = id;
        isUnlocked = false;
        isPremium = true;
        priceUSD = usdPrice;
        onPremiumClick = onClickPurchase;

        iconImage.sprite = iconSprite;
        lockOverlay.gameObject.SetActive(true);

        if (priceText)
        {
            priceText.gameObject.SetActive(true);
            priceText.text = $"${priceUSD:0.00}";
        }
    }

    // ─────────────────────────────────────────────
    // Click Handling
    // ─────────────────────────────────────────────
    public async void OnClick()
    {
        // 🔓 Normal unlocked behavior (UNCHANGED)
        if (isUnlocked)
        {
            var profile = ActiveProfileStore.Instance?.CurrentProfile;
            if (profile == null) return;

            profile.activeIcon = iconId;

            await ProfileUploader.UpdateActiveIcon(profile.wallet, iconId);
            ProfileUIRenderer.Instance.RefreshUI();

            Debug.Log("[Icon Select] Active icon set to " + iconId);
            return;
        }

        // 💰 Premium locked → start purchase flow
        if (isPremium)
        {
            Debug.Log("[Icon Select] Premium icon clicked: " + iconId);
            onPremiumClick?.Invoke();
            return;
        }

        // 🔒 Locked (non-premium)
        Debug.Log("Icon locked: " + iconId);
    }
}
