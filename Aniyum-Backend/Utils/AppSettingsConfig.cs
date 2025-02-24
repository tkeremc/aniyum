namespace Aniyum.Utils;

public class AppSettingConfig
{
    private static IConfiguration _configuration { get; set; }
    public static IConfiguration Configuration
    {
        get
        {
            if (_configuration == null)
            {
#if (DEBUG)
                var builder = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.Development.json", optional: true);
#else
                    var builder = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: true);

#endif
                _configuration = builder.Build();
            }

            return _configuration;
        }
    }
}