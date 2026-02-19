// Assets/NEWSCRIPTS/Multiplayer/ReadyButton.cs
using UnityEngine;

public class ReadyButton : MonoBehaviour
{
    private bool state = false;

    // Hook this to the Button's OnClick
    public void OnClick()
    {
        state = !state;

        if (ReadySync.Instance == null)
        {
            Debug.LogError("[ReadyButton] ReadySync.Instance is null.");
            return;
        }

        // Use the ReadySync API (we removed the old RPC call)
        ReadySync.Instance.SetReady(state);
        // Alternatively: ReadySync.Instance.ToggleReady();
    }
}
