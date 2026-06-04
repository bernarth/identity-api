# Architecture — Identity.Api

Here it is documented the decisions taken and the reasons why those were taken while building this project.

---

## The decision

`Identity.Api` follows a simple architecture **one project, organized by feature**. Files are grouped by what they do for the user (a feature), with thin technical folders for cross-cutting pieces.

```
Identity.Api/
├── Features/
│   ├── Auth/
│   │   ├── AuthController.cs
│   │   └── Dtos/                # RegisterRequest, LoginRequest, AuthResponse, ...
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
└── Program.cs                   # thin: calls the extension methods
```

## Why

This is a **small** service: one controller, five endpoints, two entities, one service. Architecture exists to manage *change* and *complexity*, and there is very little of either here. So the real risk is **over-engineering**. A single feature-organized project matches the size of the problem, reads like the API surface, and evolves trivially into a layered design later if the service ever genuinely grows.

## Alternatives considered

- **Clean / Onion (4 projects: Domain, Application, Infrastructure, Api).** 
Genuinely valuable for large, multi-team, or infrastructure-volatile systems. For 5 endpoints it's too much: four projects and layers of interfaces to wrap a single `AuthController`. Rejected as over-engineering for this size, and it would pull focus away from the auth concepts that goal.
- **Vertical Slice (MediatR / CQRS per feature).** 
Modern and a nice talking point, but adds MediatR and handler boilerplate plus a command/query split this surface doesn't need. Rejected as unjustified complexity.

## Relationship to the existing plan

This **follows** [../IDENTITY_API_PLAN.md](../IDENTITY_API_PLAN.md) folder structure defined.

## If this project later grows

Migrate to a layered (Clean) design only when *real* pressure appears: multiple consumers of the domain logic, a second delivery mechanism (gRPC, a background worker) alongside the API, or a team large enough that enforced layer boundaries prevent mistakes. Until then this is the correct, defensible choice, and saying exactly that in an interview is the win.

---

## Decisions made

Architecture is settled, and the following implementation decisions are now locked in.
Status legend: **Decided** = build it this way; **Deferred** = revisit at the noted point.


| #   | Decision                      | Choice                                                                                | Status       | Notes                                                                                                                                                  |
| --- | ----------------------------- | ------------------------------------------------------------------------------------- | ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------ |
| 1   | **Endpoint style**            | **Controllers** (not Minimal API)                                                     | Decided      | Matches the plan/workbook; most common interview style.                                                                                                |
| 2   | **Refresh token delivery**    | **JSON response body** for now                                                        | Decided      | See "Topics to revisit" — you want to study *when* a body vs an `HttpOnly` cookie is the right call.                                                   |
| 3   | **JWT config binding**        | **Options pattern** (`JwtOptions` + `IOptions<>`)                                     | Decided      | No raw `_config["Jwt:..."]` string lookups scattered around. Bind once.                                                                                |
| 4   | **Error handling**            | **ProblemDetails** (RFC 7807) on the wire + lightweight **Result pattern** internally | Decided      | See "Result pattern + ProblemDetails" below for how they fit together.                                                                                 |
| 5   | **Global exception handling** | **One global handler** (`IExceptionHandler`)                                          | Decided      | Keeps controllers clean; unexpected errors become a consistent ProblemDetails.                                                                         |
| 6   | **Migrations**                | **Manual in dev, `MigrateAsync()` on boot in the container**                          | Decided      | Same migrations, two contexts: you run `dotnet ef database update` while developing (Ch4); the container applies them automatically on startup (Ch10). |
| 7   | **First-admin bootstrap**     | **Seed from config** at startup                                                       | Decided      | `Seed:AdminEmail` / password seeded alongside roles. Solves the chicken-and-egg problem.                                                               |
| 8   | **Testing**                   | **Unit tests + integration tests** (Testcontainers Postgres)                          | Decided      | Unit-test pure logic (e.g. `TokenService`); integration-test the auth flows over real HTTP + DB. Don't chase 100% coverage.                            |
| 9   | **CORS**                      | **Explicit policy**, documented                                                       | Decided      | Add a named dev policy; document the allowed origins in the README.                                                                                    |
| 10  | **Audience (`aud`) handling** | TBD                                                                                   | **Deferred** | Revisit at the token-design chapter (Workbook Ch5). The plan's client-supplied `aud` is weak; we'll likely move to a server-fixed audience/allowlist.  |
| 11  | **Logging / health**          | **Serilog** (structured logs) + `**/health`** endpoint                                | Decided      | Low effort, high signal for a "production-shaped" portfolio API.                                                                                       |


### Result pattern + ProblemDetails

These are two different layers that work together:

- **Result pattern (internal):** services return a `Result` / `Result<T>` value that is either a success (carrying data) or a failure (carrying an error), *instead of* throwing exceptions for expected failures like "invalid credentials" or "expired refresh token". This models  failure as data and keeps control flow explicit.



- **ProblemDetails (the wire):** the controller inspects the `Result` and, on failure, returns a standard RFC 7807 `ProblemDetails` response with the right status code.



Flow: `Service → Result<T> → Controller maps to → 200 OK | ProblemDetails`.

Keep the Result type **small and hand-rolled** (a success flag, a value, an error). Do not pull in a heavyweight library — that would be over-engineering for this size. The global exception handler (#5) is still the safety net for *unexpected* errors; Result is for *expected* outcomes.

## Topics to revisit

These are intentionally parked. They're not blockers, but you flagged that you want to
understand them properly rather than copy a default.

- **Refresh token: body vs `HttpOnly` cookie (from #2).** Returning the refresh token in the JSON body is simplest and fine for API clients / Postman. An `HttpOnly`, `Secure`, `SameSite` cookie is safer against XSS for browser apps but introduces CSRF considerations and tighter coupling to a web origin.



- **Audience (`aud`) handling (from #10).** Decided later at Workbook Ch5. The question: should the *client* pick its audience (current plan) or should the *server* control it? Short version of the concern: a client choosing its own `aud` can mint tokens aimed at any consumer.

