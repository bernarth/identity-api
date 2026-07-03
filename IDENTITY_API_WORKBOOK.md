# Identity.Api — Learning Workbook

Work through one chapter per session. Do not move to the next chapter until you can answer the quiz questions out loud without looking at the code.

**How to use this file:**

1. Read the concept section.
2. Complete the task using the listed tools/files as your starting point.
3. Use the nudges only if you are stuck for more than 10 minutes.
4. When done, paste the quiz questions into Claude one at a time and answer them yourself before Claude responds.

---

## Chapter 1 — Project Scaffolding & Solution Structure

### Concept

A .NET Web API project is scaffolded from a template and then extended with NuGet packages. Understanding what each package is responsible for before you add it prevents confusion later — you should be able to say in one sentence what each dependency does.

### Your Task

**Files you will create:** the solution folder, the `.csproj` file (via CLI), `appsettings.json`, `appsettings.Development.json`, `.gitignore`.

1. Run `dotnet new webapi -n Identity.Api` to scaffold the project.
2. Add the following NuGet packages one by one using `dotnet add package`. After adding each one, document the purpose of each one:
  - `Microsoft.AspNetCore.Identity.EntityFrameworkCore` (version 10.*)
  - `Microsoft.EntityFrameworkCore.Design` (version 10.*)
  - `Npgsql.EntityFrameworkCore.PostgreSQL` (version 10.*)
  - `Microsoft.AspNetCore.Authentication.JwtBearer` (version 10.*)
  - `Microsoft.IdentityModel.Tokens` (version 8.*)
3. Create `appsettings.Development.json` with the `Jwt` and `ConnectionStrings` sections from the plan. Add this file to `.gitignore`.
4. Review the auto-generated `Program.cs` — identify what `builder` is, what `app` is, and what the difference between them is.

### Nudges

- `dotnet add package <name> --version "10.*"` is the CLI syntax for adding a versioned package.
- `appsettings.Development.json` is loaded automatically by .NET when `ASPNETCORE_ENVIRONMENT=Development`. It overrides values from `appsettings.json`.
- The auto-generated `Program.cs` uses the minimal API style. You will keep this style and add to it.

### Quiz — paste these into Claude one at a time

1. What is the difference between `builder.Services` and `app` in `Program.cs`? When do you use each?
2. Why is `appsettings.Development.json` in `.gitignore` but `appsettings.json` is not?
3. I added five NuGet packages. Can you explain what each one is responsible for in one sentence each?
4. What does `dotnet new webapi` give you out of the box, and what does it not give you?

---

## Chapter 2 — JWT Anatomy

### Concept

A JSON Web Token (JWT) has three Base64url-encoded parts separated by dots: `header.payload.signature`. The header declares the algorithm. The payload carries claims (key-value pairs about the user). The signature is a cryptographic proof that the token was issued by someone who knows the secret key. Understanding this structure is essential for any backend interview.

### Your Task

**Files you will create:** none permanent — this is a scratch exercise you can delete after.

1. Go to [jwt.io](https://jwt.io) and decode an example token. Identify the `alg`, `sub`, `exp`, `iss`, and `aud` fields.
2. In a temporary `.cs` scratch file, manually construct a JWT using `JwtSecurityTokenHandler` and `SecurityTokenDescriptor`. Include these claims: `sub`, `email`, `role`, `jti`, `iss`, `aud`, `exp`.
3. Print the raw token string and paste it back into jwt.io. Verify the claims appear correctly.
4. Change the key by one character and try to validate the token. Observe what happens.
5. Delete the scratch file when done.

**Namespace you will need:** `System.IdentityModel.Tokens.Jwt`, `Microsoft.IdentityModel.Tokens`, `System.Security.Claims`

**Classes you will use:** `JwtSecurityTokenHandler`, `SecurityTokenDescriptor`, `SymmetricSecurityKey`, `SigningCredentials`, `ClaimsIdentity`, `Claim`

### Nudges

- `SymmetricSecurityKey` takes a `byte[]`. Use `Encoding.UTF8.GetBytes(yourKey)`.
- `SigningCredentials` takes a key and an algorithm. The algorithm constant is `SecurityAlgorithms.HmacSha256`.
- `SecurityTokenDescriptor` has a `Subject` property (type `ClaimsIdentity`) where you add your claims.
- `JwtSecurityTokenHandler.WriteToken(token)` returns the string you can decode on jwt.io.

### Quiz — paste these into Claude one at a time

1. What are the three parts of a JWT and what does each one contain?
2. If I change one character in the signature of a JWT, what happens when a consumer API tries to validate it?
3. What is the difference between `exp`, `iat`, and `nbf` claims?
4. What does `HS256` mean and what are the two parties that need to share the secret key?
5. Why is a JWT considered stateless? What is the implication of that for logout?

---

## Chapter 3 — ASP.NET Core Identity Setup

### Concept

ASP.NET Core Identity is a membership system built into .NET. It manages users, passwords (hashed with PBKDF2), roles, and claims. It does not know about JWT — it is purely about user storage and validation. You layer JWT on top of it. Identity creates its own database tables (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, etc.) through EF Core migrations.

### Your Task

**Files you will create:** `Domain/ApplicationUser.cs`, `Domain/RefreshToken.cs`, `Infrastructure/Data/ApplicationDbContext.cs`.

1. Create `ApplicationUser.cs` in the `Domain/` folder. It must extend `IdentityUser` and add `FirstName`, `LastName`, `CreatedAt`, and a navigation property to `RefreshToken`.
2. Create `RefreshToken.cs` in the `Domain/` folder with all fields from the plan. Pay attention to which fields are nullable and why.
3. Create `ApplicationDbContext.cs` in the `Infrastructure/Data/` folder. It must extend `IdentityDbContext<ApplicationUser>` (not plain `DbContext`). Add the `RefreshTokens` DbSet and configure the relationships in `OnModelCreating`.
4. In `Program.cs`, register Identity and EF Core:
  - `builder.Services.AddDbContext<ApplicationDbContext>(...)` — connection string from config
  - `builder.Services.AddIdentity<ApplicationUser, IdentityRole>(...)` — chain `.AddEntityFrameworkStores<ApplicationDbContext>()`

**Namespaces you will need:** `Microsoft.AspNetCore.Identity`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore`

**Classes you will use:** `IdentityUser`, `IdentityRole`, `IdentityDbContext<TUser>`, `UserManager<TUser>`, `RoleManager<TRole>` (you won't implement these — Identity provides them — but you'll use them via DI)

### Nudges

- `IdentityDbContext<ApplicationUser>` already has a `DbSet<ApplicationUser>` (called `Users`). You only need to add `DbSet<RefreshToken>`.
- `OnModelCreating` must call `base.OnModelCreating(builder)` as its first line — without this, Identity tables won't be configured correctly.
- `AddIdentity` vs `AddIdentityCore`: use `AddIdentity` here — it registers more services including role management.
- The connection string format for Npgsql: `"Host=localhost;Port=5432;Database=identity_db;Username=identity_user;Password=identity_pass"`

### Quiz — paste these into Claude one at a time

1. What is the difference between `DbContext` and `IdentityDbContext`? Why do we extend `IdentityDbContext`?
2. What tables does ASP.NET Core Identity create automatically, and what is each one for?
3. What does `UserManager<ApplicationUser>` give you? Name three methods you expect it to have.
4. Why do we call `base.OnModelCreating(builder)` in our override? What happens if we forget it?
5. What is PBKDF2 and why does Identity use it instead of something like MD5?

---

## Chapter 4 — EF Core Migrations & Database

### Concept

EF Core's code-first approach means you define your schema in C# classes and EF generates SQL migrations from them. Migrations are versioned files that describe how to evolve the database schema over time. Running `dotnet ef database update` applies pending migrations to the actual database.

### Your Task

**Files you will create:** the migration files (auto-generated), no manual files.

1. Make sure your PostgreSQL container is running (`docker compose up -d --build`).
2. Run `dotnet ef migrations add InitialCreate`. Inspect the generated migration file — read both `Up()` and `Down()` methods.
3. Run `dotnet ef database update`. Connect to the database with a tool (pgAdmin, TablePlus, or `psql`) and verify the tables were created.
4. Identify the `AspNetUsers` table and find where `FirstName` and `LastName` appear — confirming your custom properties were included.
5. Check that an index exists on `RefreshTokens.Token` — this is what you configured in `OnModelCreating`.

**CLI tools you will use:** `dotnet ef migrations add`, `dotnet ef database update`, `dotnet ef migrations list`

**Namespaces involved (already used in previous chapter):** `Microsoft.EntityFrameworkCore`

### Nudges

- If `dotnet ef` is not found, install the tool globally: `dotnet tool install --global dotnet-ef`.
- The migration is generated based on what EF detects has changed versus the last migration. If your models are not registered in `DbContext`, they won't appear.
- The `Down()` method in a migration is the rollback — it undoes exactly what `Up()` did.
- To connect with `psql` from the terminal: `psql -h localhost -p 5432 -U identity_user -d identity_db`

### Quiz — paste these into Claude one at a time

1. What is the purpose of a database migration? What problem does it solve compared to writing SQL by hand?
2. What does EF Core's `Up()` method do, and what does `Down()` do?
3. If two developers on the same team each add a migration independently, what problem can occur?
4. Why did we add an index on `RefreshTokens.Token`? What would happen at scale without it?
5. What is the difference between `dotnet ef database update` and `dotnet ef migrations add`?

---

## Chapter 5 — Token Service (JWT Generation)

### Concept

The token service is the heart of this project. It takes a user object and produces a signed JWT. It also generates cryptographically random refresh tokens. The access token is signed with HMAC-SHA256 using the shared key. The refresh token is a random byte array encoded as Base64url — it has no internal structure, it is just an unguessable string.

### Your Task

**Files you will create:** `Features/Tokens/ITokenService.cs`, `Features/Tokens/TokenService.cs`, `Features/Tokens/JwtOptions.cs`. (If you started in `Services/`, move the files and update the namespace — the decided layout is feature-organized, ARCHITECTURE #16.)

1. Create `ITokenService.cs` with two method signatures:
  - `string GenerateAccessToken(ApplicationUser user, IEnumerable<string> roles)`
  - `string GenerateRefreshToken()` — returns the raw (unhashed) token string
2. Create a `JwtOptions` class (`Key`, `Issuer`, `Audience`, `AccessTokenExpiryMinutes`, `RefreshTokenExpiryDays`), bind it once in `Program.cs` with `builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"))`, and inject `IOptions<JwtOptions>` into `TokenService` — ARCHITECTURE decision #3: no raw `_config["Jwt:..."]` lookups.
3. In `GenerateAccessToken`: build a `ClaimsIdentity` with `sub`, `email`, `given_name`, `family_name`, `jti`, and one `role` claim per role. Set `Issuer` and `Audience` on the `SecurityTokenDescriptor` from your options — the audience is **server-fixed** (ARCHITECTURE #10): clients never choose it. Use `SecurityTokenDescriptor` and `JwtSecurityTokenHandler` to produce the token string.
4. In `GenerateRefreshToken`: use `RandomNumberGenerator.GetBytes(64)` to generate 64 random bytes, then encode them with `Convert.ToBase64String`.
5. Register `ITokenService` / `TokenService` in `Program.cs` using `builder.Services.AddScoped`.

**Namespaces you will need:** `System.IdentityModel.Tokens.Jwt`, `Microsoft.IdentityModel.Tokens`, `System.Security.Claims`, `System.Security.Cryptography`

**Classes you will use:** `JwtSecurityTokenHandler`, `SecurityTokenDescriptor`, `SymmetricSecurityKey`, `SigningCredentials`, `ClaimsIdentity`, `Claim`, `RandomNumberGenerator`, `IOptions<JwtOptions>`

### Nudges

- Use `JwtRegisteredClaimNames.Sub` (the literal string `"sub"`) for the subject claim — **not** `ClaimTypes.NameIdentifier`. `JwtSecurityTokenHandler` has a default *outbound claim type map* that silently rewrites `ClaimTypes.NameIdentifier` to `nameid`, so the token would end up with no `sub` claim at all. The same applies to `email`, `given_name`, `family_name`, and `jti`: the `JwtRegisteredClaimNames` constants pass through unmapped.
- `ClaimTypes.Role` is the constant for role claims. Add one `Claim` per role in the list. (The outbound map rewrites it to the short name `role`, which is exactly what you want.)
- The `roles` parameter is `IList<string>` only to match what `UserManager.GetRolesAsync` returns. Since you just iterate it once, `IEnumerable<string>` is an equally valid (arguably better) signature — accept the weakest interface you actually need.
- `SecurityTokenDescriptor.Expires` takes a `DateTime` — use `DateTime.UtcNow.AddMinutes(...)`.
- With the Options pattern, expiry is just `_jwt.AccessTokenExpiryMinutes` (capture `IOptions<JwtOptions>.Value` once in the constructor) — the binding happened at startup.
- `RandomNumberGenerator.GetBytes(64)` is the modern API (no need to instantiate the class).

### Quiz — paste these into Claude one at a time

1. Why do we use `RandomNumberGenerator` for the refresh token instead of `Guid.NewGuid()`?
2. What is a `ClaimsIdentity` and how does it relate to the JWT payload?
3. Why does the access token have a short expiry (15 minutes) while the refresh token has a long one (7 days)?
4. What is the `jti` claim and why is it useful?
5. What is the difference between `DateTime.UtcNow` and `DateTime.Now` and why does it matter for JWTs?

---

## Chapter 6 — Registration & Login Endpoints

### Concept

Registration creates a new user in the Identity store. Login verifies credentials and, on success, issues a token pair. The access token is returned in the response body. The refresh token is also returned in the body (some implementations use HTTP-only cookies — both are valid patterns with different trade-offs).

### Your Task

**Files you will create:** `Features/Auth/Dtos/RegisterRequest.cs`, `Features/Auth/Dtos/LoginRequest.cs`, `Features/Auth/Dtos/AuthResponse.cs`, `Features/Auth/AuthController.cs`.

1. Create the three DTO classes. `AuthResponse` must contain `AccessToken`, `RefreshToken`, `AccessTokenExpiresAt`, and `TokenType` ("Bearer").
2. Create `AuthController` inheriting from `ControllerBase`. Decorate with `[ApiController]` and `[Route("api/auth")]`. Inject `UserManager<ApplicationUser>`, `SignInManager<ApplicationUser>`, `ITokenService`, and `ApplicationDbContext`.
3. Implement `POST /register`:
  - Validate the request (Identity does password validation for you).
  - Create the user with `UserManager.CreateAsync(user, password)`.
  - Assign the `User` role with `UserManager.AddToRoleAsync`.
  - Return `201 Created`.
4. Implement `POST /login`:
  - Find user by email with `UserManager.FindByEmailAsync`.
  - Verify the password with `SignInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true)` — **not** `UserManager.CheckPasswordAsync`, which silently bypasses the lockout counter you configured in `Program.cs` (ARCHITECTURE #12). A locked-out account gets the same uniform `401` — never reveal lockout state.
  - Get roles with `UserManager.GetRolesAsync`.
  - Generate access token and refresh token via `ITokenService`.
  - Hash the refresh token (SHA-256) and save it to `RefreshTokens` table.
  - Return `200 OK` with `AuthResponse`.

**Namespaces you will need:** `Microsoft.AspNetCore.Mvc`, `Microsoft.AspNetCore.Identity`, `System.Security.Cryptography`

**Classes you will use:** `UserManager<ApplicationUser>`, `SignInManager<ApplicationUser>`, `ControllerBase`, `ITokenService`

**For hashing the refresh token before saving:** `Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)))`

### Nudges

- `UserManager.CreateAsync` returns an `IdentityResult`. Check `.Succeeded` before continuing — if false, return `400 BadRequest` with `result.Errors`.
- `UserManager.FindByEmailAsync` returns `null` if not found. Return `401 Unauthorized` for both "user not found" and "wrong password" — do not distinguish between them (prevents user enumeration).
- Set `RefreshToken.CreatedAt = DateTime.UtcNow` and `ExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenExpiryDays)` (from `JwtOptions`) before saving.
- `await _context.SaveChangesAsync()` is required after adding to `_context.RefreshTokens`.

### Quiz — paste these into Claude one at a time

1. Why do we return `401 Unauthorized` for both "user not found" and "wrong password" instead of different error messages?
2. What does `IdentityResult` represent and how do you check if an operation succeeded?
3. Why do we hash the refresh token before storing it in the database?
4. What is user enumeration and why is it a security concern?
5. What HTTP status code should registration return on success and why `201` instead of `200`?

---

## Chapter 7 — Refresh & Revoke Endpoints

### Concept

Token rotation means every time a refresh token is used, it is immediately invalidated and a new one is issued. This limits the damage of a stolen refresh token — once the legitimate client rotates it, the stolen copy is useless. Revocation (logout) explicitly marks a token as revoked so it cannot be used even before expiry.

### Your Task

**Files you will create:** `DTOs/RefreshTokenRequest.cs`, `DTOs/RevokeTokenRequest.cs` (add endpoints to existing `AuthController.cs`).

1. Warm-up: make the index on `RefreshTokens.Token` **unique** (`.IsUnique()` in your `RefreshTokenConfiguration`), add a migration, and update the database (ARCHITECTURE #14). Your lookup below uses `SingleOrDefaultAsync` — the DB should enforce what the code assumes.
2. Wire up JWT **validation** for this API itself in `Program.cs`: `builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)` with `TokenValidationParameters` matching your `JwtOptions` (key, issuer, audience, `ClockSkew = TimeSpan.Zero`), then `app.UseAuthentication()` before `app.UseAuthorization()`. Without this, `[Authorize]` on `/revoke` cannot work — it's the same code the plan shows for consumer APIs.
3. Implement `POST /refresh`:
  - Hash the incoming refresh token (same SHA-256 method as before).
  - Find the matching `RefreshToken` record in the DB (include the `User` navigation property).
  - Validate: token must exist, not be expired, not be revoked.
  - If valid: mark old token as revoked (`IsRevoked = true`, `RevokedAt = DateTime.UtcNow`).
  - Generate new access + refresh token pair.
  - Hash and save the new refresh token.
  - Set `ReplacedByToken` on the old record to the hash of the new one.
  - Save changes and return `200 OK` with `AuthResponse`.
4. Implement `POST /revoke` (requires `[Authorize]`):
  - Hash the incoming refresh token.
  - Find it in the DB (must belong to the authenticated user — get user ID from `User.FindFirstValue(ClaimTypes.NameIdentifier)`).
  - Mark as revoked and save.
  - Return `204 No Content`.
5. **Reuse detection (ARCHITECTURE #15 — you are deploying this):** if the token presented at `/refresh` exists but is *already revoked*, treat it as theft — someone is replaying a rotated token. Revoke **all** of that user's active refresh tokens and return `401`. The legitimate user just logs in again; the attacker is locked out.

**Namespaces you will need:** `Microsoft.AspNetCore.Authorization`, `System.Security.Claims`, `Microsoft.EntityFrameworkCore`

**EF Core method you will need:** `.Include(rt => rt.User)` to load the navigation property in a single query.

### Nudges

- Use `_context.RefreshTokens.Include(rt => rt.User).SingleOrDefaultAsync(rt => rt.Token == hashedToken)` to load the token with its user.
- Return `401` for all invalid token scenarios (not found, expired, revoked) — same reason as login: don't leak information.
- `[Authorize]` on the revoke action requires `app.UseAuthentication()` and `app.UseAuthorization()` to be in `Program.cs` in that order.
- `User.FindFirstValue(ClaimTypes.NameIdentifier)` gives you the user ID from the JWT of the current request.

### Quiz — paste these into Claude one at a time

1. What is refresh token rotation and what attack does it protect against?
2. What is the `ReplacedByToken` field for? Could you implement rotation without it?
3. If a user logs in on three devices, how many refresh tokens will be in the database? What happens when they revoke on one device?
4. Why does the revoke endpoint require a valid access token (`[Authorize]`) but the refresh endpoint does not?
5. What does `.Include(rt => rt.User)` do in EF Core and why do we need it here?

---

## Chapter 8 — Roles & Admin Registration

### Concept

Roles are seeded once at startup and then assigned to users at registration time. Because roles are embedded as claims in the JWT, downstream APIs can use `[Authorize(Roles = "Admin")]` without any database call. The admin registration endpoint is itself protected — only an existing admin can create another admin.

### Your Task

**Files you will create:** no new files — modify `Program.cs` and `AuthController.cs`.

1. In `Program.cs`, after `app.Build()` and before `app.Run()`, add role seeding:
  - Create a scope with `app.Services.CreateScope()`.
  - Resolve `RoleManager<IdentityRole>` from the scope.
  - For each role name (`"Admin"`, `"User"`): check `RoleExistsAsync` and if false, call `CreateAsync`.
2. Implement `POST /register-admin` in `AuthController`:
  - Decorate with `[Authorize(Roles = "Admin")]`.
  - Same logic as regular register, but assign the `Admin` role instead of `User`.
3. Test the full role flow with Postman or a `.http` file:
  - Register a regular user → login → inspect the JWT on jwt.io → confirm `role: "User"`.
  - Use an existing admin token to call `/register-admin` → login as new admin → confirm `role: "Admin"`.

**Namespaces you will need:** `Microsoft.AspNetCore.Authorization`, `Microsoft.AspNetCore.Identity`

**Classes you will use:** `RoleManager<IdentityRole>`, `IdentityRole`

### Nudges

- `app.Services.CreateScope()` must be inside a `using` block or the scope will not be disposed.
- `RoleManager.RoleExistsAsync(name)` returns `bool` — no need to handle a null case.
- For the first admin user (chicken-and-egg problem): seed an initial admin directly in the startup seeding block using `UserManager` alongside the role seeding, or create one manually in the DB.
- If `[Authorize(Roles = "Admin")]` returns 403 instead of 401, it means authentication succeeded but the role claim is missing — check that roles are included in the JWT in `TokenService`.

### Quiz — paste these into Claude one at a time

1. Why do we seed roles at startup instead of creating them through an endpoint?
2. What is the difference between `401 Unauthorized` and `403 Forbidden`? When does each occur?
3. If an admin's role is changed in the database after they log in, will their existing JWT reflect the change? Why or why not?
4. How does `[Authorize(Roles = "Admin")]` know what roles the user has without querying the database?
5. What is the "chicken-and-egg" problem with the first admin user and how do you solve it?

---

## Chapter 9 — Rate Limiting & Security Hardening

### Concept

Rate limiting prevents brute-force attacks on the login endpoint. Without it, an attacker can try thousands of passwords per second. ASP.NET Core has built-in rate limiting middleware (`Microsoft.AspNetCore.RateLimiting`) since .NET 7 — no third-party package needed. A fixed window limiter allows N requests per time window per client IP.

### Your Task

**Files you will create:** no new files — modify `Program.cs` and `AuthController.cs`.

1. In `Program.cs`, add `builder.Services.AddRateLimiter(...)` with a named policy called `"auth"`:
  - Use a fixed window: 5 requests per 1 minute per IP address.
  - On rejection, return `429 Too Many Requests`.
2. Add `app.UseRateLimiter()` to the middleware pipeline (must come before `app.MapControllers()`).
3. Decorate the `login`, `register`, and `refresh` actions with `[EnableRateLimiting("auth")]` — `/refresh` is just as brute-forceable as login (ARCHITECTURE #13).
4. Verify `.gitignore` contains `appsettings.Development.json` and that the JWT key is never in `appsettings.json`.
5. Check the `Dockerfile` does not copy `.env` or `appsettings.Development.json` into the image.

**Namespaces you will need:** `Microsoft.AspNetCore.RateLimiting`, `System.Threading.RateLimiting`

**Classes you will use:** `RateLimiterOptions`, `FixedWindowRateLimiterOptions`

### Nudges

- Careful: `options.AddFixedWindowLimiter("auth", ...)` creates **one global bucket** shared by every client — 5 requests/min *total*, so one attacker could lock everyone out. For per-IP, use `options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { Window = TimeSpan.FromMinutes(1), PermitLimit = 5 }))`.
- `options.RejectionStatusCode = StatusCodes.Status429TooManyRequests` sets the HTTP status when the limit is hit.
- `[EnableRateLimiting("auth")]` is the attribute on the controller action (or the whole controller class).
- Behind a reverse proxy (any real deployment), the client IP arrives in `X-Forwarded-For` — configure the `ForwardedHeaders` middleware before trusting `RemoteIpAddress`, or every request appears to come from the proxy and shares one bucket.

### Quiz — paste these into Claude one at a time

1. What is a brute-force attack on a login endpoint and how does rate limiting mitigate it?
2. What is the difference between a fixed window limiter and a sliding window limiter?
3. Why should secrets never be committed to version control, even in a private repository?
4. What HTTP status code does rate limiting return and what does it mean?
5. What other security headers or middleware would you add to a production auth API?

---

## Chapter 10 — Docker, Integration & End-to-End Test

### Concept

The Dockerfile builds a production-ready image using a multi-stage build: one stage compiles the app, a second stage copies only the output (no SDK, no source code). This produces a small, secure image. `docker-compose.yml` wires the API and the database together so the full stack runs with one command.

### Your Task

**Files you will create:** `Dockerfile`, `docker-compose.yml`, `identity-api.http` (optional but useful).

1. Write a multi-stage `Dockerfile`:
  - Stage 1 (`build`): use `mcr.microsoft.com/dotnet/sdk:10.0` as base, copy `.csproj`, run `dotnet restore`, copy source, run `dotnet publish -c Release -o /app/publish`.
  - Stage 2 (`runtime`): use `mcr.microsoft.com/dotnet/aspnet:10.0`, copy from `/app/publish`, set `ENTRYPOINT`.
2. Extend your existing `docker-compose.yml`: rename the `db` service to `identity-db` (the service name is the DNS name the API container uses in `Host=...`), and add the `identity-api` service from the plan.
3. Run `docker compose up -d --build` and verify both containers start.
4. Create a `identity-api.http` file (VS Code REST Client format) or use Postman to test the full flow:
  - `POST /api/auth/register` — create a user
  - `POST /api/auth/login` — get token pair
  - Decode the access token on jwt.io — verify all claims
  - `POST /api/auth/refresh` — get a new pair
  - Try the old refresh token again — confirm it is rejected
  - `POST /api/auth/revoke` — logout
5. Finally, update the movies API `appsettings.Development.json` with the same `Key` and `Issuer` and verify a token from Identity.Api is accepted by the movies API.

**Tools you will use:** Docker CLI, `docker compose`, VS Code REST Client or Postman

### Nudges

- Multi-stage builds: the key instruction is `COPY --from=build /app/publish .` in the runtime stage.
- The API container needs the connection string injected as an environment variable in `docker-compose.yml`: `ConnectionStrings__Default=Host=identity-db;...` — the name after `__` must match what `Program.cs` reads (`GetConnectionString("Default")`). Double underscore `__` maps to `:` in the .NET configuration hierarchy.
- EF Core migrations must run on startup if you want the container to be self-contained. Add this before `app.Run()`: `await context.Database.MigrateAsync()` (resolve `ApplicationDbContext` from a scope).
- If the API starts before the DB is ready, it will crash. Add a `healthcheck` on the DB service and `depends_on: condition: service_healthy` on the API service.
- Production secrets (`Jwt__Key`, DB password) are injected as environment variables at *run* time — never copied into the image. Chapter 12 covers this properly.

### Quiz — paste these into Claude one at a time

1. What is a multi-stage Docker build and why does it produce a smaller, more secure image than a single-stage build?
2. How does the `__` (double underscore) in environment variables map to the .NET configuration hierarchy?
3. What is `context.Database.MigrateAsync()` and why is it useful in a containerized deployment?
4. Inside `docker-compose.yml` the API's connection string says `Host=identity-db`, but your local `psql` connects with `Host=localhost`. Why are they different, and what makes the name `identity-db` resolvable from the API container?
5. A consumer API (movies API) accepts tokens from Identity.Api without ever calling it at runtime. How is this possible? What would need to change if we switched from HMAC-SHA256 to RSA?

---

## Chapter 11 — Result Pattern, ProblemDetails & Global Exception Handling

### Concept

Expected failures (wrong password, expired refresh token) are not exceptions — they are ordinary outcomes your code should model as *data*. A small hand-rolled `Result`/`Result<T>` type makes success-or-failure explicit in every service signature. On the wire, every error becomes an RFC 7807 `ProblemDetails` body so consumers parse one shape for all errors. Truly *unexpected* errors (a dead DB, a bug) are caught by one global `IExceptionHandler` that logs them and returns a generic 500 ProblemDetails — never a stack trace. This is ARCHITECTURE decisions #4 and #5.

### Your Task

**Files you will create:** `Common/Result.cs`, `Infrastructure/GlobalExceptionHandler.cs`, `Features/Auth/IAuthService.cs`, `Features/Auth/AuthService.cs` (extracting logic from `AuthController`).

1. Hand-roll `Result` and `Result<T>`: a success flag, a value (for `Result<T>`), and an error (code + description). No library — that's the point (over-engineering is the enemy, per `docs/ARCHITECTURE.md`).
2. Extract the auth logic out of `AuthController` into an `AuthService` that returns `Result<AuthResponse>` (login, refresh) or `Result` (register, revoke). The controller's only job becomes: call the service, map the `Result` to `200`/`201`/`204` or a `ProblemDetails` response.
3. Implement a global `IExceptionHandler`: register with `builder.Services.AddExceptionHandler<GlobalExceptionHandler>()` + `builder.Services.AddProblemDetails()`, enable with `app.UseExceptionHandler()`. It logs the exception and returns a 500 ProblemDetails with no internal details.
4. Verify: force an exception (temporarily stop Postgres and hit login) and confirm you get a clean ProblemDetails 500, not a stack trace.

**Namespaces you will need:** `Microsoft.AspNetCore.Diagnostics`, `Microsoft.AspNetCore.Mvc`

**Classes you will use:** `IExceptionHandler`, `ProblemDetails`, your own `Result<T>`

### Nudges

- Keep `Result` tiny: `bool IsSuccess`, `T? Value`, `Error? Error` where `Error` is a record of `(string Code, string Description)`. Static factories `Result.Success(...)` / `Result.Failure(...)` keep call sites readable.
- The internal error code can be precise (`Auth.InvalidCredentials`, `Auth.LockedOut`) — but the controller still maps *all* credential failures to the same uniform `401`. Precision inside, uniformity on the wire.
- `[ApiController]` already converts model-validation failures to `ValidationProblemDetails` automatically — you get request validation for free.
- `ControllerBase.Problem(...)` is the built-in helper for producing ProblemDetails responses with a status code.

### Quiz — paste these into Claude one at a time

1. Why model expected failures as values instead of throwing exceptions? What does an exception cost that a `Result` doesn't?
2. What is RFC 7807 and what standard fields does a ProblemDetails response have?
3. Where exactly is the line between a failure the `Result` pattern should carry and one the global exception handler should catch?
4. The `Result` inside knows *why* login failed. Why must the HTTP response stay a uniform `401` anyway?
5. What does `[ApiController]` do with model-validation failures out of the box?

---

## Chapter 12 — Observability, CORS & Production Configuration

### Concept

A deployed API must tell you three things: that it's alive (`/health`), what happened (structured logs), and it must never leak what it shouldn't (secrets, tokens, CORS). Serilog writes *structured* logs — key-value events you can query, not strings you grep. In containers, logs go to stdout and the platform collects them. Secrets come from the environment at runtime, never from files baked into the image. This is ARCHITECTURE decisions #9 and #11.

### Your Task

**Files you will create:** none new — modify `Program.cs` (consider `Extensions/` methods to keep it thin) and `docker-compose.yml`.

1. Add Serilog: `dotnet add package Serilog.AspNetCore`, then `builder.Host.UseSerilog(...)` reading config from `appsettings.json`, and `app.UseSerilogRequestLogging()` for one structured event per request. Console sink only — containers log to stdout.
2. Add health checks: `builder.Services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>()` and `app.MapHealthChecks("/health")`. Verify it returns `Healthy` with the DB up and `Unhealthy` with it stopped.
3. Add a named CORS policy with explicit origins read from config (`Cors:AllowedOrigins`), and `app.UseCors(...)` in the pipeline. No `AllowAnyOrigin`.
4. Production config pass: `Jwt__Key` and the DB password enter via environment variables in `docker-compose.yml`; grep your logs to confirm no token, password, or connection string is ever logged.

**Packages you will use:** `Serilog.AspNetCore`, `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`

### Nudges

- `UseSerilog((context, config) => config.ReadFrom.Configuration(context.Configuration))` lets you tune log levels from `appsettings.json` without recompiling.
- Middleware order: `UseCors` goes after routing but before `UseAuthentication`/`UseAuthorization`.
- Never log DTOs from the auth endpoints — Serilog destructuring (`{@Request}`) would happily serialize a password. Log user IDs and event names, not payloads.
- `/health` should be cheap and unauthenticated so an orchestrator can poll it — but it must not expose details (connection strings, versions) publicly.

### Quiz — paste these into Claude one at a time

1. What does a structured log event give you that a plain-text log line doesn't? Give a concrete query you could run.
2. What should a `/health` endpoint check, and what must it never expose?
3. What is a CORS preflight request, and why is `AllowAnyOrigin` + credentials a forbidden combination?
4. Why do containerized apps log to stdout instead of files?
5. Name two places production secrets can live that are not `appsettings.json`, and the trade-off between them.

---

## Chapter 13 — Testing: Unit + Integration (Testcontainers)

### Concept

Two layers, per ARCHITECTURE #8. **Unit tests** cover pure logic with no I/O — `TokenService` is the perfect target: given a user and roles, is the JWT correct? **Integration tests** boot the real app (`WebApplicationFactory`) against a real Postgres in a throwaway container (Testcontainers) and exercise the actual HTTP flows. Auth is exactly where integration tests earn their keep: rotation and revocation are multi-step, stateful behaviors a unit test can't prove. EF's InMemory provider is a trap here — it isn't relational and would happily pass tests Postgres would fail.

### Your Task

**Files you will create:** a new `Identity.Api.Tests` project (xunit) next to `Identity.Api/`.

1. `dotnet new xunit -n Identity.Api.Tests`, reference the API project, add both to a solution file if you don't have one.
2. Unit-test `TokenService`: parse the generated token back with `JwtSecurityTokenHandler.ReadJwtToken` and assert `sub`, `email`, `role`, `iss`, `aud` are what you put in; assert expiry ≈ configured minutes; assert two `GenerateRefreshToken()` calls differ and decode to 64 bytes.
3. Add `Microsoft.AspNetCore.Mvc.Testing` + `Testcontainers.PostgreSql`. Build a `WebApplicationFactory<Program>` that starts a `postgres:17-alpine` container and points the app's connection string at it.
4. Integration-test the full flow as HTTP calls: register → login → refresh → **the old refresh token is rejected with 401** → revoke → the revoked token is rejected. That last pair is your rotation and revocation proof.

**Packages you will use:** `xunit`, `Microsoft.AspNetCore.Mvc.Testing`, `Testcontainers.PostgreSql`

### Nudges

- Top-level `Program.cs` generates an internal `Program` class — add `public partial class Program { }` at the bottom of `Program.cs` so `WebApplicationFactory<Program>` can see it.
- Start the container in `IAsyncLifetime.InitializeAsync` (`new PostgreSqlBuilder().WithImage("postgres:17-alpine").Build()`), and override config in `ConfigureWebHost` with `builder.UseSetting("ConnectionStrings:Default", _container.GetConnectionString())`.
- Construct `TokenService` directly in unit tests with `Options.Create(new JwtOptions { ... })` — no DI container needed.
- Test behaviors, not methods: "a rotated refresh token cannot be reused" is worth more than ten property assertions. Don't chase coverage.

### Quiz — paste these into Claude one at a time

1. Which parts of this API belong in unit tests and which demand integration tests? Why is `TokenService` the former and `/refresh` the latter?
2. Why is EF Core's InMemory provider a bad substitute for Postgres in these tests? Name something it would let pass that Postgres would reject.
3. What does `WebApplicationFactory<Program>` actually spin up, and what does it *not* spin up?
4. How do you prove token rotation works using only HTTP calls, without querying the database?
5. Why does the test project need `public partial class Program { }` to exist?

---

## After All Chapters — Final Challenge

Without looking at any code, draw on paper (or in a text file) a diagram showing:

- The client (browser/app)
- Identity.Api
- Movies.Api
- The database

Draw the arrows for: registration, login, token refresh, and a protected API call to movies. Label each arrow with the HTTP method, route, and what is sent/returned.

If you can draw this accurately without notes, you understand the system end to end.
