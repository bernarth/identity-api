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
3. Create `appsettings.Development.json` with the `Jwt` and `Database` sections from the plan. Add this file to `.gitignore`.
4. Review the auto-generated `Program.cs` — identify what `builder` is, what `app` is, and what the difference between them is.

### Nudges

- `dotnet add package <name> --version 10.`* is the CLI syntax for adding a versioned package.
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

**Files you will create:** `Models/ApplicationUser.cs`, `Models/RefreshToken.cs`, `Data/ApplicationDbContext.cs`.

1. Create `ApplicationUser.cs` in the `Models/` folder. It must extend `IdentityUser` and add `FirstName`, `LastName`, `CreatedAt`, and a navigation property to `RefreshToken`.
2. Create `RefreshToken.cs` in the `Models/` folder with all fields from the plan. Pay attention to which fields are nullable and why.
3. Create `ApplicationDbContext.cs` in the `Data/` folder. It must extend `IdentityDbContext<ApplicationUser>` (not plain `DbContext`). Add the `RefreshTokens` DbSet and configure the relationships in `OnModelCreating`.
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

**Files you will create:** `Services/ITokenService.cs`, `Services/TokenService.cs`.

1. Create `ITokenService.cs` with two method signatures:
  - `string GenerateAccessToken(ApplicationUser user, IList<string> roles)`
  - `string GenerateRefreshToken()` — returns the raw (unhashed) token string
2. Implement `TokenService.cs`. Inject `IConfiguration` to read JWT settings.
3. In `GenerateAccessToken`: build a `ClaimsIdentity` with `sub`, `email`, `given_name`, `family_name`, `jti`, and one `role` claim per role. Use `SecurityTokenDescriptor` and `JwtSecurityTokenHandler` to produce the token string.
4. In `GenerateRefreshToken`: use `RandomNumberGenerator.GetBytes(64)` to generate 64 random bytes, then encode them with `Convert.ToBase64String`.
5. Register `ITokenService` / `TokenService` in `Program.cs` using `builder.Services.AddScoped`.

**Namespaces you will need:** `System.IdentityModel.Tokens.Jwt`, `Microsoft.IdentityModel.Tokens`, `System.Security.Claims`, `System.Security.Cryptography`

**Classes you will use:** `JwtSecurityTokenHandler`, `SecurityTokenDescriptor`, `SymmetricSecurityKey`, `SigningCredentials`, `ClaimsIdentity`, `Claim`, `RandomNumberGenerator`

### Nudges

- `ClaimTypes.NameIdentifier` is the standard constant for the `sub` claim.
- `ClaimTypes.Role` is the constant for role claims. Add one `Claim` per role in the list.
- `SecurityTokenDescriptor.Expires` takes a `DateTime` — use `DateTime.UtcNow.AddMinutes(...)`.
- Read expiry minutes from config: `_config.GetValue<int>("Jwt:AccessTokenExpiryMinutes")`.
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

**Files you will create:** `DTOs/RegisterRequest.cs`, `DTOs/LoginRequest.cs`, `DTOs/AuthResponse.cs`, `Controllers/AuthController.cs`.

1. Create the three DTO classes. `AuthResponse` must contain `AccessToken`, `RefreshToken`, `AccessTokenExpiresAt`, and `TokenType` ("Bearer").
2. Create `AuthController` inheriting from `ControllerBase`. Decorate with `[ApiController]` and `[Route("api/auth")]`. Inject `UserManager<ApplicationUser>`, `ITokenService`, and `ApplicationDbContext`.
3. Implement `POST /register`:
  - Validate the request (Identity does password validation for you).
  - Create the user with `UserManager.CreateAsync(user, password)`.
  - Assign the `User` role with `UserManager.AddToRoleAsync`.
  - Return `201 Created`.
4. Implement `POST /login`:
  - Find user by email with `UserManager.FindByEmailAsync`.
  - Verify password with `UserManager.CheckPasswordAsync`.
  - Get roles with `UserManager.GetRolesAsync`.
  - Generate access token and refresh token via `ITokenService`.
  - Hash the refresh token (SHA-256) and save it to `RefreshTokens` table.
  - Return `200 OK` with `AuthResponse`.

**Namespaces you will need:** `Microsoft.AspNetCore.Mvc`, `Microsoft.AspNetCore.Identity`, `System.Security.Cryptography`

**Classes you will use:** `UserManager<ApplicationUser>`, `ControllerBase`, `ITokenService`

**For hashing the refresh token before saving:** `Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)))`

### Nudges

- `UserManager.CreateAsync` returns an `IdentityResult`. Check `.Succeeded` before continuing — if false, return `400 BadRequest` with `result.Errors`.
- `UserManager.FindByEmailAsync` returns `null` if not found. Return `401 Unauthorized` for both "user not found" and "wrong password" — do not distinguish between them (prevents user enumeration).
- Set `RefreshToken.CreatedAt = DateTime.UtcNow` and `ExpiresAt = DateTime.UtcNow.AddDays(7)` before saving.
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

1. Implement `POST /refresh`:
  - Hash the incoming refresh token (same SHA-256 method as before).
  - Find the matching `RefreshToken` record in the DB (include the `User` navigation property).
  - Validate: token must exist, not be expired, not be revoked.
  - If valid: mark old token as revoked (`IsRevoked = true`, `RevokedAt = DateTime.UtcNow`).
  - Generate new access + refresh token pair.
  - Hash and save the new refresh token.
  - Set `ReplacedByToken` on the old record to the hash of the new one.
  - Save changes and return `200 OK` with `AuthResponse`.
2. Implement `POST /revoke` (requires `[Authorize]`):
  - Hash the incoming refresh token.
  - Find it in the DB (must belong to the authenticated user — get user ID from `User.FindFirstValue(ClaimTypes.NameIdentifier)`).
  - Mark as revoked and save.
  - Return `204 No Content`.

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
3. Decorate the `login` and `register` actions with `[EnableRateLimiting("auth")]`.
4. Verify `.gitignore` contains `appsettings.Development.json` and that the JWT key is never in `appsettings.json`.
5. Check the `Dockerfile` does not copy `.env` or `appsettings.Development.json` into the image.

**Namespaces you will need:** `Microsoft.AspNetCore.RateLimiting`, `System.Threading.RateLimiting`

**Classes you will use:** `RateLimiterOptions`, `FixedWindowRateLimiterOptions`

### Nudges

- `AddRateLimiter` takes an `Action<RateLimiterOptions>`. Use `options.AddFixedWindowLimiter("auth", o => { o.Window = TimeSpan.FromMinutes(1); o.PermitLimit = 5; })`.
- `options.RejectionStatusCode = StatusCodes.Status429TooManyRequests` sets the HTTP status when the limit is hit.
- `[EnableRateLimiting("auth")]` is the attribute on the controller action (or the whole controller class).
- The key selector for per-IP limiting: use `httpContext.Connection.RemoteIpAddress?.ToString()` in the partition key factory.

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
2. Write `docker-compose.yml` with the `identity-db` (postgres:17-alpine) and `identity-api` services from the plan.
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
- The API container needs the connection string injected as an environment variable in `docker-compose.yml`: `ConnectionStrings__DefaultConnection=Host=identity-db;...`. Double underscore `__` maps to `:` in .NET configuration hierarchy.
- EF Core migrations must run on startup if you want the container to be self-contained. Add this before `app.Run()`: `await context.Database.MigrateAsync()` (resolve `ApplicationDbContext` from a scope).
- If the API starts before the DB is ready, it will crash. Add a `healthcheck` on the DB service and `depends_on: condition: service_healthy` on the API service.

### Quiz — paste these into Claude one at a time

1. What is a multi-stage Docker build and why does it produce a smaller, more secure image than a single-stage build?
2. How does the `__` (double underscore) in environment variables map to the .NET configuration hierarchy?
3. What is `context.Database.MigrateAsync()` and why is it useful in a containerized deployment?
4. Why did we use port `5432` for the database instead of `5432`?
5. A consumer API (movies API) accepts tokens from Identity.Api without ever calling it at runtime. How is this possible? What would need to change if we switched from HMAC-SHA256 to RSA?

---

## After All Chapters — Final Challenge

Without looking at any code, draw on paper (or in a text file) a diagram showing:

- The client (browser/app)
- Identity.Api
- Movies.Api
- The database

Draw the arrows for: registration, login, token refresh, and a protected API call to movies. Label each arrow with the HTTP method, route, and what is sent/returned.

If you can draw this accurately without notes, you understand the system end to end.
