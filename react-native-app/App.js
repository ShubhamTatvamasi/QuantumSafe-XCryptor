/**
 * QuantumSafe-XCryptor - React Native Mobile Client
 * 
 * Post-quantum hybrid encryption client using ML-KEM-1024 + AES-256-GCM.
 * 
 * Architecture:
 * - Downloads server's ML-KEM-1024 public key from Flask server
 * - Encapsulates shared secret using server's public key (@noble/post-quantum)
 * - Derives AES-256 key using HKDF-SHA256 (identical to server and .NET)
 * - Encrypts file locally (zero-knowledge upload)
 * - Uploads encrypted packet to server: [Kyber CT][AES encrypted data]
 * 
 * HKDF Parameters (MUST match server and .NET):
 * - Salt: 32 zero bytes
 * - Info: "AES-256-GCM"
 * - Hash: SHA-256
 * - Output: 32 bytes (AES-256 key)
 * 
 * Dependencies:
 * - @noble/post-quantum (WASM Kyber1024)
 * - crypto-browserify (HKDF, HMAC)
 * - react-native-aes-crypto (AES-256-GCM)
 */

import React, { useEffect, useState } from "react";
import { Text, SafeAreaView, ScrollView, StyleSheet, ActivityIndicator } from "react-native";
import { Buffer } from "buffer";

// Cryptographic imports
import { kyber1024 } from "@noble/post-quantum/kyber";
import crypto from "crypto-browserify";

// For file operations (if available)
import * as RNFS from "react-native-fs";

export default function App() {
  const [status, setStatus] = useState("🚀 Initializing...");
  const [logs, setLogs] = useState([]);
  const [isLoading, setIsLoading] = useState(true);

  // Configuration
  const SERVER_URL = "http://python-service:5000";
  const KYBER_PUBLIC_KEY_SIZE = 1568;
  const KYBER_CIPHERTEXT_SIZE = 1568;
  const KYBER_SHARED_SECRET_SIZE = 32;
  const HKDF_SALT = Buffer.alloc(32);  // 32 zero bytes
  const HKDF_INFO = Buffer.from("AES-256-GCM");
  const HKDF_OUTPUT_LENGTH = 32;

  const addLog = (message) => {
    console.log(message);
    setLogs(prev => [...prev, message]);
  };

  const deriveAesKey = (sharedSecret) => {
    /**
     * Derive AES-256 key from Kyber shared secret using HKDF-SHA256.
     * CRITICAL: Must be IDENTICAL on all platforms (server, .NET, React Native)
     */
    if (sharedSecret.length !== KYBER_SHARED_SECRET_SIZE) {
      throw new Error(`Shared secret must be ${KYBER_SHARED_SECRET_SIZE} bytes`);
    }

    // HKDF-SHA256: Extract phase
    const hmac1 = crypto.createHmac("sha256", HKDF_SALT);
    hmac1.update(sharedSecret);
    const prk = hmac1.digest();

    // HKDF-SHA256: Expand phase
    const hmac2 = crypto.createHmac("sha256", prk);
    hmac2.update(Buffer.concat([HKDF_INFO, Buffer.from([0x01])]));
    const okm = hmac2.digest();

    return okm.slice(0, HKDF_OUTPUT_LENGTH);
  };

  const encryptAesGcm = async (plaintext, aesKey) => {
    /**
     * Encrypt plaintext with AES-256-GCM.
     * Returns: [nonce: 12][ciphertext][tag: 16]
     */
    if (aesKey.length !== 32) {
      throw new Error("AES key must be 32 bytes");
    }

    // Generate random 12-byte nonce
    const nonce = crypto.randomBytes(12);

    // For React Native, we'd use react-native-aes-crypto
    // For now, use a placeholder implementation
    // In production, import and use the proper module

    // Create cipher (using a simple approach for demo)
    const cipher = crypto.createCipheriv("aes-256-gcm", aesKey, nonce);
    const ciphertext = Buffer.concat([
      cipher.update(plaintext, "utf8"),
      cipher.final(),
    ]);
    const tag = cipher.getAuthTag();

    // Return: [nonce][ciphertext][tag]
    return Buffer.concat([nonce, ciphertext, tag]);
  };

  const fetchServerPublicKey = async () => {
    /**
     * Download server's ML-KEM-1024 public key from /api/kyber/public-key endpoint
     */
    try {
      addLog(`📥 Fetching public key from ${SERVER_URL}/api/kyber/public-key...`);
      const response = await fetch(`${SERVER_URL}/api/kyber/public-key`, {
        method: "GET",
        timeout: 30000,
      });

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }

      const arrayBuffer = await response.arrayBuffer();
      const publicKey = Buffer.from(arrayBuffer);

      if (publicKey.length !== KYBER_PUBLIC_KEY_SIZE) {
        throw new Error(
          `Invalid public key size: ${publicKey.length} (expected ${KYBER_PUBLIC_KEY_SIZE})`
        );
      }

      addLog(`✓ Downloaded public key: ${publicKey.length} bytes`);
      return publicKey;
    } catch (error) {
      throw new Error(`Failed to fetch public key: ${error.message}`);
    }
  };

  const uploadEncryptedFile = async (encryptedPacket) => {
    /**
     * Upload encrypted file to server's /api/files/upload endpoint
     * Packet format: [Kyber CT: 1568][AES encrypted: variable]
     */
    try {
      addLog(
        `📤 Uploading encrypted file to ${SERVER_URL}/api/files/upload (${encryptedPacket.length} bytes)...`
      );

      const response = await fetch(`${SERVER_URL}/api/files/upload`, {
        method: "POST",
        headers: {
          "Content-Type": "application/octet-stream",
        },
        body: encryptedPacket,
        timeout: 120000,
      });

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }

      const responseText = await response.text();
      addLog(`✓ Upload successful: ${responseText}`);
    } catch (error) {
      throw new Error(`Failed to upload file: ${error.message}`);
    }
  };

  const encryptFile = async (plaintext, serverPublicKey) => {
    /**
     * Encrypt file using ML-KEM-1024 + AES-256-GCM hybrid encryption.
     *
     * Flow:
     * 1. Encapsulate shared secret using server's public key
     * 2. Derive AES key from shared secret using HKDF-SHA256
     * 3. Encrypt plaintext with AES-256-GCM
     * 4. Return: [Kyber ciphertext: 1568][AES encrypted: variable]
     */
    try {
      // Step 1: Encapsulate
      addLog("  [1/3] Encapsulating shared secret with Kyber1024...");
      const { ciphertext: kyberCiphertext, sharedSecret } = await kyber1024.encapsulate(
        serverPublicKey
      );

      if (kyberCiphertext.length !== KYBER_CIPHERTEXT_SIZE) {
        throw new Error(`Invalid Kyber ciphertext size: ${kyberCiphertext.length}`);
      }
      if (sharedSecret.length !== KYBER_SHARED_SECRET_SIZE) {
        throw new Error(`Invalid Kyber shared secret size: ${sharedSecret.length}`);
      }

      addLog(`      ✓ Ciphertext: ${kyberCiphertext.length} bytes`);
      addLog(`      ✓ Shared secret: ${sharedSecret.length} bytes`);

      // Step 2: Derive AES key using HKDF-SHA256 (IDENTICAL to server)
      addLog("  [2/3] Deriving AES key (HKDF-SHA256)...");
      const aesKey = deriveAesKey(sharedSecret);
      addLog(`      ✓ AES key: ${aesKey.length} bytes (salt=32x0, info='AES-256-GCM')`);

      // Step 3: Encrypt with AES-256-GCM
      addLog("  [3/3] Encrypting with AES-256-GCM...");
      const plaintextBuffer = Buffer.from(plaintext, "utf8");
      const aesEncrypted = await encryptAesGcm(plaintextBuffer, aesKey);
      addLog(`      ✓ Encrypted: ${aesEncrypted.length} bytes (includes nonce:12 + CT + tag:16)`);

      // Combine into packet: [Kyber CT][AES encrypted]
      const packet = Buffer.concat([kyberCiphertext, aesEncrypted]);
      return packet;
    } catch (error) {
      throw new Error(`Encryption failed: ${error.message}`);
    }
  };

  useEffect(() => {
    async function runEncryptionWorkflow() {
      try {
        addLog("\n" + "=".repeat(60));
        addLog("  QuantumSafe-XCryptor - React Native Mobile Client");
        addLog("=".repeat(60) + "\n");

        // Step 1: Fetch server's public key
        setStatus("📥 Fetching server public key...");
        const serverPublicKey = await fetchServerPublicKey();

        // Step 2: Prepare sample plaintext
        setStatus("🔐 Encrypting file...");
        addLog("\n🔐 Encrypting file...");
        const plaintext = "Hello from React Native! 🚀 Post-quantum encryption is now available on mobile.";
        const encryptedPacket = await encryptFile(plaintext, serverPublicKey);
        addLog(`✓ File encrypted: ${plaintext.length} → ${encryptedPacket.length} bytes\n`);

        // Step 3: Upload encrypted file to server
        setStatus("📤 Uploading to server...");
        await uploadEncryptedFile(encryptedPacket);

        // Step 4: Save locally (if RNFS available)
        try {
          await RNFS.writeFile(
            "/data/encrypted-reactnative.bin",
            encryptedPacket.toString("base64"),
            "base64"
          );
          addLog("✓ Saved encrypted packet: /data/encrypted-reactnative.bin");
        } catch (e) {
          addLog("⚠ Could not save locally (RNFS unavailable)");
        }

        addLog("\n" + "=".repeat(60));
        addLog("✓ Encryption workflow completed successfully");
        addLog("=".repeat(60));

        setStatus("✅ Encryption complete!");
        setIsLoading(false);
      } catch (error) {
        addLog(`\n✗ ERROR: ${error.message}`);
        console.error(error);
        setStatus(`❌ ${error.message}`);
        setIsLoading(false);
      }
    }

    runEncryptionWorkflow();
  }, []);

  return (
    <SafeAreaView style={styles.container}>
      <ScrollView style={styles.scrollView}>
        <Text style={styles.title}>🔐 QuantumSafe-XCryptor</Text>
        <Text style={styles.subtitle}>React Native Mobile Client</Text>
        <Text style={styles.subtitle2}>ML-KEM-1024 + AES-256-GCM</Text>

        {isLoading && <ActivityIndicator size="large" color="#00ff00" />}

        <Text style={styles.status}>{status}</Text>

        <Text style={styles.logTitle}>📝 Console Output:</Text>
        {logs.map((log, index) => (
          <Text key={index} style={styles.log}>
            {log}
          </Text>
        ))}
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: "#1a1a1a",
  },
  scrollView: {
    flex: 1,
    padding: 20,
  },
  title: {
    fontSize: 24,
    fontWeight: "bold",
    color: "#00ff00",
    marginBottom: 5,
    marginTop: 10,
  },
  subtitle: {
    fontSize: 16,
    color: "#00cc00",
    marginBottom: 5,
    fontWeight: "600",
  },
  subtitle2: {
    fontSize: 12,
    color: "#00aa00",
    marginBottom: 20,
  },
  status: {
    fontSize: 16,
    color: "#ffff00",
    marginBottom: 20,
    fontWeight: "bold",
    padding: 10,
    backgroundColor: "#333333",
    borderRadius: 5,
  },
  logTitle: {
    fontSize: 14,
    color: "#ffffff",
    marginBottom: 10,
    fontWeight: "bold",
    marginTop: 20,
  },
  log: {
    fontSize: 11,
    color: "#cccccc",
    marginBottom: 4,
    fontFamily: "monospace",
    lineHeight: 14,
  },
});


