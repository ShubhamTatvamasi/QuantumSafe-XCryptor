"""
QuantumSafe-XCryptor - Python Server (Cloud)

Post-quantum hybrid encryption server using ML-KEM-1024 + AES-256-GCM.

Architecture:
- Server generates and maintains ML-KEM-1024 keypair
- Clients download public key for encryption
- Clients send Kyber ciphertext + AES-encrypted file
- Server decrypts using private key and HKDF-derived AES key

Key Design Principles:
1. Server is trusted authority (holds private key)
2. Clients are untrusted (only have public key)
3. Forward secrecy: Each client gets unique Kyber ciphertext
4. Zero-knowledge upload: Server never sees plaintext during transmission
5. Identical HKDF parameters ensure cross-platform AES key derivation

API Endpoints:
- GET /api/kyber/public-key - Get server's ML-KEM public key
- POST /api/files/upload - Upload encrypted file (ct + encrypted data)
- POST /api/files/decrypt - Decrypt and return plaintext (for testing)

HKDF Parameters (MUST match all clients):
- Salt: 32 zero bytes
- Info: "AES-256-GCM"
- Hash: SHA-256
- Output: 32 bytes (AES-256 key)
"""

import os
import base64
import json
import warnings
from flask import Flask, request, jsonify, send_file
from cryptography.hazmat.primitives.ciphers.aead import AESGCM
from cryptography.hazmat.primitives.kdf.hkdf import HKDF
from cryptography.hazmat.primitives import hashes
from cryptography.hazmat.backends import default_backend
import io

try:
    warnings.filterwarnings(
        "ignore",
        message=r"liboqs version \(major, minor\) .* differs from liboqs-python version .*",
        category=UserWarning,
        module="oqs"
    )
    import oqs
    KYBER_AVAILABLE = True
    print(f"✓ liboqs version: {oqs.oqs_version()}")
except ImportError:
    KYBER_AVAILABLE = False
    print("✗ liboqs not available")

app = Flask(__name__)

# ====================
# Global Configuration
# ====================

KYBER_PUBLIC_KEY_SIZE = 1568
KYBER_PRIVATE_KEY_SIZE = 3168
KYBER_CIPHERTEXT_SIZE = 1568
KYBER_SHARED_SECRET_SIZE = 32

# HKDF Parameters (MUST be identical across all platforms)
HKDF_SALT = b'\x00' * 32
HKDF_INFO = b"AES-256-GCM"
HKDF_OUTPUT_LENGTH = 32

# Storage directory
DATA_DIR = "/data"
os.makedirs(DATA_DIR, exist_ok=True)

PUBLIC_KEY_PATH = os.path.join(DATA_DIR, "kyber_public.key")
PRIVATE_KEY_PATH = os.path.join(DATA_DIR, "kyber_private.key")
UPLOADED_FILES_DIR = os.path.join(DATA_DIR, "uploads")
os.makedirs(UPLOADED_FILES_DIR, exist_ok=True)

# Global ML-KEM keypair (loaded at startup)
kyber_private_key = None
kyber_public_key = None

# ====================
# Key Management
# ====================

def generate_kyber_keypair():
    """Generate ML-KEM-1024 keypair (server-side)"""
    print("🔑 Generating ML-KEM-1024 keypair...")
    
    with oqs.KeyEncapsulation("ML-KEM-1024") as kem:
        public_key = kem.generate_keypair()
        private_key = kem.export_secret_key()
    
    # Save to files
    with open(PUBLIC_KEY_PATH, "wb") as f:
        f.write(public_key)
    with open(PRIVATE_KEY_PATH, "wb") as f:
        f.write(private_key)
    
    print(f"✓ Generated keypair: pk={len(public_key)} bytes, sk={len(private_key)} bytes")
    return public_key, private_key

def load_kyber_keypair():
    """Load ML-KEM-1024 keypair from disk, or generate if not exists"""
    global kyber_private_key, kyber_public_key
    
    if os.path.exists(PUBLIC_KEY_PATH) and os.path.exists(PRIVATE_KEY_PATH):
        with open(PUBLIC_KEY_PATH, "rb") as f:
            kyber_public_key = f.read()
        with open(PRIVATE_KEY_PATH, "rb") as f:
            kyber_private_key = f.read()
        print(f"✓ Loaded keypair from disk")
    else:
        kyber_public_key, kyber_private_key = generate_kyber_keypair()
    
    return kyber_public_key, kyber_private_key

# ====================
# Cryptographic Functions
# ====================

def derive_aes_key(shared_secret: bytes) -> bytes:
    """
    Derive AES-256 key from ML-KEM shared secret using HKDF-SHA256.
    
    CRITICAL: Must be IDENTICAL on all platforms (server, .NET, React Native)
    
    Parameters:
    - Salt: 32 zero bytes
    - Info: "AES-256-GCM"
    - Hash: SHA-256
    - Output: 32 bytes
    """
    if len(shared_secret) != 32:
        raise ValueError(f"Shared secret must be 32 bytes, got {len(shared_secret)}")
    
    hkdf = HKDF(
        algorithm=hashes.SHA256(),
        length=HKDF_OUTPUT_LENGTH,
        salt=HKDF_SALT,
        info=HKDF_INFO,
        backend=default_backend()
    )
    
    return hkdf.derive(shared_secret)

def decrypt_aes_gcm(aes_key: bytes, encrypted_data: bytes) -> bytes:
    """
    Decrypt AES-256-GCM encrypted data.
    
    Format: [nonce:12][ciphertext][tag:16]
    """
    if len(aes_key) != 32:
        raise ValueError(f"AES key must be 32 bytes, got {len(aes_key)}")
    if len(encrypted_data) < 12 + 16:
        raise ValueError(f"Encrypted data too short, got {len(encrypted_data)} bytes")
    
    nonce = encrypted_data[:12]
    ciphertext = encrypted_data[12:-16]
    tag = encrypted_data[-16:]
    
    aesgcm = AESGCM(aes_key)
    
    try:
        plaintext = aesgcm.decrypt(nonce, ciphertext + tag, None)
        return plaintext
    except Exception as e:
        raise ValueError(f"AES-GCM decryption failed: {e}")

# ====================
# Flask Routes
# ====================

@app.route("/api/kyber/public-key", methods=["GET"])
def get_public_key():
    """
    Endpoint: Get server's ML-KEM-1024 public key
    
    Response:
    - Content-Type: application/octet-stream
    - Body: 1568-byte public key (binary)
    """
    return app.response_class(
        response=kyber_public_key,
        status=200,
        mimetype="application/octet-stream"
    )

@app.route("/api/files/upload", methods=["POST"])
def upload_encrypted_file():
    """
    Endpoint: Upload encrypted file
    
    Expected format:
    [Kyber ciphertext: 1568 bytes][AES encrypted data: variable]
    
    Response:
    - 200: File uploaded and stored
    - 400: Invalid format
    - 500: Server error
    """
    try:
        encrypted_packet = request.get_data()
        
        if len(encrypted_packet) < KYBER_CIPHERTEXT_SIZE + 12 + 16:
            return jsonify({"error": "Packet too short"}), 400
        
        # Extract Kyber ciphertext
        kyber_ciphertext = encrypted_packet[:KYBER_CIPHERTEXT_SIZE]
        aes_encrypted = encrypted_packet[KYBER_CIPHERTEXT_SIZE:]
        
        # Validate ciphertext size
        if len(kyber_ciphertext) != KYBER_CIPHERTEXT_SIZE:
            return jsonify({"error": f"Invalid ciphertext size: {len(kyber_ciphertext)}"}), 400
        
        # Decapsulate to recover shared secret
        print(f"📦 Decapsulating ML-KEM ciphertext ({len(kyber_ciphertext)} bytes)...")
        
        with oqs.KeyEncapsulation("ML-KEM-1024", secret_key=kyber_private_key) as kem:
            shared_secret = kem.decap_secret(kyber_ciphertext)
        
        print(f"✓ Shared secret recovered ({len(shared_secret)} bytes)")
        
        # Derive AES key (IDENTICAL to client)
        aes_key = derive_aes_key(shared_secret)
        print(f"✓ AES key derived (HKDF: salt=32x0, info='AES-256-GCM')")
        
        # Decrypt AES-GCM data
        plaintext = decrypt_aes_gcm(aes_key, aes_encrypted)
        
        # Store uploaded file
        filename = f"upload_{len(os.listdir(UPLOADED_FILES_DIR))}.bin"
        filepath = os.path.join(UPLOADED_FILES_DIR, filename)
        with open(filepath, "wb") as f:
            f.write(plaintext)
        
        print(f"✓ File decrypted and stored: {filename}")
        
        return jsonify({
            "status": "success",
            "filename": filename,
            "size": len(plaintext)
        }), 200
        
    except Exception as e:
        print(f"✗ Error processing upload: {e}")
        return jsonify({"error": str(e)}), 500

@app.route("/api/files/decrypt", methods=["POST"])
def decrypt_file():
    """
    Endpoint: Decrypt file (for testing/API access)
    
    Request body: [Kyber ciphertext: 1568 bytes][AES encrypted data: variable]
    
    Response:
    - 200: Decrypted plaintext (Content-Type: text/plain or application/octet-stream)
    - 400: Invalid format
    - 500: Decryption failed
    """
    try:
        encrypted_packet = request.get_data()
        
        if len(encrypted_packet) < KYBER_CIPHERTEXT_SIZE + 12 + 16:
            return jsonify({"error": "Packet too short"}), 400
        
        # Extract components
        kyber_ciphertext = encrypted_packet[:KYBER_CIPHERTEXT_SIZE]
        aes_encrypted = encrypted_packet[KYBER_CIPHERTEXT_SIZE:]
        
        # Decapsulate
        print(f"🔓 Decrypting ML-KEM ciphertext...")
        
        with oqs.KeyEncapsulation("ML-KEM-1024", secret_key=kyber_private_key) as kem:
            shared_secret = kem.decap_secret(kyber_ciphertext)
        
        # Derive AES key
        aes_key = derive_aes_key(shared_secret)
        
        # Decrypt
        plaintext = decrypt_aes_gcm(aes_key, aes_encrypted)
        
        print(f"✓ Decryption successful ({len(plaintext)} bytes)")
        
        # Return plaintext
        return app.response_class(
            response=plaintext,
            status=200,
            mimetype="application/octet-stream"
        )
        
    except Exception as e:
        print(f"✗ Decryption failed: {e}")
        return jsonify({"error": str(e)}), 500

@app.route("/api/health", methods=["GET"])
def health():
    """Health check endpoint"""
    return jsonify({
        "status": "ok",
        "kyber": "available" if KYBER_AVAILABLE else "unavailable",
        "public_key_size": len(kyber_public_key),
        "private_key_size": len(kyber_private_key)
    }), 200

# ====================
# Startup
# ====================

def test_kyber_roundtrip():
    """Test ML-KEM-1024 round-trip (for validation)"""
    print("\n🧪 Testing ML-KEM-1024 round-trip...")
    
    try:
        with oqs.KeyEncapsulation("ML-KEM-1024") as kem:
            pk = kem.generate_keypair()
            sk = kem.export_secret_key()
        
        with oqs.KeyEncapsulation("ML-KEM-1024") as kem:
            ct, ss1 = kem.encap_secret(pk)
        
        with oqs.KeyEncapsulation("ML-KEM-1024", secret_key=sk) as kem:
            ss2 = kem.decap_secret(ct)
        
        if ss1 == ss2:
            print("✓ ML-KEM-1024 round-trip successful")
        else:
            print("✗ ML-KEM-1024 round-trip failed (secrets don't match)")
    except Exception as e:
        print(f"✗ ML-KEM-1024 test failed: {e}")

if __name__ == "__main__":
    if not KYBER_AVAILABLE:
        print("✗ ERROR: liboqs not available. Install with: pip install liboqs-python")
        exit(1)
    
    print("\n" + "="*60)
    print("  QuantumSafe-XCryptor - Python Server")
    print("="*60 + "\n")
    
    # Load or generate keypair
    load_kyber_keypair()
    
    # Test Kyber
    test_kyber_roundtrip()
    
    print("\n" + "="*60)
    print("  Starting Flask Server on 0.0.0.0:5000")
    print("="*60 + "\n")
    
    # Start Flask app
    app.run(host="0.0.0.0", port=5000, debug=False)


