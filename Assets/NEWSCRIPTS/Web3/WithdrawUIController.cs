// Assets/NEWSCRIPTS/Web3/WithdrawUIController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class WithdrawUIController : MonoBehaviour
{
    [Header("UI Wiring")]
    [SerializeField] private TMP_Text totalMccText;
    [SerializeField] private TMP_InputField amountInput;
    [SerializeField] private Button withdrawButton;
    [SerializeField] private TMP_Text statusText;

    [Header("Behavior")]
    [SerializeField] private int minWithdraw = 500;
    [SerializeField] private bool autoFindBridge = true;
    [SerializeField] private FirebaseMSSService firebaseMSS;

    private int lastRequestedAmount = 0;
    private PhantomBridgeHandler bridge;

    private void Awake()
    {
        Debug.Log("[WithdrawUIController] Awake");

        if (withdrawButton)
        {
            withdrawButton.onClick.RemoveAllListeners();
            withdrawButton.onClick.AddListener(OnWithdrawClicked);
            Debug.Log("[WithdrawUIController] Hooked button: " + withdrawButton.name);
        }
        else
        {
            Debug.LogWarning("[WithdrawUIController] withdrawButton is NOT assigned!");
        }

        Validate();

        if (!firebaseMSS)
            firebaseMSS = FindFirstObjectByType<FirebaseMSSService>(FindObjectsInactive.Include);
    }

    // =============================================================
    // SINGLE SOURCE OF TRUTH (ACTIVE PROFILE STORE)
    // =============================================================
    private int GetAvailableMcc()
    {
        var aps = FindFirstObjectByType<ActiveProfileStore>(FindObjectsInactive.Include);
        if (aps != null && aps.CurrentProfile is NewProfileData p)
            return Mathf.Max(0, p.mssBanked); // <-- uses live profile value

        return 0;
    }

    private void Validate()
    {
        int available = GetAvailableMcc();

        if (withdrawButton)
            withdrawButton.interactable = available >= minWithdraw;

        if (totalMccText)
            totalMccText.text = $"Total MCC: {available}";

        if (statusText)
        {
            if (available < minWithdraw)
                statusText.text = $"Min withdraw is {minWithdraw}. Banked: {available}";
            else
                statusText.text = "";
        }

        Debug.Log($"[WithdrawUIController] Validate: available={available}, min={minWithdraw}");
    }

    // -------------------------------------------------------------
    // MAIN BUTTON CLICK
    // -------------------------------------------------------------
    public void OnWithdrawClicked()
    {
        Debug.Log("[WithdrawUIController] OnWithdrawClicked fired");

        if (!amountInput)
        {
            Debug.LogError("[WithdrawUIController] amountInput is null");
            if (statusText) statusText.text = "Amount input missing.";
            return;
        }

        Debug.Log("[WithdrawUIController] Raw input text = '" + amountInput.text + "'");

        if (!int.TryParse(amountInput.text, out int amt) || amt <= 0)
        {
            Debug.LogWarning("[WithdrawUIController] Invalid amount input");
            if (statusText) statusText.text = "Enter a valid amount.";
            return;
        }

        int available = GetAvailableMcc();

        if (amt > available)
        {
            if (statusText) statusText.text = "Not enough banked MCC.";
            return;
        }

        if (amt < minWithdraw)
        {
            Debug.LogWarning($"[WithdrawUIController] Amount {amt} < min {minWithdraw}");
            if (statusText) statusText.text = $"Minimum withdraw is {minWithdraw} MCC.";
            return;
        }

        lastRequestedAmount = amt;

        if (!bridge && autoFindBridge)
        {
            bridge = FindFirstObjectByType<PhantomBridgeHandler>(FindObjectsInactive.Include);
            Debug.Log("[WithdrawUIController] Auto-found bridge: " + (bridge ? bridge.name : "NULL"));
        }

        if (!bridge)
        {
            Debug.LogError("[WithdrawUIController] PhantomBridgeHandler not found.");
            if (statusText) statusText.text = "Withdraw system not ready.";
            return;
        }

        string wallet = ResolveWallet();
        Debug.Log("[WithdrawUIController] Resolved wallet: " + (string.IsNullOrEmpty(wallet) ? "NULL/EMPTY" : wallet));

        if (string.IsNullOrEmpty(wallet))
        {
            if (statusText) statusText.text = "No wallet found.";
            return;
        }

        if (statusText)
            statusText.text = $"Withdrawing {amt} MCC…";

        Debug.Log("[WithdrawUIController] Calling bridge.StartWithdraw(" + amt + ", wallet)");
        bridge.StartWithdraw(amt, wallet);
    }

    private string ResolveWallet()
    {
        var aps = FindFirstObjectByType<ActiveProfileStore>(FindObjectsInactive.Include);
        if (aps != null && aps.CurrentProfile is NewProfileData p)
            return p.wallet;

        Debug.LogWarning("[WithdrawUIController] ActiveProfileStore or wallet not found.");
        return null;
    }

    // -------------------------------------------------------------
    // SUCCESS / ERROR ROUTES (CALLED BY PhantomBridgeHandler)
    // -------------------------------------------------------------
    public void HandleWithdrawSuccess()
    {
        Debug.Log("[WithdrawUIController] HandleWithdrawSuccess");

        if (lastRequestedAmount <= 0)
        {
            if (statusText) statusText.text = "Withdraw complete.";
            return;
        }

        StartCoroutine(ApplyWithdrawToBankCoroutine(lastRequestedAmount));
        lastRequestedAmount = 0;
    }

    public void HandleWithdrawError(string error)
    {
        Debug.Log("[WithdrawUIController] HandleWithdrawError: " + error);
        if (statusText)
            statusText.text = "Withdraw failed: " + error;
    }

    // -------------------------------------------------------------
    // FIREBASE UPDATE PATH
    // -------------------------------------------------------------
    private IEnumerator ApplyWithdrawToBankCoroutine(int amount)
    {
        if (!firebaseMSS)
            firebaseMSS = FindFirstObjectByType<FirebaseMSSService>(FindObjectsInactive.Include);

        if (!firebaseMSS)
        {
            Debug.LogWarning("[WithdrawUIController] No FirebaseMSSService found.");
            Validate();
            yield break;
        }

        string wallet = ResolveWallet();
        if (string.IsNullOrEmpty(wallet))
        {
            Debug.LogWarning("[WithdrawUIController] No wallet when applying withdraw.");
            Validate();
            yield break;
        }

        bool ok = false;
        yield return firebaseMSS.SubtractFromBank(wallet, amount, success => ok = success);

        if (!ok)
        {
            Debug.LogWarning("[WithdrawUIController] SubtractFromBank failed.");

            if (statusText)
                statusText.text = "Withdraw failed on server (not enough banked?).";

            Validate();
            yield break;
        }

        if (statusText) statusText.text = "Withdraw complete.";

        Validate();
    }

    // -------------------------------------------------------------
    // OPTIONAL: REFRESH FROM FIREBASE
    // -------------------------------------------------------------
    public void RefreshBankedFromFirebase()
    {
        StartCoroutine(RefreshBankedCoroutine());
    }

    private IEnumerator RefreshBankedCoroutine()
    {
        var svc = FirebaseMSSService.Instance ??
                  FindFirstObjectByType<FirebaseMSSService>(FindObjectsInactive.Include);

        if (svc == null)
        {
            Debug.LogWarning("[WithdrawUIController] No FirebaseMSSService found for refresh.");
            yield break;
        }

        string wallet = ResolveWallet();
        if (string.IsNullOrEmpty(wallet))
        {
            Debug.LogWarning("[WithdrawUIController] No wallet for refresh.");
            yield break;
        }

        int banked = 0;
        yield return svc.GetBanked(wallet, v => banked = v);

        Validate();
    }
}
