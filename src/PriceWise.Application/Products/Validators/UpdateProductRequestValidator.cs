using FluentValidation;
using PriceWise.Application.Products.Dtos;

namespace PriceWise.Application.Products.Validators;

public sealed class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .WithMessage("O nome é obrigatório.")
            .MaximumLength(150)
            .WithMessage("O nome deve ter no máximo 150 caracteres.");

        RuleFor(request => request.Description)
            .MaximumLength(500)
            .WithMessage("A descrição deve ter no máximo 500 caracteres.");

        RuleFor(request => request.Brand)
            .MaximumLength(100)
            .WithMessage("A marca deve ter no máximo 100 caracteres.");

        RuleFor(request => request.Category)
            .MaximumLength(100)
            .WithMessage("A categoria deve ter no máximo 100 caracteres.");

        RuleFor(request => request.ProductUrl)
            .NotEmpty()
            .WithMessage("A URL do produto é obrigatória.")
            .Must(BeValidUrl)
            .WithMessage("A URL do produto é inválida.");

        RuleFor(request => request.ImageUrl)
            .Must(BeValidOptionalUrl)
            .WithMessage("A URL da imagem é inválida.");
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
