using Aniyum.Authentication;
using Aniyum.Services;

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
    
    ServiceCaller.RegisterServices(builder.Services);
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
}
