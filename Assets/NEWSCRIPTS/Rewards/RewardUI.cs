using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class RewardUI : MonoBehaviour
{
    [System.Serializable]
    public struct RewardData
    {
        public string title;
        public string body;
        public int mssBanked;
    }

    [Header("Wiring")]
    [SerializeField] private CanvasGroup group;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text mssBankedText;
    [SerializeField] private Button continueButton;

    [Header("Behavior")]
    [SerializeField] private bool pauseTimeWhileOpen = true;
    [SerializeField] private bool showCursor = true;

    private string _menuScene;
    private bool _open;
    private float _prevTimeScale = 1f;

    private void Awake()
    {
        if (!group) group = GetComponent<CanvasGroup>();
        if (!continueButton) continueButton = GetComponentInChildren<Button>(true);

        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinue);

        SetVisible(false, instant:true);
    }

    public void Open(RewardData data, string menuSceneName)
    {
        _menuScene = menuSceneName;

        if (titleText) titleText.text = string.IsNullOrEmpty(data.title) ? "Level Complete!" : data.title;
        if (bodyText)  bodyText.text  = data.body ?? "";
        if (mssBankedText) mssBankedText.text = data.mssBanked > 0 ? $"+{data.mssBanked} mssBanked" : "";

        if (pauseTimeWhileOpen)
        {
            _prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        if (showCursor) { Cursor.visible = true; Cursor.lockState = CursorLockMode.None; }

        SetVisible(true);
        _open = true;
        Debug.Log("[RewardUI] Opened. Paused time.");
    }

    private void OnContinue()
    {
        if (!_open) return;

        // restore time before changing scenes
        if (pauseTimeWhileOpen)
            Time.timeScale = (_prevTimeScale <= 0f) ? 1f : _prevTimeScale;

        _open = false;
        SetVisible(false);

        if (!string.IsNullOrEmpty(_menuScene))
        {
            Debug.Log($"[RewardUI] Loading menu scene '{_menuScene}'");
            SceneManager.LoadScene(_menuScene); // ensure scene is in Build Settings
        }
        else
        {
            Debug.LogError("[RewardUI] No menu scene name provided — set it on EndLevelPortal.");
        }
    }

    private void SetVisible(bool v, bool instant = false)
    {
        if (!group)
        {
            gameObject.SetActive(v);
            return;
        }
        group.alpha = v ? 1f : 0f;
        group.blocksRaycasts = v;
        group.interactable = v;
        gameObject.SetActive(true); // keep root active so alpha works
    }
}
