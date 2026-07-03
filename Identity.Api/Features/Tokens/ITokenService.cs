using Identity.Api.Domain;

namespace Identity.Api.Features.Tokens;

public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user, IEnumerable<string> roles);
    string GenerateRefreshToken();
}
