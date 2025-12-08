"""
ML-KEM-1024 Decapsulation + AES Decryption Service
Loads private key and ciphertext, decapsulates shared secret, and decrypts file.
"""
import os
import time
import oqs
from cryptography.hazmat.primitives.ciphers.aead import AESGCM
from cryptography.hazmat.primitives import hashes, hmac

# Paths in shared volume
DATA_DIR = "/data"
PRIVATE_KEY_PATH = os.path.join(DATA_DIR, "kyber_private.key")
CIPHERTEXT_PATH = os.path.join(DATA_DIR, "kyber_ciphertext.bin")
ENCRYPTED_PACKET_PATH = os.path.join(DATA_DIR, "encrypted-dotnet.bin")
OUTPUT_PATH = os.path.join(DATA_DIR, "decrypted-python.txt")

# ML-KEM-1024 constants
KEM_ALG = "Kyber1024"
KYBER_CIPHERTEXT_SIZE = 1568
KYBER_SHARED_SECRET_SIZE = 32

# HKDF parameters (MUST match .NET)
HKDF_SALT = b'\x00' * 32
HKDF_INFO = b"AES-256-GCM"
HKDF_OUTPUT_LENGTH = 32

def wait_for_file(path: str, timeout: int = 30) -> None:
    """Wait for a file to exist, with timeout."""
    start = time.time()
    while not os.path.exists(path):
        if time.time() - start > timeout:
            raise TimeoutError(f"File not found after {timeout}s: {path}")
        time.sleep(0.5)

def derive_aes_key(shared_secret: bytes) -> bytes:
    """
    Derive AES-256 key from Kyber shared secret using HKDF-SHA256.
    
    CRITICAL: Must be IDENTICAL to .NET implementation.
    
    Parameters:
    - Salt: 32 zero bytes
    - Info: "AES-256-GCM"
    - Hash: SHA-256
    - Output: 32 bytes
    """
    if len(shared_secret) != KYBER_SHARED_SECRET_SIZE:
        raise ValueError(f"Shared secret must be {KYBER_SHARED_SECRET_SIZE} bytes")
    
    # HKDF Extract phase
    h = hmac.HMAC(HKDF_SALT, hashes.SHA256())
    h.update(shared_secret)
    prk = h.finalize()
    
    # HKDF Expand phase
    okm = bytearray()
    previous = b''
    counter = 1
    
    while len(okm) < HKDF_OUTPUT_LENGTH:
        h = hmac.HMAC(prk, hashes.SHA256())
        h.update(previous)
        h.update(HKDF_INFO)
        h.update(bytes([counter]))
        previous = h.finalize()
        okm.extend(previous)
        counter += 1
    
    return bytes(okm[:HKDF_OUTPUT_LENGTH])

def decrypt_packet():
    """Decapsulate + decrypt packet from .NET service."""
    print("=" * 60)
    print("🔓 ML-KEM-1024 Decapsulation + AES Decryption Service (Python)")
    print("=" * 60 + "\n")
    
    # Wait for private key
    print(f"⏳ Waiting for private key: {PRIVATE_KEY_PATH}")
    wait_for_file(PRIVATE_KEY_PATH, 30)
    with open(PRIVATE_KEY_PATH, "rb") as f:
        private_key = f.read()
    print(f"✓ Private key loaded: {len(private_key)} bytes from {PRIVATE_KEY_PATH}\n")
    
    # Wait for encrypted packet
    print(f"⏳ Waiting for encrypted packet: {ENCRYPTED_PACKET_PATH}")
    wait_for_file(ENCRYPTED_PACKET_PATH, 30)
    with open(ENCRYPTED_PACKET_PATH, "rb") as f:
        packet = f.read()
    print(f"✓ Encrypted packet loaded: {len(packet)} bytes from {ENCRYPTED_PACKET_PATH}\n")
    
    if len(packet) < KYBER_CIPHERTEXT_SIZE + 12 + 16:
        raise ValueError(f"Packet too short: {len(packet)} bytes")
    
    # Extract components
    kyber_ciphertext = packet[:KYBER_CIPHERTEXT_SIZE]
    aes_payload = packet[KYBER_CIPHERTEXT_SIZE:]
    
    print(f"📦 Packet structure:")
    print(f"   • Kyber ciphertext: {len(kyber_ciphertext)} bytes")
    print(f"   • AES payload: {len(aes_payload)} bytes\n")
    
    # Decapsulate
    print(f"🔐 [1/3] Decapsulating with private key: {PRIVATE_KEY_PATH}")
    print(f"        Using ciphertext from: {ENCRYPTED_PACKET_PATH} (first {KYBER_CIPHERTEXT_SIZE} bytes)")
    with oqs.KeyEncapsulation(KEM_ALG, secret_key=private_key) as kem:
        shared_secret = kem.decap_secret(kyber_ciphertext)
    print(f"        ✓ Shared secret recovered: {len(shared_secret)} bytes\n")
    
    # Derive AES key
    print(f"🔑 [2/3] Deriving AES key (HKDF-SHA256)...")
    aes_key = derive_aes_key(shared_secret)
    print(f"        ✓ AES key derived: {len(aes_key)} bytes (salt=32x0, info='AES-256-GCM')\n")
    
    # Split AES payload
    nonce = aes_payload[:12]
    ciphertext = aes_payload[12:-16]
    tag = aes_payload[-16:]
    
    print(f"🔓 [3/3] Decrypting with AES-256-GCM...")
    print(f"        • Nonce: {len(nonce)} bytes")
    print(f"        • Ciphertext: {len(ciphertext)} bytes")
    print(f"        • Tag: {len(tag)} bytes")
    
    # Decrypt
    aesgcm = AESGCM(aes_key)
    plaintext = aesgcm.decrypt(nonce, ciphertext + tag, None)
    
    # Write output
    with open(OUTPUT_PATH, "wb") as f:
        f.write(plaintext)
    
    print(f"        ✓ Decrypted successfully!\n")
    print(f"📄 Output written to: {OUTPUT_PATH} ({len(plaintext)} bytes)")
    print(f"📝 Content: \"{plaintext.decode('utf-8')}\"\n")
    
    print("=" * 60)
    print("✅ Decryption complete!")
    print("=" * 60)

if __name__ == "__main__":
    try:
        decrypt_packet()
    except Exception as e:
        print(f"\n❌ Decryption failed: {e}")
        import traceback
        traceback.print_exc()
        exit(1)
