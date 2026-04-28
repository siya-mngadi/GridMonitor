using FluentValidation;
using GridMonitor.Domain.Shared;

namespace GridMonitor.Api.Requests.Validators;

public class UpdateAlertWindowValidator : AbstractValidator<UpdateAlertWindowRequest>
{
	public UpdateAlertWindowValidator()
	{
		RuleFor(x => x.AlertMinutesBefore)
			.Must(x => TierPolicy.ValidAlertMinutes.Contains(x))
			.WithMessage(x => $"Alert minutes before must be one of the following values: {string.Join(", ", TierPolicy.ValidAlertMinutes)}.");
	}
}
