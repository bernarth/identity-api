namespace Identity.Api.Features.Auth.Dtos;

public sealed record RevokeTokenRequest
{
    public required string RefreshToken { get; init; }
}
