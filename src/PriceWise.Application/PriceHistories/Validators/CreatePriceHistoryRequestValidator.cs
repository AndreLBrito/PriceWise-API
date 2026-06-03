using FluentValidation;
using PriceWise.Application.PriceHistories.Dtos;

namespace PriceWise.Application.PriceHistories.Validators;

public sealed class CreatePriceHistoryRequestValidator : AbstractValidator<CreatePriceHistoryRequest>
{
    public CreatePriceHistoryRequestValidator()
    {
        RuleFor(request => request.ProductId)
            .NotEmpty()
            .WithMessage("O produto é obrigatório.");

        RuleFor(request => request.StoreId)
            .NotEmpty()
            .WithMessage("A loja é obrigatória.");

        RuleFor(request => request.Price)
            .GreaterThan(0)
            .WithMessage("O preço deve ser maior que zero.");

        RuleFor(request => request.Currency)
            .NotEmpty()
            .WithMessage("A moeda é obrigatória.")
            .Length(3)
            .WithMessage("A moeda deve ter 3 caracteres.");

        RuleFor(request => request.SourceUrl)
            .Must(BeValidOptionalUrl)
            .WithMessage("A URL de origem é inválida.");
    }

    private static bool BeValidOptionalUrl(string? url)
    {
        return string.IsNullOrWhiteSpace(url) || BeValidUrl(url);
    }

    private static bool BeValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
