using GridMonitor.Domain.Enums;

namespace GridMonitor.Api.Responses;

public record UsageResponse(int TodayCalls, int DailyLimit, int Remaining, PricingTier Tier);