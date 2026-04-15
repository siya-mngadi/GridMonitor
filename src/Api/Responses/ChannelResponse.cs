using GridMonitor.Domain.Enums;

namespace GridMonitor.Api.Responses;

public record ChannelResponse(
	Guid Id,
	ChannelType Type,
	string Destination,
	bool Active
);
