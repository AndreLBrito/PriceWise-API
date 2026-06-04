using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MimeKit;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Abstractions.Notifications;
using PriceWise.Application.Abstractions.Telemetry;
using PriceWise.Domain.Entities;
using PriceWise.Domain.Enums;
using PriceWise.Infrastructure.Notifications;

namespace PriceWise.Tests.Unit.Notifications;

public sealed class EmailNotificationSenderTests
{
    [Fact]
    public async Task SendAsyncSendsEmailSuccessfully()
    {
        var smtpClient = new FakeSmtpEmailClient();
        var sender = CreateSender(smtpClient, enabled: true);
        var delivery = CreateDelivery();

        await sender.SendAsync(delivery);

        smtpClient.Messages.Should().ContainSingle();
        smtpClient.Messages[0].To.Mailboxes.Single().Address.Should().Be(delivery.Channel.Destination);
        smtpClient.Messages[0].Subject.Should().Be("Alerta de preço: Notebook Demo");
    }

    [Fact]
    public async Task SendAsyncDoesNotThrowWhenSmtpFails()
    {
        var smtpClient = new FakeSmtpEmailClient { ShouldFail = true };
        var sender = CreateSender(smtpClient, enabled: true, maxRetryAttempts: 2);
        var delivery = CreateDelivery();

        var act = async () => await sender.SendAsync(delivery);

        await act.Should().NotThrowAsync();
        smtpClient.Attempts.Should().Be(2);
    }

    [Fact]
    public async Task SendAsyncDoesNotSendWhenEmailIsDisabled()
    {
        var smtpClient = new FakeSmtpEmailClient();
        var sender = CreateSender(smtpClient, enabled: false);
        var delivery = CreateDelivery();

        await sender.SendAsync(delivery);

        smtpClient.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task SendAsyncDoesNotSendWhenChannelIsInactive()
    {
        var smtpClient = new FakeSmtpEmailClient();
        var sender = CreateSender(smtpClient, enabled: true);
        var delivery = CreateDelivery(inactiveChannel: true);

        await sender.SendAsync(delivery);

        smtpClient.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task SendAsyncBuildsExpectedPayloadIntoMessage()
    {
        var smtpClient = new FakeSmtpEmailClient();
        var sender = CreateSender(smtpClient, enabled: true);
        var delivery = CreateDelivery();

        await sender.SendAsync(delivery);

        var message = smtpClient.Messages.Single();
        var htmlBody = GetBody(message, isHtml: true);
        var textBody = GetBody(message, isHtml: false);

        message.From.Mailboxes.Single().Address.Should().Be("noreply@pricewise.local");
        message.To.Mailboxes.Single().Address.Should().Be("user@example.com");
        message.Subject.Should().Be("Alerta de preço: Notebook Demo");
        htmlBody.Should().Contain("Notebook Demo");
        htmlBody.Should().Contain("R$ 100,00");
        htmlBody.Should().Contain("R$ 89,90");
        htmlBody.Should().Contain("https://example.com/products/notebook-demo");
        textBody.Should().Contain("Notebook Demo");
        textBody.Should().Contain("R$ 100,00");
        textBody.Should().Contain("R$ 89,90");
    }

    [Fact]
    public void TemplateContainsProductTargetPriceAndTriggeredPrice()
    {
        var payload = CreatePayload();

        var htmlBody = EmailNotificationTemplate.BuildHtml(payload);
        var textBody = EmailNotificationTemplate.BuildText(payload);

        htmlBody.Should().Contain("Notebook Demo");
        htmlBody.Should().Contain("R$ 100,00");
        htmlBody.Should().Contain("R$ 89,90");
        textBody.Should().Contain("Notebook Demo");
        textBody.Should().Contain("R$ 100,00");
        textBody.Should().Contain("R$ 89,90");
    }

    private static EmailNotificationSender CreateSender(
        FakeSmtpEmailClient smtpClient,
        bool enabled,
        int maxRetryAttempts = 3)
    {
        return new EmailNotificationSender(
            smtpClient,
            new InMemoryProductRepository(CreateProduct()),
            Options.Create(new EmailNotificationOptions
            {
                Enabled = enabled,
                Host = "localhost",
                Port = 1025,
                UseSsl = false,
                FromName = "PriceWise",
                FromEmail = "noreply@pricewise.local",
                TimeoutInSeconds = 5,
                MaxRetryAttempts = maxRetryAttempts
            }),
            NullLogger<EmailNotificationSender>.Instance,
            new NoOpApplicationTelemetry());
    }

    private static NotificationDelivery CreateDelivery(bool inactiveChannel = false)
    {
        var notification = AlertNotification.Restore(
            TestIds.NotificationId,
            TestIds.UserId,
            TestIds.PriceAlertId,
            TestIds.ProductId,
            TestIds.PriceHistoryId,
            89.90m,
            100.00m,
            new DateTime(2026, 6, 4, 10, 30, 0, DateTimeKind.Utc),
            DateTime.UtcNow,
            null);
        var channel = NotificationChannel.Create(
            TestIds.UserId,
            NotificationChannelType.Email,
            "Email principal",
            "user@example.com");

        if (inactiveChannel)
        {
            channel.Deactivate();
        }

        return new NotificationDelivery(notification, channel);
    }

    private static EmailNotificationPayload CreatePayload()
    {
        return new EmailNotificationPayload(
            TestIds.NotificationId,
            TestIds.UserId,
            TestIds.ProductId,
            TestIds.PriceAlertId,
            TestIds.PriceHistoryId,
            "Notebook Demo",
            "https://example.com/products/notebook-demo",
            100.00m,
            89.90m,
            new DateTime(2026, 6, 4, 10, 30, 0, DateTimeKind.Utc),
            "user@example.com",
            "Alerta de preço: Notebook Demo");
    }

    private static Product CreateProduct()
    {
        return Product.Restore(
            TestIds.ProductId,
            TestIds.UserId,
            "Notebook Demo",
            "Produto para teste",
            "PriceWise",
            "Notebook",
            "https://example.com/products/notebook-demo",
            null,
            true,
            DateTime.UtcNow,
            null);
    }

    private static string GetBody(MimeMessage message, bool isHtml)
    {
        return message.BodyParts
            .OfType<TextPart>()
            .Single(part => part.IsHtml == isHtml)
            .Text;
    }

    private sealed class FakeSmtpEmailClient : ISmtpEmailClient
    {
        public List<MimeMessage> Messages { get; } = [];

        public bool ShouldFail { get; init; }

        public int Attempts { get; private set; }

        public Task SendAsync(
            MimeMessage message,
            EmailNotificationOptions options,
            CancellationToken cancellationToken = default)
        {
            Attempts++;

            if (ShouldFail)
            {
                throw new InvalidOperationException("SMTP indisponível.");
            }

            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryProductRepository : IProductRepository
    {
        private readonly Product product;

        public InMemoryProductRepository(Product product)
        {
            this.product = product;
        }

        public Task<IReadOnlyCollection<Product>> ListByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<Product> products = product.UserId == userId ? [product] : [];
            return Task.FromResult(products);
        }

        public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(product.Id == id ? product : null);
        }

        public Task<Product?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(product.Id == id && product.UserId == userId ? product : null);
        }

        public Task<Product?> GetByProductUrlAsync(Guid userId, string productUrl, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(product.UserId == userId && product.ProductUrl == productUrl ? product : null);
        }

        public Task AddAsync(Product entity, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Product entity, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private static class TestIds
    {
        public static readonly Guid UserId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        public static readonly Guid ProductId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        public static readonly Guid PriceAlertId = Guid.Parse("30000000-0000-0000-0000-000000000001");
        public static readonly Guid PriceHistoryId = Guid.Parse("40000000-0000-0000-0000-000000000001");
        public static readonly Guid NotificationId = Guid.Parse("50000000-0000-0000-0000-000000000001");
    }
}
