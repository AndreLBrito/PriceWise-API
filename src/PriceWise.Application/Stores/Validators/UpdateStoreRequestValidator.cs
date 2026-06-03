using FluentValidation;
using PriceWise.Application.Stores.Dtos;

namespace PriceWise.Application.Stores.Validators;

public sealed class UpdateStoreRequestValidator : AbstractValidator<UpdateStoreRequest>
{
    public UpdateStoreRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .WithMessage("O nome é obrigatório.")
            .MaximumLength(120)
            .WithMessage("O nome deve ter no máximo 120 caracteres.");

        RuleFor(request => request.BaseUrl)
            .NotEmpty()
            .WithMessage("A URL base é obrigatória.")
            .Must(BeValidUrl)
            .WithMessage("A URL base é inválida.");

        RuleFor(request => request.LogoUrl)
            .Must(BeValidOptionalUrl)
            .WithMessage("A URL do logo é inválida.");
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
