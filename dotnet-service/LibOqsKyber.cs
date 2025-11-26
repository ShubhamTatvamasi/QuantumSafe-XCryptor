using System;
using System.Runtime.InteropServices;

public static class LibOqsKyber
{
    private const string LibName = "oqs_shim";

    public const int PublicKeyLength = 1568;
    public const int PrivateKeyLength = 3168;
    public const int CiphertextLength = 1568;
    public const int SharedSecretLength = 32;

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int oqs_kyber1024_keypair(byte[] pk, UIntPtr pk_len, byte[] sk, UIntPtr sk_len);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int oqs_kyber1024_encaps(byte[] pk, UIntPtr pk_len, byte[] ct, UIntPtr ct_len, byte[] ss, UIntPtr ss_len);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int oqs_kyber1024_decaps(byte[] ct, UIntPtr ct_len, byte[] sk, UIntPtr sk_len, byte[] ss, UIntPtr ss_len);

    public static (byte[] PublicKey, byte[] PrivateKey) GenerateKeyPair()
    {
        var pk = new byte[PublicKeyLength];
        var sk = new byte[PrivateKeyLength];
        int rc = oqs_kyber1024_keypair(pk, (UIntPtr)pk.Length, sk, (UIntPtr)sk.Length);
        if (rc != 0) throw new InvalidOperationException($"liboqs keypair failed: {rc}");
        return (pk, sk);
    }

    public static (byte[] Ciphertext, byte[] SharedSecret) Encapsulate(byte[] publicKey)
    {
        if (publicKey == null || publicKey.Length != PublicKeyLength)
            throw new ArgumentException($"publicKey must be {PublicKeyLength} bytes");
        var ct = new byte[CiphertextLength];
        var ss = new byte[SharedSecretLength];
        int rc = oqs_kyber1024_encaps(publicKey, (UIntPtr)publicKey.Length, ct, (UIntPtr)ct.Length, ss, (UIntPtr)ss.Length);
        if (rc != 0) throw new InvalidOperationException($"liboqs encaps failed: {rc}");
        return (ct, ss);
    }

    public static byte[] Decapsulate(byte[] privateKey, byte[] ciphertext)
    {
        if (privateKey == null || privateKey.Length != PrivateKeyLength)
            throw new ArgumentException($"privateKey must be {PrivateKeyLength} bytes");
        if (ciphertext == null || ciphertext.Length != CiphertextLength)
            throw new ArgumentException($"ciphertext must be {CiphertextLength} bytes");
        var ss = new byte[SharedSecretLength];
        int rc = oqs_kyber1024_decaps(ciphertext, (UIntPtr)ciphertext.Length, privateKey, (UIntPtr)privateKey.Length, ss, (UIntPtr)ss.Length);
        if (rc != 0) throw new InvalidOperationException($"liboqs decaps failed: {rc}");
        return ss;
    }
}
