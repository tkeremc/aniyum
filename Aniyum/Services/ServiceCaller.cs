using System.Text;
using Aniyum.DbContext;
using Aniyum.Interfaces;
using Aniyum.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace Aniyum.Services;

public sealed class ServiceCaller
{
    public static void RegisterServices(IServiceCollection services)
    {
        SingletonServices(services);
        ScopedServices(services);
        // HostServices(services);
        StartSettings(services);
    }

    private static void ScopedServices(IServiceCollection services)
    {
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>(); 
    }

    private static void SingletonServices(IServiceCollection services)
    {
        services.AddSingleton<IMongoDbContext, MongoDbContext>();
    }

    // private static void HostServices(IServiceCollection services)
    // {
    //     throw new NotImplementedException();
    // }

    private static void StartSettings(IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new() { Title = "Aniyum API", Version = "v1" });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Tokenınızı girerken başına 'Bearer ' eklemeyi unutmayın. Örnek: 'Bearer eyJhbGc...' "
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference 
                        { 
                            Type = ReferenceType.SecurityScheme, 
                            Id = "Bearer" 
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
        
        services.AddAuthentication(options =>
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
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AppSettingConfig.Configuration["JwtSettings:SecretKey"]!)),
                ValidateIssuer = true,
                ValidIssuer = AppSettingConfig.Configuration["JwtSettings:Issuer"],
                ValidateAudience = true,
                ValidAudience = AppSettingConfig.Configuration["JwtSettings:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });
    }
}