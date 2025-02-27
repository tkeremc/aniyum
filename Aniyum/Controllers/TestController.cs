using Aniyum.Interfaces;
using Aniyum.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aniyum.Controllers;

public class TestController(ICurrentUserService currentUserService): ControllerBase
{
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
    
    [HttpGet("appsetting-test")]
    public async Task<string> AppSettingTest()
    {
        
        var jwt = AppSettingConfig.Configuration["JwtSettings:SecretKey"];
        var mongo = AppSettingConfig.Configuration["MongoDBSettings:MongoDb"];
        return ($"JWT_SECRET: {jwt}, MONGO_CONNECTION_STRING: {mongo}");
    }
}