/**
 * Kyber1024 Post-Quantum Key Encapsulation Module for React Native
 * 
 * Provides full Kyber1024 KEM operations via @noble/post-quantum WASM library,
 * with optional native module fallback for enhanced performance.
 * Compatible with .NET liboqs and Python liboqs implementations.
 */

import { NativeModules } from "react-native";
import { Buffer } from "buffer";
const crypto = require("crypto-browserify");

// Import WASM Kyber implementation
let kyber1024 = null;
try {
  // Dynamically import @noble/post-quantum to handle cases where it's not installed
  const noble = require("@noble/post-quantum/kyber");
  kyber1024 = noble.kyber1024;
} catch (e) {
  console.warn("@noble/post-quantum not available, Kyber operations will require native module");
}

// Note: For production, you would need a native module or WASM implementation
// This is a placeholder implementation that demonstrates the API structure
// 
// Recommended approaches:
// 1. Use react-native-pqc (if available) for native Kyber support
// 2. Use @noble/post-quantum or kyber-crystals WASM library
// 3. Create a native bridge to liboqs or PQClean

/**
 * Kyber1024 public key size in bytes
 */
export const KYBER_PUBLIC_KEY_BYTES = 1568;

/**
 * Kyber1024 private key size in bytes
 */
export const KYBER_PRIVATE_KEY_BYTES = 3168;

/**
 * Kyber1024 ciphertext size in bytes
 */
export const KYBER_CIPHERTEXT_BYTES = 1568;

/**
 * Kyber1024 shared secret size in bytes
 */
export const KYBER_SHARED_SECRET_BYTES = 32;

/**
 * Generates a Kyber1024 keypair for post-quantum key encapsulation.
 * 
 * @returns {Promise<{publicKey: Buffer, privateKey: Buffer}>} The keypair
 * 
 * @example
 * const { publicKey, privateKey } = await generateKeyPair();
 */
export function isNativeKyberAvailable() {
  // Check for WASM implementation first
  if (kyber1024 && typeof kyber1024.keygen === "function") {
    return true;
  }
  // Fall back to checking for native module
  const mod = NativeModules && NativeModules.KyberModule;
  return !!(mod && typeof mod.encapsulate === "function" && typeof mod.decapsulate === "function");
}

export async function generateKeyPair() {
  // Try WASM implementation first
  if (kyber1024 && typeof kyber1024.keygen === "function") {
    try {
      const keys = kyber1024.keygen();
      return {
        publicKey: Buffer.from(keys.publicKey),
        privateKey: Buffer.from(keys.secretKey)
      };
    } catch (error) {
      throw new Error(`Kyber WASM keygen failed: ${error.message}`);
    }
  }
  
  // Fall back to native module
  const mod = NativeModules && NativeModules.KyberModule;
  if (mod && typeof mod.generateKeyPair === "function") {
    const res = await mod.generateKeyPair();
    return {
      publicKey: Buffer.from(res.publicKey, "base64"),
      privateKey: Buffer.from(res.privateKey, "base64"),
    };
  }
  
  throw new Error(
    "Kyber1024 key generation requires @noble/post-quantum or a native Kyber module. Install: npm install @noble/post-quantum"
  );
}

/**
 * Encapsulates a shared secret using the recipient's public key.
 * 
 * @param {Buffer|string} publicKey - The recipient's Kyber1024 public key (Buffer or base64)
 * @returns {Promise<{ciphertext: Buffer, sharedSecret: Buffer}>} The encapsulated data
 * 
 * @example
 * const publicKey = Buffer.from(publicKeyBase64, 'base64');
 * const { ciphertext, sharedSecret } = await encapsulate(publicKey);
 */
export async function encapsulate(publicKey) {
  // Convert to Buffer if string
  const pkBuffer = typeof publicKey === 'string' 
    ? Buffer.from(publicKey, 'base64') 
    : publicKey;
  
  // Validate key size
  if (pkBuffer.length !== KYBER_PUBLIC_KEY_BYTES) {
    throw new Error(
      `Invalid public key size: expected ${KYBER_PUBLIC_KEY_BYTES} bytes, got ${pkBuffer.length}`
    );
  }
  
  // Try WASM implementation first
  if (kyber1024 && typeof kyber1024.encapsulate === "function") {
    try {
      const result = kyber1024.encapsulate(new Uint8Array(pkBuffer));
      return {
        ciphertext: Buffer.from(result.ciphertext),
        sharedSecret: Buffer.from(result.sharedSecret)
      };
    } catch (error) {
      throw new Error(`Kyber WASM encapsulation failed: ${error.message}`);
    }
  }
  
  // Fall back to native module
  const mod = NativeModules && NativeModules.KyberModule;
  if (mod && typeof mod.encapsulate === "function") {
    const result = await mod.encapsulate(pkBuffer.toString("base64"));
    return {
      ciphertext: Buffer.from(result.ciphertext, "base64"),
      sharedSecret: Buffer.from(result.sharedSecret, "base64"),
    };
  }
  
  throw new Error(
    "Kyber1024 encapsulation requires @noble/post-quantum or a native Kyber module. Install: npm install @noble/post-quantum"
  );
}

/**
 * Decapsulates a shared secret using the private key and ciphertext.
 * 
 * @param {Buffer|string} privateKey - The recipient's Kyber1024 private key (Buffer or base64)
 * @param {Buffer|string} ciphertext - The encapsulated ciphertext (Buffer or base64)
 * @returns {Promise<Buffer>} The shared secret
 * 
 * @example
 * const privateKey = Buffer.from(privateKeyBase64, 'base64');
 * const ciphertext = Buffer.from(ciphertextBase64, 'base64');
 * const sharedSecret = await decapsulate(privateKey, ciphertext);
 */
export async function decapsulate(privateKey, ciphertext) {
  // Convert to Buffers if strings
  const skBuffer = typeof privateKey === 'string' 
    ? Buffer.from(privateKey, 'base64') 
    : privateKey;
  
  const ctBuffer = typeof ciphertext === 'string' 
    ? Buffer.from(ciphertext, 'base64') 
    : ciphertext;
  
  // Validate sizes
  if (skBuffer.length !== KYBER_PRIVATE_KEY_BYTES) {
    throw new Error(
      `Invalid private key size: expected ${KYBER_PRIVATE_KEY_BYTES} bytes, got ${skBuffer.length}`
    );
  }
  
  if (ctBuffer.length !== KYBER_CIPHERTEXT_BYTES) {
    throw new Error(
      `Invalid ciphertext size: expected ${KYBER_CIPHERTEXT_BYTES} bytes, got ${ctBuffer.length}`
    );
  }
  
  // Try WASM implementation first
  if (kyber1024 && typeof kyber1024.decapsulate === "function") {
    try {
      const sharedSecret = kyber1024.decapsulate(
        new Uint8Array(ctBuffer),
        new Uint8Array(skBuffer)
      );
      return Buffer.from(sharedSecret);
    } catch (error) {
      throw new Error(`Kyber WASM decapsulation failed: ${error.message}`);
    }
  }
  
  // Fall back to native module
  const mod = NativeModules && NativeModules.KyberModule;
  if (mod && typeof mod.decapsulate === "function") {
    const sharedSecretBase64 = await mod.decapsulate(
      ctBuffer.toString("base64"),
      skBuffer.toString("base64")
    );
    return Buffer.from(sharedSecretBase64, "base64");
  }
  
  throw new Error(
    "Kyber1024 decapsulation requires @noble/post-quantum or a native Kyber module. Install: npm install @noble/post-quantum"
  );
}

/**
 * Derives a 32-byte AES-256 key from the Kyber shared secret using HKDF-SHA256.
 * 
 * @param {Buffer} sharedSecret - The shared secret from Kyber encapsulation/decapsulation
 * @param {Buffer} salt - Optional salt for key derivation (default: 32 zero bytes)
 * @param {Buffer|string} info - Optional context information (default: "AES-256-GCM")
 * @returns {Promise<Buffer>} A 32-byte AES-256 key
 * 
 * @example
 * const aesKey = await deriveAesKey(sharedSecret);
 */
export async function deriveAesKey(sharedSecret, salt = null, info = "AES-256-GCM") {
  // Default salt to 32 zero bytes
  if (!salt) {
    salt = Buffer.alloc(32);
  }
  
  // Convert info to Buffer if string
  const infoBuffer = typeof info === 'string' 
    ? Buffer.from(info, 'utf8') 
    : info;
  
  // Use HKDF-SHA256 to derive AES key
  return new Promise((resolve, reject) => {
    try {
      // HKDF implementation using crypto-browserify
      const hash = 'sha256';
      const hashLen = 32; // SHA-256 output length
      const okm = Buffer.alloc(32); // Output keying material (32 bytes for AES-256)
      
      // Step 1: Extract
      const prk = crypto.createHmac(hash, salt)
        .update(sharedSecret)
        .digest();
      
      // Step 2: Expand
      const n = Math.ceil(32 / hashLen);
      let t = Buffer.alloc(0);
      let result = Buffer.alloc(0);
      
      for (let i = 1; i <= n; i++) {
        const hmac = crypto.createHmac(hash, prk);
        hmac.update(t);
        hmac.update(infoBuffer);
        hmac.update(Buffer.from([i]));
        t = hmac.digest();
        result = Buffer.concat([result, t]);
      }
      
      resolve(result.slice(0, 32));
    } catch (error) {
      reject(error);
    }
  });
}

/**
 * Helper function to load keys and ciphertext from the shared directory
 * for cross-platform testing.
 * 
 * @param {Object} RNFS - React Native FS module instance
 * @returns {Promise<Object>} Object containing publicKey, privateKey, and kyberCiphertext
 */
export async function loadSharedKyberData(RNFS) {
  try {
    const publicKey = await RNFS.readFile("/data/kyber_public.key", "base64");
    const privateKey = await RNFS.readFile("/data/kyber_private.key", "base64");
    const kyberCiphertext = await RNFS.readFile("/data/kyber_ciphertext.bin", "base64");
    
    return {
      publicKey: Buffer.from(publicKey, 'base64'),
      privateKey: Buffer.from(privateKey, 'base64'),
      kyberCiphertext: Buffer.from(kyberCiphertext, 'base64')
    };
  } catch (error) {
    throw new Error(`Failed to load Kyber data: ${error.message}`);
  }
}
