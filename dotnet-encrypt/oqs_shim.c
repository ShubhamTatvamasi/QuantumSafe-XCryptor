#include <stdint.h>
#include <stddef.h>
#include <oqs/oqs.h>

#ifdef _WIN32
#define EXPORT __declspec(dllexport)
#else
#define EXPORT __attribute__((visibility("default")))
#endif

// Returns 0 on success, non-zero on error
EXPORT int oqs_kyber1024_keypair(uint8_t *pk, size_t pk_len, uint8_t *sk, size_t sk_len) {
    OQS_KEM *kem = OQS_KEM_new(OQS_KEM_alg_kyber_1024);
    if (kem == NULL) return 1;
    if (pk_len < kem->length_public_key || sk_len < kem->length_secret_key) {
        OQS_KEM_free(kem);
        return 2;
    }
    OQS_STATUS rc = OQS_KEM_keypair(kem, pk, sk);
    OQS_KEM_free(kem);
    return rc == OQS_SUCCESS ? 0 : 3;
}

// Returns 0 on success, non-zero on error
EXPORT int oqs_kyber1024_encaps(const uint8_t *pk, size_t pk_len, uint8_t *ct, size_t ct_len, uint8_t *ss, size_t ss_len) {
    OQS_KEM *kem = OQS_KEM_new(OQS_KEM_alg_kyber_1024);
    if (kem == NULL) return 1;
    if (pk_len < kem->length_public_key || ct_len < kem->length_ciphertext || ss_len < kem->length_shared_secret) {
        OQS_KEM_free(kem);
        return 2;
    }
    OQS_STATUS rc = OQS_KEM_encaps(kem, ct, ss, pk);
    OQS_KEM_free(kem);
    return rc == OQS_SUCCESS ? 0 : 3;
}

// Returns 0 on success, non-zero on error
EXPORT int oqs_kyber1024_decaps(const uint8_t *ct, size_t ct_len, const uint8_t *sk, size_t sk_len, uint8_t *ss, size_t ss_len) {
    OQS_KEM *kem = OQS_KEM_new(OQS_KEM_alg_kyber_1024);
    if (kem == NULL) return 1;
    if (ct_len < kem->length_ciphertext || sk_len < kem->length_secret_key || ss_len < kem->length_shared_secret) {
        OQS_KEM_free(kem);
        return 2;
    }
    OQS_STATUS rc = OQS_KEM_decaps(kem, ss, ct, sk);
    OQS_KEM_free(kem);
    return rc == OQS_SUCCESS ? 0 : 3;
}
