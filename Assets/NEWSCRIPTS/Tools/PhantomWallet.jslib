mergeInto(LibraryManager.library, {
  ConnectPhantomWallet: function () {
    if (typeof window.solana !== 'undefined') {
      window.solana.connect().then((res) => {
        let walletAddress = res.publicKey.toString();
        SendMessage('PhantomWalletConnector', 'OnWalletConnected', walletAddress);
      }).catch((err) => {
        console.error("Phantom connection failed:", err);
      });
    } else {
      alert("Phantom Wallet not found.");
    }
  },

  SendSPLToken: function (recipient, amount) {
    // Placeholder: You'll need a JS script that signs and sends a custom SPL token via Phantom
    console.log("Sending SPL token to " + UTF8ToString(recipient) + " amount: " + amount);
    // You’ll likely invoke an external signed transaction here
  }
});