using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Threading.Tasks;

public class TitleDropdownSelector : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Dropdown dropdown;

    private CosmeticLibrary cosmeticLib;
    private NewProfileData profile;

    // IDs matching dropdown indices
    private readonly List<string> titleIds = new();

    private bool isPopulating = false;

    void Awake()
    {
        if (!dropdown)
            dropdown = GetComponent<TMP_Dropdown>();

        cosmeticLib = Resources.Load<CosmeticLibrary>("CosmeticLibrary");
    }

    void OnEnable()
    {
        ActiveProfileStore.Instance.OnProfileChanged += RefreshDropdown;
        RefreshDropdown(ActiveProfileStore.Instance.CurrentProfile);
    }

    void OnDisable()
    {
        ActiveProfileStore.Instance.OnProfileChanged -= RefreshDropdown;
    }

    // --------------------------------------------------------
    // BUILD DROPDOWN JUST LIKE ICON GRID BUILDER
    // --------------------------------------------------------
    private void RefreshDropdown(NewProfileData p)
    {
        if (p == null || cosmeticLib == null) return;

        profile = p;
        isPopulating = true;

        dropdown.ClearOptions();
        titleIds.Clear();

        List<string> labels = new();

        // EXACTLY LIKE ICONS — iterate whole cosmetic list
        foreach (var item in cosmeticLib.items)
        {
            if (item.type != CosmeticType.Title) 
                continue;

            // only unlocked titles
            if (!profile.unlockedCosmetics.Contains(item.id))
                continue;

            // add to dropdown
            titleIds.Add(item.id);
            labels.Add(item.displayName);
        }

        dropdown.AddOptions(labels);

        // pick currently active title
        int index = Mathf.Max(0, titleIds.IndexOf(profile.activeTitle));
        dropdown.value = index;
        dropdown.RefreshShownValue();

        isPopulating = false;
    }

    // --------------------------------------------------------
    // ON USER SELECTION — EXACT MIRROR OF ICON SELECT
    // --------------------------------------------------------
    public async void OnDropdownChanged(int index)
    {
        if (isPopulating) return;
        if (index < 0 || index >= titleIds.Count) return;

        string chosenId = titleIds[index];
        if (profile.activeTitle == chosenId)
            return;

        profile.activeTitle = chosenId;

        // SAVE → FIREBASE
        await ProfileUploader.UpdateActiveTitle(profile.wallet, chosenId);

        // APPLY → GAME STATE
        ActiveProfileStore.Instance.SetProfile(profile);

        Debug.Log("[TitleDropdown] Selected Title: " + chosenId);
    }
}
