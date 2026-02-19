const functions = require("firebase-functions");
const admin = require("firebase-admin");
admin.initializeApp();

exports.generateFirebaseToken = functions.https.onRequest(async (req, res) => {
  const walletAddress = req.body.walletAddress;

  if (!walletAddress) {
    return res.status(400).send("Missing wallet address");
  }

  const uid = `wallet:${walletAddress}`;
  try {
    const customToken = await admin.auth().createCustomToken(uid);
    return res.status(200).json({ customToken });
  } catch (error) {
    console.error("Error creating custom token:", error);
    return res.status(500).send("Token generation failed");
  }
});
