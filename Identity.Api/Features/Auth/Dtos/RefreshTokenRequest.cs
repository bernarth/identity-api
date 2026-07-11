namespace Identity.Api.Features.Auth.Dtos;

public sealed record RefreshTokenRequest
{
    public required string RefreshToken { get; init; }
}
