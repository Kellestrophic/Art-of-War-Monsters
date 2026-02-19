using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectFrameSetter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image frameDisplay;
    [SerializeField] private FrameLibrary frameLibrary;

    private void OnEnable()
    {
        ApplyFrame();
    }

    public void ApplyFrame()
    {
        var profile = ActiveProfileStore.Instance?.CurrentProfile;

        if (profile == null)
        {
            Debug.LogWarning("[CharacterSelectFrameSetter] No profile found.");
            return;
        }

        if (frameLibrary == null)
        {
            Debug.LogWarning("[CharacterSelectFrameSetter] Missing FrameLibrary!");
            return;
        }

        if (frameDisplay == null)
        {
            Debug.LogWarning("[CharacterSelectFrameSetter] No frameDisplay assigned!");
            return;
        }

        // Correct frame based on player level
        string correctFrame = frameLibrary.GetBestKeyForLevel(profile.level);

        // Save ONLY if changed
        if (profile.activeFrame != correctFrame)
        {
            profile.activeFrame = correctFrame;
            ProfileUploader.UpdateActiveFrame(profile.wallet, correctFrame);
        }

        // Update UI
        frameDisplay.sprite = frameLibrary.GetByKey(profile.activeFrame);
    }
}
