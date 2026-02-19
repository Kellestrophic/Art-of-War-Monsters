// functions/src/awardMatchPayout.ts
import * as functions from "firebase-functions/v2/https";
import * as admin from "firebase-admin";
import {
  PublicKey,
  Keypair,
  Connection,
  clusterApiUrl,
  Transaction,
} from "@solana/web3.js";
import {
  getOrCreateAssociatedTokenAccount,
  createTransferInstruction,
  getMint,
} from "@solana/spl-token";
import { defineSecret } from "firebase-functions/params";

if (admin.apps.length === 0) admin.initializeApp();

// Store your Solana CLI treasury keypair (id.json contents) in Secret Manager
const TREASURY_SECRET = defineSecret("TREASURY_SECRET");

// Env vars (set in Cloud Run / Cloud Functions v2 UI or via gcloud)
const ENV = {
  SOLANA_CLUSTER: process.env.SOLANA_CLUSTER || "devnet", // "mainnet-beta" when live
  SOLANA_RPC: process.env.SOLANA_RPC || "",               // optional custom RPC
  MCC_MINT: process.env.MCC_MINT || "",                   // SPL mint address for MCC
  WIN_XP: parseInt(process.env.WIN_XP || "100", 10),
  LOSS_XP: parseInt(process.env.LOSS_XP || "60", 10),
  WIN_MCC: parseInt(process.env.WIN_MCC || "50", 10),
  LOSS_MCC: parseInt(process.env.LOSS_MCC || "10", 10),
};

type PlayerIn = { clientId: number; wallet: string };
type Payout = {
  clientId: number;
  wallet: string;
  isWin: boolean;
  xp: number;
  mcc: number;
};

export const awardMatchPayout = functions.onRequest(
  { cors: false, secrets: [TREASURY_SECRET] },
  async (req, res) => {
    try {
      const { matchId, winnerWallet, players, disableMcc } = req.body as {
        matchId: string;
        winnerWallet: string;
        players: PlayerIn[];
        disableMcc?: boolean; // <-- AFK flag comes from server
      };

      if (!matchId || !winnerWallet || !players?.length) {
        res.status(400).send({ error: "bad payload" });
        return;
      }
      if (!ENV.MCC_MINT) throw new Error("Missing MCC_MINT env var");

      // --- Solana setup ---
      const rpcUrl = ENV.SOLANA_RPC || clusterApiUrl(ENV.SOLANA_CLUSTER as any);
      const connection = new Connection(rpcUrl, "confirmed");

      const secretStr = TREASURY_SECRET.value(); // read from Secret Manager
      const treasury = keypairFromSecret(secretStr);

      const mintPk = new PublicKey(ENV.MCC_MINT);
      const mintInfo = await getMint(connection, mintPk);
      const decimals = mintInfo.decimals;
      const toUnits = (n: number) => Number(n) * 10 ** decimals; // amounts are small → safe as number

      // --- Compute payouts (server-authoritative) ---
      const payouts: Payout[] = players.map((p) => {
        const isWin = sameAddr(p.wallet, winnerWallet);
        const baseXP = isWin ? ENV.WIN_XP : ENV.LOSS_XP;
        const baseMCC = isWin ? ENV.WIN_MCC : ENV.LOSS_MCC;

        return {
          clientId: p.clientId,
          wallet: p.wallet,
          isWin,
          xp: baseXP,
          mcc: disableMcc ? 0 : baseMCC, // <-- zero MCC if AFK flagged
        };
      });

      // --- Firestore totals + per-match ledger ---
      const db = admin.firestore();
      const batch = db.batch();

      // Match audit document (records AFK flag)
      batch.set(db.collection("matches").doc(matchId), {
        winnerWallet,
        afk: !!disableMcc,
        players: players.map((p) => ({ wallet: p.wallet })),
        createdAt: admin.firestore.FieldValue.serverTimestamp(),
      });

      for (const p of payouts) {
        const pref = db.collection("profiles").doc(p.wallet);
        batch.set(
          pref,
          {
            wallet: p.wallet,
            totals: {
              wins: admin.firestore.FieldValue.increment(p.isWin ? 1 : 0),
              losses: admin.firestore.FieldValue.increment(p.isWin ? 0 : 1),
              xp: admin.firestore.FieldValue.increment(p.xp),
              mcc: admin.firestore.FieldValue.increment(p.mcc),
            },
          },
          { merge: true }
        );

        batch.set(pref.collection("ledger").doc(matchId), {
          matchId,
          isWin: p.isWin,
          xp: p.xp,
          mcc: p.mcc,
          at: admin.firestore.FieldValue.serverTimestamp(),
        });
      }

      await batch.commit();

      // --- SPL transfers (skip entirely if AFK flagged) ---
      if (!disableMcc) {
        for (const p of payouts) {
          if (p.mcc <= 0) continue;
          if (!isValidAddr(p.wallet)) continue;

          try {
            await sendSPL(
              connection,
              treasury,
              mintPk,
              new PublicKey(p.wallet),
              toUnits(p.mcc)
            );
          } catch (e) {
            console.error("MCC transfer failed to", p.wallet, e);
            // Optional: enqueue a retry via Cloud Tasks if you need guaranteed delivery.
          }
        }
      }

      // --- Return per-client results with current totals (post-commit) ---
      const result: Array<{
        clientId: number;
        result: {
          isWin: boolean;
          xp: number;
          mcc: number;
          newWins: number;
          newLosses: number;
          newXP: number;
          newMCC: number;
        };
      }> = [];

      for (const p of payouts) {
        const snap = await db.collection("profiles").doc(p.wallet).get();
        const t = snap.data()?.totals || { wins: 0, losses: 0, xp: 0, mcc: 0 };
        result.push({
          clientId: p.clientId,
          result: {
            isWin: p.isWin,
            xp: p.xp,
            mcc: p.mcc,
            newWins: t.wins ?? 0,
            newLosses: t.losses ?? 0,
            newXP: t.xp ?? 0,
            newMCC: t.mcc ?? 0,
          },
        });
      }

      res.status(200).send(result);
    } catch (err: any) {
      console.error(err);
      res.status(500).send({ error: err.message || "internal" });
    }
  }
);

// ---------------- helpers ----------------

function keypairFromSecret(secret: string): Keypair {
  // Expecting the JSON array contents of Solana CLI id.json
  try {
    const arr = JSON.parse(secret) as number[];
    return Keypair.fromSecretKey(Uint8Array.from(arr));
  } catch {
    throw new Error(
      "TREASURY_SECRET must be the JSON array content of your Solana CLI id.json"
    );
  }
}

function sameAddr(a: string, b: string) {
  return a?.trim()?.toLowerCase() === b?.trim()?.toLowerCase();
}

function isValidAddr(a: string) {
  try {
    new PublicKey(a);
    return true;
  } catch {
    return false;
  }
}

async function sendSPL(
  connection: Connection,
  treasury: Keypair,
  mint: PublicKey,
  toOwner: PublicKey,
  amountUnits: number
) {
  // Ensure treasury & recipient ATAs exist; use treasury ATA as source
  const fromAta = (
    await getOrCreateAssociatedTokenAccount(connection, treasury, mint, treasury.publicKey)
  ).address;

  const toAta = (
    await getOrCreateAssociatedTokenAccount(connection, treasury, mint, toOwner)
  ).address;

  const ix = createTransferInstruction(fromAta, toAta, treasury.publicKey, amountUnits);
  const tx = new Transaction().add(ix);
  const sig = await connection.sendTransaction(tx, [treasury], { skipPreflight: false });
  await connection.confirmTransaction(sig, "confirmed");
}
