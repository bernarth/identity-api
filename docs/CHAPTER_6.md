# Chapter 6

In this chapter we created the registration and login endpoints but they have some flaws that were introduced intentionally.

For example: 

`GenerateAccessToken` should ideally return the expiry along with the token, with one source of truth. We didn't do it because it would mean redesigning `ITokenService` interface and in future moves when the logic moves out of `AuthController` into an `AuthService` that returns `Result<AuthResponse>`. Every call site of `GenerateAccessToken` get rewritten in that refactor anyway.

## Questions

1. Why do we return `401 Unauthorized` for both "user not found" and "wrong password" instead of different error messages?

If "user not found" and "wrong password" return different responses, an attacker can submit random emails and learn which emails have accounts here without ever guessing a password. A uniform `401` makes the two cases indistinguishable, so a failed login teaches the attacker nothing.

2. What does `IdentityResult` represent and how do you check if an operation succeeded?

It is the response when the operation of creating a user is completed it could succeed or not. And we could know that by the `Succeeded` property.

3. Why do we hash the refresh token before storing it in the database?

The `RefreshTokens` table is a table of live credentials. If the database leaks (backup theft, SQL injection, insider), plaintext rows would let the attacker mint fresh access tokens for any user until expiry. Storing only the SHA-256 hash makes leaked rows useless because the attacker can't reverse the hash

4. What is user enumeration and why is it a security concern?

It is when trying to build a list of valid accounts by observing how its reponses differ between existing and non-existing users with: different error messages, status codes, or even reponse timing.

- `LoginAsync` handled: both user is null and bad password return the same empty `Unauthorized()`
- `RegisterAsync` not handled: Identity's `DuplicateEmail` error goes straight into 400 response, which tell the caller "this emails has an account". That's a real, commonly accepted trade-off (registration UX usually demands it), typically mitigated with rate limiting. Knowing this leak and why we accept it is great than just throwing 401 for everything.

5. What HTTP status code should registration return on success and why `201` instead of `200`?

Because the registration is something that was created on the database so, it means creation `201`
