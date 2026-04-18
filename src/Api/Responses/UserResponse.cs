using GridMonitor.Domain.Enums;

namespace GridMonitor.Api.Responses;

public record UserResponse(Guid Id, string Email, PricingTier Tier);