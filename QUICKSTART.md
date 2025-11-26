# Quick Start Guide - QuantumSafe-XCryptor

## 🚀 Running the Post-Quantum Demo

### Step 1: Build the Services

```bash
docker-compose build
```

This will:
- Build .NET service with liboqs (Kyber1024 via native shim)
- Build Python service with liboqs + liboqs-python
- Set up all dependencies

### Step 2: Run the Demo

```bash
docker-compose up && docker-compose down
```

Watch the output to see:
1. **.NET** generating Kyber1024 keypair
2. **.NET** encrypting with post-quantum security
3. **Python** decrypting using Kyber private key
4. Both services verifying the output

### Step 3: Verify the Results

```bash
# Check original plaintext
cat shared/sample.txt

# Check .NET decryption
cat shared/decrypted-dotnet.txt

# Check Python decryption
cat shared/decrypted-python.txt

# All three should be identical!
```

### Step 4: Inspect the Keys

```bash
# View Kyber public key (base64)
cat shared/kyber_public.txt

# View Kyber private key (base64)
cat shared/kyber_private.txt

# Check key sizes
ls -lh shared/kyber_*.key
```

Expected sizes:
- Public key: 1,568 bytes
- Private key: 3,168 bytes
- Ciphertext: 1,568 bytes

## 🔍 Understanding the Output

### .NET Service Output

```
DotNet: Generating Kyber1024 keypair...
DotNet: Public key size: 1568 bytes
DotNet: Private key size: 3168 bytes

DotNet: Starting encryption...
DotNet: Kyber ciphertext size: 1568 bytes
DotNet: Shared secret size: 32 bytes
DotNet: File encrypted with post-quantum security.

DotNet: Starting decryption...
DotNet: File decrypted successfully.
DotNet: Decrypted content: [your plaintext]
```

### Python Service Output

```
=== Python: Testing Kyber1024 Compatibility with liboqs ===
Python: Generated keypair - Public: 1568 bytes, Private: 3168 bytes
Python: Encapsulated - Ciphertext: 1568 bytes, Secret: 32 bytes
Python: Decapsulated - Secret: 32 bytes
Python: ✓ Kyber1024 round-trip successful!

=== Python: Starting Hybrid Decryption ===
Python: Loaded private key (3168 bytes)
Python: Kyber ciphertext size: 1568 bytes
Python: AES encrypted size: [varies] bytes
Python: Shared secret recovered (32 bytes)
Python: File decrypted successfully.
Python: Decrypted content: [your plaintext]
```

## 🧪 Testing Different Plaintexts

Edit the plaintext:

```bash
echo "Your secret message here" > shared/sample.txt
docker-compose up && docker-compose down
```

## 📊 File Structure After Running

```
shared/
├── sample.txt                    # Original plaintext
├── encrypted.bin                 # Hybrid encrypted file
├── kyber_public.key             # Binary public key (1568 bytes)
├── kyber_public.txt             # Base64 public key
├── kyber_private.key            # Binary private key (3168 bytes)
├── kyber_private.txt            # Base64 private key
├── kyber_ciphertext.bin         # Encapsulated secret (1568 bytes)
├── kyber_ciphertext.txt         # Base64 ciphertext
├── decrypted-dotnet.txt         # .NET decryption result
└── decrypted-python.txt         # Python decryption result
```

## 🔧 Troubleshooting

### Build Errors

**Issue:** NuGet restore fails
```bash
# Clear Docker build cache
docker-compose build --no-cache
```

**Issue:** Python dependencies fail
```bash
# Ensure liboqs builds (system package) before installing liboqs-python
# Base image is python:3.11-slim and builds liboqs 0.15.0
```

### Runtime Errors

**Issue:** "Kyber keys not found"
- Make sure .NET service runs first
- Check `shared/` directory permissions

**Issue:** "Decryption failed"
- Verify all files were created by .NET service
- Check file sizes match expected values

## 🎓 Learning More

### Examine the Code

1. **Kyber Implementation**
   - .NET: `dotnet-service/KyberHelper.cs`
   - Python: `python-service/main.py`

2. **AES-GCM Integration**
   - .NET: `dotnet-service/AesGcmHelper.cs`
   - Python: Uses cryptography library

3. **Hybrid Encryption**
   - .NET: `dotnet-service/Program.cs`
   - Python: `python-service/main.py`

### Key Concepts

- **KEM (Key Encapsulation Mechanism)**: Public key encryption for shared secrets
- **Hybrid Encryption**: Combine KEM with symmetric encryption
- **HKDF**: Key derivation from shared secret
- **AES-GCM**: Authenticated encryption with associated data

## 🚢 Production Considerations

Before using in production:

1. ✅ Implement proper key management (HSM/TPM)
2. ✅ Add key rotation mechanisms
3. ✅ Implement secure key distribution (certificates)
4. ✅ Add audit logging
5. ✅ Consider forward secrecy
6. ✅ Implement rate limiting
7. ✅ Add monitoring and alerts

## 📝 Common Commands

```bash
# Clean everything
rm -rf shared/*.bin shared/*.key shared/*.txt shared/decrypted-*

# Rebuild from scratch
docker-compose down -v
docker-compose build --no-cache
docker-compose up

# View logs
docker-compose logs dotnet-service
docker-compose logs python-service

# Interactive debugging
docker-compose run dotnet-service /bin/bash
docker-compose run python-service /bin/bash
```

## ✨ Success Indicators

Your implementation is working correctly if:

1. ✅ No errors in Docker logs
2. ✅ All key files are generated (6 Kyber files)
3. ✅ `decrypted-dotnet.txt` matches `sample.txt`
4. ✅ `decrypted-python.txt` matches `sample.txt`
5. ✅ Python shows "✓ Kyber1024 round-trip successful!"
6. ✅ File sizes match expected values

## 🎉 Congratulations!

You now have a working post-quantum secure encryption system that's:
- Quantum-resistant (Kyber1024)
- Cross-platform compatible (.NET ↔️ Python)
- Production-ready architecture
- Well-documented and testable

For questions or improvements, refer to `IMPLEMENTATION.md` and the main `README.md`.
