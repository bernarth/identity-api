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

## Husky.Net setup reference

Husky.Net lets a .NET project use Git hooks without adding Node.js tooling. The goal is to keep project checks close to the repository: when someone commits, the repo can verify formatting and commit message rules automatically.

### 1. Install Husky.Net as a local dotnet tool

Run these commands from the repository root:

```bash
dotnet new tool-manifest
dotnet tool install Husky
```

This creates `.config/dotnet-tools.json`. This file should be committed because it pins the repo tooling.

Expected shape:

```json
{
  "version": 1,
  "isRoot": true,
  "tools": {
    "husky": {
      "version": "0.9.1",
      "commands": [
        "husky"
      ],
      "rollForward": false
    }
  }
}
```

After cloning the repo, another developer can restore the local tools with:

```bash
dotnet tool restore
```

### 2. Install Husky into the Git repository

Run:

```bash
dotnet husky install
```

This creates the `.husky/` folder and configures the local Git repository to use it as the hooks directory. Internally, it sets:

```bash
git config core.hooksPath .husky
```

That Git config value is local to each clone, so each developer needs it installed once in their clone.

### 3. Configure the task runner

Create or update `.husky/task-runner.json`:

```json
{
  "$schema": "https://alirezanet.github.io/Husky.Net/schema.json",
  "tasks": [
    {
      "name": "dotnet-format",
      "group": "pre-commit",
      "command": "dotnet",
      "args": ["format", "--verify-no-changes", "--include", "${staged}"],
      "include": ["**/*.cs"]
    },
    {
      "name": "commit-message-linter",
      "command": "dotnet",
      "args": ["husky", "exec", ".husky/csx/commit-lint.csx", "--args", "${args}"]
    }
  ]
}
```

Important parts:

- `group: "pre-commit"` means this task runs when the pre-commit hook runs.
- `--verify-no-changes` makes `dotnet format` fail instead of silently changing files.
- `${staged}` means Husky passes only the staged files to the command.
- `include: ["**/*.cs"]` means the format task only runs when C# files are staged.

### 4. Add the pre-commit hook

Run:

```bash
dotnet husky add pre-commit -c "dotnet husky run --group pre-commit"
```

Expected `.husky/pre-commit`:

```sh
#!/bin/sh
. "$(dirname "$0")/_/husky.sh"

dotnet husky run --group pre-commit
```

This hook runs before a commit is created. If `dotnet format --verify-no-changes` finds formatting issues, the hook exits with an error and Git stops the commit.

### 5. Add the commit message validator

Create `.husky/csx/commit-lint.csx`:

```csharp
using System.Text.RegularExpressions;

// Conventional Commits: <type>(optional scope)!: <subject>
private var pattern =
    @"^(?=.{1,90}$)(?:build|chore|ci|docs|feat|fix|perf|refactor|revert|style|test)(?:\(.+\))?!?: .{4,}(?<![\.\s])$";

private var msg = File.ReadAllLines(Args[0])[0];

if (Regex.IsMatch(msg, pattern))
    return 0;

Console.ForegroundColor = ConsoleColor.Red;
Console.WriteLine("Invalid commit message. Use Conventional Commits.");
Console.ResetColor();
Console.WriteLine();
Console.WriteLine("Format: <type>(optional-scope)!: <subject>");
Console.WriteLine("Types: build | chore | ci | docs | feat | fix | perf | refactor | revert | style | test");
Console.WriteLine();
Console.WriteLine("Examples:");
Console.WriteLine("  feat(auth): add refresh token rotation");
Console.WriteLine("  fix: reject expired JWT");
Console.WriteLine("  docs(readme): document setup");
Console.WriteLine();
Console.WriteLine($"You wrote: \"{msg}\"");
Console.WriteLine("More info: https://www.conventionalcommits.org/en/v1.0.0/");

return 1;
```

Then add the `commit-msg` hook:

```bash
dotnet husky add commit-msg -c "dotnet husky run --name commit-message-linter --args \"\$1\""
```

Expected `.husky/commit-msg`:

```sh
#!/bin/sh
. "$(dirname "$0")/_/husky.sh"

dotnet husky run --name commit-message-linter --args "$1"
```

Git passes the path of the temporary commit message file as `$1`. The hook forwards that path into the C# script, and the script validates the first line of the commit message.

### 6. Optional: auto-install Husky when the project restores

Because `core.hooksPath` is local to each clone, a fresh clone needs `dotnet husky install` once. To automate that, create `Directory.Build.targets` in the repository root:

```xml
<Project>
  <Target Name="RestoreDotnetToolsAndInstallHusky"
          BeforeTargets="Restore"
          Condition="'$(HUSKY)' != '0' And Exists('$(MSBuildThisFileDirectory).config/dotnet-tools.json')">
    <Exec Command="dotnet tool restore"
          WorkingDirectory="$(MSBuildThisFileDirectory)"
          StandardOutputImportance="Low"
          StandardErrorImportance="High" />

    <Exec Command="dotnet husky install"
          WorkingDirectory="$(MSBuildThisFileDirectory)"
          StandardOutputImportance="Low"
          StandardErrorImportance="High" />
  </Target>
</Project>
```

Now when someone runs a normal restore/build/run command, MSBuild restores the local tools and installs the Husky hooks for their clone:

```bash
dotnet restore
dotnet build
dotnet run --project Identity.Api
```

The hooks do not run when the project runs. Running/restoring the project only installs the hooks. The hooks run later when Git commands like `git commit` are used.

Use this escape hatch if needed:

```bash
HUSKY=0 dotnet restore
```

### 7. Files to commit

Commit these files:

```text
.config/dotnet-tools.json
.husky/task-runner.json
.husky/pre-commit
.husky/commit-msg
.husky/csx/commit-lint.csx
Directory.Build.targets
```

## Questions

1. What is the difference between `builder.Services` and `app` in `Program.cs`? When do you use each?

`builder.Services` is the preparation before opening for example in a restaurant, hiring staff, stocking the kitchen, setting up the menu, etc

`app` is the web application and the ordered chain of middleware that every incoming request follows. In a restaurant it would mean to have people seated, take orders, and devliver food.

Both run once when the app starts

2. Why is `appsettings.Development.json` in `.gitignore` but `appsettings.json` is not?

Because those are just for local development.
