# Identity.Api — Design Plan

Standalone .NET 10 authentication service that issues JWT access tokens and refresh tokens.
Other APIs (movies, future projects) and frontends consume tokens from this single source.

---

## Purpose

- Centralized auth so every project shares one user store and one signing key.
- Issues short-lived JWT access tokens + long-lived refresh tokens.
- Embeds roles (`Admin`, `User`) in JWT claims so downstream APIs can gate endpoints without calling back to this service.

---

## Tech Stack

| Concern | Choice | Reason |
|---|---|---|
| Runtime | .NET 10 | Latest LTS |
| User management | ASP.NET Core Identity | Industry-standard, handles hashing/lockouts |
| ORM | Entity Framework Core 10 | Same pattern as movies API |
| Database | PostgreSQL 17 | Latest stable, consistent with ecosystem |
| DB driver | Npgsql.EntityFrameworkCore.PostgreSQL 10.* | Official Npgsql provider, tracks EF Core versions |
| JWT | `Microsoft.AspNetCore.Authentication.JwtBearer 10.*` + `Microsoft.IdentityModel.Tokens 8.*` | Standard .NET JWT stack |
| Containerization | Docker + docker-compose | Match movies API dev workflow |

---

## Project Structure

```
Identity.Api/
├── Features/
│   ├── Auth/
│   │   ├── AuthController.cs
│   │   └── Dtos/                # RegisterRequest, LoginRequest, AuthResponse, etc
│   └── Tokens/
│       ├── ITokenService.cs
│       └── TokenService.cs
├── Domain/                      # ApplicationUser, RefreshToken (the entities)
├── Infrastructure/
│   └── Data/
│       ├── ApplicationDbContext.cs
│       └── Migrations/          # EF-generated
├── Extensions/                  # ServiceCollection / pipeline extension methods
│   ├── IdentityServiceExtensions.cs
│   ├── JwtServiceExtensions.cs
│   └── ...
├── appsettings.json
├── appsettings.Development.json # gitignored
└── Program.cs                   # thin and organized
```

---

## NuGet Packages

```xml
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="10.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.*" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.*" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.*" />
<PackageReference Include="Microsoft.IdentityModel.Tokens" Version="8.*" />
```

No third-party auth servers (no IdentityServer, no Keycloak). Pure custom implementation on top of ASP.NET Core Identity — full ownership, fully understandable.

---

## Database Models

### ApplicationUser (extends IdentityUser)

```csharp
public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; }  = string.Empty;
    public DateTime CreatedAt { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
```

ASP.NET Core Identity manages the `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles` tables automatically via migrations.

### RefreshToken

```csharp
public class RefreshToken
{
    public Guid Id { get; set; }
    public string Token { get; set; } = string.Empty;   // stored as SHA-256 hash
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; }        // for rotation audit trail
}
```

Refresh tokens are **stored hashed** (SHA-256). The raw token is only ever returned to the client; the DB holds the hash.

---

## API Endpoints

| Method | Route | Auth required | Description |
|---|---|---|---|
| POST | `/api/auth/register` | No | Create account (default role: User) |
| POST | `/api/auth/login` | No | Returns access token + refresh token |
| POST | `/api/auth/refresh` | No | Exchange refresh token for new token pair |
| POST | `/api/auth/revoke` | Yes (Bearer) | Invalidate a specific refresh token (logout) |
| POST | `/api/auth/register-admin` | Yes (Admin role) | Create admin accounts |

### Request / Response shapes

**Register**
```json
// POST /api/auth/register
{
  "firstName": "string",
  "lastName": "string",
  "email": "string",
  "password": "string"
}
// 201 Created
```

**Login**
```json
// POST /api/auth/login
{ "email": "string", "password": "string" }

// 200 OK
{
  "accessToken": "eyJ...",
  "refreshToken": "base64-random-512-bits",
  "accessTokenExpiresAt": "2026-05-25T14:00:00Z",
  "tokenType": "Bearer"
}
```

**Refresh**
```json
// POST /api/auth/refresh
{ "refreshToken": "base64-random-512-bits" }

// 200 OK — same shape as login response (new pair, old refresh token revoked)
```

**Revoke**
```json
// POST /api/auth/revoke  [Authorization: Bearer <access_token>]
{ "refreshToken": "base64-random-512-bits" }

// 204 No Content
```

---

## JWT Token Design

### Configuration (appsettings.Development.json)

```json
{
  "Jwt": {
    "Key": "<minimum 32-char random secret — same key shared with consumer APIs>",
    "Issuer": "https://id.issuer.com",
    "Audience": "<fixed value shared with every consumer API — server-controlled>",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 7
  }
}
```

The `Issuer`, `Key` **and `Audience`** must match what each consumer API has in its own
`appsettings.Development.json`. The audience is **server-fixed** (ARCHITECTURE decision #10):
Identity.Api stamps `aud` from its own config; clients never choose it. A client-supplied
audience would let any caller mint tokens aimed at any consumer.

### JWT Claims

```
sub          → user.Id
email        → user.Email
given_name   → user.FirstName
family_name  → user.LastName
role         → ["User"] or ["Admin", "User"]
jti          → Guid.NewGuid() (unique token ID)
iss          → https://id.issuer.com
aud          → fixed value from Jwt:Audience config (server-controlled; clients never pick it)
iat          → issued-at (Unix timestamp)
exp          → iat + 15 minutes
```

Roles are embedded directly in the token so downstream APIs enforce them locally — no network call back to this service.

### Signing Algorithm

**HMAC-SHA256 (HS256)** — symmetric, single shared secret.

For a portfolio project this is appropriate. For production multi-tenant at scale you'd move to RS256 (RSA key pair with a public JWKS endpoint), but that adds significant complexity and isn't needed here.

---

## Refresh Token Strategy: Rotation

Every time a refresh token is used, it is:
1. Marked as revoked (`IsRevoked = true`, `RevokedAt = now`)
2. The `ReplacedByToken` field records the hash of the new token (audit trail)
3. A brand-new refresh token is generated and returned alongside the new access token

This prevents refresh token reuse. If an attacker steals a refresh token and uses it after the legitimate client already rotated it, the stolen token is already revoked and will be rejected.

**Expiry:** Refresh tokens expire after 7 days (configurable). After expiry the user must log in again.

---

## Security Checklist

- [ ] Passwords validated by ASP.NET Core Identity (min length, complexity configurable)
- [ ] Refresh tokens stored as SHA-256 hashes — raw token never persisted
- [ ] Token rotation on every refresh — no reuse
- [ ] Revocation endpoint for explicit logout
- [ ] HTTPS enforced in production (`app.UseHttpsRedirection()`)
- [ ] Rate limiting on `/api/auth/login` and `/api/auth/register` (use `Microsoft.AspNetCore.RateLimiting`)
- [ ] `appsettings.Development.json` is in `.gitignore` (never commit secrets)
- [ ] JWT Key is at least 256 bits (32 characters)
- [ ] Account lockout actually enforced — password check via `SignInManager.CheckPasswordSignInAsync(..., lockoutOnFailure: true)`
- [ ] Rate limiting is **per-IP** and also covers `/api/auth/refresh`
- [ ] Unique index on `RefreshTokens.Token`
- [ ] Refresh-token **reuse detection**: a revoked token presented at `/refresh` revokes the user's whole token family
- [ ] Production secrets (JWT key, DB password) injected via environment variables — never baked into the image

---

## Database Setup

### ApplicationDbContext

```csharp
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // required — sets up Identity tables

        builder.Entity<RefreshToken>()
            .HasIndex(rt => rt.Token)
            .IsUnique();                // unique — lookups use SingleOrDefault, the DB enforces it

        builder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId);
    }
}
```

### Seed Roles

In `Program.cs`, before `app.Run()`, seed the two roles so they exist from the first run:

```csharp
using var scope = app.Services.CreateScope();
var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
foreach (var role in new[] { "Admin", "User" })
{
    if (!await roleManager.RoleExistsAsync(role))
        await roleManager.CreateAsync(new IdentityRole(role));
}
```

Also seed the **first admin user** from config (`Seed:AdminEmail`, `Seed:AdminPassword`) in this same startup block using `UserManager` — ARCHITECTURE decision #7; solves the chicken-and-egg problem.

---

## Docker / Dev Setup

### .env

```
POSTGRES_USER=identity_user
POSTGRES_PASSWORD=identity_pass
POSTGRES_DB=identity_db
```

### docker-compose.yml

```yaml
services:
  db:
    image: postgres:17-alpine
    container_name: identity_db
    restart: always
    environment:
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
      POSTGRES_DB: ${POSTGRES_DB}
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  postgres_data:
```

Run with: `docker compose up -d --build`

---

## How Consumer APIs Use This

The movies API (and any future API) needs zero changes beyond what it already has in `appsettings.Development.json`:

```json
{
  "Jwt": {
    "Key": "<same key as Identity.Api>",
    "Issuer": "https://id.issuer.com",
    "Audience": "<the same fixed audience value Identity.Api issues>"
  }
}
```

The consumer API validates the token locally using the shared key. It never calls Identity.Api at runtime — only the client (browser, mobile app, Postman) calls Identity.Api to get tokens.

In each consumer API's `Program.cs`:

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero  // strict expiry — no 5-min grace period
        };
    });
```

---

## Implementation Order (for your new Claude Code session)

1. `dotnet new webapi -n Identity.Api` — scaffold the project
2. Add NuGet packages
3. Create `ApplicationUser`, `RefreshToken` models
4. Create `ApplicationDbContext` (extends `IdentityDbContext<ApplicationUser>`)
5. Wire up Identity + EF Core + JWT auth in `Program.cs`
6. Add role seeding in `Program.cs`
7. Implement `ITokenService` / `TokenService` (JWT generation + refresh token logic)
8. Implement `AuthController` with all 5 endpoints
9. Add rate limiting middleware
10. `dotnet ef migrations add InitialCreate` — generate migration
11. Write `Dockerfile` + `docker-compose.yml`
12. Test all endpoints with Postman or `.http` file
13. Update movies API `appsettings.Development.json` with matching Key + Issuer

---

## Summary

> I need to implement Identity.Api — a standalone .NET 10 Web API that issues JWT access tokens and refresh tokens. The full design is in here. I need to follow the plan exactly: ASP.NET Core Identity, EF Core 10 + PostgreSQL 17, HMAC-SHA256 JWT, token rotation for refresh tokens, Admin + User roles seeded on startup, and docker-compose for local dev."
