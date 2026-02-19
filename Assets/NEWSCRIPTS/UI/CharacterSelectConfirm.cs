using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class CharacterSelectConfirm : MonoBehaviour
{
    [Header("Scene names")]
    [SerializeField] string aiArenaScene = "Arena_GraveyardAI";

    [Header("Direct Match fallback (keeps your current behavior)")]
    [SerializeField] UnityEvent onDirectConfirm;

    const string Key = "MODE_VSAI";

    // Hook this to your Select button
    public void Confirm()
    {
        bool vsAI = PlayerPrefs.GetInt(Key, 0) == 1;

        // Clear flag so it doesn't stick for the next time
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();

        if (vsAI)
        {
            // Go to your AI arena
            if (!Application.CanStreamedLevelBeLoaded(aiArenaScene))
            {
                Debug.LogError("[CharacterSelectConfirm] Scene not in Build Settings: " + aiArenaScene);
                return;
            }
            SceneManager.LoadScene(aiArenaScene, LoadSceneMode.Single);
        }
        else
        {
            // Keep your existing Direct flow (ready-up / network / whatever you already had)
            onDirectConfirm?.Invoke();
        }
    }
}
