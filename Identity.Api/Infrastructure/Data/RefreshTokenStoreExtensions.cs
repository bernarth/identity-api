using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Infrastructure.Data;

public static class RefreshTokenStoreExtensions
{
    public static Task<int> RevokeActiveRefreshTokensAsync(
        this ApplicationDbContext dbContext, string userId) =>
        dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(rt => rt.IsRevoked, true)
                .SetProperty(rt => rt.RevokedAt, DateTime.UtcNow));
}
