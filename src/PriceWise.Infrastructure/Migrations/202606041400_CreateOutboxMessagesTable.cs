using FluentMigrator;

namespace PriceWise.Infrastructure.Migrations;

[Migration(202606041400)]
public sealed class CreateOutboxMessagesTable : Migration
{
    public override void Up()
    {
        Create.Table("outbox_messages")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("type").AsString(120).NotNullable()
            .WithColumn("payload").AsString(int.MaxValue).NotNullable()
            .WithColumn("status").AsString(40).NotNullable()
            .WithColumn("retry_count").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("max_retries").AsInt32().NotNullable()
            .WithColumn("next_attempt_at").AsDateTime().NotNullable()
            .WithColumn("processed_at").AsDateTime().Nullable()
            .WithColumn("error_message").AsString(1000).Nullable()
            .WithColumn("correlation_id").AsString(100).Nullable()
            .WithColumn("created_at_utc").AsDateTime().NotNullable()
            .WithColumn("updated_at_utc").AsDateTime().Nullable();

        Create.Index("ix_outbox_messages_status_next_attempt")
            .OnTable("outbox_messages")
            .OnColumn("status").Ascending()
            .OnColumn("next_attempt_at").Ascending();

        Create.Index("ix_outbox_messages_created_at_utc")
            .OnTable("outbox_messages")
            .OnColumn("created_at_utc").Descending();
    }

    public override void Down()
    {
        Delete.Table("outbox_messages");
    }
}
