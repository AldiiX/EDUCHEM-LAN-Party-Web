using dotenv.net;
using EduchemLP.Server.Infrastructure;
using EduchemLP.Server.Data.Entities;
using EduchemLP.Server.Data;
using EduchemLP.Server.Middlewares;
using EduchemLP.Server.Repositories;
using EduchemLP.Server.Services;
using EduchemLP.Server.WebSockets;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Npgsql;
using MySqlConnector;
using StackExchange.Redis;

namespace EduchemLP.Server;

public static class Program {

    public static WebApplication App { get; private set; } = null!;
    public static IDictionary<string, string> ENV { get; private set; } = null!;
    public static ILogger Logger => App.Logger;

    // random picovinky
    public const string ROOT_DOMAIN = "educhemlan.emsio.cz";

    #if DEBUG
        public const bool DEVELOPMENT_MODE = true;
    #elif RELEASE
        public const bool DEVELOPMENT_MODE = false;
    #elif TESTING
        public const bool DEVELOPMENT_MODE = true;
    #endif



    public static void Main(string[] args) {
        ENV = DotEnv.Read();

        // zvyseni min threadu aby nedochazelo ke thread pool starvation pri spickach
        ThreadPool.SetMinThreads(workerThreads: 200, completionPortThreads: 200);

        var builder = WebApplication.CreateBuilder(args);

        // pripojeni k redisu
        var rhost = ENV.GetValueOrNull("REDIS_IP") ?? ENV["DATABASE_IP"];
        var rport = ENV["REDIS_PORT"];
        var rpassword = ENV.GetValueOrNull("REDIS_PASSWORD");

        // konfigurace redisu vcetne timeoutu a keepalive
        var redisOptions = new ConfigurationOptions {
            AbortOnConnectFail = false,
            Password = string.IsNullOrWhiteSpace(rpassword) ? null : rpassword,
            KeepAlive = 30,             // keepalive v sekundach
            SyncTimeout = 10000,        // 10s pro sync operace (session atd.)
            AsyncTimeout = 10000,       // 10s pro async operace
            ConnectTimeout = 5000,      // 5s na navazani spojeni
            ClientName = "EduchemLP_Server"
        };

        // pridani endpointu
        redisOptions.EndPoints.Add(rhost, int.Parse(rport));

        var redis = ConnectionMultiplexer.Connect(redisOptions);

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
                // pouzijeme stejnou konfiguraci jako pro hlavni redis connection
                ConfigurationOptions = redisOptions,
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

        // PostgreSQL / EF Core datasource
        builder.Services.AddSingleton(sp => {
            var pgHost = ENV.GetValueOrNull("POSTGRES_IP") ?? ENV["DATABASE_IP"];
            var pgPort = ENV.GetValueOrNull("POSTGRES_PORT") ?? ENV.GetValueOrNull("DATABASE_PORT") ?? "5432";
            var pgUser = ENV.GetValueOrNull("POSTGRES_USERNAME") ?? ENV["DATABASE_USERNAME"];
            var pgPassword = ENV.GetValueOrNull("POSTGRES_PASSWORD") ?? ENV["DATABASE_PASSWORD"];
            var pgDb = ENV.GetValueOrNull("POSTGRES_DBNAME") ?? ENV["DATABASE_DBNAME"];

            var cs = $"Host={pgHost};Port={pgPort};Username={pgUser};Password={pgPassword};Database={pgDb};Pooling=true;Maximum Pool Size=300;";
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(cs);

            dataSourceBuilder.MapEnum<Account.AccountGender>("account.account_gender_enum");
            dataSourceBuilder.MapEnum<Account.AccountType>("account.account_type_enum");
            dataSourceBuilder.MapEnum<Account.AccountAccessToken.AccountAccessTokenPlatform>("account.account_access_token_platform_enum");
            dataSourceBuilder.MapEnum<Account.AccountAccessToken.AccountAccessTokenType>("account.account_access_token_type_enum");

            return dataSourceBuilder.Build();
        });

        builder.Services.AddDbContext<EduchemLpDbContext>((sp, options) => {
            var npgsqlDataSource = sp.GetRequiredService<NpgsqlDataSource>();
            options.UseNpgsql(npgsqlDataSource);
        });

        // databaze source service
        builder.Services.AddSingleton(sp => {
            var cs = $"server={ENV["DATABASE_IP"]};userid={ENV["DATABASE_USERNAME"]};password={ENV["DATABASE_PASSWORD"]};database={ENV["DATABASE_DBNAME"]};pooling=true;Max Pool Size=300;";
            var dsb = new MySqlDataSourceBuilder(cs);
            return dsb.Build();
        });

        // repozitare a service
        builder.Services.AddScoped<IAccountRepository, AccountRepository>();
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
            KeepAliveInterval = TimeSpan.FromSeconds(20),
        });

        App.UseMiddleware<WebSocketMiddleware>();

        App.UseRouting();
        //App.UseAuthorization();
        //App.UseMiddleware<ErrorHandlingMiddleware>();
        App.MapControllerRoute(name: "default", pattern: "/");

        App.Run();
    }
}
