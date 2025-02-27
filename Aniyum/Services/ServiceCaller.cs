using Aniyum.DbContext;
using Aniyum.Interfaces;
using Microsoft.OpenApi.Models;

namespace Aniyum.Services;

public sealed class ServiceCaller
{
    public static void RegisterServices(IServiceCollection services)
    {
        SingletonServices(services);
        ScopedServices(services);
        // HostServices(services);
        SwaggerSettings(services);
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

    private static void SwaggerSettings(IServiceCollection services)
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
    }
}