mergeInto(LibraryManager.library, {

    // =====================================================
    // CONNECT WALLET
    // =====================================================
    phantomBridge_connect: function(goPtr, okPtr, errPtr) {
        var go  = UTF8ToString(goPtr);
        var ok  = UTF8ToString(okPtr);
        var err = UTF8ToString(errPtr);

        if (!window.solana || !window.solana.isPhantom) {
            SendMessage(go, err, "Phantom Wallet not detected.");
            return;
        }

        window.solana.connect()
            .then(function(res) {
                var pub = res && res.publicKey ? res.publicKey.toString() : "";
                if (!pub) {
                    SendMessage(go, err, "No public key returned.");
                    return;
                }
                SendMessage(go, ok, pub);
            })
            .catch(function(e) {
                SendMessage(go, err, "Connect rejected: " + e.message);
            });
    },


    // =====================================================
    // SIGN MESSAGE (FOR AUTH NONCE LOGIN)
    // =====================================================
    phantomBridge_signMessage: function(goPtr, okPtr, errPtr, msgPtr) {

        var go  = UTF8ToString(goPtr);
        var ok  = UTF8ToString(okPtr);
        var err = UTF8ToString(errPtr);
        var msg = UTF8ToString(msgPtr);

        if (!window.solana || !window.solana.isPhantom) {
            SendMessage(go, err, "Phantom Wallet not detected.");
            return;
        }

        if (typeof bs58 === "undefined") {
            SendMessage(go, err, "bs58 not loaded in WebGL template.");
            return;
        }

        try {
            var encoded = new TextEncoder().encode(msg);

            window.solana.signMessage(encoded, "utf8")
                .then(function(res) {

                    if (!res || !res.signature) {
                        SendMessage(go, err, "No signature returned.");
                        return;
                    }

                    // Convert Uint8Array → base58
                    var sigBase58 = bs58.encode(res.signature);

                    SendMessage(go, ok, sigBase58);
                })
                .catch(function(e) {
                    SendMessage(go, err, "signMessage failed: " + e.message);
                });

        } catch (e) {
            SendMessage(go, err, "signMessage exception: " + e.message);
        }
    },


    // =====================================================
    // SIGN + SEND TRANSACTION (V0 OR LEGACY)
    // =====================================================
    phantomBridge_signAndSend_v4: function(goPtr, okPtr, errPtr, b64Ptr) {

        var go  = UTF8ToString(goPtr);
        var ok  = UTF8ToString(okPtr);
        var err = UTF8ToString(errPtr);
        var b64 = UTF8ToString(b64Ptr);

        if (!window.solana || !window.solana.isPhantom) {
            SendMessage(go, err, "Phantom Wallet not detected.");
            return;
        }

        if (typeof solanaWeb3 === "undefined") {
            SendMessage(go, err, "solanaWeb3 not loaded.");
            return;
        }

        try {

            // Base64 → Uint8Array
            var raw = atob(b64);
            var arr = new Uint8Array(raw.length);
            for (var i = 0; i < raw.length; i++)
                arr[i] = raw.charCodeAt(i);

            var tx = null;

            // Try versioned
            try {
                if (solanaWeb3.VersionedTransaction)
                    tx = solanaWeb3.VersionedTransaction.deserialize(arr);
            } catch (e) {}

            // Fallback → legacy
            if (!tx) {
                tx = solanaWeb3.Transaction.from(arr);
            }

            window.solana.signAndSendTransaction(tx)
                .then(function(resp) {

                    var sig = resp.signature || resp.txSignature || "";

                    if (!sig) {
                        SendMessage(go, err, "Transaction returned no signature.");
                        return;
                    }

                    SendMessage(go, ok, sig);
                })
                .catch(function(e) {
                    SendMessage(go, err, "signAndSendTransaction failed: " + e.message);
                });

        } catch (e) {
            SendMessage(go, err, "Transaction parse error: " + e.message);
        }
    }

});
