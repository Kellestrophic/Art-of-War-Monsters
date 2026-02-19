using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class MultiplayerHubController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject panelHub;              // Multiplayer menu root
    [SerializeField] private GameObject panelCharacterSelect;  // Character Select screen
    [SerializeField] private GameObject panelPlayerCode;       // Panel_PlayerCode

    [Header("Hub Buttons")]
    [SerializeField] private Button directMatchButton;         // Direct Match
    [SerializeField] private Button aiMatchButton;             // AI Match (optional)

    [Header("Hooks")]
    [Tooltip("What to call when the player confirms a character for AI Match.")]
    [SerializeField] private UnityEvent onStartAIMatch;        // Assign your existing AI start method here

    private CanvasGroup hubCG;

    private void Awake()
    {
        if (panelHub)
        {
            hubCG = panelHub.GetComponent<CanvasGroup>();
            if (!hubCG) hubCG = panelHub.AddComponent<CanvasGroup>();
        }

        if (directMatchButton)
        {
            directMatchButton.onClick.RemoveAllListeners();
            directMatchButton.onClick.AddListener(OnDirectMatchPressed);
        }
        if (aiMatchButton)
        {
            aiMatchButton.onClick.RemoveAllListeners();
            aiMatchButton.onClick.AddListener(OnAIMatchPressed);
        }

        ShowHub();
    }

    // ===================== Button handlers =====================

    // Direct Match -> Character Select (then Player Code on Select)
    private void OnDirectMatchPressed()
    {
        MatchContext.CurrentPath = MatchContext.Path.Direct;
        ShowCharacterSelect();
    }

    // AI Match -> Character Select (then start AI match on Select)
    private void OnAIMatchPressed()
    {
        MatchContext.CurrentPath = MatchContext.Path.AI;
        ShowCharacterSelect();
    }

    /// Call this from the Character Select screen's "Select" button (single OnClick).
    public void OnCharacterSelectConfirm()
    {
        if (MatchContext.CurrentPath == MatchContext.Path.Direct)
        {
            ShowPlayerCode();                 // Direct path → Player code screen
        }
        else // Path == AI
        {
            onStartAIMatch?.Invoke();         // AI path → fire your existing AI start logic
        }
    }

    // ===================== Panel routers =====================

    public void ShowHub()
    {
        if (panelHub)              panelHub.SetActive(true);
        if (panelCharacterSelect)  panelCharacterSelect.SetActive(false);
        if (panelPlayerCode)       panelPlayerCode.SetActive(false);

        if (hubCG)
        {
            hubCG.alpha = 1f;
            hubCG.interactable = true;
            hubCG.blocksRaycasts = true;
        }
    }

    public void ShowCharacterSelect()
    {
        if (panelHub)             panelHub.SetActive(true);
        if (panelCharacterSelect) panelCharacterSelect.SetActive(true);
        if (panelPlayerCode)      panelPlayerCode.SetActive(false);

        if (hubCG)
        {
            hubCG.alpha = 1f;
            hubCG.interactable = false;
            hubCG.blocksRaycasts = false;
        }

        if (panelCharacterSelect) panelCharacterSelect.transform.SetAsLastSibling();
    }

    public void ShowPlayerCode()
    {
        if (panelHub)             panelHub.SetActive(false);
        if (panelCharacterSelect) panelCharacterSelect.SetActive(false);
        if (panelPlayerCode)      panelPlayerCode.SetActive(true);
    }
}
