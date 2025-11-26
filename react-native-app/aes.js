/**
 * AES-GCM Encryption/Decryption Module for React Native
 * 
 * Provides encryption and decryption functionality compatible with Python's cryptography
 * library and .NET's AesGcm class, demonstrating cross-platform cryptographic
 * interoperability in a mobile environment.
 */

import aes from "react-native-aes-gcm";
import { Buffer } from "buffer";
import { randomBytes } from "react-native-randombytes";

/**
 * Encrypts plaintext using AES-256-GCM.
 * 
 * @param {string} plaintext - The plaintext data to encrypt
 * @param {string} keyBase64 - The base64-encoded 32-byte AES-256 key
 * @returns {Promise<string>} The encrypted data in base64 format
 *                            Format: [12-byte nonce][ciphertext][16-byte tag]
 * 
 * @example
 * const plaintext = "Secret message";
 * const key = "...base64 key...";
 * const encrypted = await encryptFile(plaintext, key);
 */
export async function encryptFile(plaintext, keyBase64) {
  // Generate a random 12-byte nonce (initialization vector)
  const iv = await new Promise((resolve, reject) => {
    randomBytes(12, (err, bytes) => {
      if (err) reject(err);
      else resolve(Buffer.from(bytes));
    });
  });

  // Perform AES-GCM encryption
  // react-native-aes-gcm returns { content, tag } where both are base64 strings
  const { content: ctBase64, tag: tagBase64 } = await aes.encrypt(
    plaintext,
    keyBase64,
    iv.toString("base64")
  );

  // Convert base64 strings back to Buffers
  const ct = Buffer.from(ctBase64, "base64");
  const tag = Buffer.from(tagBase64, "base64");

  // Combine: [nonce][ciphertext][tag] to match .NET and Python format
  const result = Buffer.concat([iv, ct, tag]);

  // Return as base64 string
  return result.toString("base64");
}

/**
 * Decrypts a file encrypted with AES-256-GCM.
 * 
 * @param {string} tokenBase64 - The complete encrypted data in base64 format
 *                               Format: [12-byte nonce][ciphertext][16-byte tag]
 * @param {string} keyBase64 - The base64-encoded 32-byte AES-256 key
 * @returns {Promise<string>} The decrypted plaintext as a UTF-8 string
 * 
 * @example
 * const encrypted = "...base64 encrypted data...";
 * const key = "...base64 key...";
 * const plaintext = await decryptFile(encrypted, key);
 */
export async function decryptFile(tokenBase64, keyBase64) {
  // Convert base64 encrypted data to a Buffer
  const raw = Buffer.from(tokenBase64, "base64");
  
  // Extract the 12-byte nonce (initialization vector) from the beginning
  const iv = raw.slice(0, 12);
  
  // Extract the ciphertext (between nonce and tag)
  const ct = raw.slice(12, -16);
  
  // Extract the 16-byte authentication tag from the end
  const tag = raw.slice(-16);

  // Perform AES-GCM decryption with authentication verification
  // react-native-aes-gcm expects all parameters as base64 strings
  const plaintext = await aes.decrypt(
    ct.toString("base64"),
    keyBase64,
    iv.toString("base64"),
    tag.toString("base64")
  );

  return plaintext;
}
