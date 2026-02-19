// Assets/NEWSCRIPTS/Multiplayer/LoadingCurtain.cs
using UnityEngine;
using TMPro;

public class LoadingCurtain : MonoBehaviour
{
    [SerializeField] TMP_Text message;            // assign in Inspector
    [SerializeField] string baseText = "Waiting for players";

    float t;

    public void Show() { gameObject.SetActive(true); ResetMsg(); }
    public void Hide() { gameObject.SetActive(false); }

    void OnEnable() => ResetMsg();

    void ResetMsg()
    {
        t = 0f;
        if (message) message.text = baseText + "...";
    }

    void Update()
    {
        if (!message || !message.gameObject.activeInHierarchy) return;
        t += Time.deltaTime;
        int dots = 1 + (int)(t * 2f) % 3;
        message.text = baseText + new string('.', dots);
    }
}
