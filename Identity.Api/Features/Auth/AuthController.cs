using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Identity.Api.Domain;
using Identity.Api.Features.Auth.Dtos;
using Identity.Api.Features.Tokens;
using Identity.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace Identity.Api.Features.Auth;

[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ITokenService tokenService,
    ApplicationDbContext dbContext,
    IOptions<JwtOptions> jwtOptions) : ControllerBase
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    [HttpPost("register")]
    public async Task<IActionResult> RegisterAsync(RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CreatedAt = DateTime.UtcNow,
        };

        IdentityResult result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            foreach (IdentityError error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }

            return ValidationProblem(ModelState);
        }

        // the user role must already exist for now
        await userManager.AddToRoleAsync(user, "User");

        return Created();
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync(LoginRequest request)
    {
        ApplicationUser? user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Unauthorized();
        }

        SignInResult signIn = await signInManager.CheckPasswordSignInAsync(
            user, request.Password, lockoutOnFailure: true);
        if (!signIn.Succeeded)
        {
            return Unauthorized();
        }

        AuthResponse response = await IssueTokenPairAsync(user);

        return Ok(response);
    }

    private async Task<AuthResponse> IssueTokenPairAsync(ApplicationUser user)
    {
        IList<string> roles = await userManager.GetRolesAsync(user);
        string accessToken = tokenService.GenerateAccessToken(user, roles);
        string refreshToken = tokenService.GenerateRefreshToken();

        DateTime now = DateTime.UtcNow;
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Token = HashToken(refreshToken),
            UserId = user.Id,
            User = user,
            CreatedAt = now,
            ExpiresAt = now.AddDays(_jwt.RefreshTokenExpiryDays),
        });
        await dbContext.SaveChangesAsync();

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = now.AddMinutes(_jwt.AccessTokenExpiryMinutes),
        };
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshAsync(RefreshTokenRequest request)
    {
        string hashedToken = HashToken(request.RefreshToken);

        RefreshToken? storedToken = await dbContext.RefreshTokens
            .Include(rt => rt.User)
            .SingleOrDefaultAsync(rt => rt.Token == hashedToken);

        if (storedToken is null)
        {
            return Unauthorized();
        }

        if (storedToken.IsRevoked)
        {
            // reuse of a rotated token so, we assume theft and kill all sessions
            await dbContext.RefreshTokens
                .Where(rt => rt.UserId == storedToken.UserId && !rt.IsRevoked)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(rt => rt.IsRevoked, true)
                    .SetProperty(rt => rt.RevokedAt, DateTime.UtcNow));

            return Unauthorized();
        }

        if (storedToken.ExpiresAt <= DateTime.UtcNow)
        {
            return Unauthorized();
        }

        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;

        AuthResponse response = await IssueTokenPairAsync(storedToken.User);

        storedToken.ReplacedByToken = HashToken(response.RefreshToken);
        await dbContext.SaveChangesAsync();

        return Ok(response);
    }

    [Authorize]
    [HttpPost("revoke")]
    public async Task<IActionResult> RevokeAsync(RevokeTokenRequest request)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        string hashedToken = HashToken(request.RefreshToken);

        RefreshToken? storedToken = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(rt => rt.Token == hashedToken && rt.UserId == userId);

        if (storedToken is null || storedToken.IsRevoked)
        {
            return Unauthorized();
        }

        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        return NoContent();
    }
}
