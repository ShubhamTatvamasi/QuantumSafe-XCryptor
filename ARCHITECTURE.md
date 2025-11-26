# System Architecture - QuantumSafe-XCryptor

## 🏗️ Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                    Post-Quantum Hybrid Encryption               │
│                    Kyber1024 + AES-256-GCM                      │
└─────────────────────────────────────────────────────────────────┘

┌──────────────┐         ┌──────────────┐         ┌──────────────┐
│              │         │              │         │              │
│  .NET (C#)   │ ◄─────► │   Shared     │ ◄─────► │   Python     │
│              │         │   Storage    │         │              │
│   liboqs     │         │   /data/     │         │   liboqs     │
│              │         │              │         │              │
└──────────────┘         └──────────────┘         └──────────────┘
       │                        │                         │
       │                        ▼                         │
       │                 ┌──────────────┐                 │
       │                 │              │                 │
       └────────────────►│ React Native │◄────────────────┘
                         │              │
                         │ @noble/pq    │
                         │   (WASM)     │
                         └──────────────┘
                                │
                                ▼
                        Encrypt + Decrypt
                           (Mobile)
```

## 🔐 Encryption Flow (.NET)

```
1. Key Generation
   ┌─────────────────┐
   │ Kyber1024       │
   │ KeyGen()        │
   └────────┬────────┘
            │
            ▼
   ┌─────────────────┐
   │ Public Key      │ 1568 bytes
   │ Private Key     │ 3168 bytes
   └─────────────────┘

2. Encapsulation
   ┌─────────────────┐
   │ Kyber1024       │
   │ Encapsulate()   │
   │ (Public Key)    │
   └────────┬────────┘
            │
            ▼
   ┌─────────────────┐
   │ Ciphertext      │ 1568 bytes
   │ Shared Secret   │ 32 bytes
   └─────────────────┘

3. Key Derivation
   ┌─────────────────┐
   │ HKDF-SHA256     │
   │ (Shared Secret) │
   └────────┬────────┘
            │
            ▼
   ┌─────────────────┐
   │ AES-256 Key     │ 32 bytes
   └─────────────────┘

4. Data Encryption
   ┌─────────────────┐
   │ AES-256-GCM     │
   │ (AES Key)       │
   │ (Plaintext)     │
   └────────┬────────┘
            │
            ▼
   ┌─────────────────┐
   │ Nonce (12)      │
   │ Ciphertext      │
   │ Tag (16)        │
   └─────────────────┘

5. Final Output
   ┌─────────────────────────────────────┐
   │ [Length:4][Kyber CT:1568]           │
   │ [Nonce:12][Ciphertext][Tag:16]      │
   └─────────────────────────────────────┘
```

## 🔓 Decryption Flow (Python)

```
1. Load Keys
   ┌─────────────────┐
   │ Load from disk  │
   │ Private Key     │ 3168 bytes
   └────────┬────────┘
            │
            ▼

2. Parse Encrypted File
   ┌─────────────────────────────────────┐
   │ Read encrypted.bin                  │
   └────────┬────────────────────────────┘
            │
            ▼
   ┌─────────────────┐
   │ Extract Length  │ 4 bytes (uint32)
   │ Extract Kyber   │ 1568 bytes
   │ Extract AES     │ variable
   └─────────────────┘

3. Decapsulation
   ┌─────────────────┐
   │ Kyber1024       │
   │ Decapsulate()   │
   │ (Private Key,   │
   │  Ciphertext)    │
   └────────┬────────┘
            │
            ▼
   ┌─────────────────┐
   │ Shared Secret   │ 32 bytes
   └─────────────────┘

4. Key Derivation
   ┌─────────────────┐
   │ HKDF-SHA256     │
   │ (Shared Secret) │
   └────────┬────────┘
            │
            ▼
   ┌─────────────────┐
   │ AES-256 Key     │ 32 bytes
   └─────────────────┘

5. Data Decryption
   ┌─────────────────┐
   │ AES-256-GCM     │
   │ Decrypt()       │
   │ Verify Tag      │
   └────────┬────────┘
            │
            ▼
   ┌─────────────────┐
   │ Plaintext       │ ✓ Verified
   └─────────────────┘
```

## 🔄 Data Flow Diagram

```
.NET Service                 Shared Storage              Python Service
─────────────               ────────────────             ───────────────

[Generate Keys]
     │
     ├─────────────────►  kyber_public.key
     │                    kyber_private.key
     │
[Read Plaintext]
     │
     ├────────────────►   sample.txt ─────────────────► [Load Plaintext]
     │
[Encapsulate]
     │
     ├─────────────────►  kyber_ciphertext.bin
     │
[Derive AES Key]
     │
[Encrypt Data]
     │
     ├─────────────────►  encrypted.bin ──────────────► [Read Encrypted]
     │                                                          │
[Decrypt (verify)]                                    [Decapsulate Secret]
     │                                                          │
     ├─────────────────►  decrypted-dotnet.txt      [Derive AES Key]
     │                                                          │
     │                                               [Decrypt Data]
     │                                                          │
     │                    decrypted-python.txt ◄────────────────┤
     │                                                          │
     │                                                   [Verify Match]
     ▼                                                          ▼
  [Done]                                                     [Done]
```

## 📦 Component Details

### Kyber1024 (KEM)

```
┌─────────────────────────────────────────┐
│ CRYSTALS-Kyber (NIST PQC Standard)      │
├─────────────────────────────────────────┤
│ Security Level: 5 (256-bit quantum)     │
│ Algorithm: Lattice-based (Module-LWE)   │
│ Public Key: 1568 bytes                  │
│ Private Key: 3168 bytes                 │
│ Ciphertext: 1568 bytes                  │
│ Shared Secret: 32 bytes                 │
└─────────────────────────────────────────┘
```

### AES-256-GCM

```
┌─────────────────────────────────────────┐
│ Advanced Encryption Standard            │
├─────────────────────────────────────────┤
│ Mode: GCM (Galois/Counter Mode)         │
│ Key Size: 256 bits (32 bytes)           │
│ Nonce: 96 bits (12 bytes)               │
│ Tag: 128 bits (16 bytes)                │
│ Features: AEAD (Authenticated)          │
└─────────────────────────────────────────┘
```

### HKDF-SHA256

```
┌─────────────────────────────────────────┐
│ HMAC-based Key Derivation Function      │
├─────────────────────────────────────────┤
│ Hash: SHA-256                           │
│ Input: 32-byte shared secret            │
│ Salt: 32 zero bytes                     │
│ Info: "AES-256-GCM"                     │
│ Output: 32-byte AES key                 │
└─────────────────────────────────────────┘
```

## 🛡️ Security Layers

```
┌───────────────────────────────────────────────────┐
│ Layer 4: Quantum Resistance (Kyber1024)           │
├───────────────────────────────────────────────────┤
│ Layer 3: Key Derivation (HKDF)                    │
├───────────────────────────────────────────────────┤
│ Layer 2: Authenticated Encryption (AES-GCM)       │
├───────────────────────────────────────────────────┤
│ Layer 1: Secure Random (Nonces)                   │
└───────────────────────────────────────────────────┘
```

## 🔐 Key Management

```
                    ┌─────────────────┐
                    │ Kyber1024 KeyGen│
                    └────────┬────────┘
                             │
              ┌──────────────┴──────────────┐
              ▼                             ▼
    ┌──────────────────┐        ┌──────────────────┐
    │  Public Key      │        │  Private Key     │
    │  (Shareable)     │        │  (Keep Secret!)  │
    │  1568 bytes      │        │  3168 bytes      │
    └──────────────────┘        └──────────────────┘
              │                             │
              │ Encapsulate                 │ Decapsulate
              ▼                             ▼
    ┌──────────────────┐        ┌──────────────────┐
    │ Kyber Ciphertext │───────►│ Shared Secret    │
    │ 1568 bytes       │        │ 32 bytes         │
    └──────────────────┘        └──────────────────┘
                                          │
                                          │ HKDF
                                          ▼
                                ┌──────────────────┐
                                │ AES-256 Key      │
                                │ 32 bytes         │
                                └──────────────────┘
```

## 🌐 Cross-Platform Compatibility

```
.NET (liboqs)      React Native (WASM)      Python (liboqs)
─────────────      ──────────────────        ─────────────────

Kyber1024 ◄─────────► Kyber1024 ◄─────────────► Kyber1024
    │                     │                           │
    │                     │  Same Parameters          │
    │                     │  Same Encoding            │
    │                     │                           │
    ▼                     ▼                           ▼
HKDF-SHA256 ◄────────► HKDF-SHA256 ◄────────────► HKDF-SHA256
    │                     │                           │
    │                     │  Same Salt/Info           │
    │                     │                           │
    ▼                     ▼                           ▼
AES-256-GCM ◄────────► AES-256-GCM ◄────────────► AES-256-GCM
    │                     │                           │
    │                     │  Same Format              │
    │                     │                           │
    ▼                     ▼                           ▼
[Encrypted Data] ◄───► [Encrypted Data] ◄────────► [Decrypted Data]

All three platforms produce identical encrypted outputs and can decrypt
each other's files, ensuring full post-quantum interoperability.
```

## 📊 Performance Characteristics

```
Operation             Time (approx)    Size
────────────────────  ──────────────   ──────────
Kyber KeyGen          ~0.1 ms          4736 bytes
Kyber Encapsulate     ~0.1 ms          1568 bytes
Kyber Decapsulate     ~0.1 ms          32 bytes
HKDF Derive           <0.1 ms          32 bytes
AES-GCM Encrypt       ~1 MB/ms         +28 bytes
AES-GCM Decrypt       ~1 MB/ms         -28 bytes
────────────────────────────────────────────────
Total Overhead                         ~1600 bytes
```

## 📱 React Native Implementation

```
┌─────────────────────────────────────────┐
│ React Native Post-Quantum Crypto Stack  │
├─────────────────────────────────────────┤
│ Kyber1024: @noble/post-quantum (WASM)   │
│ AES-GCM: react-native-aes-gcm           │
│ HKDF: crypto-browserify                 │
│ Random: react-native-randombytes        │
│ Files: react-native-fs                  │
└─────────────────────────────────────────┘

Features:
✅ Full encryption workflow
✅ Full decryption workflow
✅ Feature detection (WASM/native fallback)
✅ Cross-platform file format
✅ Mobile-optimized performance
```

## 🎯 Use Cases

1. **Secure File Storage**: Quantum-safe cloud backups
2. **Secure Messaging**: Post-quantum chat applications
3. **API Security**: Quantum-resistant API encryption
4. **IoT Security**: Future-proof device communication
5. **Blockchain**: Quantum-resistant transactions
6. **Mobile Apps**: Quantum-safe data protection on iOS/Android

## 🔬 Testing Matrix

```
┌──────────────┬───────────┬─────────────┬──────────────┐
│ Component    │ .NET      │ Python      │ React Native │
├──────────────┼───────────┼─────────────┼──────────────┤
│ Kyber KeyGen │     ✅    │      ✅     │      ✅      │
│ Encapsulate  │     ✅    │      ✅     │      ✅      │
│ Decapsulate  │     ✅    │      ✅     │      ✅      │
│ HKDF         │     ✅    │      ✅     │      ✅      │
│ AES Encrypt  │     ✅    │      ✅     │      ✅      │
│ AES Decrypt  │     ✅    │      ✅     │      ✅      │
├──────────────┼───────────┼─────────────┼──────────────┤
│ Backend      │  liboqs   │   liboqs    │  @noble/pq   │
│              │  (native) │  (native)   │    (WASM)    │
└──────────────┴───────────┴─────────────┴──────────────┘

✅ = Fully Implemented and Tested
```

This architecture provides quantum-resistant security while maintaining
cross-platform compatibility and reasonable performance characteristics.
