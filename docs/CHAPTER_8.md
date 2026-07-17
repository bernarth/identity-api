# Chapter 8

In this chapter the roles and users management were added.

## Questions

1. Why do you seed roles at startup instead of creating them through and endpoint?

Because since we need and Admin to assign roles we need those roles to exist in first place so, they are needed at the beginning of the project when the database is empty and there is no information.
But the main reason is that a role only does anything if some API has `[Authorize(Roles="ThatRole")]` compiled into it. Role names are compile-time contracts bewteen services, and endpoint that creates roles at runtime inserts rows now code will ever check.

2. What is the difference between `401 Unauthorized` and `403 Forbidden`? When does each occur?

`401 Unauthorized` means the system does not know who you are
`403 Forbidden` means the system knowns who you are but you do not have permission to perform certain action

3. If an admin's role is changed in the database after they log in, will their existing JWT reflect the change? Why or why not?

Since after the token is issued we don't have control over it it won't reflect the change with the existing JWT. In theory the user could still use its token x min.
But, when trying to refresh since the role change revoke the refresh tokens, `/refresh` returns 401 and the user must log in again.

4. How does `[Authorize(Roles="Admin")]` know what roles the user has without querying the database?

Because the role is in the token and since the token is signed to prevent changes it is only required to check the JWT

5. What is the "chicken-and-egg" problem with the first admin user, and why is "see only when the users talb is empty" safer than "see if the admin account is missing"?

The chicken-and-egg problem is when you need A to create/use B and at the same time you would need B to create/use A so, you need one first.
The empty-check is a security property: with "see if the admin account is missing", an admin that is only to start the application would still exist and if an attacker gets those old credentials would be a security issue.

6. Login already returns 401 for a locked-out-user so, why must blocking also revoke their refresh tokens?

`lockout` is only enforced where a password is checked in `/login`. The `/refresh` endpoint never sees a password and never looks at `LockoutEnd` it only checks the refresh token itself. So a blocked user holding live refresh token could keep minting fresh access tokens for 7 days, fully "blocked" the whole time.
Revoking their refresh tokens is what actually cuts them off.

7. An interviewer asks: "should you hard-delete or disable user accounts"? Argue both sides, and say which API uses for which purpose.

Soft delete is better for retaining relational history and supporting account recovery
Hard delete is strictly necessary for strict data privacy compliance and freeing up database space. Moreover, when DELETE a user the refresh tokens are deleted on cascade.
