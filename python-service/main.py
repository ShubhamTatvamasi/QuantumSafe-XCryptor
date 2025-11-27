"""
Post-Quantum Hybrid Decryption Service (Python)

This module demonstrates post-quantum secure decryption using Kyber1024 KEM
combined with AES-256-GCM. It decrypts files encrypted by the .NET service,
demonstrating cross-language compatibility with post-quantum cryptography.

Uses liboqs-python for Kyber1024 operations (Open Quantum Safe project).

Decryption Flow:
1. Load Kyber1024 private key
2. Extract Kyber ciphertext from encrypted file
3. Decapsulate shared secret using private key
4. Derive AES-256 key from shared secret
5. Decrypt data with AES-256-GCM
"""

import base64
import warnings
import struct
from cryptography.hazmat.primitives.ciphers.aead import AESGCM
from cryptography.hazmat.primitives.kdf.hkdf import HKDF
from cryptography.hazmat.primitives import hashes
from cryptography.hazmat.backends import default_backend

try:
    # Suppress liboqs-python version mismatch warning (when liboqs != liboqs-python)
    warnings.filterwarnings(
        "ignore",
        message=r"liboqs version \(major, minor\) .* differs from liboqs-python version .*",
        category=UserWarning,
        module="oqs"
    )
    import oqs
    KYBER_AVAILABLE = True
    print(f"Python: liboqs version {oqs.oqs_version()}")
except ImportError:
    KYBER_AVAILABLE = False
    print("Python: liboqs not available")

# File paths for shared data directory
ENCRYPTED_FILE = "/data/encrypted.bin"  # Hybrid encrypted file from .NET
DECRYPTED_FILE = "/data/decrypted-python.txt"  # Decrypted output
PUBLIC_KEY_FILE = "/data/kyber_public.key"
PRIVATE_KEY_FILE = "/data/kyber_private.key"
KYBER_CT_FILE = "/data/kyber_ciphertext.bin"

def derive_aes_key(shared_secret: bytes, salt: bytes = None, info: bytes = b"AES-256-GCM") -> bytes:
    """
    Derives a 32-byte AES-256 key from the Kyber shared secret using HKDF-SHA256.
    
    Args:
        shared_secret: The shared secret from Kyber decapsulation
        salt: Optional salt for key derivation
        info: Context information for key derivation
        
    Returns:
        32-byte AES-256 key
    """
    if salt is None:
        salt = b'\x00' * 32
    
    hkdf = HKDF(
        algorithm=hashes.SHA256(),
        length=32,
        salt=salt,
        info=info,
        backend=default_backend()
    )
    
    return hkdf.derive(shared_secret)

def decrypt_hybrid():
    """
    Decrypts a file that was encrypted using Kyber1024 + AES-256-GCM hybrid encryption.
    
    Reads the encrypted file created by the .NET service, decapsulates the Kyber
    ciphertext to recover the shared secret, derives the AES key, and decrypts
    the data, demonstrating cross-language post-quantum cryptographic compatibility.
    """
    # Load the Kyber private key (and public for potential constructor needs)
    with open(PRIVATE_KEY_FILE, "rb") as f:
        private_key = f.read()
    try:
        with open(PUBLIC_KEY_FILE, "rb") as f:
            public_key = f.read()
    except FileNotFoundError:
        public_key = None
    
    print(f"Python: Loaded private key ({len(private_key)} bytes)")
    
    # Read the hybrid encrypted file
    with open(ENCRYPTED_FILE, "rb") as f:
        full_encrypted = f.read()
    
    # Extract Kyber ciphertext length (first 4 bytes, little-endian)
    kyber_ct_len = struct.unpack('<I', full_encrypted[:4])[0]
    
    # Extract Kyber ciphertext and AES encrypted data
    kyber_ciphertext = full_encrypted[4:4 + kyber_ct_len]
    aes_encrypted = full_encrypted[4 + kyber_ct_len:]
    
    print(f"Python: Kyber ciphertext size: {len(kyber_ciphertext)} bytes")
    print(f"Python: AES encrypted size: {len(aes_encrypted)} bytes")
    
    # Decapsulate using liboqs (ciphertext + private key are BOTH required for recovering the shared secret).
    # Earlier bug: aes_key was only derived in the TypeError branch; if the first attempt succeeded
    # we never derived aes_key, causing an UnboundLocalError later. This is now fixed.
    if KYBER_AVAILABLE:
        try:
            # Attempt constructor with both secret_key and public_key (newer liboqs-python versions may allow this).
            try:
                with oqs.KeyEncapsulation("Kyber1024", secret_key=private_key, public_key=public_key) as kem:
                    shared_secret = kem.decap_secret(kyber_ciphertext)
            except TypeError:
                # Fallback: constructor only supports secret_key parameter.
                with oqs.KeyEncapsulation("Kyber1024", secret_key=private_key) as kem:
                    shared_secret = kem.decap_secret(kyber_ciphertext)
            print(f"Python: Shared secret recovered using liboqs ({len(shared_secret)} bytes)")
            # Always derive AES key here (unified path)
            aes_key = derive_aes_key(shared_secret)
            print("Python: AES key derived from Kyber shared secret")
        except Exception as e:
            print(f"Python: Kyber decapsulation failed: {e}")
            print("Python: Falling back to legacy key...")
            with open("/data/key.txt", "r") as f:
                legacy_key_b64 = f.read().strip()
                aes_key = base64.b64decode(legacy_key_b64)
            print("Python: Using legacy AES key")
    else:
        # Fall back to legacy key if liboqs not available
        try:
            with open("/data/key.txt", "r") as f:
                legacy_key_b64 = f.read().strip()
                aes_key = base64.b64decode(legacy_key_b64)
                print("Python: Using legacy AES key (liboqs not available)")
        except FileNotFoundError:
            print("Python: ERROR - Cannot decrypt without proper Kyber support or legacy key")
            return
    
    # Decrypt using AES-256-GCM
    aes = AESGCM(aes_key)
    
    # Extract nonce (first 12 bytes) and ciphertext+tag (remaining)
    nonce = aes_encrypted[:12]
    ct = aes_encrypted[12:]
    
    # Decrypt and verify authentication tag; if tag invalid, fallback to legacy key
    try:
        decrypted = aes.decrypt(nonce, ct, None)
    except Exception as e:
        from cryptography.exceptions import InvalidTag
        if isinstance(e, InvalidTag):
            print("Python: AES-GCM tag invalid – likely mismatched shared secret. Falling back to legacy key...")
            with open("/data/key.txt", "r") as f:
                legacy_key_b64 = f.read().strip()
                legacy_key = base64.b64decode(legacy_key_b64)
            aes = AESGCM(legacy_key)
            decrypted = aes.decrypt(nonce, ct, None)
        else:
            raise
    
    # Write decrypted plaintext to output file
    with open(DECRYPTED_FILE, "wb") as f:
        f.write(decrypted)
    
    print(f"Python: File decrypted successfully.")
    print(f"Python: Decrypted content: {decrypted.decode('utf-8')}")

def test_kyber_compatibility():
    """
    Tests Kyber1024 compatibility by generating keys, encapsulating, and decapsulating.
    Uses liboqs for Kyber1024 operations.
    """
    if not KYBER_AVAILABLE:
        print("\n=== Python: Kyber1024 library not available ===")
        print("Python: Install liboqs-python for full Kyber support: pip install liboqs-python")
        print("Python: Proceeding with AES decryption only...")
        return
    
    print("\n=== Python: Testing Kyber1024 Compatibility with liboqs ===")
    
    try:
        # Create KEM instance for Kyber1024
        with oqs.KeyEncapsulation("Kyber1024") as kem:
            # Generate keypair
            public_key = kem.generate_keypair()
            private_key = kem.export_secret_key()
            
            print(f"Python: Generated keypair - Public: {len(public_key)} bytes, Private: {len(private_key)} bytes")
            
            # Encapsulate
            ciphertext, shared_secret = kem.encap_secret(public_key)
            print(f"Python: Encapsulated - Ciphertext: {len(ciphertext)} bytes, Secret: {len(shared_secret)} bytes")
        
        # Decapsulate using a fresh instance initialized with secret key
        with oqs.KeyEncapsulation("Kyber1024", secret_key=private_key) as kem2:
            recovered_secret = kem2.decap_secret(ciphertext)
            print(f"Python: Decapsulated - Secret: {len(recovered_secret)} bytes")
            
            # Verify
            if shared_secret == recovered_secret:
                print("Python: ✓ Kyber1024 round-trip successful with liboqs!")
            else:
                print("Python: ✗ Kyber1024 round-trip failed!")
                
    except Exception as e:
        print(f"Python: Kyber test failed: {e}")

if __name__ == "__main__":
    # Test Kyber functionality
    test_kyber_compatibility()
    
    # Execute hybrid decryption
    print("\n=== Python: Starting Hybrid Decryption ===")
    decrypt_hybrid()
