using FluentMigrator;

namespace PriceWise.Infrastructure.Migrations;

[Migration(202606032120)]
public sealed class CreatePriceCheckExecutionsTable : Migration
{
    public override void Up()
    {
        Create.Table("price_check_executions")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("started_at").AsDateTime().NotNullable()
            .WithColumn("completed_at").AsDateTime().NotNullable()
            .WithColumn("status").AsString(80).NotNullable()
            .WithColumn("message").AsString(500).NotNullable()
            .WithColumn("products_checked").AsInt32().NotNullable()
            .WithColumn("histories_created").AsInt32().NotNullable()
            .WithColumn("products_skipped").AsInt32().NotNullable()
            .WithColumn("products_failed").AsInt32().NotNullable()
            .WithColumn("created_at_utc").AsDateTime().NotNullable();

        Create.Index("ix_price_check_executions_completed_at")
            .OnTable("price_check_executions")
            .OnColumn("completed_at").Descending();
    }

    public override void Down()
    {
        Delete.Table("price_check_executions");
    }
}
