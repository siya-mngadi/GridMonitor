using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Enums;
using GridMonitor.Domain.Shared;

namespace GridMonitor.Domain.Services;

public interface ISubscriptionService
{
	ValueTask<Result<AlertSubscription>> SubscribeAsync(Guid userId, int suburbId, short alertMinutesBefore, CancellationToken ct = default);
	ValueTask<Result> UnsubscribeAsync(Guid userId, Guid subscriptionId, CancellationToken ct = default);
	ValueTask<Result<AlertChannel>> AddChannelAsync(Guid userId, Guid subscriptionId, ChannelType type, string destination, CancellationToken ct = default);
	ValueTask<Result> RemoveChannelAsync(Guid userId, Guid channelId, CancellationToken ct = default);
	ValueTask<Result> UpdateAlertWindowAsync(Guid userId, Guid subscriptionId, short alertMinutesBefore, CancellationToken ct = default);
	ValueTask<Result<List<AlertSubscription>>> GetUserSubscriptionsAsync(Guid userId, CancellationToken ct = default);
}
