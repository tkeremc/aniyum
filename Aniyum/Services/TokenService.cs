using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Aniyum.DbContext;
using Aniyum.Interfaces;
using Aniyum.Models;
using Aniyum.Utils;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;

namespace Aniyum.Services;

public class TokenService(IMongoDbContext mongoDbContext, ICurrentUserService currentUserService) : ITokenService
{
    private readonly IMongoCollection<RefreshTokenModel> _tokenCollection = mongoDbContext.GetCollection<RefreshTokenModel>(AppSettingConfig.Configuration["MongoDBSettings:TokenCollection"]!);
    private readonly IMongoCollection<UserModel> _userCollection = mongoDbContext.GetCollection<UserModel>(AppSettingConfig.Configuration["MongoDBSettings:UserCollection"]!);
    
    public async Task<string> GenerateAccessToken(UserModel user, CancellationToken cancellationToken)
    {
        var secretKey = AppSettingConfig.Configuration["JwtSettings:SecretKey"];
        var audience = AppSettingConfig.Configuration["JwtSettings:Audience"];
        var issuer = AppSettingConfig.Configuration["JwtSettings:Issuer"];
        
        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)), SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new ("user-id", user.Id),
            new ("name", user.FullName),
            new ("username", user.Username),
            new ("email", user.Email),
            new ("is-active", user.IsActive.ToString()),
            new (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.Now.AddMinutes(30),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
    public async Task<RefreshTokenModel> GenerateRefreshToken(string userId, CancellationToken cancellationToken)
    {
        try
        {
            var randomNumber = new byte[32];
            var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            var refreshToken = new RefreshTokenModel
            {
                UserId = userId,
                RefreshToken = Convert.ToBase64String(randomNumber),
                Expiration = DateTime.UtcNow.AddSeconds(30),
                CreatedAt = DateTime.UtcNow,
                IsUsed = false,
                IsRevoked = false,
                Ip = currentUserService.GetIpAddress()
            };
            await RevokeAllUserRefreshTokens(userId, cancellationToken);
            await _tokenCollection.InsertOneAsync(refreshToken, cancellationToken: cancellationToken);
            await DeleteOldRefreshTokens(userId, cancellationToken);
            return refreshToken;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<TokensModel> RenewRefreshToken(string refreshToken, CancellationToken cancellationToken)
{
    try
    {
        // Verilen refresh token ile eşleşen kullanıcıyı bul
        var existingToken = await _tokenCollection.Find(x => x.RefreshToken == refreshToken)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingToken == null)
            throw new Exception("Refresh token bulunamadı");

        // Token süresi dolmuş mu?
        if (existingToken.Expiration < DateTime.UtcNow)
        {
            await _tokenCollection.UpdateOneAsync(
                x => x.Id == existingToken.Id,
                Builders<RefreshTokenModel>.Update.Set(x => x.IsRevoked, true),
                cancellationToken: cancellationToken
            );
            throw new Exception("Refresh token süresi dolmuş");
        }

        // Token zaten kullanılmış mı veya iptal edilmiş mi?
        if (existingToken.IsUsed || existingToken.IsRevoked)
            throw new Exception("Bu refresh token zaten kullanılmış veya iptal edilmiş");

        // Kullanıcıyı veritabanında ara
        var user = await _userCollection.Find(x => x.Id == existingToken.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
            throw new Exception("Kullanıcı bulunamadı");

        // Yeni Access Token ve Refresh Token oluştur
        var newAccessToken = await GenerateAccessToken(user, cancellationToken);
        var newRefreshToken = await GenerateRefreshToken(user.Id, cancellationToken);


        // Kullanılan refresh tokeni güncelle (Artık kullanıldı ve tekrar kullanılamaz)
        await _tokenCollection.UpdateOneAsync(
            x => x.Id == existingToken.Id,
            Builders<RefreshTokenModel>.Update
                .Set(x => x.IsUsed, true),
            cancellationToken: cancellationToken
        );

        // Eski refresh tokenleri temizle (Son 3 token hariç)
        await DeleteOldRefreshTokens(user.Id, cancellationToken);

        return new TokensModel
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken.RefreshToken
        };
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        throw;
    }
}

    
    private async Task DeleteOldRefreshTokens(string userId, CancellationToken cancellationToken)
    {
        var tokens = await _tokenCollection
            .Find(x => x.UserId == userId)
            .SortByDescending(x => x.CreatedAt) // En yeni refresh token'lar başta olsun
            .ToListAsync();
        
        if (tokens.Count > 3) // Eğer 3'ten fazla varsa, en eski olanları silelim
        {
            var tokensToDelete = tokens.Skip(3).Select(t => t.Id).ToList(); // 3. indexten sonrası silinecek
            await _tokenCollection.DeleteManyAsync(x => tokensToDelete.Contains(x.Id), cancellationToken: cancellationToken);
        }
    }
    
    private async Task RevokeAllUserRefreshTokens(string userId, CancellationToken cancellationToken)
    {
        try
        {
            var update = Builders<RefreshTokenModel>.Update
                .Set(x => x.IsRevoked, true);

            var result = await _tokenCollection.UpdateManyAsync(
                x => x.UserId == userId,
                update,
                cancellationToken: cancellationToken
            );
        }
        catch (Exception e)
        {
            Console.WriteLine($"Refresh tokenleri iptal etme hatası: {e.Message}");
            throw;
        }
    }

}