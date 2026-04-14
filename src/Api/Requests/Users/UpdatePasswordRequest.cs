namespace GridMonitor.Api.Requests.Users;

public class UpdatePasswordRequest
{
	public string CurrentPassword { get; init; }
	public string NewPassword { get; init; }
}
