using Aniyum.Models;
using Microsoft.AspNetCore.Mvc;

namespace Aniyum.Interfaces;

public interface IUserService
{
    // Task<List<UserModel>> GetAll();
    // Task<UserModel> Get(string id);
    // Task<UserModel> Register(UserModel userModel);
    // Task<string> Login(string emailOrUsername, string password);
    // Task<UserModel> Update(string username, UserModel newUserModel);
    // Task Delete(string username);
    
    Task<string> GetEmail(string username, CancellationToken cancellationToken);
    Task<UserModel> Get(CancellationToken cancellationToken);
    Task<TokensModel> Login(string email, string password, CancellationToken cancellationToken);
    Task<UserModel> Register([FromBody] UserModel newUser, CancellationToken cancellationToken);
    Task<UserModel> Update([FromBody] UserModel updatedUserModel, CancellationToken cancellationToken);
    Task<TokensModel> RefreshToken(string refreshToken, CancellationToken cancellationToken);
}