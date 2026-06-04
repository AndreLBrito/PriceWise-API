using FluentValidation;
using PriceWise.Application.Admin.Dtos;

namespace PriceWise.Application.Admin.Validators;

public sealed class UpdateUserRoleRequestValidator : AbstractValidator<UpdateUserRoleRequest>
{
    public UpdateUserRoleRequestValidator()
    {
        RuleFor(request => request.Role)
            .NotEmpty()
            .WithMessage("O papel é obrigatório.")
            .Must(role => string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, "User", StringComparison.OrdinalIgnoreCase))
            .WithMessage("O papel informado é inválido.");
    }
}
