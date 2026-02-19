using UnityEngine;

[CreateAssetMenu(menuName = "Cosmetics/Icon Def", fileName = "IconDef")]
public class IconDef : ScriptableObject
{
    [Tooltip("Stable string key (must match Firebase / profiles)")]
    public string key;   // "icon_headknight"

    public Sprite sprite;
}
