using dotenv.net;
using EduchemLP.Server.Middlewares;
using EduchemLP.Server.Services;
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
        public const bool USE_LOCALHOST_CONNECTION = false;
        public const string PROFILE = "DEBUG";
    #elif RELEASE
        public const bool DEVELOPMENT_MODE = false;
        public const bool USE_LOCALHOST_CONNECTION = true;
        public const string PROFILE = "RELEASE";
    #elif TESTING
        public const bool DEVELOPMENT_MODE = true;
        public const bool USE_LOCALHOST_CONNECTION = true;
        public const string PROFILE = "TESTING";
    #endif

    
    

    public static void Main(string[] args) {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllersWithViews();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddRazorPages();
        builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        builder.Services.AddStackExchangeRedisCache(options => {
            if (DEVELOPMENT_MODE) {
                options.ConfigurationOptions = new ConfigurationOptions {
                    EndPoints = { $"{ENV["DATABASE_IP"]}:{ENV["REDIS_PORT"]}" },
                    Password = ENV["REDIS_PASSWORD"],
                };
            } else options.Configuration = "localhost:6379";

            options.InstanceName = "EduchemLPR_session";
        });
        builder.Services.AddSession(options => {
            options.IdleTimeout = TimeSpan.FromDays(365);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;

            options.Cookie.MaxAge = TimeSpan.FromDays(365); // Trvání cookie na 365 dní
            //options.Cookie.Expiration = TimeSpan.FromDays(365);
            options.Cookie.Name = "educhemlpr_session";
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
        ENV = DotEnv.Read();
        
        
        
        // Konfigurace HttpContextService
        var httpContextAccessor = App.Services.GetRequiredService<IHttpContextAccessor>();
        HttpContextService.Configure(httpContextAccessor);
        

        
        // Configure the HTTP request pipeline.
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
        App.UseAuthorization();
        if(ENV.TryGetValue("WEBSITE_AVAILABLE", out var websiteAvailable) && websiteAvailable == "false")
            App.UseMiddleware<WebsiteNotAvailableRedirectMiddleware>();
        //App.UseMiddleware<ErrorHandlingMiddleware>();
        App.UseMiddleware<FunctionalQueryParameterMiddleware>();
        App.MapControllerRoute(name: "default", pattern: "/");

        

        // Spuštění aplikace
        App.Run();
    }
}