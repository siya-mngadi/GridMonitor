using GridMonitor.Domain.Enums;

namespace GridMonitor.Api.Requests;

public record AddChannelRequest(ChannelType Type, string Destination);
