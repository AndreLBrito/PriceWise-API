using FluentMigrator;

namespace PriceWise.Infrastructure.Migrations;

[Migration(202606032050)]
public sealed class CreatePriceHistoriesTable : Migration
{
    public override void Up()
    {
        Create.Table("price_histories")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("user_id").AsGuid().NotNullable()
            .WithColumn("product_id").AsGuid().NotNullable()
            .WithColumn("store_id").AsGuid().NotNullable()
            .WithColumn("price").AsDecimal(18, 2).NotNullable()
            .WithColumn("currency").AsString(3).NotNullable()
            .WithColumn("captured_at").AsDateTime().NotNullable()
            .WithColumn("source_url").AsString(2048).Nullable()
            .WithColumn("created_at_utc").AsDateTime().NotNullable()
            .WithColumn("created_by").AsString(120).Nullable()
            .WithColumn("updated_at_utc").AsDateTime().Nullable()
            .WithColumn("updated_by").AsString(120).Nullable();

        Create.ForeignKey("fk_price_histories_users")
            .FromTable("price_histories").ForeignColumn("user_id")
            .ToTable("users").PrimaryColumn("id");

        Create.ForeignKey("fk_price_histories_products")
            .FromTable("price_histories").ForeignColumn("product_id")
            .ToTable("products").PrimaryColumn("id");

        Create.ForeignKey("fk_price_histories_stores")
            .FromTable("price_histories").ForeignColumn("store_id")
            .ToTable("stores").PrimaryColumn("id");

        Create.Index("ix_price_histories_user_product_captured_at")
            .OnTable("price_histories")
            .OnColumn("user_id").Ascending()
            .OnColumn("product_id").Ascending()
            .OnColumn("captured_at").Descending();
    }

    public override void Down()
    {
        Delete.Table("price_histories");
    }
}
