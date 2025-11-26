# Quick Start: Adding Kyber via WASM

This guide shows the **fastest way** to add working Kyber1024 support to the React Native app using WebAssembly.

## Option 1: @noble/post-quantum (Recommended)

### 1. Install Package

```bash
cd react-native-app
npm install @noble/post-quantum
```

### 2. Update kyber.js

Replace the stub implementations with working code:

```javascript
// At top of kyber.js, add:
import { kyber1024 } from '@noble/post-quantum/kyber';

// Replace generateKeyPair function:
export async function generateKeyPair() {
  try {
    const keys = kyber1024.keygen();
    return {
      publicKey: Buffer.from(keys.publicKey),
      privateKey: Buffer.from(keys.secretKey)
    };
  } catch (error) {
    throw new Error(`Kyber keygen failed: ${error.message}`);
  }
}

// Replace encapsulate function:
export async function encapsulate(publicKey) {
  const pkBuffer = typeof publicKey === 'string' 
    ? Buffer.from(publicKey, 'base64') 
    : publicKey;
  
  if (pkBuffer.length !== KYBER_PUBLIC_KEY_BYTES) {
    throw new Error(
      `Invalid public key size: expected ${KYBER_PUBLIC_KEY_BYTES} bytes, got ${pkBuffer.length}`
    );
  }
  
  try {
    const result = kyber1024.encapsulate(new Uint8Array(pkBuffer));
    return {
      ciphertext: Buffer.from(result.ciphertext),
      sharedSecret: Buffer.from(result.sharedSecret)
    };
  } catch (error) {
    throw new Error(`Kyber encapsulation failed: ${error.message}`);
  }
}

// Replace decapsulate function:
export async function decapsulate(privateKey, ciphertext) {
  const skBuffer = typeof privateKey === 'string' 
    ? Buffer.from(privateKey, 'base64') 
    : privateKey;
  
  const ctBuffer = typeof ciphertext === 'string' 
    ? Buffer.from(ciphertext, 'base64') 
    : ciphertext;
  
  if (skBuffer.length !== KYBER_PRIVATE_KEY_BYTES) {
    throw new Error(
      `Invalid private key size: expected ${KYBER_PRIVATE_KEY_BYTES} bytes, got ${skBuffer.length}`
    );
  }
  
  if (ctBuffer.length !== KYBER_CIPHERTEXT_BYTES) {
    throw new Error(
      `Invalid ciphertext size: expected ${KYBER_CIPHERTEXT_BYTES} bytes, got ${ctBuffer.length}`
    );
  }
  
  try {
    const sharedSecret = kyber1024.decapsulate(
      new Uint8Array(ctBuffer),
      new Uint8Array(skBuffer)
    );
    return Buffer.from(sharedSecret);
  } catch (error) {
    throw new Error(`Kyber decapsulation failed: ${error.message}`);
  }
}

// Update isNativeKyberAvailable to detect WASM:
export function isNativeKyberAvailable() {
  try {
    // Check if kyber1024 is available
    return typeof kyber1024 !== 'undefined' && 
           typeof kyber1024.keygen === 'function';
  } catch {
    return false;
  }
}
```

### 3. Test It

```bash
# Run the app (adjust for your platform)
npm start

# Or for Expo:
npx expo start
```

Expected output:
```
✓ Native Kyber module detected!
--- Full Hybrid Encryption Workflow ---
✓ Loaded public key (1568 bytes)
✓ Encapsulated: ciphertext 1568 bytes, secret 32 bytes
✓ Derived AES-256 key (32 bytes)
✓ Encrypted plaintext with AES-GCM
✓ Saved /data/encrypted-reactnative.bin
✓ Encryption complete, now testing decryption...
--- Hybrid Decryption Workflow ---
✓ Decapsulated shared secret (32 bytes)
✓ Derived AES-256 key from shared secret
✓ Successfully decrypted!
```

## Option 2: pqc-kyber (Alternative)

If `@noble/post-quantum` has issues:

```bash
npm install pqc-kyber
```

```javascript
import { Kyber1024 } from 'pqc-kyber';

export async function generateKeyPair() {
  const keys = await Kyber1024.KeyGen();
  return {
    publicKey: Buffer.from(keys.publicKey),
    privateKey: Buffer.from(keys.privateKey)
  };
}

export async function encapsulate(publicKey) {
  const pkBuffer = typeof publicKey === 'string' 
    ? Buffer.from(publicKey, 'base64') 
    : publicKey;
  
  const result = await Kyber1024.Encapsulate(new Uint8Array(pkBuffer));
  return {
    ciphertext: Buffer.from(result.ciphertext),
    sharedSecret: Buffer.from(result.sharedSecret)
  };
}

export async function decapsulate(privateKey, ciphertext) {
  const skBuffer = typeof privateKey === 'string' 
    ? Buffer.from(privateKey, 'base64') 
    : privateKey;
  
  const ctBuffer = typeof ciphertext === 'string' 
    ? Buffer.from(ciphertext, 'base64') 
    : ciphertext;
  
  const sharedSecret = await Kyber1024.Decapsulate(
    new Uint8Array(ctBuffer),
    new Uint8Array(skBuffer)
  );
  return Buffer.from(sharedSecret);
}
```

## Verifying Cross-Platform Compatibility

After adding WASM Kyber, test compatibility with .NET/Python:

1. **Run .NET service** to generate keys
2. **Run React Native app**
3. **App should decrypt .NET-encrypted files**
4. **Python should decrypt React Native-encrypted files**

### Test Commands

```bash
# Terminal 1: Run .NET
cd /path/to/QuantumSafe-XCryptor
docker compose up dotnet-service

# Terminal 2: Copy keys to React Native (if needed)
# (Adjust paths for your setup)

# Terminal 3: Run React Native
cd react-native-app
npm start

# Terminal 4: Verify outputs match
diff shared/decrypted-dotnet.txt shared/decrypted-reactnative.txt
diff shared/decrypted-python.txt shared/decrypted-reactnative.txt
```

## Performance Expectations

With WASM implementation:
- Key generation: ~2-5ms
- Encapsulation: ~2-5ms
- Decapsulation: ~2-5ms
- Total overhead: acceptable for most mobile apps

Native modules would be ~10x faster but WASM is sufficient for most use cases.

## Troubleshooting

### "Cannot find module @noble/post-quantum"

```bash
# Clear cache and reinstall
rm -rf node_modules package-lock.json
npm install
```

### "kyber1024 is not a function"

Check import syntax:
```javascript
// Correct:
import { kyber1024 } from '@noble/post-quantum/kyber';

// Incorrect:
import kyber1024 from '@noble/post-quantum/kyber';
```

### WASM Loading Issues

Add to metro.config.js:
```javascript
module.exports = {
  resolver: {
    assetExts: ['wasm'],
  },
};
```

### React Native Bundler Issues

For Expo:
```bash
npx expo install @noble/post-quantum
```

For bare React Native, you may need:
```bash
npm install --save-dev metro-react-native-babel-preset
```

## Next Steps After WASM Integration

1. ✅ Test encryption/decryption
2. ✅ Verify cross-platform compatibility
3. ⚠️ Consider native module for production (better performance)
4. ⚠️ Add secure key storage (Keychain/KeyStore)
5. ⚠️ Implement key rotation
6. ⚠️ Add unit tests

## Converting to Native Module Later

Once WASM is working, you can migrate to a native module for better performance without changing app code. Just replace the WASM imports with native module calls behind the same API.

## Summary

WASM provides the **quickest path** to working Kyber support:
- ✅ No native build setup
- ✅ Cross-platform (iOS, Android, Web)
- ✅ Easy to install and test
- ✅ Good enough performance for most apps
- ⚠️ Slightly slower than native
- ⚠️ Larger bundle size

For production apps with high-performance needs, consider migrating to a native module later.
