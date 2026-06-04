using FluentMigrator;

namespace PriceWise.Infrastructure.Migrations;

[Migration(202606032110)]
public sealed class CreateAlertNotificationsTable : Migration
{
    public override void Up()
    {
        Create.Table("alert_notifications")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("user_id").AsGuid().NotNullable()
            .WithColumn("price_alert_id").AsGuid().NotNullable()
            .WithColumn("product_id").AsGuid().NotNullable()
            .WithColumn("price_history_id").AsGuid().NotNullable()
            .WithColumn("triggered_price").AsDecimal(18, 2).NotNullable()
            .WithColumn("target_price").AsDecimal(18, 2).NotNullable()
            .WithColumn("triggered_at").AsDateTime().NotNullable()
            .WithColumn("created_at_utc").AsDateTime().NotNullable()
            .WithColumn("created_by").AsString(120).Nullable()
            .WithColumn("updated_at_utc").AsDateTime().Nullable()
            .WithColumn("updated_by").AsString(120).Nullable();

        Create.ForeignKey("fk_alert_notifications_users")
            .FromTable("alert_notifications").ForeignColumn("user_id")
            .ToTable("users").PrimaryColumn("id");

        Create.ForeignKey("fk_alert_notifications_price_alerts")
            .FromTable("alert_notifications").ForeignColumn("price_alert_id")
            .ToTable("price_alerts").PrimaryColumn("id");

        Create.ForeignKey("fk_alert_notifications_products")
            .FromTable("alert_notifications").ForeignColumn("product_id")
            .ToTable("products").PrimaryColumn("id");

        Create.ForeignKey("fk_alert_notifications_price_histories")
            .FromTable("alert_notifications").ForeignColumn("price_history_id")
            .ToTable("price_histories").PrimaryColumn("id");

        Create.Index("ix_alert_notifications_user_triggered_at")
            .OnTable("alert_notifications")
            .OnColumn("user_id").Ascending()
            .OnColumn("triggered_at").Descending();

        Create.Index("ix_alert_notifications_price_alert_price_history")
            .OnTable("alert_notifications")
            .OnColumn("price_alert_id").Ascending()
            .OnColumn("price_history_id").Ascending()
            .WithOptions().Unique();
    }

    public override void Down()
    {
        Delete.Table("alert_notifications");
    }
}
