namespace GridMonitor.Domain.ValueObjects;

public record ApiKeyResult(string Prefix, string PlainKey, Guid KeyId);
