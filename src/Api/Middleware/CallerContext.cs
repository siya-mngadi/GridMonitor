using GridMonitor.Domain.Enums;

namespace GridMonitor.Api.Middleware;

public class CallerContext
{
	public Guid UserId { get; set; }
	public PricingTier Tier { get; set; }
	public bool IsJwt { get; set; }
	public bool IsApiKey { get; set; }
	public bool IsResolved => UserId != Guid.Empty;

	// Keycloak-specific — populated only when IsJwt = true
	public string KeycloakId { get; set; } = string.Empty;

	// API key-specific — populated only when IsApiKey = true
	public Guid ApiKeyId { get; set; }

	/// <summary>
	/// Returns 403 if the caller authenticated with an API key.
	/// Returns null if the caller is a JWT user — endpoint may proceed.
	/// Call this at the top of any write or account-management endpoint.
	/// </summary>
	public IResult EnforceJwtOnly(string operationDescription) =>
		IsApiKey
			? Results.Json(new
			{
				error = $"This operation is only allowed for web users: {operationDescription}.",
				reason = "This operation is not allowed. Please sign in with a valid account to continue.",
			}, statusCode: StatusCodes.Status403Forbidden)
			: null;
}
