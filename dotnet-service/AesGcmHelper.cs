/*
 * AES-GCM Encryption and Decryption Helper Class
 * 
 * Provides utility methods for AES-256-GCM encryption and decryption that are compatible
 * with the Python cryptography library's AESGCM implementation.
 */

using System;
using System.Linq;
using System.Security.Cryptography;

public static class AesGcmHelper
{
    /// <summary>
    /// Encrypts data using AES-256-GCM.
    /// </summary>
    /// <param name="plaintext">The plaintext data to encrypt</param>
    /// <param name="key">The 32-byte AES-256 key</param>
    /// <returns>The encrypted data in format: [12-byte nonce][ciphertext+tag combined]</returns>
    /// <remarks>
    /// This method produces encrypted data compatible with Python's AESGCM:
    /// - First 12 bytes: Random nonce (IV)
    /// - Remaining bytes: Ciphertext with 16-byte authentication tag appended
    /// </remarks>
    public static byte[] Encrypt(byte[] plaintext, byte[] key)
    {
        // Generate a random 12-byte nonce (recommended size for GCM)
        byte[] nonce = new byte[12];
        RandomNumberGenerator.Fill(nonce);

        // Allocate buffers for ciphertext and tag
        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[16]; // AES-GCM uses 16-byte authentication tag

        // Perform AES-GCM encryption
        using var aes = new AesGcm(key);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        // Combine: [nonce][ciphertext][tag] to match Python's format
        byte[] result = new byte[nonce.Length + ciphertext.Length + tag.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
        Buffer.BlockCopy(ciphertext, 0, result, nonce.Length, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, result, nonce.Length + ciphertext.Length, tag.Length);

        return result;
    }

    /// <summary>
    /// Decrypts data encrypted with AES-256-GCM.
    /// </summary>
    /// <param name="raw">The complete encrypted data: [12-byte nonce][ciphertext+tag combined]</param>
    /// <param name="key">The 32-byte AES-256 key</param>
    /// <returns>The decrypted plaintext as a byte array</returns>
    /// <remarks>
    /// This method expects the encrypted data format produced by .NET's Encrypt() or Python's AESGCM:
    /// - Bytes 0-11: 12-byte nonce (IV)
    /// - Bytes 12 onwards: Ciphertext with 16-byte authentication tag appended at the end
    /// </remarks>
    public static byte[] Decrypt(byte[] raw, byte[] key)
    {
        // Extract the 12-byte nonce from the beginning
        byte[] nonce = raw[..12];
        
        // The rest is ciphertext + tag combined
        byte[] ciphertextAndTag = raw[12..];
        
        // The tag is the last 16 bytes
        byte[] tag = ciphertextAndTag[^16..];
        
        // The ciphertext is everything except the last 16 bytes (tag)
        byte[] ct = ciphertextAndTag[..^16];

        // Allocate buffer for the plaintext (same size as ciphertext)
        byte[] pt = new byte[ct.Length];

        // Perform AES-GCM decryption with authentication verification
        using var aes = new AesGcm(key);
        aes.Decrypt(nonce, ct, tag, pt);

        return pt;
    }
}
