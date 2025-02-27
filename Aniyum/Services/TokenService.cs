using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Aniyum_Backend.Models;
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
    
    public async Task<string> GenerateToken(UserModel user, CancellationToken cancellationToken)
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

    // public async Task<string?> GenerateRefreshToken(string userId)
    // {
    //     try
    //     {
    //         var exitedRefreshToken = await _tokenCollection.Find(x => x.UserId == userId).FirstOrDefaultAsync();
    //         if (exitedRefreshToken == null)
    //         {
    //             var randomNumber = new byte[32];
    //             using (var rng = RandomNumberGenerator.Create())
    //             {
    //                 rng.GetBytes(randomNumber);
    //             }
    //
    //             var refreshToken = new RefreshTokenModel
    //             {
    //                 UserId = userId,
    //                 RefreshToken = Convert.ToBase64String(randomNumber),
    //                 Expiration = DateTime.Now.AddMinutes(10),
    //                 CreatedAt = DateTime.Now
    //             };
    //             await _tokenCollection.InsertOneAsync(refreshToken);
    //             return refreshToken.RefreshToken.ToString();
    //         }
    //
    //         if (exitedRefreshToken.Expiration < DateTime.Now)
    //         {
    //             await _tokenCollection.DeleteOneAsync(x => x.Id == exitedRefreshToken.Id);
    //             throw new UnauthorizedAccessException("Refresh token expired");
    //         }
    //         var user = await _userCollection.Find(x => x.Id == userId).FirstOrDefaultAsync();
    //         if (user is null) throw new UnauthorizedAccessException("User does not exist.");
    //         return await GenerateToken(user);
    //     }
    //     catch (Exception e)
    //     {
    //         Console.WriteLine(e);
    //         throw;
    //     }
    // }
    
    public async Task<(string AccessToken, string RefreshToken)> GenerateRefreshToken(string userId,CancellationToken cancellationToken, 
        string? oldRefreshToken = null, bool isLogin = false)
{
    try
    {
        var existingTokens = await _tokenCollection
            .Find(x => x.UserId == userId)
            .SortByDescending(x => x.CreatedAt) // Yeni oluşturulan en üstte olsun
            .ToListAsync(cancellationToken: cancellationToken);

        
        
        // Kullanıcıyı doğrula
        var user = await _userCollection.Find(x => x.Id == userId).FirstOrDefaultAsync(cancellationToken: cancellationToken);
        if (user is null)
        {
            throw new UnauthorizedAccessException("User does not exist.");
        }
        
        if (!existingTokens.Any())
        {
            // Yeni bir access token oluştur
            var _newAccessToken = await GenerateToken(user, cancellationToken);

            // Yeni refresh token oluştur
            var _newRefreshToken = GenerateNewRefreshToken(userId);

            // MongoDB’ye yeni refresh token ekle
            await _tokenCollection.InsertOneAsync(_newRefreshToken, cancellationToken: cancellationToken);
            
            return (_newAccessToken, _newRefreshToken.RefreshToken);
        }

        if (isLogin && existingTokens[0].Expiration > DateTime.UtcNow)
            return
            (
                await GenerateToken(user, cancellationToken),
                existingTokens[0].RefreshToken
            )!;

        // Eğer en eski refresh token süresi dolmuşsa veya iptal edilmişse, silebiliriz
        if (existingTokens.Count > 0 && existingTokens[0].Expiration < DateTime.UtcNow)
        {
            if (!isLogin)
            {
                existingTokens[0].IsRevoked = true;
                await _tokenCollection.ReplaceOneAsync(x => x.Id == existingTokens[0].Id, existingTokens[0],
                    cancellationToken: cancellationToken);
                throw new UnauthorizedAccessException("Refresh token expired, please log in again.");
            }
        }

        

        // Yeni bir access token oluştur
        var newAccessToken = await GenerateToken(user, cancellationToken);

        // Yeni refresh token oluştur
        var newRefreshToken = GenerateNewRefreshToken(userId);

        // MongoDB’ye yeni refresh token ekle
        await _tokenCollection.InsertOneAsync(newRefreshToken, cancellationToken: cancellationToken);

        // Eski refresh tokenleri temizle (yalnızca en yeni 3 tanesi kalsın)
        await DeleteOldRefreshTokens(userId);

        return (newAccessToken, newRefreshToken.RefreshToken);
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        throw;
    }
}

private RefreshTokenModel GenerateNewRefreshToken(string userId)
{
    var randomNumber = new byte[32];
    using (var rng = RandomNumberGenerator.Create())
    {
        rng.GetBytes(randomNumber);
    }

    return new RefreshTokenModel
    {
        UserId = userId,
        RefreshToken = Convert.ToBase64String(randomNumber),
        Expiration = DateTime.UtcNow.AddDays(7), // Refresh token süresi 7 gün
        CreatedAt = DateTime.UtcNow,
        IsUsed = false,
        IsRevoked = false,
        Ip = currentUserService.GetIpAddress()
    };
}

private async Task DeleteOldRefreshTokens(string userId)
{
    var tokens = await _tokenCollection
        .Find(x => x.UserId == userId)
        .SortByDescending(x => x.CreatedAt) // En yeni refresh token'lar başta olsun
        .ToListAsync();
    
    if (tokens.Count > 3) // Eğer 3'ten fazla varsa, en eski olanları silelim
    {
        var tokensToDelete = tokens.Skip(3).Select(t => t.Id).ToList(); // 3. indexten sonrası silinecek
        await _tokenCollection.DeleteManyAsync(x => tokensToDelete.Contains(x.Id));
    }
}

}