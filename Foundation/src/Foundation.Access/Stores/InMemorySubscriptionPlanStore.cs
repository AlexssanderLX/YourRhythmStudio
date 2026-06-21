using System.Collections.Concurrent;
using Foundation.Access.Abstractions;
using Foundation.Access.Plans;

namespace Foundation.Access.Stores;

public sealed class InMemorySubscriptionPlanStore : ISubscriptionPlanStore
{
    private readonly ConcurrentDictionary<Guid, SubscriptionPlan> _storage = new();

    public Task SaveAsync(SubscriptionPlan plan, CancellationToken cancellationToken = default)
    {
        _storage[plan.Id] = plan;
        return Task.CompletedTask;
    }

    public Task<SubscriptionPlan?> FindByIdAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        _storage.TryGetValue(planId, out var plan);
        return Task.FromResult(plan);
    }

    public Task<SubscriptionPlan?> FindByCodeAsync(string planCode, CancellationToken cancellationToken = default)
    {
        var normalizedCode = NormalizeCode(planCode);
        var plan = _storage.Values.FirstOrDefault(item => NormalizeCode(item.Code) == normalizedCode);
        return Task.FromResult(plan);
    }

    public Task<IReadOnlyCollection<SubscriptionPlan>> ListActiveAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<SubscriptionPlan> plans = _storage.Values.Where(item => item.IsActive).ToArray();
        return Task.FromResult(plans);
    }

    public Task UpdateAsync(SubscriptionPlan plan, CancellationToken cancellationToken = default)
    {
        _storage[plan.Id] = plan;
        return Task.CompletedTask;
    }

    private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();
}
