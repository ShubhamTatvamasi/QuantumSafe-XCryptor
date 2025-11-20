/*
 * AES-256-GCM Encryption and Decryption Service (.NET)
 * 
 * This program demonstrates .NET's AES-256-GCM implementation by encrypting
 * a plaintext file and then decrypting it. The encrypted file can also be
 * decrypted by the Python service to demonstrate cross-language compatibility.
 */

using System;
using System.IO;
using System.Security.Cryptography;

class Program
{
    static void Main()
    {
        // Path to the shared data directory
        string sharedDir = "/data";
        
        // Read and decode the base64-encoded AES-256 key
        string keyB64 = File.ReadAllText($"{sharedDir}/key.txt").Trim();
        byte[] key = Convert.FromBase64String(keyB64);

        // Define file paths
        string plainPath = $"{sharedDir}/sample.txt";
        string encPath = $"{sharedDir}/encrypted.bin";
        string decPath = $"{sharedDir}/decrypted-dotnet.txt";

        // ============ ENCRYPTION ============
        // Read the plaintext file
        byte[] plaintext = File.ReadAllBytes(plainPath);
        
        // Encrypt the data using AES-256-GCM
        byte[] encrypted = AesGcmHelper.Encrypt(plaintext, key);
        
        // Write the encrypted data to file (format: [nonce][ciphertext][tag])
        File.WriteAllBytes(encPath, encrypted);
        
        Console.WriteLine("DotNet: File encrypted.");

        // ============ DECRYPTION ============
        // Read the encrypted file
        byte[] raw = File.ReadAllBytes(encPath);
        
        // Decrypt using the AesGcmHelper utility class
        byte[] decrypted = AesGcmHelper.Decrypt(raw, key);

        // Write the decrypted plaintext to the output file
        File.WriteAllBytes(decPath, decrypted);

        Console.WriteLine("DotNet: File decrypted.");
    }
}
