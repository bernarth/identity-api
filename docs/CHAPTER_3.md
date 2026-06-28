# Chapter 3

## Questions

1. What is the difference between `DbContext` and `IdentityDbContext` ? Why do we extend `IdentityDbContext` ?

- `DbContext` is the base EF Core class. It manages connections, change tracking, querying, it is esentially the ORM.
- `IdentityDbContext` extends DbContext and add pre-built `DbSets` for all identity tables and their EF configurations
- We extend `IdentityDbContext` because we want Identity to manage those tables for us. If we used plain `DbContext`, none of the Identity tables would be created and `AddIdentity` at the `Program` would be useless.

> Note: `ASP.NET Core Identity` provides a secure, fully-featured, and customizable user management system out of the box. Instead of writting complex security code from scratch, this combination handles database storage, password hashing, and session management automatically.

2. What tables does ASP.NET Core Identity create automatically, and what is each one for ?

| Table                | Purpose                                         |
|----------------------|-------------------------------------------------|
| AspNetUsers          | User accounts (email, password hash, etc.)      |
| AspNetRoles          | Role definitions (Admin, User)                  |
| AspNetUserRoles      | Join table, which users have which roles        |
| AspNetUserClaims     | Extra claims attached directly to a user        |
| AspNetUserLogins     | External login providers (Google, Facebook)     |
| AspNetUserTokens     | Tokens issued by Identity (e.g. password reset) |
| AspNetRoleClaims     | Claims attached to a role                       |

3. What does `UserManager<ApplicationUser>` give you ? Name three methods you expect it to have.

> Note: We never call `new UserManager(...)` it comes from DI. You can see it in `Program` at the `AddIdentity(...).AddIdentityFrameworkStore<ApplicationDbContext>()`

`UserManager` is the high-level service Identity provides for user operations. Three key methods:

- `CreateAsync(user, password)`: creates a user with a hashed password
- `FindByEmailAsync(email)`: looks up a user by email
- `CheckPasswordAsync(user, password)`: verifies a password against the stored hash

4. Why do we call `base.OnModelCreating(builder)` in our override? What happens if we forget it ?

`IdentityDbContext` overrides `OnModelCreating` to configure all its own tables (primary keys, indexes, max lengths on every Identity column). If we don't call `base.OnModelCreating(builder)`, that configuration never runs therefore, Identity tables will eigher be created wrong or the migrations will fail because expected columns are missing.

5. What is PBKDF2 and why does Identity use it instead of something like MD5 ?

PBKDF2 = Password-Based Key Derivation Function 2: It takes a password, a random salt, and iteratively hashes them many times (Identity uses 100000+ iterations by default) to produce a derived key

MD5: It is fast, general purpose hash, Fas is bad for passwords because attackers can try billions of guesses per second. MD5 is cryptographically broken collisions are practical. An attacker with a GPU can compute billions of MD5 hashes per second and try to find the password by brute-force.
