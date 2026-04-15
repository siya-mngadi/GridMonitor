namespace GridMonitor.Api.Responses;

public record SubscriptionResponse(
	Guid Id,
	int SuburbId,
	string SuburbName,
	int AlertMinutesBefore,
	bool IsActive,
	IEnumerable<ChannelResponse> Channels
);
