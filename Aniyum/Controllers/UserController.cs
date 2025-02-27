using Aniyum_Backend.Models;
using Aniyum.Interfaces;
using Aniyum.Models;
using Aniyum.ViewModels;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aniyum.Controllers;
[ApiController]
[Route("user")]
public class UserController(IUserService userService, IMapper mapper, ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet("get-email")]
    public async Task<ActionResult<string>> GetEmail(string username, CancellationToken cancellationToken)
    {
        var email = await userService.GetEmail(username, cancellationToken);
        return StatusCode(StatusCodes.Status200OK, email);
    }

    [HttpPost("login")]
    public async Task<ActionResult<TokensModel>> Login(string email, string password, CancellationToken cancellationToken)
    {
        var userTokens = await userService.Login(email, password, cancellationToken);
        return StatusCode(StatusCodes.Status200OK, userTokens);
    }

    [HttpPost("register")]
    public async Task<ActionResult<UserViewModel>> Register([FromBody] UserCreateViewModel newUserViewModel, CancellationToken cancellationToken)
    {
        var userModel = mapper.Map<UserModel>(newUserViewModel);
        var newUserModel = await userService.Register(userModel, cancellationToken);
        var userViewModel = mapper.Map<UserViewModel>(newUserModel);
        return StatusCode(StatusCodes.Status200OK, userViewModel);
    }

    [HttpPut("update")]
    [Authorize]
    public async Task<ActionResult<UserViewModel>> Update([FromBody] UserUpdateViewModel userUpdateViewModel,
        CancellationToken cancellationToken)
    {
        var userModel = mapper.Map<UserModel>(userUpdateViewModel);
        var updatedUserModel = await userService.Update(userModel, cancellationToken);
        var userViewModel = mapper.Map<UserViewModel>(updatedUserModel);
        return StatusCode(StatusCodes.Status200OK, userViewModel);
    }
    
    [HttpPost("refresh-token")]
    public async Task<ActionResult<TokensModel>> RefreshToken(string refreshToken, CancellationToken cancellationToken)
    {
        var userTokens = await userService.RefreshToken(refreshToken, cancellationToken);
        return StatusCode(StatusCodes.Status200OK, userTokens);
    }

    [Authorize]
    [HttpGet("test")]
    public async Task<IActionResult> Test()
    {
        var list = new
        {
            id = currentUserService.GetUserId(),
            username = currentUserService.GetUsername(),
            email = currentUserService.GetEmail(),
            roles = currentUserService.GetRoles(),
            ipAddress = currentUserService.GetIpAddress()
        };
        return Ok(list);
    }
}