using FluentValidation;
using PriceWise.Application.PriceAlerts.Dtos;

namespace PriceWise.Application.PriceAlerts.Validators;

public sealed class UpdatePriceAlertRequestValidator : AbstractValidator<UpdatePriceAlertRequest>
{
    public UpdatePriceAlertRequestValidator()
    {
        RuleFor(request => request.TargetPrice)
            .GreaterThan(0)
            .WithMessage("O preço alvo deve ser maior que zero.");
    }
}
