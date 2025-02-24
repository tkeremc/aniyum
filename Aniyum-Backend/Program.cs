using System.Text;
using System.Threading.RateLimiting;
using Aniyum_Backend.Authentication;
using Aniyum_Backend.Middleware;
using Aniyum_Backend.Services;
using Aniyum.Utils;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey =
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_SECRET")!)),
        ValidateIssuer = true,
        ValidIssuer = AppSettingConfig.Configuration["JwtSettings:Issuer"],
        ValidateAudience = true,
        ValidAudience = AppSettingConfig.Configuration["JwtSettings:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("IpBasedPolicy", httpContext =>
    {
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ipAddress, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10, // Dakikada en fazla 10 istek
            Window = TimeSpan.FromMinutes(1)
        });
    });
});


ServiceCaller.RegisterServices(builder.Services);

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();




app.UseRateLimiter();
app.UseMiddleware<TokenAuthenticationHandler>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseHttpsRedirection();
if (app.Environment.IsProduction())
{
    app.UseMiddleware<LocationMiddleware>();
}
app.Run();