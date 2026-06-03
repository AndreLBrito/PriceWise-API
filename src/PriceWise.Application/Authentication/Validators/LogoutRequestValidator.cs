using FluentValidation;
using PriceWise.Application.Authentication.Dtos;

namespace PriceWise.Application.Authentication.Validators;

public sealed class LogoutRequestValidator : AbstractValidator<LogoutRequest>
{
    public LogoutRequestValidator()
    {
        RuleFor(request => request.RefreshToken)
            .NotEmpty();
    }
}
