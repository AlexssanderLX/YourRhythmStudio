namespace Foundation.Core.Abstractions;

public interface IClock
{
    DateTime UtcNow { get; }
}
