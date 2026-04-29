using StackExchange.Redis;

namespace ChatApplication.Infrastructure.Cache.Redis;

public class RedisBackplaneService
{
    private readonly IConnectionMultiplexer _multiplexer;

    public RedisBackplaneService(IConnectionMultiplexer multiplexer) => _multiplexer = multiplexer;

    public ISubscriber GetSubscriber() => _multiplexer.GetSubscriber();
}
