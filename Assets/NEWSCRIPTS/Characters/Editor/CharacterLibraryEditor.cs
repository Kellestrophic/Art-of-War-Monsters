#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CharacterLibrary))]
public class CharacterLibraryEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var lib = (CharacterLibrary)target;
        GUILayout.Space(8);

        if (GUILayout.Button("Validate & Sort (A→Z)"))
        {
            Undo.RecordObject(lib, "Validate & Sort");
            lib.characters.Sort((a, b) => string.Compare(a.displayName, b.displayName, true));
            EditorUtility.SetDirty(lib);
            Debug.Log("[CharacterLibrary] Validated and sorted.");
        }

        if (GUILayout.Button("Add Dracula (Quick)"))
        {
            Undo.RecordObject(lib, "Add Dracula");
            lib.characters.Add(new CharacterLibrary.CharacterDef
            {
                key = "dracula",
                displayName = "Dracula",
                icon = null,
                epicCost = 0
            });
            EditorUtility.SetDirty(lib);
        }
    }
}
#endif
