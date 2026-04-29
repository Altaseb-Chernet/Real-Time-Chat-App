using StackExchange.Redis;

namespace ChatApplication.Infrastructure.Cache.Redis;

public class RedisConnection
{
    private readonly IConnectionMultiplexer _multiplexer;

    public RedisConnection(IConnectionMultiplexer multiplexer) => _multiplexer = multiplexer;

    public IDatabase GetDatabase() => _multiplexer.GetDatabase();
}
