using System.Text;
using Aniyum.Authentication;
using Aniyum.Services;
using Aniyum.Utils;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

ConfigureServices(builder);

var app = builder.Build();

ConfigureApp(app);

app.Run();

void ConfigureServices(WebApplicationBuilder builder)
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddControllers();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

    builder.Configuration.AddEnvironmentVariables();

    ConfigureAuthentication(builder);
    
    ServiceCaller.RegisterServices(builder.Services);
}

void ConfigureAuthentication(WebApplicationBuilder builder)
{
    var jwtSecret = AppSettingConfig.Configuration["JwtSettings:SecretKey"];
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = AppSettingConfig.Configuration["JwtSettings:Issuer"],
            ValidateAudience = true,
            ValidAudience = AppSettingConfig.Configuration["JwtSettings:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
}

void ConfigureApp(WebApplication app)
{
    app.UseSwagger(); 
    app.UseSwaggerUI();
    
    app.UseMiddleware<TokenAuthenticationHandler>();

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // Log environment variables for debugging
    var jwt = AppSettingConfig.Configuration["JwtSettings:SecretKey"];
    var mongo = AppSettingConfig.Configuration["MongoDBSettings:MongoDb"];
    Console.WriteLine($"JWT_SECRET: {jwt}, MONGO_CONNECTION_STRING: {mongo}");
}
