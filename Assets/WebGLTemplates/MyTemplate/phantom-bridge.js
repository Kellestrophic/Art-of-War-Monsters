// phantom-bridge.js

// CHANGE THESE if your receiver uses different names:
const UNITY_TARGET_GO  = 'PhantomBridgeHandler';
const UNITY_METHOD_OK  = 'OnWalletConnectedFromJS';
const UNITY_METHOD_ERR = 'OnWalletErrorFromJS';
const UNITY_METHOD_TX_OK  = 'OnTxSuccessFromJS';
const UNITY_METHOD_TX_ERR = 'OnTxErrorFromJS';


// Safe sender: queues until Unity is ready
const _queue = [];
function safeSend(go, method, arg) {
  if (window.unityInstance && window.unityInstance.SendMessage) {
    try { window.unityInstance.SendMessage(go, method, String(arg ?? "")); }
    catch (e) { console.warn('[phantom-bridge] SendMessage failed', e); }
  } else {
    _queue.push([go, method, String(arg ?? "")]);
  }
}
window.addEventListener('UnityReady', () => {
  while (_queue.length) {
    const [go, m, a] = _queue.shift();
    safeSend(go, m, a);
  }
});

// Called from C# via your .jslib → ConnectPhantomWallet()
window.phantomBridge_connect = async function () {
  try {
    if (!window.solana || !window.solana.isPhantom) {
      safeSend(UNITY_TARGET_GO, UNITY_METHOD_ERR, 'Phantom not found');
      return;
    }
    const res = await window.solana.connect({ onlyIfTrusted: false }); // prompts user
    const addr = res.publicKey.toString();
    console.log('[phantom-bridge] Connected', addr);
    safeSend(UNITY_TARGET_GO, UNITY_METHOD_OK, addr);
  } catch (e) {
    console.warn('[phantom-bridge] connect error', e);
    safeSend(UNITY_TARGET_GO, UNITY_METHOD_ERR, String(e && e.message || e));
  }
};

// Optional: silent reconnect after first click if already trusted
document.addEventListener('pointerdown', async function once() {
  document.removeEventListener('pointerdown', once);
  try {
    if (window.solana && window.solana.isPhantom) {
      await window.solana.connect({ onlyIfTrusted: true });
      const addr = window.solana.publicKey && window.solana.publicKey.toString();
      if (addr) safeSend(UNITY_TARGET_GO, UNITY_METHOD_OK, addr);
    }
  } catch {}
});

// helper: base64 -> Uint8Array
function b64ToU8(b64) {
  const bin = atob(b64);
  const u8 = new Uint8Array(bin.length);
  for (let i = 0; i < bin.length; i++) u8[i] = bin.charCodeAt(i);
  return u8;
}

// Called by Unity: phantomBridge_signAndSend(base64Tx, relayUrlOrEmpty)
// Tries signAndSendTransaction(Uint8Array). If wallet only supports signTransaction,
// it signs and then posts raw bytes to relayUrl (server /relay).
window.phantomBridge_signAndSend = async function (base64Tx, relayUrl) {
  try {
    if (!window.solana || !window.solana.isPhantom) {
      safeSend(UNITY_TARGET_GO, UNITY_METHOD_TX_ERR, 'Phantom not found');
      return;
    }
    const u8 = b64ToU8(base64Tx);
    const w = window.solana;

    // Preferred path: signAndSendTransaction (Wallet Standard)
    if (w.signAndSendTransaction) {
      const res = await w.signAndSendTransaction(u8);
      const sig = res && (res.signature || res); // some wallets return string, some {signature}
      safeSend(UNITY_TARGET_GO, UNITY_METHOD_TX_OK, sig || '');
      return;
    }

    // Fallback: signTransaction, then server relay
    if (!relayUrl) {
      safeSend(UNITY_TARGET_GO, UNITY_METHOD_TX_ERR, 'Wallet lacks signAndSend; no relay provided');
      return;
    }
    const signed = await w.signTransaction(u8);
    const raw = signed.serialize ? signed.serialize() : signed; // some wallets return bytes directly
    const resp = await fetch(relayUrl, { method:'POST', body: raw });
    const json = await resp.json();
    if (!json.ok) throw new Error(json.error || 'relay failed');
    safeSend(UNITY_TARGET_GO, UNITY_METHOD_TX_OK, json.signature || '');
  } catch (e) {
    console.warn('[phantom-bridge] signAndSend error', e);
    safeSend(UNITY_TARGET_GO, UNITY_METHOD_TX_ERR, String(e && e.message || e));
  }
};