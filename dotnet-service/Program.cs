/*
 * Post-Quantum Hybrid Encryption Service (.NET)
 * 
 * This program demonstrates post-quantum secure encryption using Kyber1024 KEM
 * combined with AES-256-GCM. The Kyber1024 algorithm provides quantum-resistant
 * key encapsulation, while AES-GCM handles the actual data encryption.
 * 
 * Encryption Flow:
 * 1. Generate Kyber1024 keypair (or load existing)
 * 2. Encapsulate a shared secret using recipient's public key
 * 3. Derive AES-256 key from shared secret
 * 4. Encrypt data with AES-256-GCM using derived key
 * 5. Store: [kyber_ciphertext][aes_encrypted_data]
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

        // Define file paths
        string plainPath = $"{sharedDir}/sample.txt";
        string encPath = $"{sharedDir}/encrypted.bin";
        string decPath = $"{sharedDir}/decrypted-dotnet.txt";
        
        // Kyber key paths
        string publicKeyPath = $"{sharedDir}/kyber_public.key";
        string privateKeyPath = $"{sharedDir}/kyber_private.key";
        string kyberCtPath = $"{sharedDir}/kyber_ciphertext.bin";

        // ============ KYBER KEY GENERATION ============
        Console.WriteLine("DotNet: Generating Kyber1024 keypair...");
        var (publicKey, privateKey) = KyberHelper.GenerateKeyPair();
        
        // Save keys for cross-platform testing
        File.WriteAllBytes(publicKeyPath, publicKey);
        File.WriteAllBytes(privateKeyPath, privateKey);
        
        // Also save as base64 for easier reading
        File.WriteAllText($"{sharedDir}/kyber_public.txt", Convert.ToBase64String(publicKey));
        File.WriteAllText($"{sharedDir}/kyber_private.txt", Convert.ToBase64String(privateKey));
        
        Console.WriteLine($"DotNet: Public key size: {publicKey.Length} bytes");
        Console.WriteLine($"DotNet: Private key size: {privateKey.Length} bytes");

        // ============ ENCRYPTION ============
        Console.WriteLine("\nDotNet: Starting encryption...");
        
        // Step 1: Encapsulate shared secret using Kyber1024
        var (kyberCiphertext, sharedSecret) = KyberHelper.Encapsulate(publicKey);
        
        // Save Kyber ciphertext
        File.WriteAllBytes(kyberCtPath, kyberCiphertext);
        File.WriteAllText($"{sharedDir}/kyber_ciphertext.txt", Convert.ToBase64String(kyberCiphertext));
        
        Console.WriteLine($"DotNet: Kyber ciphertext size: {kyberCiphertext.Length} bytes");
        Console.WriteLine($"DotNet: Shared secret size: {sharedSecret.Length} bytes");
        
        // Step 2: Derive AES-256 key from shared secret
        byte[] aesKey = KyberHelper.DeriveAesKey(sharedSecret);
        
        // Save AES key for legacy compatibility testing
        File.WriteAllText($"{sharedDir}/key.txt", Convert.ToBase64String(aesKey));
        
        // Step 3: Encrypt plaintext with AES-256-GCM
        byte[] plaintext = File.ReadAllBytes(plainPath);
        byte[] aesEncrypted = AesGcmHelper.Encrypt(plaintext, aesKey);
        
        // Step 4: Combine Kyber ciphertext and AES encrypted data
        // Format: [kyber_ciphertext_length:4bytes][kyber_ciphertext][aes_encrypted]
        byte[] fullEncrypted = new byte[4 + kyberCiphertext.Length + aesEncrypted.Length];
        BitConverter.GetBytes(kyberCiphertext.Length).CopyTo(fullEncrypted, 0);
        kyberCiphertext.CopyTo(fullEncrypted, 4);
        aesEncrypted.CopyTo(fullEncrypted, 4 + kyberCiphertext.Length);
        
        File.WriteAllBytes(encPath, fullEncrypted);
        
        Console.WriteLine("DotNet: File encrypted with post-quantum security.");

        // ============ DECRYPTION ============
        Console.WriteLine("\nDotNet: Starting decryption...");
        
        // Step 1: Read encrypted file
        byte[] fullEncryptedData = File.ReadAllBytes(encPath);
        
        // Step 2: Extract Kyber ciphertext length
        int kyberCtLen = BitConverter.ToInt32(fullEncryptedData, 0);
        
        // Step 3: Extract Kyber ciphertext and AES encrypted data
        byte[] extractedKyberCt = new byte[kyberCtLen];
        byte[] extractedAesData = new byte[fullEncryptedData.Length - 4 - kyberCtLen];
        
        Buffer.BlockCopy(fullEncryptedData, 4, extractedKyberCt, 0, kyberCtLen);
        Buffer.BlockCopy(fullEncryptedData, 4 + kyberCtLen, extractedAesData, 0, extractedAesData.Length);
        
        // Step 4: Decapsulate shared secret using Kyber private key
        byte[] recoveredSecret = KyberHelper.Decapsulate(privateKey, extractedKyberCt);
        
        // Step 5: Derive AES key from recovered secret
        byte[] recoveredAesKey = KyberHelper.DeriveAesKey(recoveredSecret);
        
        // Step 6: Decrypt AES data
        byte[] decrypted = AesGcmHelper.Decrypt(extractedAesData, recoveredAesKey);

        // Write the decrypted plaintext to the output file
        File.WriteAllBytes(decPath, decrypted);

        Console.WriteLine("DotNet: File decrypted successfully.");
        Console.WriteLine($"DotNet: Decrypted content: {System.Text.Encoding.UTF8.GetString(decrypted)}");
    }
}
