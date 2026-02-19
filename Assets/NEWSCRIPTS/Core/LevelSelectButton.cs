using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelSelectButton : MonoBehaviour
{
    [Header("Level Info")]
    public string sceneName;
    public string itemId;        // e.g. "Level2_SkeletonIsle"
    public float priceUSD = 0.99f;
    public bool isFree = false;  // Level 1 = true
[Header("Cinematic")]
public string cinematicSceneName = "Cinematic_Scene";

    [Header("UI")]
    public Button button;
    public TMP_Text label;
    public GameObject lockOverlay;

    private void Start()
    {
        if (button)
            button.onClick.AddListener(OnClick);

        RefreshState();
    }

    // --------------------------------------------------
    // STATE
    // --------------------------------------------------
    private void SetUnlocked(bool unlocked)
    {
        if (lockOverlay)
            lockOverlay.SetActive(!unlocked);

        if (button)
            button.interactable = true; // ALWAYS clickable
    }

 public void RefreshState()
{
    var profile = ActiveProfileStore.Instance?.CurrentProfile;
    if (profile == null)
    {
        gameObject.SetActive(false);
        return;
    }

    bool owned =
        isFree ||
        (profile.unlockedCosmetics != null &&
         profile.unlockedCosmetics.Contains(itemId));

    SetUnlocked(owned);
}


    private void OnClick()
    {
        var profile = ActiveProfileStore.Instance?.CurrentProfile;
        if (profile == null)
            return;

        bool owned =
            isFree ||
            profile.unlockedCosmetics.Contains(itemId);

        if (owned)
        {
            LoadScene();
            return;
        }

        // 🔒 Locked → Purchase
        PurchaseConfirmUI.Instance.Open(
            itemId,
            label ? label.text : sceneName,
            priceUSD,
            null
        );
    }

private void LoadScene()
{
    FindFirstObjectByType<UIPanelManager>()
        .LoadSceneByName(cinematicSceneName); // Level1IntroCinematic
}


}
