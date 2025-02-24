using Newtonsoft.Json.Linq;

namespace Aniyum_Backend.Middleware;

public class LocationMiddleware(RequestDelegate next)
{
    private readonly HttpClient _httpClient = new();

    public async Task Invoke(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString();
        if (string.IsNullOrEmpty(ip))
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Erişim engellendi.");
            return;
        }

        var geoLocationApiUrl = $"http://ip-api.com/json/{ip}?fields=countryCode";
        var response = await _httpClient.GetStringAsync(geoLocationApiUrl);
        var json = JObject.Parse(response);
        var countryCode = json["countryCode"]?.ToString();

        if (countryCode != "TR")
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Your country is not allowed.");
            return;
        }

        await next(context);
    }
}