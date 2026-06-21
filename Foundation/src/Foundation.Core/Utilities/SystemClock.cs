using Foundation.Core.Abstractions;

namespace Foundation.Core.Utilities;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
