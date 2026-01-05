/*
 * ML-KEM-1024 Post-Quantum Key Encapsulation Mechanism Helper
 * 
 * Provides utility methods for ML-KEM-1024 operations including key generation,
 * encapsulation, and decapsulation for post-quantum secure key exchange.
 * Uses Open Quantum Safe liboqs 0.10.0+ via native C shim and P/Invoke interop.
 */

using System;
using System.Security.Cryptography;

public static class KyberHelper
{
    /// <summary>
    /// Generates a ML-KEM-1024 keypair for post-quantum key encapsulation.
    /// </summary>
    /// <returns>Tuple containing (PublicKey, PrivateKey) as byte arrays</returns>
    public static (byte[] PublicKey, byte[] PrivateKey) GenerateKeyPair()
    {
        // Generate Kyber1024 keypair via liboqs
        return LibOqsKyber.GenerateKeyPair();
    }

    /// <summary>
    /// Encapsulates a shared secret using the recipient's public key.
    /// Generates a random 32-byte shared secret suitable for AES-256.
    /// </summary>
    /// <param name="publicKey">The recipient's ML-KEM-1024 public key</param>
    /// <returns>Tuple containing (Ciphertext, SharedSecret) where SharedSecret is 32 bytes for AES-256</returns>
    public static (byte[] Ciphertext, byte[] SharedSecret) Encapsulate(byte[] publicKey)
    {
        // Encapsulate using liboqs
        return LibOqsKyber.Encapsulate(publicKey);
    }

    /// <summary>
    /// Decapsulates a shared secret using the private key and ciphertext.
    /// </summary>
    /// <param name="privateKey">The recipient's ML-KEM-1024 private key</param>
    /// <param name="ciphertext">The encapsulated ciphertext from the sender</param>
    /// <returns>The 32-byte shared secret suitable for AES-256</returns>
    public static byte[] Decapsulate(byte[] privateKey, byte[] ciphertext)
    {
        // Decapsulate using liboqs
        return LibOqsKyber.Decapsulate(privateKey, ciphertext);
    }

    /// <summary>
    /// Derives a 32-byte AES key from the Kyber shared secret using HKDF.
    /// </summary>
    /// <param name="sharedSecret">The shared secret from Kyber encapsulation/decapsulation</param>
    /// <param name="salt">Optional salt for key derivation</param>
    /// <param name="info">Optional context information</param>
    /// <returns>A 32-byte AES-256 key</returns>
    public static byte[] DeriveAesKey(byte[] sharedSecret, byte[]? salt = null, byte[]? info = null)
    {
        // Use HKDF-SHA256 to derive a proper AES key from the shared secret
        salt ??= new byte[32]; // Default to zero salt if not provided
        info ??= System.Text.Encoding.UTF8.GetBytes("AES-256-GCM");

        // Manual HKDF implementation for .NET 8.0 compatibility
        return HkdfSha256(sharedSecret, salt, info, 32);
    }

    /// <summary>
    /// HKDF-SHA256 implementation (RFC 5869)
    /// </summary>
    private static byte[] HkdfSha256(byte[] ikm, byte[] salt, byte[] info, int outputLength)
    {
        // Step 1: Extract
        using var hmacExtract = new System.Security.Cryptography.HMACSHA256(salt);
        byte[] prk = hmacExtract.ComputeHash(ikm);

        // Step 2: Expand
        int hashLength = 32; // SHA-256 output length
        int n = (int)Math.Ceiling((double)outputLength / hashLength);
        byte[] result = new byte[outputLength];
        byte[] t = Array.Empty<byte>();
        int offset = 0;

        for (byte i = 1; i <= n; i++)
        {
            using var hmacExpand = new System.Security.Cryptography.HMACSHA256(prk);
            hmacExpand.TransformBlock(t, 0, t.Length, null, 0);
            hmacExpand.TransformBlock(info, 0, info.Length, null, 0);
            hmacExpand.TransformFinalBlock(new[] { i }, 0, 1);
            t = hmacExpand.Hash ?? throw new InvalidOperationException("HMAC hash is null");

            int copyLength = Math.Min(hashLength, outputLength - offset);
            Buffer.BlockCopy(t, 0, result, offset, copyLength);
            offset += copyLength;
        }

        return result;
    }
}
