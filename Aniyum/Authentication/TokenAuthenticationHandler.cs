using System.IdentityModel.Tokens.Jwt;

namespace Aniyum.Authentication;

public class TokenAuthenticationHandler(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

        if (token != null)
        {
            AttachUserToContext(context, token);
        }

        await next(context);
    }

    private void AttachUserToContext(HttpContext context, string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault() 
                            ?? context.Connection.RemoteIpAddress?.ToString();
            
            var userId = jwtToken.Claims.FirstOrDefault(x => x.Type == "user-id")?.Value;
            var username = jwtToken.Claims.FirstOrDefault(x => x.Type == "username")?.Value;
            var email = jwtToken.Claims.FirstOrDefault(x => x.Type == "email")?.Value;
            var roles = jwtToken.Claims.Where(x => x.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role").Select(x => x.Value).ToList();

            // Header'a claim bilgilerini ekleyelim
            context.Request.Headers["X-User-Id"] = userId;
            context.Request.Headers["X-Username"] = username;
            context.Request.Headers["X-Email"] = email;
            context.Request.Headers["X-Roles"] = string.Join(",", roles);
            context.Request.Headers["X-IP-Address"] = ipAddress;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}