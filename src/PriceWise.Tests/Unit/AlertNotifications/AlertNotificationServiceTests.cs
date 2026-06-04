using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using PriceWise.Application.Abstractions.Notifications;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.AlertNotifications;
using PriceWise.Domain.Entities;
using PriceWise.Domain.Enums;

namespace PriceWise.Tests.Unit.AlertNotifications;

public sealed class AlertNotificationServiceTests
{
    [Fact]
    public async Task CheckPriceAlertsAsyncCreatesNotificationWhenPriceReachesTarget()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var priceHistory = PriceHistory.Create(userId, productId, Guid.NewGuid(), 90, "BRL", DateTime.UtcNow, null);
        var priceAlert = PriceAlert.Create(userId, productId, 100);
        var notificationRepository = new InMemoryAlertNotificationRepository();
        var service = CreateService(notificationRepository, new InMemoryPriceAlertRepository(priceAlert));

        await service.CheckPriceAlertsAsync(priceHistory);

        notificationRepository.Notifications.Should().ContainSingle();
        notificationRepository.Notifications[0].TriggeredPrice.Should().Be(90);
        notificationRepository.Notifications[0].TargetPrice.Should().Be(100);
        priceAlert.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CheckPriceAlertsAsyncDoesNotCreateNotificationWhenPriceIsAboveTarget()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var priceHistory = PriceHistory.Create(userId, productId, Guid.NewGuid(), 110, "BRL", DateTime.UtcNow, null);
        var priceAlert = PriceAlert.Create(userId, productId, 100);
        var notificationRepository = new InMemoryAlertNotificationRepository();
        var service = CreateService(notificationRepository, new InMemoryPriceAlertRepository(priceAlert));

        await service.CheckPriceAlertsAsync(priceHistory);

        notificationRepository.Notifications.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckPriceAlertsAsyncDoesNotCreateDuplicateNotification()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var priceHistory = PriceHistory.Create(userId, productId, Guid.NewGuid(), 90, "BRL", DateTime.UtcNow, null);
        var priceAlert = PriceAlert.Create(userId, productId, 100);
        var notificationRepository = new InMemoryAlertNotificationRepository();
        var service = CreateService(notificationRepository, new InMemoryPriceAlertRepository(priceAlert));

        await service.CheckPriceAlertsAsync(priceHistory);
        await service.CheckPriceAlertsAsync(priceHistory);

        notificationRepository.Notifications.Should().ContainSingle();
    }

    [Fact]
    public async Task CheckPriceAlertsAsyncSendsNotificationToActiveChannels()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var priceHistory = PriceHistory.Create(userId, productId, Guid.NewGuid(), 90, "BRL", DateTime.UtcNow, null);
        var priceAlert = PriceAlert.Create(userId, productId, 100);
        var notificationRepository = new InMemoryAlertNotificationRepository();
        var channelRepository = new InMemoryNotificationChannelRepository(
            NotificationChannel.Create(
                userId,
                NotificationChannelType.Webhook,
                "Webhook",
                "https://example.com/webhook"),
            NotificationChannel.Create(
                userId,
                NotificationChannelType.Email,
                "Email",
                "user@example.com"));
        var webhookSender = new SpyWebhookNotificationSender();
        var emailSender = new SpyEmailNotificationSender();
        var service = CreateService(
            notificationRepository,
            new InMemoryPriceAlertRepository(priceAlert),
            channelRepository,
            webhookSender,
            emailSender);

        await service.CheckPriceAlertsAsync(priceHistory);

        webhookSender.Deliveries.Should().ContainSingle();
        emailSender.Deliveries.Should().ContainSingle();
    }

    private static AlertNotificationService CreateService(
        IAlertNotificationRepository notificationRepository,
        IPriceAlertRepository priceAlertRepository,
        INotificationChannelRepository? notificationChannelRepository = null,
        IWebhookNotificationSender? webhookNotificationSender = null,
        IEmailNotificationSender? emailNotificationSender = null)
    {
        return new AlertNotificationService(
            notificationRepository,
            priceAlertRepository,
            notificationChannelRepository ?? new InMemoryNotificationChannelRepository(),
            webhookNotificationSender ?? new SpyWebhookNotificationSender(),
            emailNotificationSender ?? new SpyEmailNotificationSender(),
            NullLogger<AlertNotificationService>.Instance);
    }

    private sealed class InMemoryAlertNotificationRepository : IAlertNotificationRepository
    {
        public List<AlertNotification> Notifications { get; } = [];

        public Task<IReadOnlyCollection<AlertNotification>> ListByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<AlertNotification> result = Notifications
                .Where(notification => notification.UserId == userId)
                .ToArray();

            return Task.FromResult(result);
        }

        public Task<AlertNotification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Notifications.SingleOrDefault(notification => notification.Id == id));
        }

        public Task<AlertNotification?> GetByIdAsync(
            Guid id,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Notifications.SingleOrDefault(notification =>
                notification.Id == id && notification.UserId == userId));
        }

        public Task<bool> ExistsAsync(
            Guid priceAlertId,
            Guid priceHistoryId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Notifications.Any(notification =>
                notification.PriceAlertId == priceAlertId
                && notification.PriceHistoryId == priceHistoryId));
        }

        public Task AddAsync(AlertNotification entity, CancellationToken cancellationToken = default)
        {
            Notifications.Add(entity);

            return Task.CompletedTask;
        }

        public Task UpdateAsync(AlertNotification entity, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Notifications.RemoveAll(notification => notification.Id == id);

            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryPriceAlertRepository : IPriceAlertRepository
    {
        private readonly List<PriceAlert> priceAlerts;

        public InMemoryPriceAlertRepository(params PriceAlert[] priceAlerts)
        {
            this.priceAlerts = priceAlerts.ToList();
        }

        public Task<IReadOnlyCollection<PriceAlert>> ListByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<PriceAlert> result = priceAlerts
                .Where(priceAlert => priceAlert.UserId == userId && priceAlert.IsActive)
                .ToArray();

            return Task.FromResult(result);
        }

        public Task<PriceAlert?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(priceAlerts.SingleOrDefault(priceAlert => priceAlert.Id == id && priceAlert.IsActive));
        }

        public Task<PriceAlert?> GetByIdAsync(
            Guid id,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(priceAlerts.SingleOrDefault(priceAlert =>
                priceAlert.Id == id && priceAlert.UserId == userId && priceAlert.IsActive));
        }

        public Task<PriceAlert?> GetActiveByProductIdAsync(
            Guid userId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(priceAlerts.SingleOrDefault(priceAlert =>
                priceAlert.UserId == userId
                && priceAlert.ProductId == productId
                && priceAlert.IsActive));
        }

        public Task<IReadOnlyCollection<PriceAlert>> ListActiveByProductIdAsync(
            Guid userId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<PriceAlert> result = priceAlerts
                .Where(priceAlert => priceAlert.UserId == userId
                    && priceAlert.ProductId == productId
                    && priceAlert.IsActive)
                .ToArray();

            return Task.FromResult(result);
        }

        public Task AddAsync(PriceAlert entity, CancellationToken cancellationToken = default)
        {
            priceAlerts.Add(entity);

            return Task.CompletedTask;
        }

        public Task UpdateAsync(PriceAlert entity, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryNotificationChannelRepository : INotificationChannelRepository
    {
        private readonly List<NotificationChannel> channels;

        public InMemoryNotificationChannelRepository(params NotificationChannel[] channels)
        {
            this.channels = channels.ToList();
        }

        public Task<IReadOnlyCollection<NotificationChannel>> ListByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<NotificationChannel> result = channels
                .Where(channel => channel.UserId == userId && channel.IsActive)
                .ToArray();

            return Task.FromResult(result);
        }

        public Task<IReadOnlyCollection<NotificationChannel>> ListActiveByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<NotificationChannel> result = channels
                .Where(channel => channel.UserId == userId && channel.IsActive)
                .ToArray();

            return Task.FromResult(result);
        }

        public Task<NotificationChannel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(channels.SingleOrDefault(channel => channel.Id == id && channel.IsActive));
        }

        public Task<NotificationChannel?> GetByIdAsync(
            Guid id,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(channels.SingleOrDefault(channel =>
                channel.Id == id && channel.UserId == userId && channel.IsActive));
        }

        public Task<NotificationChannel?> GetActiveByTypeAndDestinationAsync(
            Guid userId,
            NotificationChannelType type,
            string destination,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(channels.SingleOrDefault(channel =>
                channel.UserId == userId
                && channel.Type == type
                && channel.Destination == destination
                && channel.IsActive));
        }

        public Task AddAsync(NotificationChannel entity, CancellationToken cancellationToken = default)
        {
            channels.Add(entity);

            return Task.CompletedTask;
        }

        public Task UpdateAsync(NotificationChannel entity, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            channels.RemoveAll(channel => channel.Id == id);

            return Task.CompletedTask;
        }
    }

    private sealed class SpyWebhookNotificationSender : IWebhookNotificationSender
    {
        public List<NotificationDelivery> Deliveries { get; } = [];

        public Task SendAsync(NotificationDelivery delivery, CancellationToken cancellationToken = default)
        {
            Deliveries.Add(delivery);

            return Task.CompletedTask;
        }
    }

    private sealed class SpyEmailNotificationSender : IEmailNotificationSender
    {
        public List<NotificationDelivery> Deliveries { get; } = [];

        public Task SendAsync(NotificationDelivery delivery, CancellationToken cancellationToken = default)
        {
            Deliveries.Add(delivery);

            return Task.CompletedTask;
        }
    }
}
