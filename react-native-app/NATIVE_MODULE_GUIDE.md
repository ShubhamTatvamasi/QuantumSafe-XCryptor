# Native Kyber Module Implementation Guide

This guide explains how to add native Kyber1024 support to the React Native app for full post-quantum encryption/decryption capabilities.

## Current State

The React Native app includes:
- ✅ Complete AES-256-GCM encryption/decryption
- ✅ HKDF-SHA256 key derivation
- ✅ Kyber API structure with feature detection
- ⚠️ Kyber operations require native implementation

## Implementation Options

### Option 1: React Native Native Module (iOS/Android)

Create a native module that bridges to liboqs:

#### iOS (Swift/Objective-C)

1. **Install liboqs** (via CocoaPods or manual build)
```ruby
# Podfile
pod 'liboqs', :git => 'https://github.com/open-quantum-safe/liboqs.git'
```

2. **Create Native Module**
```swift
// KyberModule.swift
@objc(KyberModule)
class KyberModule: NSObject {
  @objc
  func generateKeyPair(_ resolve: @escaping RCTPromiseResolveBlock,
                      reject: @escaping RCTPromiseRejectBlock) {
    // Call liboqs OQS_KEM_keypair
    // Return base64 encoded keys
  }
  
  @objc
  func encapsulate(_ publicKey: String,
                  resolve: @escaping RCTPromiseResolveBlock,
                  reject: @escaping RCTPromiseRejectBlock) {
    // Call liboqs OQS_KEM_encaps
    // Return base64 encoded ciphertext and shared secret
  }
  
  @objc
  func decapsulate(_ ciphertext: String,
                  privateKey: String,
                  resolve: @escaping RCTPromiseResolveBlock,
                  reject: @escaping RCTPromiseRejectBlock) {
    // Call liboqs OQS_KEM_decaps
    // Return base64 encoded shared secret
  }
}
```

#### Android (Java/Kotlin)

1. **Add liboqs JNI bindings** or build liboqs as .so
```gradle
// android/app/build.gradle
android {
  sourceSets {
    main {
      jniLibs.srcDirs = ['libs']
    }
  }
}
```

2. **Create Native Module**
```kotlin
// KyberModule.kt
class KyberModule(reactContext: ReactApplicationContext) : 
    ReactContextBaseJavaModule(reactContext) {
    
    override fun getName() = "KyberModule"
    
    @ReactMethod
    fun generateKeyPair(promise: Promise) {
        // Call liboqs via JNI
        // Return base64 keys
    }
    
    @ReactMethod
    fun encapsulate(publicKey: String, promise: Promise) {
        // Call liboqs via JNI
        // Return base64 ciphertext and shared secret
    }
    
    @ReactMethod
    fun decapsulate(ciphertext: String, privateKey: String, promise: Promise) {
        // Call liboqs via JNI
        // Return base64 shared secret
    }
}
```

### Option 2: WASM Library

Use a WebAssembly-based Kyber implementation:

1. **Install WASM Kyber library**
```bash
npm install @noble/post-quantum
# or
npm install kyber-crystals
```

2. **Update kyber.js to use WASM**
```javascript
import { kyber1024 } from '@noble/post-quantum/kyber';

export async function generateKeyPair() {
  const keys = kyber1024.keygen();
  return {
    publicKey: Buffer.from(keys.publicKey),
    privateKey: Buffer.from(keys.secretKey)
  };
}

export async function encapsulate(publicKey) {
  const result = kyber1024.encapsulate(publicKey);
  return {
    ciphertext: Buffer.from(result.ciphertext),
    sharedSecret: Buffer.from(result.sharedSecret)
  };
}

export async function decapsulate(privateKey, ciphertext) {
  const sharedSecret = kyber1024.decapsulate(ciphertext, privateKey);
  return Buffer.from(sharedSecret);
}
```

### Option 3: Expo Config Plugin (for Expo projects)

Create a custom config plugin that integrates liboqs:

```javascript
// app.plugin.js
const { withDangerousMod } = require('@expo/config-plugins');

module.exports = function withLiboqs(config) {
  return withDangerousMod(config, [
    'ios',
    async (config) => {
      // Add liboqs framework
      return config;
    }
  ]);
};
```

## Recommended Approach

**For Production: Option 1 (Native Module)**
- Best performance
- Direct access to liboqs
- Full control over implementation
- Native security features (keychain/keystore)

**For Quick Prototyping: Option 2 (WASM)**
- Faster to implement
- Cross-platform JavaScript
- No native build setup
- Slightly slower performance

## Testing Your Implementation

Once you've implemented native Kyber support, the React Native app will automatically detect it via `isNativeKyberAvailable()` and enable full encryption/decryption.

### Expected Behavior

1. **Without native Kyber:**
   - App detects absence of KyberModule
   - Falls back to decryption using .NET-generated keys
   - Logs warning about missing native support

2. **With native Kyber:**
   - App detects KyberModule
   - Performs full encryption workflow
   - Tests decryption with own keys
   - Verifies cross-platform compatibility

## Example Native Module Template

See `react-native-app/KyberModuleTemplate/` for a complete example (to be created).

## Performance Targets

- Kyber1024 keygen: < 1ms
- Encapsulation: < 1ms
- Decapsulation: < 1ms
- Total hybrid encryption: < 5ms (including AES-GCM)

## Security Considerations

1. ✅ Use secure random number generator (SecureRandom/os.urandom)
2. ✅ Clear sensitive buffers after use
3. ✅ Store private keys in platform keychain/keystore
4. ✅ Validate all input sizes
5. ✅ Handle errors securely (no key material in logs)

## Resources

- [liboqs Documentation](https://github.com/open-quantum-safe/liboqs/wiki)
- [React Native Native Modules](https://reactnative.dev/docs/native-modules-intro)
- [@noble/post-quantum](https://github.com/paulmillr/noble-post-quantum)
- [Kyber Specification](https://pq-crystals.org/kyber/index.shtml)

## Next Steps

1. Choose implementation approach (native module vs WASM)
2. Set up development environment
3. Implement the three core functions (keygen, encaps, decaps)
4. Test with existing .NET/Python interoperability
5. Add key storage and management
6. Implement full application flow

For questions or help, refer to the main project documentation.
