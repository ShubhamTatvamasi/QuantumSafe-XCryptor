# Implementation Guide - QuantumSafe-XCryptor

## Overview

QuantumSafe-XCryptor is a **distributed post-quantum hybrid encryption system** using **ML-KEM-1024 (formerly Kyber1024) + AES-256-GCM** with a **server-centric architecture**:

- **Python Server (Cloud)**: Generates ML-KEM keypairs, stores private key, decrypts files
- **.NET Desktop Client**: Downloads public key, encrypts files locally, uploads to server
- **React Native Mobile Client**: Same encryption model as desktop, optimized for mobile

## What Changed

### 🔐 Security Upgrade

**Before:**
- Simple AES-256-GCM with a static shared key
- Vulnerable to future quantum computer attacks

**After:**
- ML-KEM-1024 KEM (NIST-approved post-quantum algorithm)
- Hybrid encryption: Kyber for key exchange + AES for data
- Quantum-resistant security (256-bit equivalent)

## Architecture Model

### Security Model

```
Client-Side (Zero-Knowledge Upload):
  1. Download server's public key (pk)
  2. Encapsulate shared secret: ML-KEM-1024.encapsulate(pk) → (ct, ss)
  3. Derive AES key: HKDF-SHA256(ss, salt, info) → aes_key
  4. Encrypt file: AES-256-GCM.encrypt(plaintext, aes_key) → ciphertext
  5. Send to server: (ct, ciphertext)

Server-Side (Trusted Decryption):
  1. Receive (ct, ciphertext) from client
  2. Decapsulate shared secret: ML-KEM-1024.decapsulate(ct, sk) → ss
  3. Derive AES key: HKDF-SHA256(ss, salt, info) → aes_key [SAME as client]
  4. Decrypt file: AES-256-GCM.decrypt(ciphertext, aes_key) → plaintext
```

### Key Points

- **Server generates and maintains private key (sk)** - never shared with clients
- **Clients only know public key (pk)** - used for encapsulation
- **Each client gets unique shared secret** - forward secrecy (fresh Kyber CT per client)
- **Identical HKDF parameters across all platforms** - ensures all clients/server derive same AES key
- **Files never encrypted on server** - only decrypted for access/storage

## HKDF Parameters (MUST be Identical)

All three platforms MUST use these exact HKDF parameters:

```python
HKDF_HASH = "SHA-256"
HKDF_SALT = b'\x00' * 32        # 32 zero bytes
HKDF_INFO = b"AES-256-GCM"      # UTF-8 string
HKDF_OUTPUT_LENGTH = 32         # bytes (for AES-256)
```

**Why?** If these differ between platforms, derived AES keys won't match, and decryption fails.

## ML-KEM-1024 Constants

```python
KYBER_PUBLIC_KEY_SIZE = 1568    # bytes
KYBER_PRIVATE_KEY_SIZE = 3168   # bytes (server only)
KYBER_CIPHERTEXT_SIZE = 1568    # bytes (per client)
KYBER_SHARED_SECRET_SIZE = 32   # bytes
```

## Platform Implementation Details

### Python Server (`python-service/main.py`)

**Responsibilities:**
- Generate ML-KEM-1024 keypair on startup
- Expose public key via HTTPS endpoint
- Receive encrypted files with Kyber ciphertexts
- Decapsulate ciphertexts to recover shared secrets
- Derive AES keys (same HKDF parameters as clients)
- Decrypt AES-GCM data
- Return plaintext or error

**Key Functions:**

```python
def generate_kyber_keypair():
    """Generate ML-KEM-1024 keypair (server-side, run once)"""
    kemp = oqs.KeyEncapsulation("ML-KEM-1024")
    public_key = kemp.public_key
    secret_key = kemp.secret_key
    # Store secret_key securely (env var, HSM, vault)
    # Share public_key with clients

def derive_aes_key(shared_secret):
    """Derive AES-256 key from Kyber shared secret (IDENTICAL on all platforms)"""
    hkdf = HKDF(
        algorithm=hashes.SHA256(),
        length=32,
        salt=b'\x00' * 32,
        info=b"AES-256-GCM",
        backend=default_backend()
    )
    return hkdf.derive(shared_secret)

def decrypt_file(kyber_ciphertext, encrypted_data):
    """Decrypt file sent by client"""
    # 1. Decapsulate using private key
    kemp = oqs.KeyEncapsulation("ML-KEM-1024", secret_key=private_key)
    shared_secret = kemp.decap(kyber_ciphertext)
    
    # 2. Derive AES key (same as client)
    aes_key = derive_aes_key(shared_secret)
    
    # 3. Decrypt AES-GCM data
    # Extract nonce (first 12 bytes), ciphertext, tag (last 16 bytes)
    nonce = encrypted_data[:12]
    ciphertext = encrypted_data[12:-16]
    tag = encrypted_data[-16:]
    
    cipher = Cipher(algorithms.AES(aes_key), modes.GCM(nonce, tag), backend=default_backend())
    decryptor = cipher.decryptor()
    plaintext = decryptor.update(ciphertext) + decryptor.finalize()
    
    return plaintext
```

**Endpoints:**
- `GET /api/kyber/public-key` - Return server's public key
- `POST /api/files/decrypt` - Accept encrypted file, return decrypted plaintext
- `POST /api/files/upload` - Accept and store encrypted file

**Environment Variables:**
```bash
KYBER_PRIVATE_KEY_BASE64=<base64-encoded-3168-byte-private-key>
# OR
KYBER_PRIVATE_KEY_FILE=/path/to/private_key.bin
```

---

### .NET Desktop Client (`dotnet-service/Program.cs`)

**Responsibilities:**
- Download server's public key at startup
- For each file to encrypt:
  - Encapsulate using server's public key
  - Derive AES key with identical HKDF parameters
  - Encrypt file with AES-256-GCM
  - Prepare transmission packet (ct + encrypted_data)
  - Upload to server

**Key Functions:**

```csharp
// 1. Download server's public key
public async Task<byte[]> FetchServerPublicKey(string serverUrl)
{
    using (var client = new HttpClient())
    {
        var response = await client.GetAsync($"{serverUrl}/api/kyber/public-key");
        return await response.Content.ReadAsByteArrayAsync();
    }
}

// 2. Encapsulate shared secret
public (byte[] ciphertext, byte[] sharedSecret) EncapsulateKyber(byte[] publicKey)
{
    var encap = LibOqsKyber.Encapsulate(publicKey);
    // Returns (1568-byte ciphertext, 32-byte shared secret)
    return encap;
}

// 3. Derive AES-256 key (IDENTICAL to Python and React Native)
public byte[] DeriveAesKey(byte[] sharedSecret)
{
    byte[] salt = new byte[32];  // 32 zero bytes
    byte[] info = Encoding.UTF8.GetBytes("AES-256-GCM");
    
    // Manual HKDF-SHA256 implementation
    using (var hmac = new HMACSHA256(salt))
    {
        byte[] prk = hmac.ComputeHash(sharedSecret);
        
        using (var hmac2 = new HMACSHA256(prk))
        {
            byte[] okm = new byte[32];
            byte[] t = new byte[0];
            int iterations = (32 + 31) / 32;  // ceil(32/32) = 1
            
            for (int i = 1; i <= iterations; i++)
            {
                using (var hmac3 = new HMACSHA256(prk))
                {
                    hmac3.TransformBlock(t, 0, t.Length, null, 0);
                    hmac3.TransformBlock(info, 0, info.Length, null, 0);
                    hmac3.TransformFinalBlock(new byte[] { (byte)i }, 0, 1);
                    t = hmac3.Hash;
                    Buffer.BlockCopy(t, 0, okm, (i - 1) * 32, Math.Min(32, t.Length));
                }
            }
            return okm;
        }
    }
}

// 4. Encrypt file with AES-256-GCM
public byte[] EncryptFile(string filePath, byte[] aesKey)
{
    byte[] plaintext = File.ReadAllBytes(filePath);
    byte[] nonce = new byte[12];
    using (var rng = new System.Security.Cryptography.RNGCryptoServiceProvider())
    {
        rng.GetBytes(nonce);
    }
    
    using (var aes = new AesGcm(aesKey))
    {
        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[16];
        aes.Encrypt(nonce, plaintext, null, ciphertext, tag);
        
        // Return: [nonce:12][ciphertext][tag:16]
        byte[] result = new byte[12 + ciphertext.Length + 16];
        Buffer.BlockCopy(nonce, 0, result, 0, 12);
        Buffer.BlockCopy(ciphertext, 0, result, 12, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, result, 12 + ciphertext.Length, 16);
        
        return result;
    }
}

// 5. Upload encrypted file to server
public async Task UploadEncryptedFile(string serverUrl, byte[] kyberCiphertext, byte[] encryptedData)
{
    using (var client = new HttpClient())
    {
        var packet = new byte[kyberCiphertext.Length + encryptedData.Length];
        Buffer.BlockCopy(kyberCiphertext, 0, packet, 0, kyberCiphertext.Length);
        Buffer.BlockCopy(encryptedData, 0, packet, kyberCiphertext.Length, encryptedData.Length);
        
        var content = new ByteArrayContent(packet);
        var response = await client.PostAsync($"{serverUrl}/api/files/upload", content);
        response.EnsureSuccessStatusCode();
    }
}
```

**Main Workflow:**
```csharp
// 1. Startup: fetch and cache server public key
byte[] serverPublicKey = await FetchServerPublicKey("https://server.example.com");

// 2. For each file:
string filePath = "path/to/sensitive/file.txt";

// Encapsulate
var (kyberCiphertext, sharedSecret) = EncapsulateKyber(serverPublicKey);

// Derive AES key
byte[] aesKey = DeriveAesKey(sharedSecret);

// Encrypt file
byte[] encryptedData = EncryptFile(filePath, aesKey);

// Upload to server
await UploadEncryptedFile("https://server.example.com", kyberCiphertext, encryptedData);
```

---

### React Native Mobile Client (`react-native-app/App.js`)

**Responsibilities:**
- Same as .NET desktop but for React Native
- Download server's public key at startup
- Encrypt files locally before upload
- Use identical HKDF parameters
- Handle mobile file access permissions

**Key Functions:**

```javascript
// 1. Download server public key
async function fetchServerPublicKey(serverUrl) {
    const response = await fetch(`${serverUrl}/api/kyber/public-key`);
    const buffer = await response.arrayBuffer();
    return new Uint8Array(buffer);
}

// 2. Import Kyber from @noble/post-quantum
import { kyber1024 } from "@noble/post-quantum/kyber";

async function encapsulateKyber(publicKey) {
    // @noble/post-quantum handles encapsulation
    const { ciphertext, sharedSecret } = await kyber1024.encapsulate(publicKey);
    return { ciphertext, sharedSecret };
}

// 3. Derive AES-256 key (IDENTICAL to .NET and Python)
async function deriveAesKey(sharedSecret) {
    // Using crypto-browserify HKDF
    const salt = new Uint8Array(32);  // 32 zero bytes
    const info = new TextEncoder().encode("AES-256-GCM");
    
    // HKDF-SHA256: Extract phase
    const hmac1 = crypto.createHmac('sha256', salt);
    hmac1.update(Buffer.from(sharedSecret));
    const prk = hmac1.digest();
    
    // HKDF-SHA256: Expand phase
    const hmac2 = crypto.createHmac('sha256', prk);
    hmac2.update(Buffer.concat([info, Buffer.from([0x01])]));
    const okm = hmac2.digest();
    
    return okm.slice(0, 32);  // 32 bytes for AES-256
}

// 4. Encrypt file with AES-256-GCM
async function encryptFile(filePath, aesKey) {
    const plaintext = await readFile(filePath);
    const nonce = crypto.randomBytes(12);
    
    // Using react-native-aes-crypto
    const cipher = new AES.GCM(aesKey);
    const encryptedData = await cipher.encryptData(plaintext, nonce);
    
    // Return: [nonce:12][ciphertext][tag:16]
    const result = Buffer.concat([nonce, encryptedData]);
    return result;
}

// 5. Upload encrypted file
async function uploadEncryptedFile(serverUrl, kyberCiphertext, encryptedData) {
    const packet = Buffer.concat([kyberCiphertext, encryptedData]);
    
    const response = await fetch(`${serverUrl}/api/files/upload`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/octet-stream' },
        body: packet
    });
    
    if (!response.ok) {
        throw new Error(`Upload failed: ${response.status}`);
    }
}
```

**Main Workflow:**
```javascript
// 1. App startup
const serverUrl = "https://server.example.com";
const serverPublicKey = await fetchServerPublicKey(serverUrl);

// 2. User picks file for encryption
const filePath = "file:///storage/emulated/0/Documents/sensitive.txt";

// Encapsulate
const { ciphertext: kyberCiphertext, sharedSecret } = await encapsulateKyber(serverPublicKey);

// Derive AES key (IDENTICAL to .NET and Python)
const aesKey = await deriveAesKey(sharedSecret);

// Encrypt
const encryptedData = await encryptFile(filePath, aesKey);

// Upload
await uploadEncryptedFile(serverUrl, kyberCiphertext, encryptedData);
```

---

## File Format (Transmission Packet)

```
[1568 bytes]           ML-KEM-1024 Ciphertext (encapsulated secret)
[12 bytes]             AES Nonce
[variable]             AES-256-GCM Ciphertext
[16 bytes]             AES-256-GCM Authentication Tag
──────────────────────────────────────────────────────────────
Total = 1568 + 12 + len(plaintext) + 16 bytes
```

**Example (1 MB file):**
```
Total size = 1568 + 12 + 1,048,576 + 16 = 1,050,172 bytes
Overhead = 1,596 bytes (0.15%)
```

---

## Testing & Validation

### Unit Tests (Cross-Platform)

```python
# Python test: Verify all platforms derive same AES key
shared_secret = b"..." # 32 bytes from Kyber

# Reference
aes_key_python = derive_aes_key(shared_secret)

# .NET must match
assert aes_key_dotnet == aes_key_python

# React Native must match
assert aes_key_react_native == aes_key_python
```

### End-to-End Test

```
1. Python server generates ML-KEM keypair
2. .NET client: Encapsulate(pk) → derive AES → encrypt → upload
3. React Native client: Encapsulate(pk) → derive AES → encrypt → upload
4. Python server: Decrypt both files
5. Verify: plaintext_from_dotnet == plaintext_from_react_native
```

### Docker Test

```bash
# Build all services
docker-compose build

# Run tests
docker-compose up

# Expected: All services communicate successfully
# Decrypted output from server matches original
```

---

## Configuration

### Python Server Environment

```bash
# Option 1: Private key in environment variable (base64)
export KYBER_PRIVATE_KEY_BASE64=$(cat private_key.bin | base64)

# Option 2: Private key in file
export KYBER_PRIVATE_KEY_FILE=/app/private_key.bin

# Option 3: Private key path (production)
export KYBER_PRIVATE_KEY_HSM=vault://secret/kyber/sk
```

### .NET Client Configuration

```csharp
// Server URL (configurable)
private const string SERVER_URL = "https://server.example.com";

// Can cache public key
private static byte[] cachedPublicKey = null;

// Refresh period (e.g., daily)
private const int PUBLIC_KEY_CACHE_DURATION_HOURS = 24;
```

### React Native Client Configuration

```javascript
// Server URL
const SERVER_URL = "https://server.example.com";

// File permissions (Android/iOS specific)
// - Request storage permissions
// - Use appropriate file paths per platform

// Optional: Cache public key in device secure storage
import SecureStorage from 'react-native-secure-storage';
```

---

## Security Best Practices

### 1. HKDF Parameter Immutability
- **DO NOT** change HKDF parameters after deployment
- **DO NOT** use different salts or info strings per client
- **DO** version the HKDF parameters in code comments
- **DO** unit test parameter alignment

### 2. Private Key Management
- **DO NOT** transmit private key to clients
- **DO** store private key securely (HSM, TPM, encrypted)
- **DO** rotate keys periodically (recommend: annually)
- **DO** implement key version tracking (client knows which key was used)

### 3. Public Key Distribution
- **DO** serve public key over HTTPS/TLS
- **DO** include expiration date in public key metadata
- **DO** pin certificates in clients (prevent MITM)
- **DO** validate public key format before use

### 4. Upload Transmission
- **DO** use TLS/HTTPS for all uploads
- **DO** implement request signing to prevent tampering
- **DO** validate ciphertext size (must be 1568 bytes for ML-KEM)
- **DO** implement timeout handling for slow uploads

### 5. Error Handling
- **DO NOT** reveal plaintext on decryption failure
- **DO** log failed decryptions (audit trail)
- **DO** implement rate limiting to prevent brute force
- **DO** return generic error messages to clients

---

## Performance

```
Operation              Time (local)    Bottleneck
────────────────────────────────────────────────
ML-KEM-1024 KeyGen    ~0.5 ms         Server startup (once)
ML-KEM-1024 Encaps    ~1 ms           Per client (Kyber compute)
ML-KEM-1024 Decaps    ~1 ms           Per file (Kyber compute)
HKDF-SHA256           <1 ms           Pure crypto (fast)
AES-256-GCM Encrypt   ~500 µs/MB      I/O bound (disk read)
AES-256-GCM Decrypt   ~500 µs/MB      I/O bound (disk write)
────────────────────────────────────────────────
Total (1 MB file)     ~3-5 ms local   Network dominant

Network (1 MB file):
  Upload:   1.05 MB @ 10 Mbps = ~840 ms
  Download: 1.05 MB @ 10 Mbps = ~840 ms
```

---

## Troubleshooting

### Issue: Decryption Fails on Server

**Symptom:** `AES tag verification failed`

**Causes:**
1. Client and server used different HKDF parameters → different AES keys
2. Ciphertext was corrupted in transit → invalid tag
3. Client sent wrong Kyber ciphertext format

**Solution:**
1. Verify HKDF parameters are identical on all platforms
2. Enable logging to inspect shared secret values
3. Check transmission packet format (1568 + 12 + len + 16 bytes)

### Issue: Kyber Encapsulation Fails on Client

**Symptom:** `Invalid public key size`

**Causes:**
1. Public key is not 1568 bytes
2. Public key was corrupted in download
3. Server is using different Kyber variant (not ML-KEM-1024)

**Solution:**
1. Validate public key size: `assert len(pk) == 1568`
2. Re-download from server and verify format
3. Confirm server is using ML-KEM-1024 (not ML-KEM-768 or ML-KEM-1024)

### Issue: Cross-Platform AES Keys Don't Match

**Symptom:** Server derives different AES key than client

**Causes:**
1. Different HKDF salt (e.g., random instead of all-zeros)
2. Different HKDF info string (e.g., "AES-GCM" vs "AES-256-GCM")
3. Different hash algorithm (SHA-1 vs SHA-256)
4. Byte encoding issues (UTF-8 vs ASCII)

**Solution:**
1. Print HKDF parameters on all platforms (salt, info, hash algo)
2. Compare output of reference implementation
3. Unit test with known test vectors

---

## References

- **ML-KEM-1024** (formerly Kyber1024): NIST FIPS 203 (post-quantum standard)
- **HKDF-SHA256**: RFC 5869 (key derivation function)
- **AES-256-GCM**: NIST SP 800-38D (authenticated encryption)
- **liboqs**: Open Quantum Safe C library
- **@noble/post-quantum**: JavaScript Kyber implementation (WASM)

---

## Next Steps

1. ✅ Understand server-centric KEM architecture
2. ⏳ Implement Python server with key generation & decryption
3. ⏳ Implement .NET desktop client with encryption
4. ⏳ Implement React Native mobile client with encryption
5. ⏳ Test cross-platform HKDF key derivation
6. ⏳ Deploy to production with secure key management
7. ⏳ Implement key rotation mechanism
8. ⏳ Add digital signatures (ML-DSA-65) for authenticity
