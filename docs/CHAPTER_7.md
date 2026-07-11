# Chapter 7

In this chapter we added the `AddAuthentication(...)` so, we can first prevent the redirection to login page and instead return 401 and more important we enable this project to read JWT tokens

Also, we added a new migration and run:

```bash
dotnet ef migrations add UniqueRefreshTokenIndex
dotnet ef database update
```

## Questions

1. What is refresh token rotation and what attack does it protect against ?

Rotation mean every refresh token is single-use: when it's presented at `/refresh`, it is immediatly revoked and a brand-new refresh token is issued alongside the new access token. It protects against refresh token theft/replay. Without rotation, a stolen refresh token lets an attacker mint new access tokens for its entire lifetime (days). With rotation, the stolen copy works at most once - and if it's used after legitimate client already rotated it, the replay hits and already-revoked row, which is detectable (reuse detection) instead of silent.

2. What is the `ReplacedByToken` field for? Could you implement rotation without it ?

The `ReplaceByToken` it is to audit and know which token was used to rotate and issue a new one. We can implement the rotation without it.

3. If a user logs in on three devices, how many refresh tokens will be in the database ? What happens when they revoke on one device?

We could have three refresh tokens. And if one is revoked we could still have three but one is revoked already

4. Why does the revoke endpoint require a valid access token (`[Authorize]`) but the refresh endpoint does not?

`/refresh` is called at exactly the moment the client has no valid access token requiring one would be circular. There, the refresh token iteself, sent in the body, is the credential being verified.
`/revoke` is the opposite situation: a deliberate log me out from a live session, so the client can present a valid access token and must, so the server knows who is asking and can check the refresh token being revoked belongs to that user. Otherwise anyonw who obtained some refresh token could revoke it against another user's account without proving any identity.

5. What does `.Include(rt => rt.User)` do in EF Core and why do we need it here?

We need the user information to issue a new token. EF generates a SQL JOIN instead of leaving `rt.User` unpopulated.
