using TMPro;
using UnityEngine;

public class LiveLevelLabel : MonoBehaviour
{
    [SerializeField] private TMP_Text levelText;   // assign in Inspector
    [SerializeField] private string prefix = "LVL. ";

    private void OnEnable()
    {
        XPFacade.OnXPChanged += HandleXPChanged;
        XPFacade.InitializeFromProfileIfAvailable();   // safe no-op if already init
        PushOnce();
    }

    private void OnDisable()
    {
        XPFacade.OnXPChanged -= HandleXPChanged;
    }

    private void HandleXPChanged(int level, int xpInto, float pct)
    {
        if (levelText) levelText.text = prefix + level;
    }

    private void PushOnce()
    {
        if (levelText) levelText.text = prefix + XPFacade.GetLevel();
    }
}
