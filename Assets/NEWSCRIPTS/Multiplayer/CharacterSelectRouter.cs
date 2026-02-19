// CharacterSelectRouter.cs
using UnityEngine;
using Unity.Netcode;

public class CharacterSelectRouter : MonoBehaviour
{
    [SerializeField] private MultiplayerHubController hub;   // assign
    [SerializeField] private SingleCharacterViewer viewer;   // has SelectedKey

    // Wire this to each "Select" button (or call with viewer.SelectedKey)
    public void OnSelectCharacter(string keyOverride = null)
    {
        var key = string.IsNullOrEmpty(keyOverride) ? viewer.SelectedKey : keyOverride;
        MatchContext.LocalSelectedCharacter = key;

        if (MatchContext.CurrentPath == MatchContext.Path.Direct)
        {
            hub.ShowPlayerCode();
        }
        else // AI
        {
            var nm = NetworkManager.Singleton;
            if (!nm) { Debug.LogError("No NetworkManager in scene."); return; }
            if (nm.StartHost())
            {
                nm.SceneManager.LoadScene(MatchContext.ArenaSceneName,
                    UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
            else
            {
                Debug.LogError("Failed to start AI host.");
            }
        }
    }

    // Wire this to the Character Select "Back" button
    public void OnBackFromCharacterSelect()
    {
        hub.ShowHub();
    }
}
