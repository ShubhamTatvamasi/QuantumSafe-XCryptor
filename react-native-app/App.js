/**
 * React Native AES-GCM Proof of Concept
 * 
 * This application demonstrates cross-platform AES-256-GCM decryption
 * in a React Native mobile environment. It decrypts a file that was
 * encrypted by the Python service using the shared AES key.
 */

import React, { useEffect } from "react";
import { Text, SafeAreaView } from "react-native";
import { decryptFile } from "./aes";
import * as RNFS from "react-native-fs";

export default function App() {
  // Run the decryption test when the component mounts
  useEffect(() => {
    /**
     * Test function that performs the decryption workflow:
     * 1. Reads the shared AES key
     * 2. Reads the encrypted file (created by Python service)
     * 3. Decrypts the file and logs the result
     */
    async function test() {
      // Read the base64-encoded AES-256 key from the shared directory
      const key = await RNFS.readFile("/data/key.txt", "utf8");
      
      // Read the encrypted file (nonce + ciphertext + tag) in base64 format
      const encrypted = await RNFS.readFile("/data/encrypted.bin", "base64");

      // Decrypt the file using the shared key
      const dec = await decryptFile(encrypted, key.trim());
      
      // Log the decrypted plaintext to the console
      console.log("ReactNative:", dec);
    }

    // Execute the test
    test();
  }, []); // Empty dependency array ensures this runs only once on mount

  return (
    <SafeAreaView>
      <Text>React Native AES-GCM POC</Text>
    </SafeAreaView>
  );
}
