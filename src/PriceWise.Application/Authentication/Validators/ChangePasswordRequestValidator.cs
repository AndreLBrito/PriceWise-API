using FluentValidation;
using PriceWise.Application.Authentication.Dtos;

namespace PriceWise.Application.Authentication.Validators;

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(request => request.CurrentPassword)
            .NotEmpty()
            .WithMessage("A senha atual é obrigatória.");

        RuleFor(request => request.NewPassword)
            .NotEmpty()
            .WithMessage("A nova senha é obrigatória.")
            .MinimumLength(8)
            .WithMessage("A nova senha deve ter no mínimo 8 caracteres.")
            .MaximumLength(100)
            .WithMessage("A nova senha deve ter no máximo 100 caracteres.");
    }
}
