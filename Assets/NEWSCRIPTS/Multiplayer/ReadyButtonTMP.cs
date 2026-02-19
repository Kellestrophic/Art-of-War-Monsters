using UnityEngine;
using TMPro;

public class ReadyButtonTMP : MonoBehaviour
{
    [Header("Assign the TMP text on the button")]
    [SerializeField] private TMP_Text label;

    [Header("Texts")]
    [SerializeField] private string readyText = "Ready";
    [SerializeField] private string unreadyText = "Unready";

    private void OnEnable()
    {
        SyncLabel();
    }

    // Hook this to the Button's OnClick
    public void OnClick()
    {
        if (ReadySync.Instance != null)
        {
            ReadySync.Instance.ToggleReady();
            SyncLabel();
        }
    }

    private void SyncLabel()
    {
        if (!label) return;
        bool isReady = ReadySync.Instance != null && ReadySync.Instance.LocalReady;
        label.text = isReady ? unreadyText : readyText;
    }
}
