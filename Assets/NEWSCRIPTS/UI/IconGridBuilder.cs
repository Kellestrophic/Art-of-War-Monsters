using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class IconGridBuilder : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform gridParent;
    [SerializeField] private GameObject iconCosmeticButtonPrefab;

    [Header("Libraries")]
    [SerializeField] private CosmeticLibrary cosmeticLib;

    private readonly List<GameObject> spawnedButtons = new();

    private void OnEnable()
    {
        RefreshGrid();
        if (StatsTracker.Instance != null)
    StatsTracker.Instance.OnStatsChanged += RefreshGrid;

    }

    // --------------------------------------------------------------
    // MAIN BUILD FUNCTION
    // --------------------------------------------------------------
    public void RefreshGrid()
    {
        if (StatsTracker.Instance == null)
{
    Debug.Log("[IconGridBuilder] No StatsTracker instance.");
    return;
}


        ClearOldButtons();

        var profile = ActiveProfileStore.Instance?.CurrentProfile;
        if (profile == null)
        {
            Debug.LogError("[IconGridBuilder] No profile loaded.");
            return;
        }

        if (cosmeticLib == null)
        {
            Debug.LogError("[IconGridBuilder] No CosmeticLibrary assigned!");
            return;
        }

        List<CosmeticLibrary.CosmeticItem> icons =
            cosmeticLib.GetItemsByType(CosmeticType.Icon);

        foreach (var icon in icons)
        {
            if (icon == null || string.IsNullOrEmpty(icon.id))
                continue;

            SpawnIconButton(icon, profile);
        }
    }

    // --------------------------------------------------------------
    // SPAWN SINGLE BUTTON
    // --------------------------------------------------------------
    private void SpawnIconButton(CosmeticLibrary.CosmeticItem icon, NewProfileData profile)
    {
        GameObject btnObj = Instantiate(iconCosmeticButtonPrefab, gridParent);
        spawnedButtons.Add(btnObj);

        Image img = btnObj.transform.Find("IconImage")?.GetComponent<Image>();
        TMP_Text nameText = btnObj.transform.Find("NameText")?.GetComponent<TMP_Text>();
        GameObject highlight = btnObj.transform.Find("Highlight")?.gameObject;

        bool unlocked = profile.unlockedCosmetics.Contains(icon.id);

        if (img != null)
        {
            img.sprite = icon.sprite;
            img.color = unlocked ? Color.white : new Color(1f, 1f, 1f, 0.35f);
        }

        if (nameText != null)
            nameText.text = icon.displayName;

        if (highlight != null)
            highlight.SetActive(profile.activeIcon == icon.id);

        Button button = btnObj.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError("[IconGridBuilder] Icon button prefab missing Button component.");
            return;
        }

        // --------------------------------------------------------------
        // HOVER PREVIEW
        // --------------------------------------------------------------
        EventTrigger trigger = btnObj.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = btnObj.AddComponent<EventTrigger>();

        trigger.triggers.Clear();

        void AddEvent(EventTriggerType type, System.Action action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }

        AddEvent(EventTriggerType.PointerEnter, () =>
        {
            IconHoverPreviewUI.Instance.Show(
                icon.id,
                icon.sprite,
                icon.displayName,
                unlocked,
                icon.isPremium,
                icon.priceUSD
            );
        });

        AddEvent(EventTriggerType.PointerExit, () =>
        {
            IconHoverPreviewUI.Instance.Hide();
        });

        // --------------------------------------------------------------
        // CLICK LOGIC
        // --------------------------------------------------------------
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            if (unlocked)
            {
                SelectIcon(icon.id);
            }
            else if (icon.isPremium)
            {
                PurchaseConfirmUI.Instance.Open(
                    icon.id,
                    icon.displayName,
                    icon.priceUSD,
                    icon.sprite
                );
            }
            else
            {
                Debug.Log("[IconGridBuilder] Icon locked (free): " + icon.id);
            }
        });
    }

    // --------------------------------------------------------------
    // WHEN PLAYER SELECTS AN ICON
    // --------------------------------------------------------------
    private async void SelectIcon(string iconId)
    {
        NewProfileData profile = ActiveProfileStore.Instance?.CurrentProfile;
        if (profile == null) return;

        if (profile.activeIcon == iconId)
            return;

        profile.activeIcon = iconId;

        await ProfileUploader.UpdateActiveIcon(profile.wallet, iconId);

        ActiveProfileStore.Instance.SetProfile(profile);
        RefreshGrid();
    }

    // --------------------------------------------------------------
    // CLEAN OLD BUTTONS
    // --------------------------------------------------------------
    private void ClearOldButtons()
    {
        foreach (GameObject obj in spawnedButtons)
            if (obj != null)
                Destroy(obj);

        spawnedButtons.Clear();
    }
}
