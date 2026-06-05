using Microsoft.AspNetCore.Identity;

namespace Identity.Api.Domain;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public required DateTime CreatedAt { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
