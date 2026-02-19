using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private CharacterLibrary library;

    [Header("UI")]
    [SerializeField] private Transform gridParent;
    [SerializeField] private GameObject characterCardPrefab;
    [SerializeField] private Image selectedIcon;
    [SerializeField] private TMP_Text selectedName;

    public string SelectedKey { get; private set; } = "dracula";

    private readonly List<CardRefs> cards = new();

    private class CardRefs
    {
        public string key;
        public GameObject go;
        public GameObject lockedGroup;
        public GameObject unlockedGroup;
        public Button selectButton;
        public Button purchaseButton;
        public TMP_Text priceText;
        public Image icon;
        public TMP_Text nameText;
        public GameObject selectedHighlight;
    }

    private void OnEnable()
    {
        BuildGrid();
        SelectDefault();
        RefreshSelectedBar();
    }

    private void BuildGrid()
    {
        foreach (Transform c in gridParent) Destroy(c.gameObject);
        cards.Clear();

        foreach (var def in library.characters)
        {
            var go = Instantiate(characterCardPrefab, gridParent);
            var cr = new CardRefs
            {
                key = def.key,
                go = go,
                icon = go.transform.Find("Icon").GetComponent<Image>(),
                nameText = go.transform.Find("NameText").GetComponent<TMP_Text>(),
                lockedGroup = go.transform.Find("LockedGroup").gameObject,
                unlockedGroup = go.transform.Find("UnlockedGroup").gameObject,
                purchaseButton = go.transform.Find("LockedGroup/PurchaseButton").GetComponent<Button>(),
                priceText = go.transform.Find("LockedGroup/PriceText").GetComponent<TMP_Text>(),
                selectButton = go.transform.Find("UnlockedGroup/SelectButton").GetComponent<Button>(),
                selectedHighlight = go.transform.Find("SelectedHighlight").gameObject
            };

            cr.icon.sprite = def.icon;
            cr.nameText.text = def.displayName;
            cr.priceText.text = $"{def.epicCost} EPIC";

            bool unlocked = IsUnlocked(def.key);
            cr.lockedGroup.SetActive(!unlocked);
            cr.unlockedGroup.SetActive(unlocked);
            cr.selectedHighlight.SetActive(false);

            cr.selectButton.onClick.AddListener(() => { SelectedKey = def.key; RefreshSelectionVisuals(); RefreshSelectedBar(); });
            cr.purchaseButton.onClick.AddListener(() => OnPurchase(def.key, def.epicCost));

            cards.Add(cr);
        }

        RefreshSelectionVisuals();
    }

    private void SelectDefault()
    {
        if (!IsUnlocked(SelectedKey))
            SelectedKey = "dracula";
    }

    private void RefreshSelectionVisuals()
    {
        foreach (var c in cards)
            c.selectedHighlight.SetActive(c.key == SelectedKey);
    }

    private void RefreshSelectedBar()
    {
        var def = library.GetByKey(SelectedKey);
if (def != null)
{
    if (selectedIcon) selectedIcon.sprite = def.icon;
    if (selectedName) selectedName.text = def.displayName;
}

    }

    // TODO: wire to your Firebase EPIC when live
    private void OnPurchase(string key, int cost)
    {
        Debug.Log($"[CharacterSelect] Purchase clicked for '{key}' ({cost} EPIC) — currency not live yet.");
    }

    // TEMP: Dracula is unlocked by default
    private bool IsUnlocked(string key) => key == "dracula";
}
