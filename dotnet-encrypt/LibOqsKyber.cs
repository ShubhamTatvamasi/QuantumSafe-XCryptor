
using System;
using System.Runtime.InteropServices;

/// <summary>
/// Provides static methods for Kyber1024 post-quantum key encapsulation operations
/// via native interop with the liboqs C library (through oqs_shim).
/// This class enables .NET applications to generate Kyber keypairs, encapsulate shared secrets,
/// and decapsulate ciphertexts for quantum-resistant hybrid encryption.
/// </summary>
public static class LibOqsKyber
{
    // Name of the native C shim library that wraps liboqs functions
    private const string LibName = "oqs_shim";

    /// <summary>Kyber1024 public key size in bytes</summary>
    public const int PublicKeyLength = 1568;
    /// <summary>Kyber1024 private key size in bytes</summary>
    public const int PrivateKeyLength = 3168;
    /// <summary>Kyber1024 ciphertext size in bytes</summary>
    public const int CiphertextLength = 1568;
    /// <summary>Kyber1024 shared secret size in bytes (suitable for AES-256)</summary>
    public const int SharedSecretLength = 32;

    // Native P/Invoke signatures for Kyber1024 operations
    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int oqs_kyber1024_keypair(byte[] pk, UIntPtr pk_len, byte[] sk, UIntPtr sk_len);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int oqs_kyber1024_encaps(byte[] pk, UIntPtr pk_len, byte[] ct, UIntPtr ct_len, byte[] ss, UIntPtr ss_len);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int oqs_kyber1024_decaps(byte[] ct, UIntPtr ct_len, byte[] sk, UIntPtr sk_len, byte[] ss, UIntPtr ss_len);

    /// <summary>
    /// Generates a Kyber1024 keypair (public and private keys) using the native liboqs library.
    /// </summary>
    /// <returns>Tuple containing the public key and private key as byte arrays</returns>
    /// <exception cref="InvalidOperationException">Thrown if the native call fails</exception>
    public static (byte[] PublicKey, byte[] PrivateKey) GenerateKeyPair()
    {
        var pk = new byte[PublicKeyLength];
        var sk = new byte[PrivateKeyLength];
        int rc = oqs_kyber1024_keypair(pk, (UIntPtr)pk.Length, sk, (UIntPtr)sk.Length);
        if (rc != 0)
            throw new InvalidOperationException($"liboqs keypair failed: {rc}");
        return (pk, sk);
    }

    /// <summary>
    /// Encapsulates a shared secret using the recipient's Kyber1024 public key.
    /// Produces a ciphertext and a 32-byte shared secret suitable for AES-256.
    /// </summary>
    /// <param name="publicKey">The recipient's Kyber1024 public key (1568 bytes)</param>
    /// <returns>Tuple containing the ciphertext and shared secret as byte arrays</returns>
    /// <exception cref="ArgumentException">Thrown if the public key is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown if the native call fails</exception>
    public static (byte[] Ciphertext, byte[] SharedSecret) Encapsulate(byte[] publicKey)
    {
        if (publicKey == null || publicKey.Length != PublicKeyLength)
            throw new ArgumentException($"publicKey must be {PublicKeyLength} bytes");
        var ct = new byte[CiphertextLength];
        var ss = new byte[SharedSecretLength];
        int rc = oqs_kyber1024_encaps(publicKey, (UIntPtr)publicKey.Length, ct, (UIntPtr)ct.Length, ss, (UIntPtr)ss.Length);
        if (rc != 0)
            throw new InvalidOperationException($"liboqs encaps failed: {rc}");
        return (ct, ss);
    }

    /// <summary>
    /// Decapsulates a shared secret from a Kyber1024 ciphertext using the private key.
    /// </summary>
    /// <param name="privateKey">The recipient's Kyber1024 private key (3168 bytes)</param>
    /// <param name="ciphertext">The Kyber1024 ciphertext (1568 bytes)</param>
    /// <returns>The 32-byte shared secret as a byte array</returns>
    /// <exception cref="ArgumentException">Thrown if the private key or ciphertext is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown if the native call fails</exception>
    public static byte[] Decapsulate(byte[] privateKey, byte[] ciphertext)
    {
        if (privateKey == null || privateKey.Length != PrivateKeyLength)
            throw new ArgumentException($"privateKey must be {PrivateKeyLength} bytes");
        if (ciphertext == null || ciphertext.Length != CiphertextLength)
            throw new ArgumentException($"ciphertext must be {CiphertextLength} bytes");
        var ss = new byte[SharedSecretLength];
        int rc = oqs_kyber1024_decaps(ciphertext, (UIntPtr)ciphertext.Length, privateKey, (UIntPtr)privateKey.Length, ss, (UIntPtr)ss.Length);
        if (rc != 0)
            throw new InvalidOperationException($"liboqs decaps failed: {rc}");
        return ss;
    }
}
