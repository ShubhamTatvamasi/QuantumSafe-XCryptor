# QuantumSafe-XCryptor

Post-quantum hybrid encryption system demonstrating **ML-KEM-1024** (Kyber) + **AES-256-GCM** across multiple platforms.

## 🏗️ Architecture

Three-service pipeline demonstrating the proper ML-KEM-1024 workflow:

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  Python Keygen  │ →   │  .NET Encrypt   │ →   │ Python Decrypt  │
│                 │     │                 │     │                 │
│  Generate       │     │  Encapsulate    │     │  Decapsulate    │
│  ML-KEM keypair │     │  + AES encrypt  │     │  + AES decrypt  │
└─────────────────┘     └─────────────────┘     └─────────────────┘
```

### Flow

1. **Python Keygen Service** (`python-keygen/`)
   - Generates ML-KEM-1024 public/private keypair
   - Writes keys to shared volume
   - Exits after completion

2. **.NET Encryption Service** (`dotnet-encrypt/`)
   - Loads public key
   - Encapsulates shared secret using ML-KEM-1024
   - Derives AES-256 key via HKDF-SHA256
   - Encrypts file with AES-256-GCM
   - Writes ciphertext and encrypted packet
   - Exits after completion

3. **Python Decryption Service** (`python-decrypt/`)
   - Loads private key and encrypted packet
   - Decapsulates shared secret using ML-KEM-1024
   - Derives identical AES-256 key via HKDF-SHA256
   - Decrypts packet with AES-256-GCM
   - Writes decrypted plaintext
   - Exits after completion

## 🚀 Quick Start

```bash
# Build all three services
docker-compose build

# Run the complete workflow (keygen → encrypt → decrypt)
docker-compose up && docker-compose down
```

### Expected Output

All three services run sequentially:

```
Service 1: Python Keygen
============================================================
🔑 ML-KEM-1024 Key Generation Service
============================================================
✓ Public key generated: 1568 bytes
✓ Private key generated: 3168 bytes
✓ Public key written to: /data/kyber_public.key
✓ Private key written to: /data/kyber_private.key
✅ Key generation complete!

Service 2: .NET Encrypt
============================================================
  ML-KEM-1024 Encapsulation + AES Encryption Service (.NET)
============================================================
✓ Public key loaded: 1568 bytes from /data/kyber_public.key
📄 Plaintext loaded: 32 bytes from /data/sample.txt
🔐 Encrypting with ML-KEM-1024 + AES-256-GCM...
  [1/3] 🔒 Encapsulating with public key
       ✓ Ciphertext generated: 1568 bytes
       ✓ Shared secret encapsulated: 32 bytes
  [2/3] 🔑 Deriving AES key (HKDF-SHA256)
       ✓ AES key derived: 32 bytes (salt=32x0, info='AES-256-GCM')
  [3/3] 🔐 Encrypting with AES-256-GCM
       ✓ Encrypted payload: 60 bytes (nonce:12 + CT + tag:16)
✓ ML-KEM-1024 ciphertext written: /data/kyber_ciphertext.bin (1568 bytes)
✓ Encrypted packet written: /data/encrypted-dotnet.bin (1628 bytes)
✅ Encapsulation complete!

Service 3: Python Decrypt
============================================================
🔓 ML-KEM-1024 Decapsulation + AES Decryption Service (Python)
============================================================
✓ Private key loaded: 3168 bytes from /data/kyber_private.key
✓ Encrypted packet loaded: 1628 bytes from /data/encrypted-dotnet.bin
📦 Packet structure:
   • Kyber ciphertext: 1568 bytes
   • AES payload: 60 bytes
🔐 [1/3] Decapsulating with private key
        ✓ Shared secret recovered: 32 bytes
🔑 [2/3] Deriving AES key (HKDF-SHA256)
        ✓ AES key derived: 32 bytes (salt=32x0, info='AES-256-GCM')
🔓 [3/3] Decrypting with AES-256-GCM
        ✓ Decrypted successfully!
📄 Output written to: /data/decrypted-python.txt (32 bytes)
📝 Content: "Hello from QuantumSafe-XCryptor!"
✅ Decryption complete!
```

### Verify Output

```bash
# View generated files
ls -lh shared/

# Compare original and decrypted content
diff shared/sample.txt shared/decrypted-python.txt
# No output = identical files ✅
```

## 📁 Generated Files

```
shared/
├── kyber_public.key        # 1568 bytes (ML-KEM-1024 public key)
├── kyber_private.key       # 3168 bytes (ML-KEM-1024 private key)
├── kyber_ciphertext.bin    # 1568 bytes (encapsulated ciphertext)
├── encrypted-dotnet.bin    # 1628 bytes (packet: CT + AES payload)
├── decrypted-python.txt    # 32 bytes (recovered plaintext)
└── sample.txt              # 32 bytes (original plaintext)
```

## 🔐 Cryptographic Details

### ML-KEM-1024 (NIST Post-Quantum Standard)
- **Public Key**: 1568 bytes
- **Private Key**: 3168 bytes
- **Ciphertext**: 1568 bytes
- **Shared Secret**: 32 bytes
- **Security Level**: 256-bit quantum resistance

### Key Derivation (HKDF-SHA256)
```
Input:  32-byte Kyber shared secret
Salt:   32 zero bytes
Info:   "AES-256-GCM" (UTF-8)
Output: 32-byte AES key
```

### AES-256-GCM Encryption
- **Key**: 32 bytes (from HKDF)
- **Nonce**: 12 bytes (random)
- **Tag**: 16 bytes (authentication)
- **Packet**: `[Kyber CT: 1568][Nonce: 12][Ciphertext][Tag: 16]`

## 🔍 Key Concepts

### Why Three Services?

This demonstrates the **proper ML-KEM workflow**:

1. **Keypair Generation** (one-time, by receiver)
   - Private key kept secret
   - Public key distributed freely

2. **Encapsulation** (sender side)
   - Uses only public key
   - Generates unique shared secret
   - No access to private key needed

3. **Decapsulation** (receiver side)
   - Uses private key + ciphertext
   - Recovers same shared secret
   - Only holder of private key can decrypt

### Cross-Language Compatibility

**Critical**: Identical HKDF parameters ensure .NET and Python derive the **exact same AES key** from the Kyber shared secret.

## 🛡️ Security Properties

- **Post-Quantum Secure**: Resistant to quantum attacks
- **IND-CCA2**: Secure against chosen-ciphertext attacks
- **Forward Secrecy**: Fresh shared secret per encryption
- **Authenticated Encryption**: AES-GCM provides confidentiality + authenticity
- **256-bit Security**: Equivalent to AES-256

## 📚 Documentation

- [ARCHITECTURE.md](ARCHITECTURE.md) - Detailed system design and cryptographic analysis
- [NIST ML-KEM](https://csrc.nist.gov/Projects/post-quantum-cryptography) - Official standard

## 📱 React Native Client (Encapsulation + AES-GCM)

The React Native app mirrors the `.NET encrypt` flow using `@noble/post-quantum` (Kyber1024) + HKDF-SHA256 + AES-256-GCM.

### Run (Expo / RN)
```bash
cd react-native-app
npm install   # or yarn
npm start     # or yarn start
```

### Configure public key
- Set `PUBLIC_KEY_B64` in `react-native-app/App.js` to the Base64 of `kyber_public.key` (1568 bytes) produced by `python-keygen`.
- If unset, the app will generate a demo keypair (for local-only testing).

### What it does
1) Loads the ML-KEM-1024 public key (1568 bytes)
2) Encapsulates to get Kyber ciphertext (1568 bytes) + shared secret (32 bytes)
3) Derives AES key via HKDF-SHA256 (salt=32x0, info="AES-256-GCM", len=32)
4) Encrypts sample text with AES-256-GCM
5) Emits packet: `[Kyber CT][Nonce 12][Ciphertext||Tag 16]`

### Interop notes
- HKDF parameters are identical to .NET/Python (salt=32 zero bytes, info="AES-256-GCM").
- Packet format matches `.NET` output: `[1568][12][ct][16]`.
- Use the same `kyber_public.key` to encrypt, and `python-decrypt` can decapsulate/decrypt if you supply the packet.

## 🔗 Technologies

- **ML-KEM-1024**: [liboqs](https://github.com/open-quantum-safe/liboqs) 0.14.0
- **Python bindings**: [liboqs-python](https://github.com/open-quantum-safe/liboqs-python) 0.14.1
- **.NET bindings**: Custom P/Invoke wrapper with liboqs
- **HKDF-SHA256**: RFC 5869
- **AES-256-GCM**: NIST SP 800-38D
- **Docker**: Multi-stage builds for optimized images

## 📂 Project Structure

```
QuantumSafe-XCryptor/
├── python-keygen/          # Service 1: ML-KEM-1024 keypair generation
│   ├── Dockerfile
│   └── main.py
├── dotnet-encrypt/         # Service 2: Encapsulation + AES encryption
│   ├── Dockerfile
│   ├── Program.cs
│   ├── dotnet-encrypt.csproj
│   └── ...
├── python-decrypt/         # Service 3: Decapsulation + AES decryption
│   ├── Dockerfile
│   └── main.py
├── shared/                 # Shared volume for key/file exchange
│   └── (generated at runtime)
├── docker-compose.yml      # Orchestration configuration
├── README.md               # This file
└── ARCHITECTURE.md         # Detailed technical documentation
```

## ⚠️ Security Notice

**Demonstration project** for educational purposes. For production use:
- Implement proper key management (HSM, secure enclaves, key rotation)
- Add authentication/authorization mechanisms
- Use TLS for network transmission
- Implement proper error handling and logging
- Conduct thorough security audits
- Follow NIST guidelines for post-quantum cryptography migration

## 🐛 Troubleshooting

**Issue**: Services don't start in order
- **Solution**: Ensure Docker Compose version supports `condition: service_completed_successfully`

**Issue**: Files not found in `/data`
- **Solution**: Check that `./shared` directory exists and has proper permissions

**Issue**: Build fails on liboqs compilation
- **Solution**: Ensure Docker has sufficient memory (recommended: 4GB+)

**Issue**: Decryption fails with `InvalidTag`
- **Solution**: Verify HKDF parameters are identical in both .NET and Python services

---

**Built with post-quantum cryptography for a quantum-safe future.** 🔐
