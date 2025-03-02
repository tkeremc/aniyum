using Aniyum.Models;

namespace Aniyum.Interfaces;

public interface ITokenService
{
    Task<string> GenerateAccessToken(UserModel user, CancellationToken cancellationToken);
    Task<RefreshTokenModel> GenerateRefreshToken(string userId, CancellationToken cancellationToken);
    Task<TokensModel> RenewRefreshToken(string refreshToken, CancellationToken cancellationToken);
}