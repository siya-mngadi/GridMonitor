namespace GridMonitor.Api.Responses;

public record ApiKeyResponse(Guid Id, string Prefix, DateTime CreatedAt, int DailyLimit);