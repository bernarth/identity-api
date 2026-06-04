# Chapter 2

A JSON Web Token (JWT) has three Base64url encoded parts separated by dots: `header.payload.signature`. The header declares the algorithm. The payload carries claims (key-value pairs about the user). The signature is a cryptographic proof that the token was issued by someone who knows the secret key. Understanding this structure is essential for any backend interview.

## Questions

1. What are the three parts of a JWT and what does each one contain?

the three parts are:

  - header: declares the algorithm. The header says metada like `alg:HS256`.

  - payload: are the claims or the user's information, it contains claims like `sub`, `email`, `role`, `exp`, `iss`, and `aud`; 

  - signature: cryptographic proof that the token was issued by someone who knows the secret key

2. If I change one character in the signature of a JWT, what happens when a consumer API tries to validate it?

The validations fails. The consumer API recalculates the expected signature using the shared secret, and it will not match the tampered signature.

3. What is the difference between `exp`, `iat`, and `nbf` claims?

`exp` means expiration time after which the token is invalid
`iat` means issued at, when the token was created
`nbf` means not before, the earliest time the token should be accepted

4. What does `HS256` mean and what are the two parties that need to share the secret key?

It means `HCMAC-SHA256` which is a symetric signing algorithm using one shared secret key. The auth API uses that key to sign tokens, and the consumer API uses the same key to validate them.

5. Why is a JWT considered stateless? What is the implication of that for logout?

Because the API can validate it using only the token contents and signing key, without looking it up in a database. The logout implication is that an already issued access token remains valid until it expires unless you add server-side revocation/blocklist.
