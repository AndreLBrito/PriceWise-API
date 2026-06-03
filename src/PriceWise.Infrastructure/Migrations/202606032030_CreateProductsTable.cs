using FluentMigrator;

namespace PriceWise.Infrastructure.Migrations;

[Migration(202606032030)]
public sealed class CreateProductsTable : Migration
{
    public override void Up()
    {
        Create.Table("products")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("user_id").AsGuid().NotNullable()
            .WithColumn("name").AsString(150).NotNullable()
            .WithColumn("description").AsString(500).Nullable()
            .WithColumn("brand").AsString(100).Nullable()
            .WithColumn("category").AsString(100).Nullable()
            .WithColumn("product_url").AsString(2048).NotNullable()
            .WithColumn("image_url").AsString(2048).Nullable()
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("created_at_utc").AsDateTime().NotNullable()
            .WithColumn("created_by").AsString(120).Nullable()
            .WithColumn("updated_at_utc").AsDateTime().Nullable()
            .WithColumn("updated_by").AsString(120).Nullable();

        Create.ForeignKey("fk_products_users")
            .FromTable("products").ForeignColumn("user_id")
            .ToTable("users").PrimaryColumn("id");

        Create.Index("ix_products_user_product_url_active")
            .OnTable("products")
            .OnColumn("user_id").Ascending()
            .OnColumn("product_url").Ascending()
            .OnColumn("is_active").Ascending()
            .WithOptions().Unique();
    }

    public override void Down()
    {
        Delete.Table("products");
    }
}
