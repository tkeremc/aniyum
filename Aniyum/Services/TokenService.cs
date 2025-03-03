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
    
    public async Task<RefreshTokenModel> GenerateRefreshToken(string userId, string deviceId, CancellationToken cancellationToken)
    {
        try
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
        
            var refreshToken = new RefreshTokenModel
            {
                UserId = userId,
                RefreshToken = Convert.ToBase64String(randomNumber),
                Expiration = DateTime.UtcNow.AddDays(7), // 7 gün geçerli olacak
                CreatedAt = DateTime.UtcNow,
                IsUsed = false,
                IsRevoked = false,
                Ip = currentUserService.GetIpAddress(),
                DeviceId = deviceId // ✅ Cihaz ID ekleniyor
            };

            await RevokeAllUserRefreshTokens(userId, deviceId , cancellationToken);
            await _tokenCollection.InsertOneAsync(refreshToken, cancellationToken: cancellationToken);
        
            // Aynı cihaz için eski tokenleri temizle (Son 3 tokeni sakla)
            await DeleteOldDeviceTokens(userId, deviceId, cancellationToken);

            return refreshToken;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }


    public async Task<TokensModel> RenewRefreshToken(string refreshToken, string deviceId, CancellationToken cancellationToken)
    {
        try
        {
            var existingToken = await _tokenCollection
                .Find(x => x.RefreshToken == refreshToken && x.DeviceId == deviceId)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingToken == null)
                throw new Exception("Refresh token bulunamadı veya cihaz eşleşmiyor.");

            if (existingToken.Expiration < DateTime.UtcNow)
            {
                await _tokenCollection.UpdateOneAsync(
                    x => x.Id == existingToken.Id,
                    Builders<RefreshTokenModel>.Update.Set(x => x.IsRevoked, true),
                    cancellationToken: cancellationToken
                );
                throw new Exception("Refresh token süresi dolmuş");
            }

            if (existingToken.IsUsed || existingToken.IsRevoked)
                throw new Exception("Bu refresh token zaten kullanılmış veya iptal edilmiş");

            var user = await _userCollection.Find(x => x.Id == existingToken.UserId)
                .FirstOrDefaultAsync(cancellationToken);

            if (user == null)
                throw new Exception("Kullanıcı bulunamadı");

            var newAccessToken = await GenerateAccessToken(user, cancellationToken);
            var newRefreshToken = await GenerateRefreshToken(user.Id, deviceId, cancellationToken); // ✅ Cihaz ID ile yeni refresh token oluşturuluyor

            await _tokenCollection.UpdateOneAsync(
                x => x.Id == existingToken.Id,
                Builders<RefreshTokenModel>.Update.Set(x => x.IsUsed, true),
                cancellationToken: cancellationToken
            );

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
    private async Task RevokeAllUserRefreshTokens(string userId, string deviceId, CancellationToken cancellationToken)
    {
        try
        {
            var update = Builders<RefreshTokenModel>.Update
                .Set(x => x.IsRevoked, true);

            var result = await _tokenCollection.UpdateManyAsync(
                x => (x.UserId == userId && x.DeviceId == deviceId),
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
    
    private async Task DeleteOldDeviceTokens(string userId, string deviceId, CancellationToken cancellationToken)
    {
        var tokens = await _tokenCollection
            .Find(x => x.UserId == userId && x.DeviceId == deviceId)
            .SortByDescending(x => x.CreatedAt) // En yeni tokenler başta olacak
            .ToListAsync(cancellationToken: cancellationToken);

        if (tokens.Count > 3) // Eğer 3'ten fazla varsa, en eski olanları silelim
        {
            var tokensToDelete = tokens.Skip(3).Select(t => t.Id).ToList(); // 3. indexten sonrası silinecek
            await _tokenCollection.DeleteManyAsync(x => tokensToDelete.Contains(x.Id), cancellationToken: cancellationToken);
        }
    }
}