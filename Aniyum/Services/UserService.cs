using Aniyum.DbContext;
using Aniyum.Helpers;
using Aniyum.Interfaces;
using Aniyum.Models;
using Aniyum.Utils;
using MongoDB.Driver;

namespace Aniyum.Services;

public class UserService(IMongoDbContext mongoDbContext, ITokenService tokenService, ICurrentUserService currentUserService) : IUserService
{
    private readonly IMongoCollection<UserModel> _userCollection = mongoDbContext.GetCollection<UserModel>(AppSettingConfig.Configuration["MongoDBSettings:UserCollection"]!);
    private readonly IMongoCollection<RefreshTokenModel> _tokenCollection = mongoDbContext.GetCollection<RefreshTokenModel>(AppSettingConfig.Configuration["MongoDBSettings:TokenCollection"]!);
    public async Task<string> GetEmail(string username, CancellationToken cancellationToken)
    {
        try
        {
            var filter = Builders<UserModel>.Filter.Eq(x => x.Username, username);
            var email = await _userCollection.Find(filter).Project(x => x.Email)
                .SingleOrDefaultAsync(cancellationToken);
            if (string.IsNullOrEmpty(email)) throw new Exception("user not found");
            return email;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<UserModel> Get(CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userCollection.Find(x => x.Id == currentUserService.GetUserId())
                .FirstOrDefaultAsync(cancellationToken);
            if (user == null) throw new Exception("user not found");
            return user;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<TokensModel> Login(string email, string password, string deviceId, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userCollection.Find(x => x.Email == email).FirstOrDefaultAsync(cancellationToken);
            if (user == null) throw new Exception("user not found");
            if (!BCrypt.Net.BCrypt.Verify(password, user.HashedPassword)) throw new Exception("password is incorrect");
            
            var accessToken = await tokenService.GenerateAccessToken(user, cancellationToken);
            var refreshToken = await tokenService.GenerateRefreshToken(user.Id, deviceId, cancellationToken);
            
            var tokensModel = new TokensModel
            {
                RefreshToken = refreshToken.RefreshToken,
                AccessToken = accessToken
            };
            return tokensModel;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<UserModel> Register(UserModel newUser, CancellationToken cancellationToken)
    {
        try
        {
            var existingUser = await _userCollection.Find(x => x.Email == newUser.Email)
                .AnyAsync(cancellationToken);
            if (existingUser) throw new Exception("User already exists");
            newUser.IsActive = true;
            newUser.CreatedAt = DateTime.UtcNow;
            newUser.Roles ??= new List<string>();
            newUser.Roles.Add("user");
            newUser.HashedPassword = BCrypt.Net.BCrypt.HashPassword(newUser.HashedPassword,13);
            NullCheckHelper.Checker(newUser);
            await _userCollection.InsertOneAsync(newUser, cancellationToken);
            return newUser;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<UserModel> Update(UserModel updatedUserModel, CancellationToken cancellationToken)
    {
        try
        {
            var existingUser = await _userCollection.Find(x => x.Id == currentUserService.GetUserId())
                .FirstOrDefaultAsync(cancellationToken);
            if (existingUser == null) throw new Exception("user not found");
            updatedUserModel.UpdatedAt = DateTime.UtcNow;
            updatedUserModel = UpdateCheckHelper.Checker(existingUser, updatedUserModel);
            try
            {
                var result = await _userCollection.ReplaceOneAsync(u => u.Id == currentUserService.GetUserId(),
                    updatedUserModel, cancellationToken: cancellationToken);
                if (result.ModifiedCount == 0) throw new Exception("update failed");

            }
            catch (Exception e)
            {
                Console.WriteLine("DB error occured. " + e);
                throw;
            }
            return updatedUserModel;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<TokensModel> RefreshToken(string refreshToken, string deviceId, CancellationToken cancellationToken)
    {
        try
        {
            var tokens = await tokenService.RenewRefreshToken(refreshToken, deviceId, cancellationToken);
            return new TokensModel { RefreshToken = tokens.RefreshToken, AccessToken = tokens.AccessToken };
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}