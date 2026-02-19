using UnityEngine;

// NOTE:
// Sol purchases are handled by PhantomBridgeHandler via
// /purchase-build → Phantom → /purchase-verify.
// This class is intentionally unused.
public class SolPurchaseManager : MonoBehaviour
{
    public static SolPurchaseManager Instance;

    private void Awake()
    {
        Instance = this;
    }
}
