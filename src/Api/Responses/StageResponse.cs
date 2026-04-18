namespace GridMonitor.Api.Responses;

public record CurrentStageResponse(short Stage);
public record StageResponse(short Stage, DateTime CreatedAt);
