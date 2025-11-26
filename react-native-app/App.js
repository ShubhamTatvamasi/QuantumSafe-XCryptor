/**
 * React Native AES-GCM Proof of Concept
 * 
 * This application demonstrates cross-platform AES-256-GCM encryption and decryption
 * in a React Native mobile environment. It can both encrypt new data and decrypt
 * files that were encrypted by the Python or .NET services using the shared AES key.
 */

import React, { useEffect } from "react";
import { Text, SafeAreaView } from "react-native";
import { encryptFile, decryptFile } from "./aes";
import * as RNFS from "react-native-fs";

export default function App() {
  // Run the encryption/decryption tests when the component mounts
  useEffect(() => {
    /**
     * Test function that performs the encryption and decryption workflow:
     * 1. Reads the shared AES key
     * 2. Encrypts a sample message and saves it
     * 3. Decrypts the encrypted file and logs the result
     * 4. Optionally decrypts files encrypted by other services
     */
    async function test() {
      // Read the base64-encoded AES-256 key from the shared directory
      const key = await RNFS.readFile("/data/key.txt", "utf8");
      const keyTrimmed = key.trim();

      // Test Encryption
      console.log("=== Testing Encryption ===");
      const plaintext = "Hello from React Native! This message was encrypted using AES-256-GCM.";
      
      // Encrypt the plaintext
      const encrypted = await encryptFile(plaintext, keyTrimmed);
      console.log("Encrypted (base64):", encrypted.substring(0, 50) + "...");
      
      // Save the encrypted data to a file
      await RNFS.writeFile("/data/encrypted-reactnative.bin", encrypted, "base64");
      console.log("Saved encrypted file to /data/encrypted-reactnative.bin");

      // Test Decryption of our own encrypted data
      console.log("\n=== Testing Decryption (own data) ===");
      const decrypted = await decryptFile(encrypted, keyTrimmed);
      console.log("Decrypted:", decrypted);

      // Test Decryption of file encrypted by Python/dotnet service
      try {
        console.log("\n=== Testing Decryption (from Python/dotnet) ===");
        const encryptedFromOther = await RNFS.readFile("/data/encrypted.bin", "base64");
        const decryptedFromOther = await decryptFile(encryptedFromOther, keyTrimmed);
        console.log("Decrypted from Python/dotnet:", decryptedFromOther);
      } catch (error) {
        console.log("Could not decrypt external file (may not exist yet):", error.message);
      }
    }

    // Execute the test
    test();
  }, []); // Empty dependency array ensures this runs only once on mount

  return (
    <SafeAreaView>
      <Text>React Native AES-GCM POC</Text>
      <Text>Check console for encryption/decryption results</Text>
    </SafeAreaView>
  );
}
