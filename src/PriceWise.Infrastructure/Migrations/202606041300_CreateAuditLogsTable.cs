using FluentMigrator;

namespace PriceWise.Infrastructure.Migrations;

[Migration(202606041300)]
public sealed class CreateAuditLogsTable : Migration
{
    public override void Up()
    {
        Create.Table("audit_logs")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("user_id").AsGuid().Nullable()
            .WithColumn("action").AsString(80).NotNullable()
            .WithColumn("entity_name").AsString(120).NotNullable()
            .WithColumn("entity_id").AsGuid().Nullable()
            .WithColumn("old_values").AsString(int.MaxValue).Nullable()
            .WithColumn("new_values").AsString(int.MaxValue).Nullable()
            .WithColumn("ip_address").AsString(80).Nullable()
            .WithColumn("user_agent").AsString(500).Nullable()
            .WithColumn("correlation_id").AsString(100).Nullable()
            .WithColumn("created_at_utc").AsDateTime().NotNullable();

        Create.Index("ix_audit_logs_user_id")
            .OnTable("audit_logs")
            .OnColumn("user_id").Ascending();

        Create.Index("ix_audit_logs_entity")
            .OnTable("audit_logs")
            .OnColumn("entity_name").Ascending()
            .OnColumn("entity_id").Ascending();

        Create.Index("ix_audit_logs_created_at_utc")
            .OnTable("audit_logs")
            .OnColumn("created_at_utc").Descending();
    }

    public override void Down()
    {
        Delete.Table("audit_logs");
    }
}
