using System.Globalization;
using System.Net;

namespace PriceWise.Infrastructure.Notifications;

public static class EmailNotificationTemplate
{
    private static readonly CultureInfo BrazilianCulture = CultureInfo.GetCultureInfo("pt-BR");

    public static string BuildHtml(EmailNotificationPayload payload)
    {
        var productName = WebUtility.HtmlEncode(payload.ProductName);
        var targetPrice = FormatCurrency(payload.TargetPrice);
        var triggeredPrice = FormatCurrency(payload.TriggeredPrice);
        var triggeredAt = FormatDate(payload.TriggeredAt);
        var productLink = string.IsNullOrWhiteSpace(payload.ProductUrl)
            ? string.Empty
            : $"""
                <p>
                    <a href="{WebUtility.HtmlEncode(payload.ProductUrl)}">Ver produto monitorado</a>
                </p>
                """;

        return $$"""
            <!doctype html>
            <html lang="pt-BR">
            <body style="font-family: Arial, sans-serif; color: #1f2937;">
                <h2>Alerta de preço PriceWise</h2>
                <p>O produto <strong>{{productName}}</strong> atingiu o preço configurado.</p>
                <ul>
                    <li>Preço alvo: <strong>{{targetPrice}}</strong></li>
                    <li>Preço encontrado: <strong>{{triggeredPrice}}</strong></li>
                    <li>Data do disparo: <strong>{{triggeredAt}}</strong></li>
                </ul>
                {{productLink}}
                <p>Este e-mail foi enviado automaticamente pela PriceWise API.</p>
            </body>
            </html>
            """;
    }

    public static string BuildText(EmailNotificationPayload payload)
    {
        var lines = new List<string>
        {
            "Alerta de preço PriceWise",
            string.Empty,
            $"Produto: {payload.ProductName}",
            $"Preço alvo: {FormatCurrency(payload.TargetPrice)}",
            $"Preço encontrado: {FormatCurrency(payload.TriggeredPrice)}",
            $"Data do disparo: {FormatDate(payload.TriggeredAt)}"
        };

        if (!string.IsNullOrWhiteSpace(payload.ProductUrl))
        {
            lines.Add($"Link do produto: {payload.ProductUrl}");
        }

        lines.Add(string.Empty);
        lines.Add("Este e-mail foi enviado automaticamente pela PriceWise API.");

        return string.Join(Environment.NewLine, lines);
    }

    private static string FormatCurrency(decimal value)
    {
        return value.ToString("C", BrazilianCulture);
    }

    private static string FormatDate(DateTime value)
    {
        return value.ToUniversalTime().ToString("dd/MM/yyyy HH:mm 'UTC'", BrazilianCulture);
    }
}
