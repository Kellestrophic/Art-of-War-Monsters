using UnityEngine;

[System.Serializable]
public class CosmeticItem
{
    // ─── Existing fields (UNCHANGED) ────────────────────
    public string key;          // Internal ID ("default_icon", "scared_baby")
    public string displayName;  // UI name ("Scared Baby")
    public Sprite sprite;       // Icon / frame sprite
    public string type;         // "icon", "frame", "title"
    public int unlockLevel;     // Optional future logic

    // ─── Premium / Store fields (NEW, SAFE) ─────────────
    public bool isPremium = false;   // true = requires SOL
    public float priceSOL = 0f;      // e.g. 0.05 SOL
    public string purchaseId;        // optional (SKU / future NFT / analytics)
}
