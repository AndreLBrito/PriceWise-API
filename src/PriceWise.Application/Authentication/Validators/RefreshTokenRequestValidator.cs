using FluentValidation;
using PriceWise.Application.Authentication.Dtos;

namespace PriceWise.Application.Authentication.Validators;

public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(request => request.RefreshToken)
            .NotEmpty();
    }
}
