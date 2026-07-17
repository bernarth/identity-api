using System.Security.Claims;
using Identity.Api.Domain;
using Identity.Api.Features.Users.Dtos;
using Identity.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Api.Features.Users;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/users")]
public class UsersController(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    ApplicationDbContext dbContext) : ControllerBase
{
    [HttpPut("{id}/role")]
    public async Task<IActionResult> ChangeRoleAsync(string id, ChangeRoleRequest request)
    {
        if (IsCurrentUser(id))
        {
            return SelfManagementProblem();
        }

        ApplicationUser? user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        // AddToRoleAsync throws (not a failed IdentityResult) on a missing role
        if (!await roleManager.RoleExistsAsync(request.Role))
        {
            ModelState.AddModelError(nameof(request.Role), $"Role '{request.Role}' does not exist.");
            return ValidationProblem(ModelState);
        }

        IList<string> currentRoles = await userManager.GetRolesAsync(user);
        await userManager.RemoveFromRolesAsync(user, currentRoles);
        await userManager.AddToRoleAsync(user, request.Role);

        // unrevoked tokens would keep refreshing into JWTs carrying the stale roles
        await dbContext.RevokeActiveRefreshTokensAsync(user.Id);

        return NoContent();
    }

    [HttpPost("{id}/block")]
    public async Task<IActionResult> BlockAsync(string id)
    {
        if (IsCurrentUser(id))
        {
            return SelfManagementProblem();
        }

        ApplicationUser? user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

        // lockout only guards /login /refresh never sees a password
        await dbContext.RevokeActiveRefreshTokensAsync(user.Id);

        return NoContent();
    }

    [HttpPost("{id}/unblock")]
    public async Task<IActionResult> UnblockAsync(string id)
    {
        if (IsCurrentUser(id))
        {
            return SelfManagementProblem();
        }

        ApplicationUser? user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        await userManager.SetLockoutEndDateAsync(user, null);
        await userManager.ResetAccessFailedCountAsync(user);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(string id)
    {
        if (IsCurrentUser(id))
        {
            return SelfManagementProblem();
        }

        ApplicationUser? user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        IdentityResult result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            foreach (IdentityError error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }

            return ValidationProblem(ModelState);
        }

        return NoContent();
    }

    private bool IsCurrentUser(string id) =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) == id;

    private ObjectResult SelfManagementProblem() =>
        Problem(
            detail: "Administrators cannot modify their own account.",
            statusCode: StatusCodes.Status400BadRequest);
}
