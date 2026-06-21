namespace Foundation.Freight.Options;

public sealed class FreightModuleOptions
{
    public TimeSpan CacheTimeToLive { get; init; } = TimeSpan.FromHours(12);
}
