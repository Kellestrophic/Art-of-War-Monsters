using UnityEngine;
using UnityEngine.UI;

public class MatchResultOverlay : MonoBehaviour
{
    [SerializeField] private Button continueButton;
    [SerializeField] private bool opponentIsAI = true;

    private bool resultHandled;

    public void ShowResult(bool didWin, bool vsAI)
    {
        opponentIsAI = vsAI;
        gameObject.SetActive(true);

        if (!resultHandled)
{
    resultHandled = true;

    var store = RuntimeStatsStore.Instance;
    if (store == null)
    {
        Debug.LogError("[MatchResultOverlay] No RuntimeStatsStore in scene!");
        return;
    }

    bool aiWin  = vsAI && didWin;
    bool mpWin  = !vsAI && didWin;
    bool mpLoss = !vsAI && !didWin;

    store.RecordMatchResult(
        aiWin: aiWin,
        mpWin: mpWin,
        mpLoss: mpLoss
    );
}

    }

    private void Awake()
    {
        if (continueButton != null)
            continueButton.onClick.AddListener(() => gameObject.SetActive(false));
    }
}
