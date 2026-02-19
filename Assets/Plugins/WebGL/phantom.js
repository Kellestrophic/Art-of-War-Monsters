mergeInto(LibraryManager.library, {
    ConnectPhantomWallet: function () {
        if (window.solana && window.solana.isPhantom) {
            window.solana.connect()
                .then((res) => {
                    const walletAddress = res.publicKey.toString();
                    alert("Wallet Connected: " + walletAddress);
                    
                    // 🔁 Send wallet back into Unity
                    SendMessage("WalletBridge", "OnWalletConnected", walletAddress);
                })
                .catch((err) => {
                    alert("Phantom connection failed: " + err.message);
                });
        } else {
            alert("Phantom Wallet not detected. Please install it.");
        }
    }
});