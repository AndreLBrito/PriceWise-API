using FluentAssertions;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.AlertNotifications;
using PriceWise.Domain.Entities;

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
        var service = new AlertNotificationService(
            notificationRepository,
            new InMemoryPriceAlertRepository(priceAlert));

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
        var service = new AlertNotificationService(
            notificationRepository,
            new InMemoryPriceAlertRepository(priceAlert));

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
        var service = new AlertNotificationService(
            notificationRepository,
            new InMemoryPriceAlertRepository(priceAlert));

        await service.CheckPriceAlertsAsync(priceHistory);
        await service.CheckPriceAlertsAsync(priceHistory);

        notificationRepository.Notifications.Should().ContainSingle();
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
}
