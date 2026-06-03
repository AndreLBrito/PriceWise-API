using FluentValidation;
using PriceWise.Application.Authentication.Dtos;

namespace PriceWise.Application.Authentication.Validators;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(254);

        RuleFor(request => request.Password)
            .NotEmpty();
    }
}
