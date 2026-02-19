// Assets/NEWSCRIPTS/UI/IconImageBinder.cs
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class IconImageBinder : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private IconImageLibrary iconLibrary;  // assign your asset here
    [SerializeField] private string iconKey;                // e.g., "0", "warrior", etc.

    [Header("Fallback")]
    [SerializeField] private Sprite fallback;

    private Image _img;

    private void Awake()
    {
        _img = GetComponent<Image>();
        Refresh();
    }

    public void SetKey(string key)
    {
        iconKey = key;
        Refresh();
    }

    public void Refresh()
    {
        if (!iconLibrary)
        {
            Debug.LogWarning("[IconImageBinder] No IconImageLibrary assigned.");
            if (_img) _img.sprite = fallback;
            return;
        }

        iconLibrary.Initialize(); // builds internal dict once :contentReference[oaicite:7]{index=7}
        var sprite = iconLibrary.GetIcon(iconKey); // safe; returns null if missing :contentReference[oaicite:8]{index=8}
        if (_img) _img.sprite = sprite ? sprite : fallback;
    }
}
