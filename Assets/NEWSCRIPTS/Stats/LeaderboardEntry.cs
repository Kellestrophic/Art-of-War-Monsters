using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LeaderboardEntry : MonoBehaviour
{
    [Header("UI")]
    public Image IconImage;
    public Image PortraitFrameImage;

    public TMP_Text playerNameText;
    public TMP_Text levelText;
    public TMP_Text coinsText;
    public TMP_Text rankText;

   public void SetEntry(int rank, string playerName, int level, int totalMss, Sprite icon, Sprite frame)

{
    Debug.Log($"[LEADERBOARD ENTRY] " +
              $"Rank:{rank}  Name:{playerName}  " +
              $"Icon Sprite NULL? {(icon == null ? "YES" : "NO")}  " +
              $"Frame Sprite NULL? {(frame == null ? "YES" : "NO")}");

    Debug.Log($"[LEADERBOARD ENTRY] " +
              $"iconImage assigned? {(IconImage != null)}   frameImage assigned? {(PortraitFrameImage != null)}");
        // RANK
        rankText.text = "#" + rank;

        // NAME
        if (string.IsNullOrEmpty(playerName))
            playerNameText.text = "Unknown";
        else
            playerNameText.text = playerName;

        // LEVEL
        levelText.text = "Lv " + level.ToString();

        // COINS (MSS Banked)
        coinsText.text = totalMss.ToString();


        // ICON
        if (IconImage != null)
           IconImage.sprite = icon;


        // FRAME
        if (PortraitFrameImage != null)
            PortraitFrameImage.sprite = frame != null ? frame : PortraitFrameImage.sprite;
    }
}
