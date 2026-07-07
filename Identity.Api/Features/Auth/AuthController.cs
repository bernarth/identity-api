using System.Security.Cryptography;
using System.Text;
using Identity.Api.Domain;
using Identity.Api.Features.Auth.Dtos;
using Identity.Api.Features.Tokens;
using Identity.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
}
