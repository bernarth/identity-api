using System.ComponentModel.DataAnnotations;

namespace Identity.Api.Features.Auth.Dtos;

public sealed record LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}
