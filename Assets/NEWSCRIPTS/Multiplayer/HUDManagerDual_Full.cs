using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class HUDHostJoinBinder_Full : MonoBehaviour
{
    [Header("Left (Host) UI")]
    [SerializeField] TMP_Text leftName;
    [SerializeField] TMP_Text leftTitle;
    [SerializeField] Image    leftIcon;
    [SerializeField] Image    leftFrame;
    [SerializeField] Slider   leftHp;
    [SerializeField] Slider   leftStrike;
    [SerializeField] TMP_Text leftStrikeText;  // e.g., "0/25" (optional)

    [Header("Right (Joiner) UI")]
    [SerializeField] TMP_Text rightName;
    [SerializeField] TMP_Text rightTitle;
    [SerializeField] Image    rightIcon;
    [SerializeField] Image    rightFrame;
    [SerializeField] Slider   rightHp;
    [SerializeField] Slider   rightStrike;
    [SerializeField] TMP_Text rightStrikeText; // e.g., "0/25" (optional)

    [Header("Libraries (same ScriptableObjects you already use)")]
    [SerializeField] ScriptableObject cosmeticLibrary; // your CosmeticLibrary
    [SerializeField] ScriptableObject frameLibrary;    // your FrameLibrary

    [Header("Strike Fallbacks (used only if MaxStrike not found)")]
    [SerializeField] int defaultMaxStrike = 25;

    private PlayerIdentityNet hostId, joinId;
    private DamageableNet     hostHpRef, joinHpRef;

    // Strike reflection handles
    private NetworkVariable<int> hostStrikeNV, joinStrikeNV;
    private int hostMaxStrike, joinMaxStrike;

    // We store delegates so we can unsubscribe cleanly
    private NetworkVariable<int>.OnValueChangedDelegate hostHpH, joinHpH;
    private NetworkVariable<int>.OnValueChangedDelegate hostStrikeH, joinStrikeH;

    private void OnEnable()  => StartCoroutine(BindLoop());
    private void OnDisable() => UnhookAll();

    private IEnumerator BindLoop()
    {
        while (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            yield return null;

        for (;;)
        {
            TryBind();
            yield return new WaitForSeconds(0.25f);
        }
    }

    private void TryBind()
    {
        var hostIdConst = NetworkManager.ServerClientId; // Host == Server

        // Find identities for host/joiner
        var ids = FindObjectsByType<PlayerIdentityNet>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (ids.Length < 2) return;

        var newHostId = ids.FirstOrDefault(p => p.OwnerClientId == hostIdConst);
        var newJoinId = ids.FirstOrDefault(p => p.OwnerClientId != hostIdConst);
        if (!newHostId || !newJoinId) return;

        if (newHostId != hostId || newJoinId != joinId)
        {
            hostId = newHostId;
            joinId = newJoinId;

            RefreshIdentity(hostId, leftName, leftTitle, leftIcon, leftFrame);
            RefreshIdentity(joinId, rightName, rightTitle, rightIcon, rightFrame);

            // Re-hook identity changes
            hostId.DisplayName.OnValueChanged += (_, __) => RefreshIdentity(hostId, leftName, leftTitle, leftIcon, leftFrame);
            hostId.TitleKey   .OnValueChanged += (_, __) => RefreshIdentity(hostId, leftName, leftTitle, leftIcon, leftFrame);
            hostId.IconKey    .OnValueChanged += (_, __) => RefreshIdentity(hostId, leftName, leftTitle, leftIcon, leftFrame);
            hostId.FrameKey   .OnValueChanged += (_, __) => RefreshIdentity(hostId, leftName, leftTitle, leftIcon, leftFrame);

            joinId.DisplayName.OnValueChanged += (_, __) => RefreshIdentity(joinId, rightName, rightTitle, rightIcon, rightFrame);
            joinId.TitleKey   .OnValueChanged += (_, __) => RefreshIdentity(joinId, rightName, rightTitle, rightIcon, rightFrame);
            joinId.IconKey    .OnValueChanged += (_, __) => RefreshIdentity(joinId, rightName, rightTitle, rightIcon, rightFrame);
            joinId.FrameKey   .OnValueChanged += (_, __) => RefreshIdentity(joinId, rightName, rightTitle, rightIcon, rightFrame);
        }

        // HP (and Strike) components live on the player roots
        var newHostHp = hostId.GetComponent<DamageableNet>();
        var newJoinHp = joinId.GetComponent<DamageableNet>();
        if (newHostHp != hostHpRef || newJoinHp != joinHpRef)
        {
            // Unhook previous HP/Strike
            if (hostHpRef != null && hostHpH != null) hostHpRef.Hp.OnValueChanged -= hostHpH;
            if (joinHpRef != null && joinHpH != null) joinHpRef.Hp.OnValueChanged -= joinHpH;
            if (hostStrikeNV != null && hostStrikeH != null) hostStrikeNV.OnValueChanged -= hostStrikeH;
            if (joinStrikeNV != null && joinStrikeH != null) joinStrikeNV.OnValueChanged -= joinStrikeH;

            hostHpRef = newHostHp;
            joinHpRef = newJoinHp;

            // --- HP bind ---
            if (hostHpRef && leftHp)
            {
                leftHp.minValue = 0;
                leftHp.maxValue = hostHpRef.MaxHP;
                leftHp.value    = hostHpRef.Hp.Value;
                hostHpH = (oldV, newV) => leftHp.value = newV;
                hostHpRef.Hp.OnValueChanged += hostHpH;
            }
            if (joinHpRef && rightHp)
            {
                rightHp.minValue = 0;
                rightHp.maxValue = joinHpRef.MaxHP;
                rightHp.value    = joinHpRef.Hp.Value;
                joinHpH = (oldV, newV) => rightHp.value = newV;
                joinHpRef.Hp.OnValueChanged += joinHpH;
            }

            // --- STRIKE bind (optional, via reflection) ---
            BindStrike(hostHpRef, leftStrike, leftStrikeText, out hostStrikeNV, out hostStrikeH, out hostMaxStrike);
            BindStrike(joinHpRef, rightStrike, rightStrikeText, out joinStrikeNV, out joinStrikeH, out joinMaxStrike);
        }
    }

    // ---------- Identity ----------
    private void RefreshIdentity(PlayerIdentityNet id, TMP_Text nameTxt, TMP_Text titleTxt, Image iconImg, Image frameImg)
    {
        if (!id) return;
        if (nameTxt)  nameTxt.text  = string.IsNullOrEmpty(id.DisplayNameString) ? "Player" : id.DisplayNameString;
        if (titleTxt) titleTxt.text = id.TitleString ?? "";

        // Icons/Frames via your libraries (reflection to be compatible with your existing SOs)
        if (iconImg)
        {
            iconImg.enabled = false;
            if (TryGetSprite(cosmeticLibrary, id.IconIdString, out var iconSprite))
            {
                iconImg.sprite = iconSprite;
                iconImg.enabled = true;
            }
        }

        if (frameImg)
        {
            frameImg.enabled = false;
            if (TryGetSprite(frameLibrary, id.FrameIdString, out var frameSprite))
            {
                frameImg.sprite = frameSprite;
                frameImg.enabled = true;
            }
        }
    }

    private static bool TryGetSprite(ScriptableObject lib, string key, out Sprite sprite)
    {
        sprite = null;
        if (!lib || string.IsNullOrEmpty(key)) return false;

        var t = lib.GetType();
        foreach (var name in new[] { "TryGetIconSprite","GetIconSprite","TryGetFrameSprite","GetFrameSprite","TryGetSprite","GetSprite","IconFor","FrameFor","Get" })
        {
            var mi = t.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (mi == null) continue;
            var ps = mi.GetParameters();
            try
            {
                // bool TryGetX(string, out Sprite)
                if (ps.Length == 2 && ps[0].ParameterType == typeof(string) && ps[1].IsOut &&
                    ps[1].ParameterType == typeof(Sprite).MakeByRefType())
                {
                    object[] args = { key, null };
                    if (mi.Invoke(lib, args) is bool ok && ok) { sprite = (Sprite)args[1]; return true; }
                }
                // Sprite GetX(string)
                else if (ps.Length == 1 && ps[0].ParameterType == typeof(string))
                {
                    if (mi.Invoke(lib, new object[] { key }) is Sprite s && s) { sprite = s; return true; }
                }
            }
            catch { /* fallthrough */ }
        }
        return false;
    }

    private void BindStrike(
        DamageableNet hpComp,
        Slider strikeSlider,
        TMP_Text strikeLabel,
        out NetworkVariable<int> strikeNV,
        out NetworkVariable<int>.OnValueChangedDelegate handler,
        out int maxStrike)
    {
        strikeNV = null;
        handler  = null;
        maxStrike = defaultMaxStrike;

        if (!hpComp || strikeSlider == null) return;

        // Find a field or property named "Strike" of type NetworkVariable<int>
        var compType = hpComp.GetType();

        // Field
        var field = compType.GetField("Strike", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null && typeof(NetworkVariable<int>).IsAssignableFrom(field.FieldType))
            strikeNV = (NetworkVariable<int>)field.GetValue(hpComp);

        // Property
        if (strikeNV == null)
        {
            var prop = compType.GetProperty("Strike", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (prop != null && typeof(NetworkVariable<int>).IsAssignableFrom(prop.PropertyType))
                strikeNV = (NetworkVariable<int>)prop.GetValue(hpComp);
        }

        // MaxStrike (optional)
        var maxField = compType.GetField("MaxStrike", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (maxField != null && maxField.FieldType == typeof(int))
            maxStrike = (int)maxField.GetValue(hpComp);
        else
        {
            var maxProp = compType.GetProperty("MaxStrike", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (maxProp != null && maxProp.PropertyType == typeof(int))
                maxStrike = (int)maxProp.GetValue(hpComp);
        }

        // If no NV found, hide/ignore UI gracefully
        if (strikeNV == null)
        {
            if (strikeSlider) strikeSlider.gameObject.SetActive(false);
            if (strikeLabel)  strikeLabel.gameObject.SetActive(false);
            return;
        }

        // Init UI
        strikeSlider.gameObject.SetActive(true);
        strikeSlider.minValue = 0;
        strikeSlider.maxValue = Mathf.Max(1, maxStrike);
        strikeSlider.value    = Mathf.Clamp(strikeNV.Value, 0, maxStrike);
        if (strikeLabel)
            strikeLabel.text = $"{Mathf.Clamp(strikeNV.Value,0,maxStrike)}/{Mathf.Max(1,maxStrike)}";

        int cap = maxStrike; // avoid capturing out var

        handler = (oldV, newV) =>
        {
            var clamped = Mathf.Clamp(newV, 0, cap);
            strikeSlider.value = clamped;
            if (strikeLabel) strikeLabel.text = $"{clamped}/{Mathf.Max(1,cap)}";
        };
        strikeNV.OnValueChanged += handler;
    }

    private void UnhookAll()
    {
        if (hostHpRef != null && hostHpH != null) hostHpRef.Hp.OnValueChanged -= hostHpH;
        if (joinHpRef != null && joinHpH != null) joinHpRef.Hp.OnValueChanged -= joinHpH;
        if (hostStrikeNV != null && hostStrikeH != null) hostStrikeNV.OnValueChanged -= hostStrikeH;
        if (joinStrikeNV != null && joinStrikeH != null) joinStrikeNV.OnValueChanged -= joinStrikeH;

        hostHpRef = joinHpRef = null;
        hostHpH = joinHpH = null;
        hostStrikeNV = joinStrikeNV = null;
        hostStrikeH = joinStrikeH = null;
    }
}
