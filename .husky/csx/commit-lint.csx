using System.Text.RegularExpressions;

// Conventional Commits: <type>(optional scope)!: <subject>
//   - type e the allowed set below
//   - optional (scope)
//   - optional ! for breaking change
//   - ": " then a subject of at least 4 chars
//   - whole header capped at ~90 chars, no trailing space/period
private var pattern =
    @"^(?=.{1,90}$)(?:build|chore|ci|docs|feat|fix|perf|refactor|revert|style|test)(?:\(.+\))?!?: .{4,}(?<![\.\s])$";

private var msg = File.ReadAllLines(Args[0])[0];

if (Regex.IsMatch(msg, pattern))
    return 0;

Console.ForegroundColor = ConsoleColor.Red;
Console.WriteLine("✗ Invalid commit message — must follow Conventional Commits.");
Console.ResetColor();
Console.WriteLine();
Console.WriteLine("  Format:  <type>(optional-scope)!: <subject>");
Console.WriteLine("  Types:   build | chore | ci | docs | feat | fix | perf | refactor | revert | style | test");
Console.WriteLine();
Console.WriteLine("  Good:    feat(auth): add refresh token rotation");
Console.WriteLine("           fix: reject expired JWT");
Console.WriteLine("           docs(readme): document CORS policy");
Console.WriteLine();
Console.WriteLine($"  You wrote: \"{msg}\"");
Console.WriteLine("  More info: https://www.conventionalcommits.org/en/v1.0.0/");

return 1;
