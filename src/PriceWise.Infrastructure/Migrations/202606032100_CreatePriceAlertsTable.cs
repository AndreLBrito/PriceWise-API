using FluentMigrator;

namespace PriceWise.Infrastructure.Migrations;

[Migration(202606032100)]
public sealed class CreatePriceAlertsTable : Migration
{
    public override void Up()
    {
        Create.Table("price_alerts")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("user_id").AsGuid().NotNullable()
            .WithColumn("product_id").AsGuid().NotNullable()
            .WithColumn("target_price").AsDecimal(18, 2).NotNullable()
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("created_at_utc").AsDateTime().NotNullable()
            .WithColumn("created_by").AsString(120).Nullable()
            .WithColumn("updated_at_utc").AsDateTime().Nullable()
            .WithColumn("updated_by").AsString(120).Nullable();

        Create.ForeignKey("fk_price_alerts_users")
            .FromTable("price_alerts").ForeignColumn("user_id")
            .ToTable("users").PrimaryColumn("id");

        Create.ForeignKey("fk_price_alerts_products")
            .FromTable("price_alerts").ForeignColumn("product_id")
            .ToTable("products").PrimaryColumn("id");

        Create.Index("ix_price_alerts_user_product_active")
            .OnTable("price_alerts")
            .OnColumn("user_id").Ascending()
            .OnColumn("product_id").Ascending()
            .OnColumn("is_active").Ascending()
            .WithOptions().Unique();
    }

    public override void Down()
    {
        Delete.Table("price_alerts");
    }
}
