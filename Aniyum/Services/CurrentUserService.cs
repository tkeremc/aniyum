using Aniyum.Interfaces;

namespace Aniyum.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public string GetUserId()
    {
        var userId = httpContextAccessor.HttpContext?.Request.Headers["X-User-Id"];
        return userId;
    }

    public string GetUsername()
    {
        var username = httpContextAccessor.HttpContext?.Request.Headers["X-Username"];
        return username;
    }

    public string GetEmail()
    {
        var email = httpContextAccessor.HttpContext?.Request.Headers["X-Email"];
        return email;
    }

    public List<string> GetRoles()
    {
        var rolesHeader = httpContextAccessor.HttpContext?.Request.Headers["X-Roles"].ToString();
        return string.IsNullOrEmpty(rolesHeader) ? new List<string>() : rolesHeader.Split(',').ToList();
    }
    
    public string GetIpAddress()
    {
        var ipAddress = httpContextAccessor.HttpContext?.Request.Headers["X-IP-Address"];
        return ipAddress;
    }

    public string GetDeviceId()
    {
        var deviceId = httpContextAccessor.HttpContext?.Request.Headers["X-Device-Id"];
        return deviceId;
    }
}