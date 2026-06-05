using FluentAssertions;
using PriceWise.Application.Abstractions.Auditing;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Auditing;
using PriceWise.Application.Auditing.Dtos;
using PriceWise.Application.Common;
using PriceWise.Domain.Entities;

namespace PriceWise.Tests.Unit.Auditing;

public sealed class AuditLogServiceTests
{
    [Fact]
    public async Task RecordAsyncCreatesAuditLog()
    {
        var repository = new InMemoryAuditLogRepository();
        var service = new AuditLogService(repository, new FixedAuditContext("correlation-id"));
        var userId = Guid.NewGuid();
        var entityId = Guid.NewGuid();

        await service.RecordAsync(new AuditLogEntry(
            userId,
            AuditActions.Create,
            "Product",
            entityId,
            NewValues: new { Name = "Notebook" }));

        repository.AuditLogs.Should().ContainSingle();
        var auditLog = repository.AuditLogs.Single();
        auditLog.UserId.Should().Be(userId);
        auditLog.Action.Should().Be(AuditActions.Create);
        auditLog.EntityName.Should().Be("Product");
        auditLog.EntityId.Should().Be(entityId);
        auditLog.NewValues.Should().Contain("Notebook");
    }

    [Fact]
    public async Task RecordAsyncRemovesSensitiveData()
    {
        var repository = new InMemoryAuditLogRepository();
        var service = new AuditLogService(repository, new FixedAuditContext("correlation-id"));

        await service.RecordAsync(new AuditLogEntry(
            Guid.NewGuid(),
            AuditActions.Login,
            "User",
            Guid.NewGuid(),
            NewValues: new
            {
                Email = "user@example.com",
                Password = "secret",
                PasswordHash = "hash",
                RefreshToken = "refresh-token",
                Token = "access-token"
            }));

        var values = repository.AuditLogs.Single().NewValues;
        values.Should().Contain("user@example.com");
        values.Should().NotContain("secret");
        values.Should().NotContain("hash");
        values.Should().NotContain("refresh-token");
        values.Should().NotContain("access-token");
        values.Should().NotContain("Password");
        values.Should().NotContain("Token");
    }

    [Fact]
    public async Task RecordAsyncStoresCorrelationIdFromContext()
    {
        var repository = new InMemoryAuditLogRepository();
        var service = new AuditLogService(repository, new FixedAuditContext("correlation-test"));

        await service.RecordAsync(new AuditLogEntry(
            Guid.NewGuid(),
            AuditActions.Logout,
            "User",
            Guid.NewGuid()));

        repository.AuditLogs.Single().CorrelationId.Should().Be("correlation-test");
    }

    private sealed class FixedAuditContext : IAuditContext
    {
        private readonly string correlationId;

        public FixedAuditContext(string correlationId)
        {
            this.correlationId = correlationId;
        }

        public AuditContextData GetCurrent()
        {
            return new AuditContextData("127.0.0.1", "Unit Test", correlationId);
        }
    }

    private sealed class InMemoryAuditLogRepository : IAuditLogRepository
    {
        public List<AuditLog> AuditLogs { get; } = [];

        public Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
        {
            AuditLogs.Add(auditLog);

            return Task.CompletedTask;
        }

        public Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(AuditLogs.SingleOrDefault(auditLog => auditLog.Id == id));
        }

        public Task<PagedResponse<AuditLog>> ListAsync(
            AuditLogListRequest request,
            CancellationToken cancellationToken = default)
        {
            var listRequest = request.ToListRequest();

            return Task.FromResult(PagedResponse<AuditLog>.Create(
                AuditLogs.Skip(listRequest.Offset).Take(listRequest.NormalizedPageSize).ToArray(),
                listRequest.NormalizedPage,
                listRequest.NormalizedPageSize,
                AuditLogs.Count));
        }
    }
}
