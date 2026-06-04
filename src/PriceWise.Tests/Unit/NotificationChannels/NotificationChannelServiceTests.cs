using FluentAssertions;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.NotificationChannels;
using PriceWise.Application.NotificationChannels.Dtos;
using PriceWise.Application.NotificationChannels.Validators;
using PriceWise.Domain.Entities;
using PriceWise.Domain.Enums;

namespace PriceWise.Tests.Unit.NotificationChannels;

public sealed class NotificationChannelServiceTests
{
    [Fact]
    public async Task CreateAsyncCreatesActiveChannel()
    {
        var repository = new InMemoryNotificationChannelRepository();
        var service = new NotificationChannelService(repository);
        var userId = Guid.NewGuid();

        var result = await service.CreateAsync(
            userId,
            new CreateNotificationChannelRequest("Webhook", "Meu webhook", "https://example.com/webhook"));

        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeTrue();
        result.Value.Type.Should().Be("Webhook");
        repository.Channels.Should().ContainSingle();
    }

    [Fact]
    public async Task CreateAsyncFailsWhenActiveChannelAlreadyExists()
    {
        var repository = new InMemoryNotificationChannelRepository();
        var service = new NotificationChannelService(repository);
        var userId = Guid.NewGuid();
        await repository.AddAsync(NotificationChannel.Create(
            userId,
            NotificationChannelType.Email,
            "Email",
            "user@example.com"));

        var result = await service.CreateAsync(
            userId,
            new CreateNotificationChannelRequest("Email", "Email principal", "USER@example.com"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationChannelErrors.DuplicateChannel);
    }

    [Fact]
    public async Task CreateValidatorFailsWhenWebhookDestinationIsInvalid()
    {
        var validator = new CreateNotificationChannelRequestValidator();
        var request = new CreateNotificationChannelRequest("Webhook", "Webhook", "invalid-url");

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateNotificationChannelRequest.Destination));
    }

    [Fact]
    public async Task CreateValidatorFailsWhenEmailDestinationIsInvalid()
    {
        var validator = new CreateNotificationChannelRequestValidator();
        var request = new CreateNotificationChannelRequest("Email", "Email", "invalid-email");

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateNotificationChannelRequest.Destination));
    }

    [Fact]
    public async Task DeleteAsyncDeactivatesChannel()
    {
        var repository = new InMemoryNotificationChannelRepository();
        var service = new NotificationChannelService(repository);
        var userId = Guid.NewGuid();
        var channel = NotificationChannel.Create(
            userId,
            NotificationChannelType.Webhook,
            "Webhook",
            "https://example.com/webhook");
        await repository.AddAsync(channel);

        var result = await service.DeleteAsync(userId, channel.Id);

        result.IsSuccess.Should().BeTrue();
        channel.IsActive.Should().BeFalse();
    }

    private sealed class InMemoryNotificationChannelRepository : INotificationChannelRepository
    {
        public List<NotificationChannel> Channels { get; } = [];

        public Task<IReadOnlyCollection<NotificationChannel>> ListByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<NotificationChannel> result = Channels
                .Where(channel => channel.UserId == userId && channel.IsActive)
                .ToArray();

            return Task.FromResult(result);
        }

        public Task<IReadOnlyCollection<NotificationChannel>> ListActiveByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<NotificationChannel> result = Channels
                .Where(channel => channel.UserId == userId && channel.IsActive)
                .ToArray();

            return Task.FromResult(result);
        }

        public Task<NotificationChannel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Channels.SingleOrDefault(channel => channel.Id == id && channel.IsActive));
        }

        public Task<NotificationChannel?> GetByIdAsync(
            Guid id,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Channels.SingleOrDefault(channel =>
                channel.Id == id && channel.UserId == userId && channel.IsActive));
        }

        public Task<NotificationChannel?> GetActiveByTypeAndDestinationAsync(
            Guid userId,
            NotificationChannelType type,
            string destination,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Channels.SingleOrDefault(channel =>
                channel.UserId == userId
                && channel.Type == type
                && channel.Destination == destination
                && channel.IsActive));
        }

        public Task AddAsync(NotificationChannel entity, CancellationToken cancellationToken = default)
        {
            Channels.Add(entity);

            return Task.CompletedTask;
        }

        public Task UpdateAsync(NotificationChannel entity, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Channels.RemoveAll(channel => channel.Id == id);

            return Task.CompletedTask;
        }
    }
}
