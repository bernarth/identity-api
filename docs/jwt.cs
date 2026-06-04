#!/usr/bin/env dotnet
#:package System.IdentityModel.Tokens.Jwt@8.*

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

const string issuer = "Identity.Api";
const string audience = "Movies.Api";
const string signingKey = "chapter-2-demo-signing-key-32-chars-minimum";

var now = DateTime.UtcNow;
var expires = now.AddMinutes(15);

var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

var claims = new ClaimsIdentity(
[
    new Claim(JwtRegisteredClaimNames.Sub, "user-123"),
    new Claim(JwtRegisteredClaimNames.Email, "student@example.com"),
    new Claim(ClaimTypes.Role, "User"),
    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
]);

var descriptor = new SecurityTokenDescriptor
{
    Subject = claims,
    Issuer = issuer,
    Audience = audience,
    IssuedAt = now,
    NotBefore = now,
    Expires = expires,
    SigningCredentials = credentials,
};

var handler = new JwtSecurityTokenHandler();
var token = handler.CreateToken(descriptor);
var rawToken = handler.WriteToken(token);

Console.WriteLine("Raw JWT:");
Console.WriteLine(rawToken);
Console.WriteLine();

var parts = rawToken.Split('.');
Console.WriteLine($"JWT parts: {parts.Length}");
Console.WriteLine("1. Header");
PrintBase64UrlJson(parts[0]);
Console.WriteLine();

Console.WriteLine("2. Payload");
PrintBase64UrlJson(parts[1]);
Console.WriteLine();

Console.WriteLine("3. Signature");
Console.WriteLine(parts[2]);
Console.WriteLine();

Console.WriteLine("Validation with the original key:");
Validate(rawToken, signingKey);
Console.WriteLine();

Console.WriteLine("Validation after changing the key by one character:");
var wrongKey = signingKey.Replace("minimum", "minimun");
Validate(rawToken, wrongKey);

static void PrintBase64UrlJson(string base64Url)
{
    var json = Encoding.UTF8.GetString(Base64UrlEncoder.DecodeBytes(base64Url));
    Console.WriteLine(json);
}

static void Validate(string rawToken, string key)
{
    var validationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        ClockSkew = TimeSpan.Zero,
    };

    try
    {
        var principal = new JwtSecurityTokenHandler().ValidateToken(
            rawToken,
            validationParameters,
            out var validatedToken);

        Console.WriteLine("Valid token.");
        Console.WriteLine($"Validated token type: {validatedToken.GetType().Name}");
        Console.WriteLine($"Subject: {FindClaimValue(principal, JwtRegisteredClaimNames.Sub, ClaimTypes.NameIdentifier)}");
        Console.WriteLine($"Role: {principal.FindFirst(ClaimTypes.Role)?.Value}");
    }
    catch (Exception exception)
    {
        Console.WriteLine("Invalid token.");
        Console.WriteLine($"{exception.GetType().Name}: {exception.Message}");
    }
}

static string? FindClaimValue(ClaimsPrincipal principal, params string[] claimTypes)
{
    foreach (var claimType in claimTypes)
    {
        var value = principal.FindFirst(claimType)?.Value;

        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }
    }

    return null;
}
