/*
 * QuantumSafe-XCryptor - .NET Encapsulation Service
 * 
 * Loads ML-KEM-1024 public key, encapsulates shared secret, derives AES-256 key,
 * and encrypts plaintext file.
 * 
 * Input:  /data/kyber_public.key, /data/sample.txt
 * Output: /data/kyber_ciphertext.bin, /data/encrypted-dotnet.bin
 * 
 * Packet format:
 *   [ML-KEM-1024 ciphertext: 1568 bytes][AES-256-GCM payload: nonce(12) + ct + tag(16)]
 *
 * HKDF Parameters (MUST match Python):
 *   salt = 32 zero bytes
 *   info = "AES-256-GCM"
 *   hash = SHA-256
 *   length = 32 bytes
 */

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

class Program
{
    private const string SHARED_DIR = "/data";

    // ML-KEM Constants
    private const int KYBER_PUBLIC_KEY_SIZE = 1568;
    private const int KYBER_CIPHERTEXT_SIZE = 1568;
    private const int KYBER_SHARED_SECRET_SIZE = 32;

    // HKDF Parameters (MUST be identical across all platforms)
    private static readonly byte[] HKDF_SALT = new byte[32]; // 32 zero bytes
    private static readonly byte[] HKDF_INFO = Encoding.UTF8.GetBytes("AES-256-GCM");
    private const int HKDF_OUTPUT_LENGTH = 32;

    static void Main()
    {
        Console.WriteLine("=".PadRight(60, '='));
        Console.WriteLine("  ML-KEM-1024 Encapsulation + AES Encryption Service (.NET)");
        Console.WriteLine("=".PadRight(60, '=') + "\n");

        try
        {
            // Wait for public key from keygen service
            string pkPath = Path.Combine(SHARED_DIR, "kyber_public.key");
            Console.WriteLine($"⏳ Waiting for public key: {pkPath}");
            WaitForFile(pkPath, 30);
            
            byte[] publicKey = File.ReadAllBytes(pkPath);
            Console.WriteLine($"✓ Public key loaded: {publicKey.Length} bytes from {pkPath}\n");

            if (publicKey.Length != KYBER_PUBLIC_KEY_SIZE)
            {
                throw new Exception($"Invalid public key size: {publicKey.Length} bytes (expected {KYBER_PUBLIC_KEY_SIZE})");
            }

            // Ensure sample plaintext exists
            EnsureSamplePlaintext();
            string samplePath = Path.Combine(SHARED_DIR, "sample.txt");
            byte[] plaintext = File.ReadAllBytes(samplePath);
            Console.WriteLine($"📄 Plaintext loaded: {plaintext.Length} bytes from {samplePath}");
            Console.WriteLine($"📝 Content: \"{Encoding.UTF8.GetString(plaintext)}\"\n");

            // Encapsulate and encrypt
            Console.WriteLine("🔐 Encrypting with ML-KEM-1024 + AES-256-GCM...");
            var (encryptedPacket, kyberCiphertext) = EncryptFile(plaintext, publicKey);
            
            string ctPath = Path.Combine(SHARED_DIR, "kyber_ciphertext.bin");
            File.WriteAllBytes(ctPath, kyberCiphertext);
            Console.WriteLine($"✓ ML-KEM-1024 ciphertext written: {ctPath} ({kyberCiphertext.Length} bytes)");
            
            string encryptedPath = Path.Combine(SHARED_DIR, "encrypted-dotnet.bin");
            File.WriteAllBytes(encryptedPath, encryptedPacket);
            Console.WriteLine($"✓ Encrypted packet written: {encryptedPath} ({encryptedPacket.Length} bytes)");
            Console.WriteLine($"  Format: [Kyber CT: {KYBER_CIPHERTEXT_SIZE}][Nonce: 12][AES-CT: {encryptedPacket.Length - KYBER_CIPHERTEXT_SIZE - 12 - 16}][Tag: 16]\n");

            Console.WriteLine("=".PadRight(60, '='));
            Console.WriteLine("✅ Encapsulation complete! Ready for Python decryption service.");
            Console.WriteLine("=".PadRight(60, '='));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ ERROR: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            Environment.Exit(1);
        }
    }

    private static void WaitForFile(string path, int timeoutSeconds)
    {
        var timeout = DateTime.Now.AddSeconds(timeoutSeconds);
        while (!File.Exists(path))
        {
            if (DateTime.Now > timeout)
            {
                throw new TimeoutException($"File not found after {timeoutSeconds}s: {path}");
            }
            System.Threading.Thread.Sleep(500);
        }
    }

    private static void EnsureSamplePlaintext()
    {
        string samplePath = Path.Combine(SHARED_DIR, "sample.txt");
        if (!File.Exists(samplePath))
        {
            File.WriteAllText(samplePath, "Hello from QuantumSafe-XCryptor!");
            Console.WriteLine($"ℹ Created default sample.txt at {samplePath}");
        }
    }

    /// <summary>
    /// Encrypt file using ML-KEM-1024 + AES-256-GCM.
    ///
    /// Flow:
    /// 1. Encapsulate shared secret using public key
    /// 2. Derive AES key from shared secret using HKDF-SHA256 (salt=32x0, info="AES-256-GCM")
    /// 3. Encrypt plaintext with AES-256-GCM
    /// 4. Return: [Kyber ciphertext: 1568][AES encrypted: variable]
    /// </summary>
    static (byte[] Packet, byte[] KyberCiphertext) EncryptFile(byte[] plaintext, byte[] publicKey)
    {
        // Step 1: Encapsulate
        Console.WriteLine($"  [1/3] 🔒 Encapsulating with public key: {Path.Combine(SHARED_DIR, "kyber_public.key")}");
        var (kyberCiphertext, sharedSecret) = LibOqsKyber.Encapsulate(publicKey);
        
        if (kyberCiphertext.Length != KYBER_CIPHERTEXT_SIZE)
        {
            throw new Exception($"Invalid Kyber ciphertext size: {kyberCiphertext.Length}");
        }
        if (sharedSecret.Length != KYBER_SHARED_SECRET_SIZE)
        {
            throw new Exception($"Invalid Kyber shared secret size: {sharedSecret.Length}");
        }
        
        Console.WriteLine($"      ✓ Ciphertext generated: {kyberCiphertext.Length} bytes");
        Console.WriteLine($"      ✓ Shared secret encapsulated: {sharedSecret.Length} bytes");
        
        // Step 2: Derive AES key using HKDF-SHA256 (IDENTICAL to Python)
        Console.WriteLine($"  [2/3] 🔑 Deriving AES key (HKDF-SHA256)...");
        byte[] aesKey = DeriveAesKey(sharedSecret);
        Console.WriteLine($"      ✓ AES key derived: {aesKey.Length} bytes (salt=32x0, info='AES-256-GCM')");
        
        // Step 3: Encrypt with AES-256-GCM
        Console.WriteLine($"  [3/3] 🔐 Encrypting with AES-256-GCM...");
        byte[] aesEncrypted = EncryptAesGcm(plaintext, aesKey);
        Console.WriteLine($"      ✓ Encrypted payload: {aesEncrypted.Length} bytes (nonce:12 + CT + tag:16)");
        
        // Combine into packet: [Kyber CT][AES encrypted]
        byte[] packet = new byte[kyberCiphertext.Length + aesEncrypted.Length];
        Buffer.BlockCopy(kyberCiphertext, 0, packet, 0, kyberCiphertext.Length);
        Buffer.BlockCopy(aesEncrypted, 0, packet, kyberCiphertext.Length, aesEncrypted.Length);
        
        return (packet, kyberCiphertext);
    }

    /// <summary>
    /// Derive AES-256 key from Kyber shared secret using HKDF-SHA256.
    /// 
    /// CRITICAL: Must be IDENTICAL on all platforms (server, .NET, React Native)
    /// 
    /// Parameters:
    /// - Salt: 32 zero bytes
    /// - Info: "AES-256-GCM"
    /// - Hash: SHA-256
    /// - Output: 32 bytes
    /// </summary>
    static byte[] DeriveAesKey(byte[] sharedSecret)
    {
        if (sharedSecret.Length != KYBER_SHARED_SECRET_SIZE)
        {
            throw new ArgumentException($"Shared secret must be {KYBER_SHARED_SECRET_SIZE} bytes");
        }
        
        // HKDF-SHA256 Extract phase
        using (var hmac = new HMACSHA256(HKDF_SALT))
        {
            byte[] prk = hmac.ComputeHash(sharedSecret);
            
            // HKDF-SHA256 Expand phase
            using (var hmac2 = new HMACSHA256(prk))
            {
                byte[] t = new byte[0];
                byte[] okm = new byte[HKDF_OUTPUT_LENGTH];
                int iterations = (HKDF_OUTPUT_LENGTH + 31) / 32;
                
                for (int i = 1; i <= iterations; i++)
                {
                    using (var hmac3 = new HMACSHA256(prk))
                    {
                        // T(i) = HMAC-Hash(PRK, T(i-1) | info | i)
                        hmac3.TransformBlock(t, 0, t.Length, null, 0);
                        hmac3.TransformBlock(HKDF_INFO, 0, HKDF_INFO.Length, null, 0);
                        hmac3.TransformFinalBlock(new byte[] { (byte)i }, 0, 1);
                        t = hmac3.Hash;
                        
                        int copyLen = Math.Min(32, HKDF_OUTPUT_LENGTH - (i - 1) * 32);
                        Buffer.BlockCopy(t, 0, okm, (i - 1) * 32, copyLen);
                    }
                }
                
                return okm;
            }
        }
    }

    /// <summary>
    /// Encrypt plaintext with AES-256-GCM.
    /// 
    /// Returns: [nonce: 12 bytes][ciphertext][tag: 16 bytes]
    /// </summary>
    static byte[] EncryptAesGcm(byte[] plaintext, byte[] aesKey)
    {
        if (aesKey.Length != 32)
        {
            throw new ArgumentException("AES key must be 32 bytes");
        }
        
        // Generate random 12-byte nonce
        byte[] nonce = new byte[12];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(nonce);
        }
        
        // Encrypt using AES-256-GCM
        using (var aes = new AesGcm(aesKey))
        {
            byte[] ciphertext = new byte[plaintext.Length];
            byte[] tag = new byte[16];
            
            // Encrypt with no associated data; ciphertext/tag buffers already allocated
            aes.Encrypt(nonce, plaintext, ciphertext, tag, null);
            
            // Return: [nonce][ciphertext][tag]
            byte[] result = new byte[12 + ciphertext.Length + 16];
            Buffer.BlockCopy(nonce, 0, result, 0, 12);
            Buffer.BlockCopy(ciphertext, 0, result, 12, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, result, 12 + ciphertext.Length, 16);
            
            return result;
        }
    }
}
