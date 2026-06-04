using FluentValidation;
using PriceWise.Application.NotificationChannels.Dtos;
using PriceWise.Domain.Enums;

namespace PriceWise.Application.NotificationChannels.Validators;

public sealed class CreateNotificationChannelRequestValidator : AbstractValidator<CreateNotificationChannelRequest>
{
    public CreateNotificationChannelRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .WithMessage("O nome é obrigatório.")
            .MaximumLength(120)
            .WithMessage("O nome deve ter no máximo 120 caracteres.");

        RuleFor(request => request.Type)
            .NotEmpty()
            .WithMessage("O tipo é obrigatório.")
            .Must(BeValidType)
            .WithMessage("O tipo deve ser Webhook ou Email.");

        RuleFor(request => request.Destination)
            .NotEmpty()
            .WithMessage("O destino é obrigatório.")
            .MaximumLength(2048)
            .WithMessage("O destino deve ter no máximo 2048 caracteres.")
            .Must((request, destination) => BeValidDestination(request.Type, destination))
            .WithMessage("O destino informado é inválido para o tipo de canal.");
    }

    private static bool BeValidType(string type)
    {
        return Enum.TryParse<NotificationChannelType>(type, true, out _);
    }

    private static bool BeValidDestination(string type, string destination)
    {
        if (!Enum.TryParse<NotificationChannelType>(type, true, out var channelType))
        {
            return false;
        }

        return channelType switch
        {
            NotificationChannelType.Webhook => BeValidUrl(destination),
            NotificationChannelType.Email => BeValidEmail(destination),
            _ => false
        };
    }

    private static bool BeValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static bool BeValidEmail(string email)
    {
        return Uri.CheckHostName(email.Split('@').LastOrDefault()) != UriHostNameType.Unknown
            && new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email);
    }
}
