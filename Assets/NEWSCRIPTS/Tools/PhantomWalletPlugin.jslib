mergeInto(LibraryManager.library, {
  ConnectPhantomWallet: function () {
    if (typeof window.solana !== 'undefined') {
      window.solana.connect()
        .then((response) => {
          const walletAddress = response.publicKey.toString();
          console.log("🔗 Phantom connected:", walletAddress);
          unityInstance.SendMessage('PhantomBridgeHandler', 'OnWalletConnectedFromJS', walletAddress);
        })
        .catch((err) => {
          console.warn("❌ Phantom connection failed:", err);
        });
    } else {
      alert("Phantom Wallet not found. Please install it.");
    }
  }
});