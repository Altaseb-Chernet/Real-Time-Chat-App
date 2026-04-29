namespace ChatApplication.Core.Common.Base;

public abstract class BaseService
{
    protected readonly ILogger Logger;

    protected BaseService(ILogger logger) => Logger = logger;
}
