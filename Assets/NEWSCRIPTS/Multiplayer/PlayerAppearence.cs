using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerAppearance : NetworkBehaviour
{
    [SerializeField] private Image activeIcon;            // example UI
    [SerializeField] private CharacterLibrary charLibrary;  // to map key→icon

    private PlayerLoadout loadout;

    public override void OnNetworkSpawn()
    {
        loadout = GetComponent<PlayerLoadout>();
        if (!loadout) return;

        // Apply immediately & listen for changes
        Apply(loadout.SelectedCharacter.Value.ToString());
        loadout.SelectedCharacter.OnValueChanged += (_, now) => Apply(now.ToString());
    }

    private void OnDestroy()
    {
        if (loadout != null)
            loadout.SelectedCharacter.OnValueChanged -= (_, __) => {};
    }

    private void Apply(string key)
    {
        if (!activeIcon || !charLibrary) return;
        var def = charLibrary.GetByKey(key);
        if (def != null && def.icon != null)
            activeIcon.sprite = def.icon;
    }
}
