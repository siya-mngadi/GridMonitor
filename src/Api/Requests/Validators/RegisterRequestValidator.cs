using FluentValidation;

namespace GridMonitor.Api.Requests.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
	public RegisterRequestValidator()
	{
		RuleFor(x => x.Email)
			.NotEmpty()
			.WithMessage("Email is required.")	
			.EmailAddress()
			.WithMessage("Email is not valid.");

		RuleForEach(x => x.Password)
			.NotEmpty()
			.WithMessage("Password is required.");
	}
}
