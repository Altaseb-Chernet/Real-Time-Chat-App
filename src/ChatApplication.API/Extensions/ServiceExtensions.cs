using System.Text;
using ChatApplication.Core.Dependencies.Configuration;
using ChatApplication.Core.Modules.Authentication.Contracts;
using ChatApplication.Core.Modules.Authentication.Services;
using ChatApplication.Core.Modules.Authentication.Validators;
using ChatApplication.Core.Modules.Chat.Contracts;
using ChatApplication.Core.Modules.Chat.Services;
using ChatApplication.Core.Modules.User.Contracts;
using ChatApplication.Core.Modules.User.Services;
using ChatApplication.Infrastructure.Cache.Interfaces;
using ChatApplication.Infrastructure.Cache.Redis;
using ChatApplication.Infrastructure.Data.Context;
using ChatApplication.Infrastructure.Data.Repositories;
using ChatApplication.Infrastructure.Messaging.Interfaces;
using ChatApplication.Infrastructure.Messaging.RabbitMQ;
using ChatApplication.Infrastructure.SignalR.Interfaces;
using ChatApplication.Infrastructure.SignalR.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RabbitMQ.Client;
using StackExchange.Redis;

namespace ChatApplication.API.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        AddSettings(services, configuration);
        AddDatabase(services, configuration);
        AddJwtAuthentication(services, configuration);
        services.AddSignalR();
        AddRedisLazy(services, configuration);
        AddRabbitMqLazy(services, configuration);
        AddRepositories(services);
        AddCoreServices(services);
        AddInfrastructureServices(services);
        return services;
    }

    private static void AddSettings(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(nameof(JwtSettings)));
        services.Configure<RedisSettings>(configuration.GetSection(nameof(RedisSettings)));
        services.Configure<RabbitMqSettings>(configuration.GetSection(nameof(RabbitMqSettings)));
    }

    private static void AddDatabase(IServiceCollection services, IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection not configured.");

        services.AddDbContext<ApplicationDbContext>(o =>
            o.UseNpgsql(cs, n => n.MigrationsAssembly("ChatApplication.Infrastructure")));
    }

    private static void AddJwtAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(nameof(JwtSettings)).Get<JwtSettings>()
            ?? throw new InvalidOperationException("JwtSettings not configured.");

        var key = Encoding.UTF8.GetBytes(jwt.Secret);

        services.AddAuthentication(o =>
        {
            o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            o.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(o =>
        {
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateLifetime         = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer              = jwt.Issuer,
                ValidAudience            = jwt.Audience,
                IssuerSigningKey         = new SymmetricSecurityKey(key),
                ClockSkew                = TimeSpan.Zero
            };
            o.Events = new JwtBearerEvents
            {
                OnMessageReceived = ctx =>
                {
                    var t = ctx.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(t) && ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                        ctx.Token = t;
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization();
    }

    private static void AddRedisLazy(IServiceCollection services, IConfiguration configuration)
    {
        var cs = configuration
            .GetSection(nameof(RedisSettings))
            .GetValue<string>(nameof(RedisSettings.ConnectionString))
            ?? "localhost:6379";

        // Factory only runs on first actual use — never at startup
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var opts = ConfigurationOptions.Parse(cs);
            opts.AbortOnConnectFail = false;
            opts.ConnectTimeout     = 3000;
            return ConnectionMultiplexer.Connect(opts);
        });

        services.AddSingleton<RedisConnection>();
        services.AddSingleton<RedisBackplaneService>();
        services.AddScoped<ICacheService, RedisCacheService>();
    }

    private static void AddRabbitMqLazy(IServiceCollection services, IConfiguration configuration)
    {
        var s = configuration.GetSection(nameof(RabbitMqSettings)).Get<RabbitMqSettings>()
            ?? new RabbitMqSettings();

        services.AddSingleton<IConnection?>(_ =>
        {
            try
            {
                return new ConnectionFactory
                {
                    HostName               = s.Host,
                    Port                   = s.Port,
                    UserName               = s.Username,
                    Password               = s.Password,
                    DispatchConsumersAsync = true
                }.CreateConnection();
            }
            catch { return null; }
        });

        services.AddSingleton<RabbitMqConnection>();
        services.AddScoped<IMessagePublisher, MessagePublisher>();
        services.AddScoped<IMessageSubscriber, MessageSubscriber>();
    }

    private static void AddRepositories(IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IChatRoomRepository, ChatRoomRepository>();
    }

    private static void AddCoreServices(IServiceCollection services)
    {
        services.AddScoped<LoginRequestValidator>();
        services.AddScoped<RegisterRequestValidator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IChatRoomService, ChatRoomService>();
    }

    private static void AddInfrastructureServices(IServiceCollection services)
    {
        // Pure in-memory — zero network calls, works without Redis
        services.AddSingleton<IUserPresenceService, InMemoryPresenceService>();
        services.AddSingleton<IUserTracker, InMemoryUserTracker>();
        services.AddSingleton<IConnectionManager, InMemoryConnectionManager>();
    }}
