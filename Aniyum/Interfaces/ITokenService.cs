using Aniyum_Backend.Models;
using Aniyum.Models;

namespace Aniyum.Interfaces;

public interface ITokenService
{
    Task<string> GenerateToken(UserModel user, CancellationToken cancellationToken);
    Task<(string AccessToken, string RefreshToken)> GenerateRefreshToken(string userId, CancellationToken cancellationToken, string? oldRefreshToken = null, bool isLogin = false);
}