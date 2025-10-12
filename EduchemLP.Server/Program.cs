using dotenv.net;
using EduchemLP.Server.Classes;
using EduchemLP.Server.Middlewares;
using EduchemLP.Server.Repositories;
using EduchemLP.Server.Services;
using EduchemLP.Server.WebSockets;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using MySqlConnector;
using StackExchange.Redis;

namespace EduchemLP.Server;



public static class Program {

    public static WebApplication App { get; private set; } = null!;
    public static IDictionary<string, string> ENV { get; private set; } = null!;
    public static ILogger Logger => App.Logger;



    // random picovinky
    public const string ROOT_DOMAIN = /*"lanparty.educhem.it";*/"educhemlan.emsio.cz";

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
        var rhost = ENV.GetValueOrNull("REDIS_IP") ?? ENV["DATABASE_IP"];
        var rport = ENV["REDIS_PORT"];
        var rpassword = ENV.GetValueOrNull("REDIS_PASSWORD");

        var redis = ConnectionMultiplexer.Connect(new ConfigurationOptions {
            EndPoints = { $"{rhost}:{rport}" },
            AbortOnConnectFail = false,
            Password = string.IsNullOrWhiteSpace(rpassword) ? null : rpassword,
        });

        builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

        builder.Services.AddControllersWithViews();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddRazorPages();
        //builder.Services.AddOpenApi("v1");
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



        // databaze source service
        builder.Services.AddSingleton(sp => {
            string cs = $"server={ENV["DATABASE_IP"]};userid={ENV["DATABASE_USERNAME"]};password={ENV["DATABASE_PASSWORD"]};database={ENV["DATABASE_DBNAME"]};pooling=true;Max Pool Size=300;";
            var dsb = new MySqlDataSourceBuilder(cs);
            return dsb.Build();
        });



        // repozitare a service
        builder.Services.AddScoped<IAccountRepository, AccountRepository>();
        builder.Services.AddScoped<IRoomRepository, RoomRepository>();
        builder.Services.AddScoped<IComputerRepository, ComputerRepository>();
        builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IAppSettingsService, AppSettingsService>();
        builder.Services.AddSingleton<IDbLoggerService, DbLoggerService>();



        // websocket endpointy
        builder.Services.AddSingleton<IWebSocketHub, WebSocketHub>();
        builder.Services.AddHostedService<WebSocketHeartbeatService>();
        builder.Services.AddSingleton<IWebSocketEndpoint, ChatWebSocketEndpoint>();
        builder.Services.AddSingleton<IWebSocketEndpoint, ReservationsWebSocketEndpoint>();
        builder.Services.AddSingleton<IWebSocketEndpoint, SyncWebSocketEndpoint>();



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
            App.MapOpenApi();
        }

        //App.UseHttpsRedirection();
        //App.UseStaticFiles();
        App.UseSession();
        App.UseMiddleware<BeforeInitMiddleware>();

        App.UseWebSockets(new WebSocketOptions {
            KeepAliveInterval = TimeSpan.FromSeconds(15),
        });
        App.UseMiddleware<WebSocketMiddleware>();

        App.UseRouting();
        //App.UseAuthorization();
        //App.UseMiddleware<ErrorHandlingMiddleware>();
        App.MapControllerRoute(name: "default", pattern: "/");

        App.Run();
    }
}