using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IconHoverPreviewUI : MonoBehaviour
{
    public static IconHoverPreviewUI Instance;

    [Header("UI")]
    [SerializeField] private GameObject root;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text statusText;

    private void Awake()
    {
        Instance = this;
        root.SetActive(false);
    }

    // ------------------------------------------------------------
    // SHOW PREVIEW
    // ------------------------------------------------------------
    public void Show(
        string cosmeticKey,
        Sprite sprite,
        string displayName,
        bool unlocked,
        bool isPremium,
        float priceUSD
    )
    {
        if (iconImage)
            iconImage.sprite = sprite;

        if (nameText)
            nameText.text = displayName;

        if (statusText)
        {
            if (unlocked)
            {
                statusText.text = "Unlocked";
            }
            else if (isPremium)
            {
                statusText.text = $"Premium – ${priceUSD:0.00} USD";
            }
            else
            {
                statusText.text =
                    CosmeticUnlockManager.GetNextUnlockDescription(
    cosmeticKey,
    ActiveProfileStore.Instance.CurrentProfile
);

        }

        root.SetActive(true);
    }}

    // ------------------------------------------------------------
    // HIDE PREVIEW
    // ------------------------------------------------------------
    public void Hide()
    {
        root.SetActive(false);
    }
}
