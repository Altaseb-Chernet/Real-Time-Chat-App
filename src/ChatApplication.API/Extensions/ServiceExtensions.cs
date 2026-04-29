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
        services
            .AddSettings(configuration)
            .AddDatabase(configuration)
            .AddRedis(configuration)
            .AddJwtAuthentication(configuration)
            .AddSignalRWithBackplane(configuration)
            .AddRabbitMq(configuration)
            .AddRepositories()
            .AddCoreServices()
            .AddInfrastructureServices();

        return services;
    }

    private static IServiceCollection AddSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(nameof(JwtSettings)));
        services.Configure<RedisSettings>(configuration.GetSection(nameof(RedisSettings)));
        services.Configure<RabbitMqSettings>(configuration.GetSection(nameof(RabbitMqSettings)));
        return services;
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
                npgsql.MigrationsAssembly("ChatApplication.Infrastructure");
            }));

        return services;
    }

    private static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration
            .GetSection(nameof(RedisSettings))
            .GetValue<string>(nameof(RedisSettings.ConnectionString))
            ?? "localhost:6379";

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddSingleton<RedisConnection>();
        services.AddSingleton<RedisBackplaneService>();
        services.AddScoped<ICacheService, RedisCacheService>();

        return services;
    }

    private static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection(nameof(JwtSettings)).Get<JwtSettings>()
            ?? throw new InvalidOperationException("JwtSettings are not configured.");

        var key = Encoding.UTF8.GetBytes(jwtSettings.Secret);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            };

            // Allow JWT from SignalR query string
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        context.Token = accessToken;
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization();
        return services;
    }

    private static IServiceCollection AddSignalRWithBackplane(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration
            .GetSection(nameof(RedisSettings))
            .GetValue<string>(nameof(RedisSettings.ConnectionString))
            ?? "localhost:6379";

        services.AddSignalR()
                .AddStackExchangeRedis(redisConnectionString, options =>
                {
                    options.Configuration.ChannelPrefix = RedisChannel.Literal("ChatApp");
                });

        return services;
    }

    private static IServiceCollection AddRabbitMq(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(nameof(RabbitMqSettings)).Get<RabbitMqSettings>()
            ?? new RabbitMqSettings();

        services.AddSingleton<IConnection>(_ =>
        {
            var factory = new ConnectionFactory
            {
                HostName = settings.Host,
                Port = settings.Port,
                UserName = settings.Username,
                Password = settings.Password,
                DispatchConsumersAsync = true
            };
            return factory.CreateConnection();
        });

        services.AddSingleton<RabbitMqConnection>();
        services.AddScoped<IMessagePublisher, MessagePublisher>();
        services.AddScoped<IMessageSubscriber, MessageSubscriber>();

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<UserRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<MessageRepository>();
        services.AddScoped<IChatRoomRepository, ChatRoomRepository>();
        services.AddScoped<ChatRoomRepository>();
        return services;
    }

    private static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        // Validators
        services.AddScoped<LoginRequestValidator>();
        services.AddScoped<RegisterRequestValidator>();

        // Auth
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITokenService, TokenService>();

        // User
        services.AddScoped<IUserService, UserService>();

        // Chat
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IChatRoomService, ChatRoomService>();
        return services;
    }

    private static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Redis-backed presence and connection tracking (singletons — all state lives in Redis)
        services.AddSingleton<IUserPresenceService, RedisUserPresenceService>();
        services.AddSingleton<IUserTracker, SignalRUserTracker>();
        services.AddSingleton<IConnectionManager, SignalRConnectionManager>();
        return services;
    }
}
