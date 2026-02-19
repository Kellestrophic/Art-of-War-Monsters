#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FindAndFixMissingScripts
{
    [MenuItem("Tools/Missing Scripts/Report In Scene")]
    public static void ReportMissingInScene()
    {
        int goCount = 0, compCount = 0, missingCount = 0;
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            ReportOnObject(root, ref goCount, ref compCount, ref missingCount);

        Debug.Log($"[MissingScripts] GameObjects: {goCount}, Components: {compCount}, Missing: {missingCount}");
    }

    [MenuItem("Tools/Missing Scripts/Remove In Scene (UNDO)")]
    public static void RemoveMissingInScene()
    {
        int removed = 0;
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            removed += RemoveOnObject(root);

        Debug.Log($"[MissingScripts] Removed {removed} missing script component(s).");
    }

    static void ReportOnObject(GameObject go, ref int goCount, ref int compCount, ref int missingCount)
    {
        goCount++;
        var comps = go.GetComponents<Component>();
        compCount += comps.Length;
        for (int i = 0; i < comps.Length; i++)
        {
            if (comps[i] == null)
            {
                missingCount++;
                string path = GetPath(go);
                Debug.LogWarning($"[MissingScripts] {path} has a missing script (component index {i}).");
            }
        }
        foreach (Transform t in go.transform) ReportOnObject(t.gameObject, ref goCount, ref compCount, ref missingCount);
    }

    static int RemoveOnObject(GameObject go)
    {
        int removed = 0;
        var comps = go.GetComponents<Component>();
        var so = new SerializedObject(go);
        var prop = so.FindProperty("m_Component");
        int r = 0;
        for (int i = 0; i < comps.Length; i++)
        {
            if (comps[i] == null)
            {
                prop.DeleteArrayElementAtIndex(i - r);
                removed++;
                r++;
            }
        }
        if (removed > 0)
        {
            so.ApplyModifiedProperties();
            Undo.RegisterCompleteObjectUndo(go, "Remove missing scripts");
            EditorUtility.SetDirty(go);
        }
        foreach (Transform t in go.transform) removed += RemoveOnObject(t.gameObject);
        return removed;
    }

    static string GetPath(GameObject go)
    {
        string path = go.name;
        while (go.transform.parent != null)
        {
            go = go.transform.parent.gameObject;
            path = go.name + "/" + path;
        }
        return path;
    }
}
#endif
