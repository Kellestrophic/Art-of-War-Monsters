using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterLibrary", menuName = "AOW/Characters/Character Library")]
public class CharacterLibrary : ScriptableObject
{
    [System.Serializable]
    public class CharacterDef
    {
        [Tooltip("Unique, stable key used in saves and networking. Use lowercase, no spaces. e.g., 'dracula'")]
        public string key;

        [Tooltip("UI display name")]
        public string displayName;

        [Header("UI")]
        [Tooltip("Square-ish icon for selection UI")]
        public Sprite icon;

        [Header("Economy")]
        [Min(0)]
        [Tooltip("Price in EPIC when store is live. 0 = free/unlocked by default (or unlocked via starter pack).")]
        public int epicCost = 0;

        // === Future fields you can uncomment when ready ===
        // [Header("Gameplay")]
        // public RuntimeAnimatorController animator; // if each character has its own animator
        // public GameObject characterPrefab;         // if you use per-character prefabs
        // public TextAsset balanceJson;              // per-character balance config (hitboxes, speed, etc.)
        // public AudioClip selectSfx;
        // public string loreSnippet;
    }

    [Tooltip("All characters visible in Character Select. Order = display order.")]
    public List<CharacterDef> characters = new();

    public CharacterDef GetByKey(string key) => characters.Find(c => c.key == key);

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Ensure keys are lowercased and unique; warn if duplicates
        var set = new HashSet<string>();
        for (int i = 0; i < characters.Count; i++)
        {
            var c = characters[i];
            if (string.IsNullOrWhiteSpace(c.key))
            {
                Debug.LogWarning($"[CharacterLibrary] Empty key at index {i}. Assign a unique key.");
                continue;
            }

            c.key = c.key.Trim().ToLowerInvariant();

            if (!set.Add(c.key))
            {
                Debug.LogError($"[CharacterLibrary] Duplicate key '{c.key}' at index {i}. Keys must be unique.");
            }

            if (c.icon == null)
            {
                // Not an error, but helpful during setup
                // Debug.LogWarning($"[CharacterLibrary] '{c.key}' has no icon assigned.");
            }
        }
    }
#endif
}
