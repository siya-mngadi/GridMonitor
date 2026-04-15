using FluentValidation;
using GridMonitor.Domain.Enums;

namespace GridMonitor.Api.Requests.Validators;

public class AddChannelRequestValidator : AbstractValidator<AddChannelRequest>
{
	public AddChannelRequestValidator()
	{
		RuleFor(x => x.Type)
			.NotEmpty()
			.WithMessage("Channel type is required.")
			.Must(type => type == ChannelType.Email || type == ChannelType.Sms)
			.WithMessage("Channel type must be either 'Email' or 'SMS'.");

		RuleFor(x => x.Destination)
			.NotEmpty()
			.WithMessage("Destination is required.");
	}
}
