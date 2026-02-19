using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectOption : MonoBehaviour
{
    [SerializeField] private string characterKey = "dracula";
    [SerializeField] private Button selectButton;

    [Header("Panels")]
    [SerializeField] private GameObject panelCharacterSelect;
    [SerializeField] private GameObject panelPlayerCode;   // for Direct
    [SerializeField] private GameObject panelLoading;      // optional

    private void Awake()
    {
        if (selectButton)
            selectButton.onClick.AddListener(OnSelect);
    }

    private async void OnSelect()
    {
        // Save selected character
        MatchContext.LocalSelectedCharacter = characterKey;
        Debug.Log($"[CharacterSelectOption] Selected {characterKey}");

        if (MatchContext.CurrentPath == MatchContext.Path.Direct)
        {
            // Go to Player Code entry panel
            if (panelCharacterSelect) panelCharacterSelect.SetActive(false);
            if (panelPlayerCode) panelPlayerCode.SetActive(true);
        }
        else // AI
        {
            if (panelCharacterSelect) panelCharacterSelect.SetActive(false);
            if (panelLoading) panelLoading.SetActive(true);

            bool ok = Unity.Netcode.NetworkManager.Singleton.StartHost();
            if (ok)
            {
                Unity.Netcode.NetworkManager.Singleton.SceneManager.LoadScene(
                    MatchContext.ArenaSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
            else
            {
                Debug.LogError("[CharacterSelectOption] Failed to start AI host.");
                if (panelLoading) panelLoading.SetActive(false);
                if (panelCharacterSelect) panelCharacterSelect.SetActive(true);
            }
        }
    }
}
