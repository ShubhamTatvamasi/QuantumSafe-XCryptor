# QuantumSafe-XCryptor

QuantumSafe-XCryptor is a cross-language proof-of-concept demonstrating secure file encryption and decryption using AES-256-GCM across different platforms. Encryption is performed by the .NET service, and decryption is verified by both .NET and Python using the shared AES key from `key.txt`. The React Native app demonstrates full bidirectional encryption and decryption capabilities.

#### .NET (encryption + decryption)

The .NET service encrypts `sample.txt` using the shared key from `key.txt` to produce `encrypted.bin`, and also decrypts it back to `decrypted-dotnet.txt` for verification.

#### Python (decryption only)

The Python service performs decryption only: it uses the same AES key from `key.txt` and the `encrypted.bin` file produced by .NET to recover `decrypted-python.txt`, demonstrating cross-language compatibility.

#### React Native (encryption + decryption)

The React Native mobile app demonstrates full encryption and decryption capabilities: it can encrypt new data to produce `encrypted-reactnative.bin`, decrypt its own encrypted files, and decrypt files created by the .NET or Python services, showcasing complete cross-platform interoperability.

---

## Getting Started

### 1. Generate AES Key

Generate a 256-bit AES key using OpenSSL:
```bash
openssl rand -base64 32 > shared/key.txt
```

### 2. Build Docker Images

Build the Docker images for both services:
```bash
docker-compose build
```

### 3. Run the Services

Execute the encryption and decryption workflow:
```bash
docker-compose up && docker-compose down
```
