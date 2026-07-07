namespace Identity.Api.Features.Auth.Dtos;

public sealed record AuthResponse
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTime AccessTokenExpiresAt { get; init; }
    public string TokenType { get; init; } = "Bearer";
}
