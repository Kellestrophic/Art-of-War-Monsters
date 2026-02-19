using UnityEngine;
using TMPro;

public class SurvivalTimerUI : MonoBehaviour
{
    public SurvivalDirector director;
    public TextMeshProUGUI timerText;

    void Update()
    {
        float t = director.survivalTime;
        int minutes = Mathf.FloorToInt(t / 60f);
        int seconds = Mathf.FloorToInt(t % 60f);

        timerText.text = $"{minutes:00}:{seconds:00}";
    }
}
