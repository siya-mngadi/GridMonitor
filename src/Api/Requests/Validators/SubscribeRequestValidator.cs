using FluentValidation;
using GridMonitor.Domain.Shared;

namespace GridMonitor.Api.Requests.Validators;

public class SubscribeRequestValidator : AbstractValidator<SubscribeRequest>
{
	public SubscribeRequestValidator()
	{
		RuleFor(x => x.SuburbId)
			.GreaterThan(0)
			.WithMessage("SuburbId must be greater than 0.");

		RuleFor(x => x.AlertMinutesBefore)
			.GreaterThan(0)
			.Must(x => TierPolicy.ValidAlertMinutes.Contains(x))
			.WithMessage("Alert minutes must be one of the valid alert minutes.");
	}
}
