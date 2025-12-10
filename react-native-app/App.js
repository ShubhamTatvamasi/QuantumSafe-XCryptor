import React, { useEffect, useState } from 'react';
import { SafeAreaView, Text, ScrollView, StyleSheet } from 'react-native';
import { encryptHybrid, decodeBase64Key, encodeBase64 } from './crypto';
import { kyber1024 } from '@noble/post-quantum/kyber';

// Replace with your real ML-KEM-1024 public key (Base64 of 1568-byte kyber_public.key)
const PUBLIC_KEY_B64 = null; // e.g., 'AAECAwQ...'

const SAMPLE_MESSAGE = 'Hello from React Native (Kyber + AES-GCM)!';

export default function App() {
  const [log, setLog] = useState('');

  useEffect(() => {
    (async () => {
      try {
        const lines = [];
        lines.push('🚀 React Native ML-KEM-1024 Hybrid Encryption');

        // Load or generate a public key
        let publicKeyBytes;
        if (PUBLIC_KEY_B64) {
          publicKeyBytes = decodeBase64Key(PUBLIC_KEY_B64);
          lines.push(`✓ Loaded public key: ${publicKeyBytes.length} bytes`);
        } else {
          const { publicKey, secretKey } = kyber1024.keyGen();
          publicKeyBytes = publicKey;
          lines.push('⚠️ No PUBLIC_KEY_B64 set; generated a fresh keypair (demo only)');
          lines.push(`   Public key (Base64): ${encodeBase64(publicKey)}`);
          lines.push(`   Private key (Base64): ${encodeBase64(secretKey)}`);
        }

        // Encrypt sample message
        const plaintextBytes = new TextEncoder().encode(SAMPLE_MESSAGE);
        const { packet, kyberCiphertext, nonce, aesEncrypted } = encryptHybrid({
          publicKeyBytes,
          plaintextBytes,
        });

        lines.push(`✓ Encapsulated Kyber CT: ${kyberCiphertext.length} bytes`);
        lines.push(`✓ Nonce: ${nonce.length} bytes`);
        lines.push(`✓ AES payload (CT||tag): ${aesEncrypted.length} bytes`);
        lines.push(`✓ Packet size: ${packet.length} bytes`);
        lines.push('✅ Hybrid encryption complete');

        setLog(lines.join('\n'));
      } catch (err) {
        setLog(`❌ Error: ${err.message}`);
      }
    })();
  }, []);

  return (
    <SafeAreaView style={styles.container}>
      <ScrollView style={styles.scroll}>
        <Text style={styles.mono}>{log}</Text>
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#0b1224', padding: 16 },
  scroll: { flex: 1 },
  mono: {
    color: '#e8f0ff',
    fontFamily: 'Menlo',
    fontSize: 14,
    lineHeight: 20,
  },
});
