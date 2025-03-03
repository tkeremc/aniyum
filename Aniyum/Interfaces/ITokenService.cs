using Aniyum.Models;

namespace Aniyum.Interfaces;

public interface ITokenService
{
    Task<string> GenerateAccessToken(UserModel user, CancellationToken cancellationToken);
    Task<RefreshTokenModel> GenerateRefreshToken(string userId, string deviceId, CancellationToken cancellationToken);
    Task<TokensModel> RenewRefreshToken(string refreshToken, string deviceId, CancellationToken cancellationToken);
}