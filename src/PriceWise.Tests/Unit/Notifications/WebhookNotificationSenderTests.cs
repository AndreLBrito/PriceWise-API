using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Abstractions.Telemetry;
using PriceWise.Application.Abstractions.Notifications;
using PriceWise.Application.Auditing;
using PriceWise.Application.Auditing.Dtos;
using PriceWise.Application.Common;
using PriceWise.Domain.Entities;
using PriceWise.Domain.Enums;
using PriceWise.Infrastructure.Notifications;

namespace PriceWise.Tests.Unit.Notifications;

public sealed class WebhookNotificationSenderTests
{
    [Fact]
    public async Task SendAsyncSendsWebhookSuccessfully()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var sender = CreateSender(handler);
        var delivery = CreateDelivery();

        await sender.SendAsync(delivery);

        handler.Requests.Should().ContainSingle();
        handler.Requests[0].Method.Should().Be(HttpMethod.Post);
        handler.Requests[0].RequestUri.Should().Be(new Uri(delivery.Channel.Destination));
    }

    [Fact]
    public async Task SendAsyncDoesNotThrowWhenWebhookFails()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var sender = CreateSender(handler, maxRetryAttempts: 2);
        var delivery = CreateDelivery();

        var act = async () => await sender.SendAsync(delivery);

        await act.Should().NotThrowAsync();
        handler.Requests.Should().HaveCount(2);
    }

    [Fact]
    public async Task SendAsyncDoesNotSendWhenWebhookIsDisabled()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var sender = CreateSender(handler, enabled: false);
        var delivery = CreateDelivery();

        await sender.SendAsync(delivery);

        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task SendAsyncDoesNotSendWhenChannelIsInactive()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var sender = CreateSender(handler);
        var delivery = CreateDelivery(inactiveChannel: true);

        await sender.SendAsync(delivery);

        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task SendAsyncBuildsExpectedPayload()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var sender = CreateSender(handler);
        var delivery = CreateDelivery();

        await sender.SendAsync(delivery);

        var payload = await handler.Requests[0].Content!.ReadFromJsonAsync<WebhookNotificationPayload>();

        payload.Should().NotBeNull();
        payload!.NotificationId.Should().Be(delivery.AlertNotification.Id);
        payload.UserId.Should().Be(delivery.AlertNotification.UserId);
        payload.ProductId.Should().Be(delivery.AlertNotification.ProductId);
        payload.PriceAlertId.Should().Be(delivery.AlertNotification.PriceAlertId);
        payload.PriceHistoryId.Should().Be(delivery.AlertNotification.PriceHistoryId);
        payload.ProductName.Should().Be("Notebook Demo");
        payload.TargetPrice.Should().Be(delivery.AlertNotification.TargetPrice);
        payload.TriggeredPrice.Should().Be(delivery.AlertNotification.TriggeredPrice);
        payload.TriggeredAt.Should().Be(delivery.AlertNotification.TriggeredAt);
        payload.Message.Should().Contain("Notebook Demo");
    }

    private static WebhookNotificationSender CreateSender(
        StubHttpMessageHandler handler,
        bool enabled = true,
        int maxRetryAttempts = 3)
    {
        var httpClient = new HttpClient(handler);

        return new WebhookNotificationSender(
            httpClient,
            new InMemoryProductRepository(CreateProduct()),
            Options.Create(new WebhookNotificationOptions
            {
                Enabled = enabled,
                TimeoutInSeconds = 5,
                MaxRetryAttempts = maxRetryAttempts
            }),
            NullLogger<WebhookNotificationSender>.Instance,
            new NoOpApplicationTelemetry(),
            new FakeAuditLogService());
    }

    private static NotificationDelivery CreateDelivery(bool inactiveChannel = false)
    {
        var userId = TestIds.UserId;
        var notification = AlertNotification.Restore(
            TestIds.NotificationId,
            userId,
            TestIds.PriceAlertId,
            TestIds.ProductId,
            TestIds.PriceHistoryId,
            89.90m,
            100.00m,
            new DateTime(2026, 6, 4, 10, 30, 0, DateTimeKind.Utc),
            DateTime.UtcNow,
            null);
        var channel = NotificationChannel.Create(
            userId,
            NotificationChannelType.Webhook,
            "Webhook principal",
            "https://example.com/webhook");

        if (inactiveChannel)
        {
            channel.Deactivate();
        }

        return new NotificationDelivery(notification, channel);
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

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> responseFactory;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            this.responseFactory = responseFactory;
        }

        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(CloneRequest(request));
            return Task.FromResult(responseFactory(request));
        }

        private static HttpRequestMessage CloneRequest(HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri)
            {
                Content = request.Content is null
                    ? null
                    : new StringContent(request.Content.ReadAsStringAsync().GetAwaiter().GetResult())
            };

            foreach (var header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            if (request.Content is not null)
            {
                foreach (var header in request.Content.Headers)
                {
                    clone.Content!.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            return clone;
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

    private sealed class FakeAuditLogService : IAuditLogService
    {
        public List<AuditLogEntry> Entries { get; } = [];

        public Task RecordAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
        {
            Entries.Add(entry);

            return Task.CompletedTask;
        }

        public Task<Result<PagedResponse<AuditLogResponse>>> ListAsync(
            AuditLogListRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<PagedResponse<AuditLogResponse>>.Success(
                PagedResponse<AuditLogResponse>.Create([], 1, 20, 0)));
        }

        public Task<Result<AuditLogResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<AuditLogResponse>.Failure(AuditLogErrors.AuditLogNotFound));
        }
    }
}
