using UnityEngine;

[CreateAssetMenu(menuName = "Multiplayer/Rewards Config", fileName = "RewardConfig")]
public class RewardConfigSO : ScriptableObject
{
    [Header("XP")]
    public int winXP  = 100;
    public int lossXP = 60;

    [Header("MCC (soft currency)")]
    public int winMCC  = 50;
    public int lossMCC = 0;
}
