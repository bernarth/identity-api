namespace Identity.Api.Features.Users.Dtos;

public sealed record ChangeRoleRequest
{
    public required string Role { get; init; }
}
