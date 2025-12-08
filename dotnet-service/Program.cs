/*
 * QuantumSafe-XCryptor - .NET Desktop Client
 * 
 * Post-quantum hybrid encryption client using ML-KEM-1024 + AES-256-GCM.
 * 
 * Architecture:
 * - Downloads server's ML-KEM-1024 public key
 * - Encapsulates shared secret using server's public key
 * - Derives AES-256 key using HKDF-SHA256 (identical to server)
 * - Encrypts file locally (zero-knowledge upload)
 * - Uploads encrypted packet to server: [Kyber CT][AES encrypted data]
 * 
 * HKDF Parameters (MUST match server and React Native):
 * - Salt: 32 zero bytes
 * - Info: "AES-256-GCM"
 * - Hash: SHA-256
 * - Output: 32 bytes (AES-256 key)
 */

using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Text;

class Program
{
    // Configuration
    private const string SERVER_URL = "http://python-service:5000";
    private const string SHARED_DIR = "/data";
    
    // ML-KEM Constants
    private const int KYBER_PUBLIC_KEY_SIZE = 1568;
    private const int KYBER_CIPHERTEXT_SIZE = 1568;
    private const int KYBER_SHARED_SECRET_SIZE = 32;
    
    // HKDF Parameters (MUST be identical across all platforms)
    private static readonly byte[] HKDF_SALT = new byte[32];  // 32 zero bytes
    private static readonly byte[] HKDF_INFO = Encoding.UTF8.GetBytes("AES-256-GCM");
    private const int HKDF_OUTPUT_LENGTH = 32;

    static async Task Main()
    {
        Console.WriteLine("=".PadRight(60, '='));
        Console.WriteLine("  QuantumSafe-XCryptor - .NET Desktop Client");
        Console.WriteLine("=".PadRight(60, '=') + "\n");

        try
        {
            // Step 1: Fetch server's public key
            Console.WriteLine("📥 Fetching server's ML-KEM-1024 public key...");
            byte[] serverPublicKey = await FetchServerPublicKey();
            Console.WriteLine($"✓ Downloaded public key: {serverPublicKey.Length} bytes\n");

            // Step 2: Test with sample file
            string sampleFilePath = $"{SHARED_DIR}/sample.txt";
            if (!File.Exists(sampleFilePath))
            {
                Console.WriteLine("✗ ERROR: sample.txt not found in {sampleFilePath}");
                return;
            }

            // Step 3: Encrypt file
            Console.WriteLine("🔐 Encrypting file...");
            byte[] plaintext = File.ReadAllBytes(sampleFilePath);
            byte[] encryptedPacket = EncryptFile(plaintext, serverPublicKey);
            Console.WriteLine($"✓ File encrypted: {plaintext.Length} → {encryptedPacket.Length} bytes\n");

            // Step 4: Upload encrypted file to server
            Console.WriteLine("📤 Uploading encrypted file to server...");
            await UploadEncryptedFile(encryptedPacket);
            Console.WriteLine("✓ File uploaded successfully\n");

            // Step 5: Save encrypted packet locally (for testing/verification)
            string encryptedPath = $"{SHARED_DIR}/encrypted-dotnet.bin";
            File.WriteAllBytes(encryptedPath, encryptedPacket);
            Console.WriteLine($"✓ Saved encrypted packet: {encryptedPath}");

            Console.WriteLine("\n" + "=".PadRight(60, '='));
            Console.WriteLine("✓ Encryption workflow completed successfully");
            Console.WriteLine("=".PadRight(60, '='));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n✗ ERROR: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            Environment.Exit(1);
        }
    }

    // ====================
    // Network Operations
    // ====================

    /// <summary>
    /// Download server's ML-KEM-1024 public key from /api/kyber/public-key endpoint
    /// </summary>
    static async Task<byte[]> FetchServerPublicKey()
    {
        using (var client = new HttpClient() { Timeout = TimeSpan.FromSeconds(30) })
        {
            try
            {
                string url = $"{SERVER_URL}/api/kyber/public-key";
                Console.WriteLine($"  GET {url}");
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                byte[] publicKey = await response.Content.ReadAsByteArrayAsync();
                
                if (publicKey.Length != KYBER_PUBLIC_KEY_SIZE)
                {
                    throw new Exception($"Invalid public key size: {publicKey.Length} (expected {KYBER_PUBLIC_KEY_SIZE})");
                }
                
                return publicKey;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to fetch public key: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Upload encrypted file to server's /api/files/upload endpoint
    /// 
    /// Packet format: [Kyber CT: 1568][AES encrypted: variable]
    /// </summary>
    static async Task UploadEncryptedFile(byte[] encryptedPacket)
    {
        using (var client = new HttpClient() { Timeout = TimeSpan.FromSeconds(120) })
        {
            try
            {
                string url = $"{SERVER_URL}/api/files/upload";
                Console.WriteLine($"  POST {url} ({encryptedPacket.Length} bytes)");
                
                var content = new ByteArrayContent(encryptedPacket);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                
                var response = await client.PostAsync(url, content);
                response.EnsureSuccessStatusCode();
                
                string responseText = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"  Response: {responseText}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to upload file: {ex.Message}", ex);
            }
        }
    }

    // ====================
    // Cryptographic Operations
    // ====================

    /// <summary>
    /// Encrypt file using ML-KEM-1024 + AES-256-GCM hybrid encryption.
    /// 
    /// Flow:
    /// 1. Encapsulate shared secret using server's public key
    /// 2. Derive AES key from shared secret using HKDF-SHA256
    /// 3. Encrypt plaintext with AES-256-GCM
    /// 4. Return: [Kyber ciphertext: 1568][AES encrypted: variable]
    /// </summary>
    static byte[] EncryptFile(byte[] plaintext, byte[] serverPublicKey)
    {
        // Step 1: Encapsulate
        Console.WriteLine($"  [1/3] Encapsulating shared secret...");
        var (kyberCiphertext, sharedSecret) = LibOqsKyber.Encapsulate(serverPublicKey);
        
        if (kyberCiphertext.Length != KYBER_CIPHERTEXT_SIZE)
        {
            throw new Exception($"Invalid Kyber ciphertext size: {kyberCiphertext.Length}");
        }
        if (sharedSecret.Length != KYBER_SHARED_SECRET_SIZE)
        {
            throw new Exception($"Invalid Kyber shared secret size: {sharedSecret.Length}");
        }
        
        Console.WriteLine($"      ✓ Ciphertext: {kyberCiphertext.Length} bytes");
        Console.WriteLine($"      ✓ Shared secret: {sharedSecret.Length} bytes");
        
        // Step 2: Derive AES key using HKDF-SHA256 (IDENTICAL to server)
        Console.WriteLine($"  [2/3] Deriving AES key (HKDF-SHA256)...");
        byte[] aesKey = DeriveAesKey(sharedSecret);
        Console.WriteLine($"      ✓ AES key: {aesKey.Length} bytes (salt=32x0, info='AES-256-GCM')");
        
        // Step 3: Encrypt with AES-256-GCM
        Console.WriteLine($"  [3/3] Encrypting with AES-256-GCM...");
        byte[] aesEncrypted = EncryptAesGcm(plaintext, aesKey);
        Console.WriteLine($"      ✓ Encrypted: {aesEncrypted.Length} bytes (includes nonce:12 + CT + tag:16)");
        
        // Combine into packet: [Kyber CT][AES encrypted]
        byte[] packet = new byte[kyberCiphertext.Length + aesEncrypted.Length];
        Buffer.BlockCopy(kyberCiphertext, 0, packet, 0, kyberCiphertext.Length);
        Buffer.BlockCopy(aesEncrypted, 0, packet, kyberCiphertext.Length, aesEncrypted.Length);
        
        return packet;
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
            
            aes.Encrypt(nonce, plaintext, null, ciphertext, tag);
            
            // Return: [nonce][ciphertext][tag]
            byte[] result = new byte[12 + ciphertext.Length + 16];
            Buffer.BlockCopy(nonce, 0, result, 0, 12);
            Buffer.BlockCopy(ciphertext, 0, result, 12, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, result, 12 + ciphertext.Length, 16);
            
            return result;
        }
    }
}

