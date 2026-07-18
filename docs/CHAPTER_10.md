# Chapter 10

In this chapter we implemented docker for the project. For our deployment

The following commands where used to test:

```bash
docker compose up -d --build
docker compose ps
docker compose logs identity-api
```

## Questions

1. What is a multi-stage Docker build and why does it produce smaller, more secure image than a single-stage build?

It is to build and runtime. It is safer because with the first stage the code compiles and publishes. On the other hand, we only need the runtime the necessary for the API to work which is the second phase.

Smaller: the final image contains only the runtime. The SDK, compilers, NuGet cache, and your source code exist only in the build stage, which is discarded since we copy only the publish source.
More secure: whatever isn't in the image can't be stolen or expolited. No source code to read if the container is compromised, no SDK/compiler for an attacker to build tooling with.

2. How does the `__` (double underscore) in environment variables map to the .NET configuration hierarchy?

.NET configuration is a flat key-value store with `:` as the hierarchy separator. The JSON becomes the key `Jet:Key`. Environment variables can't contain `:` reliably, so .NET's environment-variable configuration provider translates `__` to `:` when it load them.
After translation, an evn var and a JSON valur are literally the same key in the same config dictionary. And since the env-var provider is registered after the JSON providers, its value wins.

3. What is `context.Database.MigrateAsync()` and why is it useful in a containerized deployment?

`context.Database.MigrateAsync()` looks at the `__EFMigrationsHistory` table in the database compares it against the migrations compiled into the assembly, and applies any that are pending, in order, at application startup.

Why it matters in a container: the container must be self-contained. There's no developer with EF CLI inside a production host, and the `dotnet ef` tool isn't even in the runtime image. Fresh volume -> app boots -> schema creates itself -> seeding runs.

4. Inside `docker-compose.yml` the API's connection string says `Host=identity-db`, buy local `psql` connects with `Host=localhost`. Why are they different, and what makes the name `identity-db` resolvable from the API container?

The connection string is evaluated from the perspective of whoever is connecting:

- The local `psql` runs locally. From there, the database is reachable because compose published the port `5432:5432`
- The API runs inside a container. Inside that container, localhost means the API container itself. The two container instead share a Docker network that compose creates for the project, and Docke runrs an embedded DNS server on that network which resolves each service name to its contaier's IP.

5. A consumer API accepts token from Identity.Api without ever calling it at runtime. How is this possible? What would need to change if we switched from HMAC-SHA256 to RSA?

A JWT is self-contained. Signature verification is pure local math: any API takes `header.payload` from the incoming token, computes `HMACSHA256(header + "." + payload, sharedKey)` itself, and compares the result with the token's signature. If they match, the token was create by someone holding the key and it also checks `iss`, `aud` and `exp` from the payload locally. No network call, no session lookup. 
