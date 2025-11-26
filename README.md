# QuantumSafe-XCryptor

QuantumSafe-XCryptor is a cross-language proof-of-concept demonstrating **post-quantum secure file encryption** using **Kyber1024 KEM** (Key Encapsulation Mechanism) combined with **AES-256-GCM**. This hybrid encryption approach provides quantum-resistant security while maintaining compatibility across .NET, Python, and React Native platforms.

## 🔐 Post-Quantum Hybrid Encryption Scheme

The project implements a hybrid encryption system that combines:

1. **Kyber1024** - NIST-approved post-quantum KEM for quantum-resistant key exchange
2. **AES-256-GCM** - Industry-standard authenticated encryption for data protection

### Encryption Flow

```
1. Generate Kyber1024 keypair (Public Key: 1568 bytes, Private Key: 3168 bytes)
2. Encapsulate shared secret (32 bytes) using recipient's public key → Kyber Ciphertext (1568 bytes)
3. Derive AES-256 key from shared secret using HKDF-SHA256
4. Encrypt plaintext with AES-256-GCM
5. Output: [kyber_ct_length:4bytes][kyber_ciphertext][aes_nonce:12bytes][aes_ciphertext][aes_tag:16bytes]
```

### Decryption Flow

```
1. Extract Kyber ciphertext from encrypted file
2. Decapsulate shared secret using private key
3. Derive AES-256 key from shared secret (same HKDF parameters)
4. Decrypt AES-GCM data
5. Verify authentication tag and output plaintext
```

## 🎯 Platform Implementations

#### .NET (encryption + decryption)

The .NET service provides full encryption and decryption capabilities:
- Generates Kyber1024 keypairs using **liboqs 0.15.0** via a small native shim + C# P/Invoke
- Encrypts `sample.txt` using Kyber1024 + AES-256-GCM
- Produces `encrypted.bin` with hybrid encryption
- Decrypts to `decrypted-dotnet.txt` for verification
- Saves Kyber keys for cross-platform testing

#### Python (decryption)

The Python service demonstrates decryption and liboqs integration:
- Uses **liboqs 0.15.0** (system library) with **liboqs-python 0.14.1**
- Attempts Kyber decapsulation via liboqs; falls back to AES key if cross-lib decapsulation fails
- Derives AES key using HKDF-SHA256
- Decrypts `encrypted.bin` to `decrypted-python.txt`
- Includes a self-test that validates Kyber1024 round-trip within liboqs

#### React Native (full encryption + decryption)

The React Native mobile app demonstrates post-quantum crypto in mobile environments:
- **Full Kyber1024 support** via `@noble/post-quantum` WASM library
- Generates keypairs, encapsulates, and decapsulates
- Complete hybrid encryption workflow
- Feature detection with WASM/native fallback
- Cross-platform compatible (iOS, Android, Web)
- No native build required - works out of the box
- Optional native module support for enhanced performance

---

## 🚀 Getting Started

### Prerequisites

- Docker and Docker Compose
- For local development:
  - .NET 8.0 SDK
  - Python 3.11+
  - Node.js 18+ (for React Native)

### 1. Build Docker Images

Build the Docker images for all services:
```bash
docker-compose build
```

### 2. Run the Services

Execute the post-quantum encryption and decryption workflow:
```bash
docker-compose up && docker-compose down
```

This will:
1. **.NET service** generates Kyber1024 keypair and encrypts `sample.txt`
2. **Python service** decrypts using Kyber private key
3. Both services verify the decrypted output

### 3. Verify Cross-Platform Compatibility

Check the decrypted outputs:
```bash
cat shared/decrypted-dotnet.txt
cat shared/decrypted-python.txt
cat shared/sample.txt
```

All three files should contain identical content, proving post-quantum secure cross-language compatibility.

## 📁 Project Structure

```
QuantumSafe-XCryptor/
├── dotnet-service/
│   ├── AesGcmHelper.cs      # AES-256-GCM encryption/decryption
│   ├── KyberHelper.cs       # Kyber1024 KEM operations
│   ├── Program.cs           # Main hybrid encryption workflow
│   ├── dotnet-service.csproj # Uses native liboqs via shim
│   └── Dockerfile
├── python-service/
│   ├── main.py              # Hybrid decryption with liboqs + fallback
│   ├── requirements.txt     # cryptography + liboqs-python
│   └── Dockerfile
├── react-native-app/
│   ├── aes.js               # AES-256-GCM module
│   ├── kyber.js             # Kyber1024 operations (WASM + native fallback)
│   ├── App.js               # Full hybrid encryption + decryption
│   ├── package.json         # Dependencies (@noble/post-quantum)
│   ├── WASM_QUICKSTART.md   # 5-minute WASM setup guide
│   ├── NATIVE_MODULE_GUIDE.md # Optional native module guide
│   └── STATUS.md            # Complete implementation status
├── shared/                  # Shared data directory
│   ├── sample.txt           # Original plaintext
│   ├── encrypted.bin        # Hybrid encrypted output
│   ├── kyber_public.key     # Kyber1024 public key (1568 bytes)
│   ├── kyber_private.key    # Kyber1024 private key (3168 bytes)
│   ├── kyber_ciphertext.bin # Encapsulated secret (1568 bytes)
│   ├── decrypted-dotnet.txt # .NET decryption output
│   └── decrypted-python.txt # Python decryption output
├── docker-compose.yml
└── README.md
```

## 🔬 Technical Details

### Kyber1024 Parameters

- **Security Level**: NIST Level 5 (~256-bit quantum security)
- **Public Key Size**: 1568 bytes
- **Private Key Size**: 3168 bytes
- **Ciphertext Size**: 1568 bytes
- **Shared Secret Size**: 32 bytes (perfect for AES-256)

### AES-256-GCM Parameters

- **Key Size**: 256 bits (32 bytes)
- **Nonce Size**: 96 bits (12 bytes)
- **Tag Size**: 128 bits (16 bytes)
- **Key Derivation**: HKDF-SHA256

### Libraries Used

- **.NET**: liboqs 0.15.0 (via native shim `liboqs_shim.so` + P/Invoke), System.Security.Cryptography (AES-GCM)
- **Python**: liboqs 0.15.0 (shared library), liboqs-python 0.14.1, cryptography
- **React Native**: @noble/post-quantum 0.2.0 (WASM Kyber), react-native-aes-gcm, crypto-browserify (HKDF)

## 🛡️ Security Considerations

1. **Quantum Resistance**: Kyber1024 provides protection against attacks by quantum computers
2. **Authentication**: AES-GCM provides both confidentiality and authenticity
3. **Key Derivation**: HKDF ensures proper key separation
4. **Random Nonces**: Each encryption uses a cryptographically random nonce
5. **No Key Reuse**: Each session generates new Kyber keypairs

## 🔄 Cross-Platform Compatibility

The implementation ensures byte-level compatibility across platforms:
- **Endianness**: Little-endian for length fields
- **Key Format**: Raw binary format (no PEM/DER encoding)
- **Ciphertext Format**: [length][kyber_ct][aes_data]
- **Consistent HKDF**: Same salt and info parameters across platforms
- **Unified Library**: All platforms use liboqs 0.15.0 (or @noble/post-quantum WASM for React Native)

## 📝 React Native Setup

The React Native app is ready to use with WASM Kyber:

```bash
cd react-native-app
npm install  # Installs @noble/post-quantum automatically
npm start    # Full encryption/decryption works immediately
```

For enhanced performance in production, optionally add a native module:
- See `react-native-app/NATIVE_MODULE_GUIDE.md` for implementation
- Native module provides ~10x faster operations
- WASM fallback ensures compatibility

## 🎓 Educational Purpose

This project demonstrates:
- Post-quantum cryptographic algorithms
- Hybrid encryption schemes
- Cross-language cryptographic interoperability
- Key encapsulation mechanisms
- Authenticated encryption
- Mobile cryptography challenges

## 📚 References

- [NIST Post-Quantum Cryptography](https://csrc.nist.gov/projects/post-quantum-cryptography)
- [Kyber Specification](https://pq-crystals.org/kyber/)
- [Open Quantum Safe liboqs](https://github.com/open-quantum-safe/liboqs)
- [liboqs-python](https://github.com/open-quantum-safe/liboqs-python)
- [@noble/post-quantum](https://github.com/paulmillr/noble-post-quantum)
