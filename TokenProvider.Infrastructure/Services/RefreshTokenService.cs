using Microsoft.EntityFrameworkCore;
using System.Net;
using TokenProvider.Infrastructure.Data.Contexts;
using TokenProvider.Infrastructure.Data.Entities;
using TokenProvider.Infrastructure.Models;

namespace TokenProvider.Infrastructure.Services;

public interface IRefreshTokenService
{
    Task<RefreshTokenResult> GetRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
    Task<bool> SaveRefreshTokenAsync(string refreshToken, string userId, CancellationToken cancellationToken);
}


public class RefreshTokenService : IRefreshTokenService
{
    private readonly IDbContextFactory<DataContext> _dbContextFactory;

    public RefreshTokenService(IDbContextFactory<DataContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    #region GetRefreshTokenAsync
    public async Task<RefreshTokenResult> GetRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        RefreshTokenResult refreshTokenResult = null!;
        var refreshTokenEntity = await context.RefreshTokens.FirstOrDefaultAsync(x => x.RefreshToken == refreshToken && x.ExpiryDate > DateTime.Now, cancellationToken);
        if (refreshTokenEntity != null)
        {
            refreshTokenResult = new RefreshTokenResult()
            {
                StatusCode = (int)HttpStatusCode.OK,
                Token = refreshTokenEntity.RefreshToken,
                ExpiryDate = refreshTokenEntity.ExpiryDate,
            };
        }
        else
        {
            refreshTokenResult = new RefreshTokenResult()
            {
                StatusCode = (int)HttpStatusCode.NotFound,
                Error = "RefreshToken not found or expired"
            };
        }

        return refreshTokenResult;

    }
    #endregion

    #region SaveRefreshTokenAsync
    public async Task<bool> SaveRefreshTokenAsync(string refreshToken, string userId, CancellationToken cancellationToken)
    {
        try
        {
            var tokenLF = double.TryParse(Environment.GetEnvironmentVariable("REFRESHTOKEN_LIFETIME"), out double refreshTokenLifeTime) ? refreshTokenLifeTime : 7;

            await using var context = _dbContextFactory.CreateDbContext();
            var refreshTokenEntity = new RefreshTokenEntity()
            {
                RefreshToken = refreshToken,
                UserId = userId,
                ExpiryDate = DateTime.Now.AddDays(tokenLF),
            };

            context.RefreshTokens.Add(refreshTokenEntity);
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }
    #endregion
}
