using FluentValidation;
using PriceWise.Application.PriceAlerts.Dtos;

namespace PriceWise.Application.PriceAlerts.Validators;

public sealed class CreatePriceAlertRequestValidator : AbstractValidator<CreatePriceAlertRequest>
{
    public CreatePriceAlertRequestValidator()
    {
        RuleFor(request => request.ProductId)
            .NotEmpty()
            .WithMessage("O produto é obrigatório.");

        RuleFor(request => request.TargetPrice)
            .GreaterThan(0)
            .WithMessage("O preço alvo deve ser maior que zero.");
    }
}
