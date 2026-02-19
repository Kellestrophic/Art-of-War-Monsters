using UnityEngine;

[System.Serializable]
public class BossFlowDefinition
{
    public string levelId;

    [Header("Scenes")]
    public string preBossCinematic;
    public string bossFight;
    public string postBossCinematic;
}
