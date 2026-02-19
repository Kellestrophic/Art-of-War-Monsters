using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "AoW/Cosmetics/Unlock Rules")]
public class CosmeticUnlockRules : ScriptableObject
{
    public enum ConditionType
    {
        // profile.aiWins >= threshold
        StatAtLeast,
        // profile.enemyKills["Reaper"] >= threshold (or any dict<int> by name)
        DictStatAtLeast,
        // profile.Level (or level) >= threshold
        LevelAtLeast,
    }

    [System.Serializable]
    public class Rule
    {
        public string cosmeticKey;                // e.g. "reaper_icon"
        public ConditionType condition = ConditionType.StatAtLeast;

        [Header("Names")]
        public string statName;                   // e.g. "aiWins" OR "enemyKills"
        public string dictKey;                    // e.g. "Reaper" (if DictStatAtLeast)

        [Header("Threshold")]
        public int threshold = 1;
    }

    public List<Rule> rules = new();
}
