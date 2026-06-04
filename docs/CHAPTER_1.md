# Chapter 1

The project was created on sdk `10.0.300`. Se the commands that run:

```bash
# Create the solution
dotnet new sln -n AuthProject
# Create the webapi project
dotnet new webapi -n Identity.Api
# Add the project to the solution
dotnet sln add Identity.Api/Identity.Api.csproj
# run the project
dotnet run --project Identity.Api
# Create the xunit project
dotnet new xunit -n Identity.Tests
# Add the project to the solution
dotnet sln add Identity.Tests/Identity.Tests.csproj
# Add the required libraries to the Identity.Api project
cd Identity.Api
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Microsoft.IdentityModel.Tokens
```

With all of these I was able to setup the project. I also added the `.gitignore` and the `.editorconfig`.

## Packages

`Microsoft.AspNetCore.Identity.EntityFrameworkCore`: Provides an identity system integrated with Entity Framework Core. It is a bridge between Identity logic and EF Core for persistance

`Microsoft.EntityFrameworkCore.Design`: A design package for EF Core tooling it is important to run migrations with a command like `dotnet ef migrations add` or `dotnet ef database update`. It is not used at runtime so, it should be out of the published version.

`Npgsql.EntityFrameworkCore.PostgreSQL`: The EF Core database provider for PosgreSQL it is the ORM

`Microsoft.AspNetCore.Authentication.JwtBearer`: Adds JWT Bearer authentication middleware to the project. It intercepts and validates the token.

`Microsoft.IdentityModel.Tokens`: The JWT Bearer depends on it. It is a library that provdes primitives for creating, validating, and parsing security tokens. It is what defines `TokenValidationParameters`, `SigningCredentials`, `SecurityKey`.

All these packages work like the following:

```
Request ->
  JwtBearer middleware -> (validates token using) ->
    Identity Model Tokens -> (key, issuer, expiry checks) -> user looked up via ->
      Identity + EF Core -> Identity EF Core -> Persisted to ->
        PosgreSQL -> (using EF Core)
```

## Questions

1. What is the difference between `builder.Services` and `app` in `Program.cs`? When do you use each?

`builder.Services` is the preparation before opening for example in a restaurant, hiring staff, stocking the kitchen, setting up the menu, etc

`app` is the web application and the ordered chain of middleware that every incoming request follows. In a restaurant it would mean to have people seated, take orders, and devliver food.

Both run once when the app starts

2. Why is `appsettings.Development.json` in `.gitignore` but `appsettings.json` is not?

Because those are just for local development.
