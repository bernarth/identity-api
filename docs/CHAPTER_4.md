# Chapter 4

In this chapter I run the following commands at `Identity.Api` directory:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

When running the second command there was an error that wasn't an error. Here's what happened:

- EF first queries __EFMigrationHistory to see which migrations have already run
- That table doesn't exist yet, so the query fails
- EF catches that, recognises it's a fresh DB, and proceeds to create everything from scratch
- The migration was applied

## Questions

1. What is the purpose of a database migration ? What problem does it solve compared to writting SQL by hand?

    - The purpose of a migration is to keep track of the database changes just like in git and to keep consistency specially in production
    - When writting SQL by hand it would cause inconsistencies within a team
    - Migrations also solve the problem of _reproductibility_ . Any developer can run `database update` and get an identical schema without manual steps

2. What does EF Core's `Up()` method do, and what does `Down()` do?

    When EF generates a migration file, it produces two methods:
    
    `Up()` describes how to advance the schema. It's the apply direction. For `InitialCreate` that means `CREATE TABLE` for every table. For a later migration adding a column it would be `ALTER TABLE ... ADD COLUMN`
    `Down()` is the exact inverse. It undoes what `Up()` did. For `InitialCreate` that means `DROP TABLE` for everything. You run it with:
        
        `dotnet ef database update <previous-migration-name>`
        
    This is how you roll back a migration in dev without touching the DB manually.

3. If two developers on the same team each add a migration independently, what problem can occur?

    The collision problem is when both developers base their migrations on the same previous migration snapshot. EF uses a model snpashot. EF uses a model snapshot file to diff against. When both add a migration independently, their migrations have the same 'parent' merging them creates a conflict in that snapshot file. The second migration may also try to create something that already exists. The fix is to coordinate: one person's migration goes in first, the other regenerates theirs on top of it.

4. Why did we add an index on `RefreshTokens.Token`? What would happen at scale without it?

    For a table with millions of refresh tokens (one per login session per user), that becomes slow. The token lookup happens on every /refresh request, so this is a hot path. An index makes it O(log n) instead of O(n)

5. What is the difference between `dotnet ef database update` and `dotnet ef migrations add`?

    - `dotnet ef migrations add` generates the migration files (the `Up()/Down()` C# Code). Nothing touches the DB
    - `dotnet ef database update` reads those files and executes the SQL against the actual database
