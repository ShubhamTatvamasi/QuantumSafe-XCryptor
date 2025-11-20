"""
AES-256-GCM decryption service.
This module demonstrates file decryption using AES-256-GCM with a shared key,
decrypting files encrypted by the .NET service for cross-language compatibility testing.
"""

import base64
from cryptography.hazmat.primitives.ciphers.aead import AESGCM
import os

# File paths for shared data directory
KEY_FILE = "/data/key.txt"           # Base64-encoded 256-bit AES key
ENCRYPTED_FILE = "/data/encrypted.bin"  # Encrypted file from .NET service (nonce + ciphertext + tag)
DECRYPTED_FILE = "/data/decrypted-python.txt"  # Decrypted output for verification

def load_key():
    """
    Load and decode the AES-256 key from the shared key file.
    
    Returns:
        bytes: The decoded 32-byte AES key
    """
    with open(KEY_FILE, "r") as f:
        return base64.b64decode(f.read().strip())

def decrypt():
    """
    Decrypt the encrypted file using AES-256-GCM.
    
    Reads the encrypted file created by the .NET service and verifies authenticity
    during decryption, demonstrating cross-language cryptographic compatibility.
    """
    # Load the shared AES key
    key = load_key()
    aes = AESGCM(key)

    # Read the entire encrypted file
    raw = open(ENCRYPTED_FILE, "rb").read()
    
    # Extract nonce (first 12 bytes) and ciphertext+tag (remaining bytes)
    nonce = raw[:12]
    ct = raw[12:]

    # Decrypt and verify authentication tag
    decrypted = aes.decrypt(nonce, ct, None)
    
    # Write decrypted plaintext to output file
    open(DECRYPTED_FILE, "wb").write(decrypted)

    print("Python: File decrypted.")

if __name__ == "__main__":
    # Execute decryption to verify cross-language compatibility
    decrypt()
