using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StrikeBarHUD : MonoBehaviour
{
    // Identify who this HUD belongs to
    [Header("Owner")]
    public bool isPlayerBar = true; // Player HUD = true, Enemy HUD = false

    // Registry
    private static readonly List<StrikeBarHUD> allBars = new();
    private void OnEnable()  { if (!allBars.Contains(this)) allBars.Add(this); }
    private void OnDisable() { allBars.Remove(this); }

    public static StrikeBarHUD GetPlayerBar()
        => allBars.Find(b => b != null && b.isPlayerBar);

    public static StrikeBarHUD GetClosestEnemyBar(Vector3 fromPos)
    {
        StrikeBarHUD best = null;
        float bestDist = float.MaxValue;

        foreach (var b in allBars)
        {
            if (b == null || b.isPlayerBar) continue;
            float d = Vector3.Distance(fromPos, b.transform.position);
            if (d < bestDist) { bestDist = d; best = b; }
        }
        return best;
    }

    // ----------------------------
    // STRIKE DATA
    // ----------------------------
    [Header("Strike Settings")]
// ----------------------------
// STRIKE DATA
// ----------------------------
[Header("Strike Settings")]
public int currentPoints = 0;
public int maxPoints = 25;
public int startingPoints = 10;


    // ----------------------------
    // UI
    // ----------------------------
    [Header("UI References")]
    public Slider strikeSlider;
    public TMP_Text strikeText;
    public Image glowOverlay;

   private void Start()
{
    currentPoints = Mathf.Clamp(startingPoints, 0, maxPoints);

    if (glowOverlay != null)
        glowOverlay.enabled = false;

    UpdateStrikeBar();

    Debug.Log($"[StrikeBarHUD] Start → {currentPoints}/{maxPoints}");
}



    public void AddPoint() => AddPoints(1);

    public void AddPoints(int amount)
    {
        currentPoints = Mathf.Clamp(currentPoints + amount, 0, maxPoints);
        UpdateStrikeBar();
    }

    public void ActivateStrike()
    {
        currentPoints = 0;
        UpdateStrikeBar();
        // Trigger strike effect elsewhere
    }
public void SetPoints(int value)
{
    currentPoints = Mathf.Clamp(value, 0, maxPoints);
    UpdateStrikeBar();
}

    private void UpdateStrikeBar()
    {
        if (strikeSlider)
        {
            strikeSlider.maxValue = maxPoints;
            strikeSlider.value = currentPoints;
        }

        if (strikeText)
            strikeText.text = $"Strike: {currentPoints}/{maxPoints}";

        if (glowOverlay)
            glowOverlay.enabled = (currentPoints >= maxPoints);
    }
}
