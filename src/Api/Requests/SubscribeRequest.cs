namespace GridMonitor.Api.Requests;

public record SubscribeRequest(int SuburbId, short AlertMinutesBefore = 30);
