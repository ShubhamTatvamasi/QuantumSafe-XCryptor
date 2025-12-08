"""
ML-KEM-1024 Key Generation Service
Generates post-quantum keypair and writes to shared volume.
"""
import os
import oqs

# Paths in shared volume
DATA_DIR = "/data"
PUBLIC_KEY_PATH = os.path.join(DATA_DIR, "kyber_public.key")
PRIVATE_KEY_PATH = os.path.join(DATA_DIR, "kyber_private.key")

# ML-KEM-1024 algorithm name (liboqs uses "Kyber1024")
KEM_ALG = "Kyber1024"

def generate_keypair():
    """Generate ML-KEM-1024 keypair and save to disk."""
    print("=" * 60)
    print("🔑 ML-KEM-1024 Key Generation Service")
    print("=" * 60)
    
    print(f"\n📂 Output directory: {DATA_DIR}")
    print(f"🔧 KEM Algorithm: {KEM_ALG}")
    
    # Generate keypair
    print(f"\n⚙️  Generating ML-KEM-1024 keypair...")
    with oqs.KeyEncapsulation(KEM_ALG) as kem:
        public_key = kem.generate_keypair()
        private_key = kem.export_secret_key()
    
    print(f"✓ Public key generated: {len(public_key)} bytes")
    print(f"✓ Private key generated: {len(private_key)} bytes")
    
    # Write public key
    with open(PUBLIC_KEY_PATH, "wb") as f:
        f.write(public_key)
    print(f"✓ Public key written to: {PUBLIC_KEY_PATH}")
    
    # Write private key
    with open(PRIVATE_KEY_PATH, "wb") as f:
        f.write(private_key)
    print(f"✓ Private key written to: {PRIVATE_KEY_PATH}")
    
    print("\n" + "=" * 60)
    print("✅ Key generation complete!")
    print("=" * 60)

if __name__ == "__main__":
    try:
        generate_keypair()
    except Exception as e:
        print(f"\n❌ Key generation failed: {e}")
        import traceback
        traceback.print_exc()
        exit(1)
