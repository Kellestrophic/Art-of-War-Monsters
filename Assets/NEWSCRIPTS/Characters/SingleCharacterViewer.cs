using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SingleCharacterViewer : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private CharacterLibrary library;

    [Header("UI")]
    [SerializeField] private Image icon;            // big square image
    [SerializeField] private TMP_Text nameText;     // "Dracula"
    [SerializeField] private TMP_Text priceText;    // "100 EPIC"
    [SerializeField] private GameObject lockedGroup;   // contains price + Purchase button
    [SerializeField] private GameObject unlockedGroup; // contains Select button
    [SerializeField] private Button purchaseButton;
    [SerializeField] private Button selectButton;
    [SerializeField] private Button nextButton;     // cycles to next
    [SerializeField] private Button prevButton;     // (optional) leave null if you only want Next

    public string SelectedKey { get; private set; } = "dracula";

    private int index = 0;

    private void Awake()
    {
        if (nextButton) nextButton.onClick.AddListener(Next);
        if (prevButton) prevButton.onClick.AddListener(Prev);
        if (purchaseButton) purchaseButton.onClick.AddListener(Purchase);
        if (selectButton) selectButton.onClick.AddListener(SelectCurrent);
    }

    private void OnEnable()
    {
        // start on SelectedKey if it exists, else first entry
        if (library && library.characters.Count > 0)
        {
            var start = library.characters.FindIndex(c => c != null && c.key == SelectedKey);
            index = Mathf.Clamp(start < 0 ? 0 : start, 0, library.characters.Count - 1);
        }
        Refresh();
    }

    private void Next()
    {
        if (library == null || library.characters.Count == 0) return;
        index = (index + 1) % library.characters.Count;
        Refresh();
    }

    private void Prev()
    {
        if (library == null || library.characters.Count == 0) return;
        index = (index - 1 + library.characters.Count) % library.characters.Count;
        Refresh();
    }

    private void Refresh()
    {
        if (library == null || library.characters.Count == 0) return;

        var def = library.characters[index];
        if (def == null) return;

        // icon + name
        if (icon) { icon.sprite = def.icon; icon.preserveAspect = true; }
        if (nameText) nameText.text = def.displayName;

        // locked vs unlocked UI
        bool unlocked = IsUnlocked(def.key);
        if (lockedGroup)   lockedGroup.SetActive(!unlocked);
        if (unlockedGroup) unlockedGroup.SetActive(unlocked);

        if (priceText) priceText.text = $"{def.epicCost} EPIC";

        // update selected key (for external readers if you want)
        if (unlocked) SelectedKey = def.key;
    }

    private void SelectCurrent()
    {
        var def = library.characters[index];
        if (def == null) return;
        if (!IsUnlocked(def.key)) return;

        SelectedKey = def.key;
        // If you want to pass to your match system immediately:
        // MatchContext.LocalSelectedCharacter = SelectedKey;
        // (or close panel, etc.)
    }

    private void Purchase()
    {
        var def = library.characters[index];
        if (def == null) return;

        // TODO: hook to EPIC spend later.
        Debug.Log($"[Viewer] Purchase '{def.key}' for {def.epicCost} EPIC (not live yet).");
        // For testing you can simulate unlock:
        // ForceUnlock(def.key); Refresh();
    }

    // TODAY: only Dracula unlocked
    private bool IsUnlocked(string key) => key == "dracula";

    // Example helper when currency is live:
    // private void ForceUnlock(string key) { /* add to profile + save */ }
}
