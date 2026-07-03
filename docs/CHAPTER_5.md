# Chapter 5

In this chapter we added the `Tokens` feature. 
We generated for **dev environment** a jwt key with the following command:

```bash
openssl rand -base64 64
```

We mitgated a known high severity vulnerability by installing a fixed version of `OpenApi`

```bash
dotnet add Identity.Api package Microsoft.OpenApi --version 2.7.5
```

## Questions

1. Why do we use `RandomNumberGenerator` for the refresh token instead of `Guid.NewGuid()`?

- `Guid.NewGuid()`'s contract is uniqueness, not unpredictability. Nothing in its API promises cruptographic randomness - historically some GUID versions were dereived from MAC address + timestamp so, they are guessable.
- Even a fully random v4 GUID has at most 122 bits of entropy (6 bits are fixed by the format).
- `RandomNumberGenerator` is a CSPRNG, cryptographically secure, and 64 bytes gives you 512 bits of entropy. A refresh token is a bearer credential; its only defense is being unguessable, so you use the API whose contract is unpredictability.

2. What is a `ClaimsIdentity` and how does it relate to the JWT payload?

It generates the clamis that will go to the subject which defines who the token is about.

`ClaimsIdentity` it's a container: a collection of `Claim` objects (key-value pairs) representing one identity. You build the claims yourself, put them in the `ClaimsIdentity`, and assign it to `SecurityTokenDescriptor.Subject`. When the handler wirtes the token each claim in that identity becomes one key-value pair in the JWT payload. So the relationship is: `ClaimsIdentity` is the in-memory .NET representation

3. Why does the access token have a short expiry (15 minutes) while the refresh token has a long one (7 days)?

- The access token is **stateless** and cannot be revoked. Consumer APIs validate it with just the signature. No database lookup, no call back to `Identity.Api`. Once issued, there is nothing you can do to kill it. The only damage control is a short lifetime.
- The refresh token is **stateful** and fully revocable. It lives hashed in your database, it's checked on every use, it's rotated every use, and you can revoke it at any moment. Because you keep control over it, a long lifetime is safe

4. What is the `jti` claim and why is it useful?

The JWT ID, a unique id for this token, Useful for logging, revocation or blacklisting.

5. What is the difference between `DateTime.UtcNow` and `DateTime.Now` and why does it matter for JWTs?

Well the UTC uses a time that is not fixed to the location and it is important because the server could be in a different time zone and the user could have less or more time for the token expiration.

