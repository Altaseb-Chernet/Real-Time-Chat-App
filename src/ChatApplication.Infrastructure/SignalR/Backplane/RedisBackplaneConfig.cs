namespace ChatApplication.Infrastructure.SignalR.Backplane;

public static class RedisBackplaneConfig
{
    public static ISignalRServerBuilder AddRedisBackplane(this ISignalRServerBuilder builder, string connectionString)
        => builder.AddStackExchangeRedis(connectionString);
}
