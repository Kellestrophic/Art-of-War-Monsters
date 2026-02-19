using UnityEngine;
using UnityEngine.Events;

public class PlayerXP : MonoBehaviour
{
    public UnityEvent<int, int> xpChanged; // (xpInto, xpNeed)

    private void OnEnable()
    {
        XPFacade.OnXPChanged += HandleXPChanged;
        PushOnce();
    }

    private void OnDisable()
    {
        XPFacade.OnXPChanged -= HandleXPChanged;
    }

    private void HandleXPChanged(int lvl, int xpInto, float pct)
    {
        int total = XPFacade.GetTotalXP();

        XPLevelCalculator.GetProgressInLevel(
            total,
            out _,
            out _,
            out _,
            out var into,
            out var need,
            out _
        );

        xpChanged?.Invoke(into, need);
    }

    private void PushOnce()
    {
        int total = XPFacade.GetTotalXP();

        XPLevelCalculator.GetProgressInLevel(
            total,
            out _,
            out _,
            out _,
            out var into,
            out var need,
            out _
        );

        xpChanged?.Invoke(into, need);
    }
}
