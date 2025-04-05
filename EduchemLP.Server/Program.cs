using dotenv.net;
using EduchemLP.Server.Middlewares;
using EduchemLP.Server.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using StackExchange.Redis;

namespace EduchemLP.Server;



public static class Program {

    public static WebApplication App { get; private set; } = null!;
    public static IDictionary<string, string> ENV { get; private set; } = null!;
    public static ILogger Logger => App.Logger;



    // random picovinky
    public const string APP_WALLPAPER = "/images/wallpapers/xmas_day.png";
    public const string ROOT_DOMAIN = "lanparty.educhem.it";//"lanparty.adminsphere.me";

    #if DEBUG
        public const bool DEVELOPMENT_MODE = true;
    #elif RELEASE
        public const bool DEVELOPMENT_MODE = false;
    #elif TESTING
        public const bool DEVELOPMENT_MODE = true;
    #endif




    public static void Main(string[] args) {
        ENV = DotEnv.Read();
        var builder = WebApplication.CreateBuilder(args);


        // pripojeni k redisu
        string? rhost, rport, rpassword;
        if (!DEVELOPMENT_MODE) {
            rhost = ENV["DATABASE_IP"];
            rport = "6379";
            rpassword = null;
        } else {
            rhost = ENV["DATABASE_IP"];
            rport = ENV["REDIS_PORT"];
            rpassword = ENV["REDIS_PASSWORD"];
        }

        var config = new ConfigurationOptions {
            EndPoints = { $"{rhost}:{rport}" },
            AbortOnConnectFail = false
        };

        if (rpassword != null!) {
            config.Password = rpassword;
        }

        var redis = ConnectionMultiplexer.Connect(config);
        builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

        builder.Services.AddControllersWithViews();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddRazorPages();
        builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        builder.Services.AddDataProtection()
            .PersistKeysToStackExchangeRedis(redis, "DataProtection-Keys")
            .SetApplicationName("EduchemLP");

        builder.Services.AddSingleton<IDistributedCache>(sp =>
            new RedisCache(new RedisCacheOptions {
                ConfigurationOptions = ConfigurationOptions.Parse(redis.Configuration),
                InstanceName = "EduchemLP_session"
            })
        );

        builder.Services.AddSession(options => {
            options.IdleTimeout = TimeSpan.FromDays(365);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.MaxAge = TimeSpan.FromDays(365);
            options.Cookie.Name = "educhemlp_session";
        });

        builder.Configuration
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        #if DEBUG
            builder.Configuration.AddJsonFile("appsettings.Debug.json", optional: true, reloadOnChange: true);
        #elif RELEASE
            builder.Configuration.AddJsonFile("appsettings.Release.json", optional: true, reloadOnChange: true);
        #elif TESTING
            builder.Configuration.AddJsonFile("appsettings.Testing.json", optional: true, reloadOnChange: true);
        #endif

        builder.Configuration.AddEnvironmentVariables();

        App = builder.Build();

        var httpContextAccessor = App.Services.GetRequiredService<IHttpContextAccessor>();
        HttpContextService.Configure(httpContextAccessor);

        if (!App.Environment.IsDevelopment()) {
            App.UseExceptionHandler("/error");
            App.UseStatusCodePagesWithReExecute("/error/{0}");
            App.UseHsts();
        }

        //App.UseHttpsRedirection();
        //App.UseStaticFiles();
        App.UseSession();
        App.UseMiddleware<BeforeInitMiddleware>();
        App.UseWebSockets(new WebSocketOptions {
            KeepAliveInterval = TimeSpan.FromSeconds(30),
        });
        App.UseMiddleware<WebSocketMiddleware>();
        App.UseRouting();
        //App.UseAuthorization();
        //App.UseMiddleware<ErrorHandlingMiddleware>();
        App.UseMiddleware<FunctionalQueryParameterMiddleware>();
        App.MapControllerRoute(name: "default", pattern: "/");

        App.Run();
    }
}