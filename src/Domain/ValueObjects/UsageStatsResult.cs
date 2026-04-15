using GridMonitor.Domain.Enums;

namespace GridMonitor.Domain.ValueObjects;

public record UsageStatsResult(int TodayCalls, int DailyLimit, int Remaining, PricingTier Tier);