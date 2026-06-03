using FluentMigrator;

namespace PriceWise.Infrastructure.Migrations;

[Migration(202606032040)]
public sealed class CreateStoresTable : Migration
{
    public override void Up()
    {
        Create.Table("stores")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("user_id").AsGuid().NotNullable()
            .WithColumn("name").AsString(120).NotNullable()
            .WithColumn("base_url").AsString(2048).NotNullable()
            .WithColumn("logo_url").AsString(2048).Nullable()
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("created_at_utc").AsDateTime().NotNullable()
            .WithColumn("created_by").AsString(120).Nullable()
            .WithColumn("updated_at_utc").AsDateTime().Nullable()
            .WithColumn("updated_by").AsString(120).Nullable();

        Create.ForeignKey("fk_stores_users")
            .FromTable("stores").ForeignColumn("user_id")
            .ToTable("users").PrimaryColumn("id");

        Create.Index("ix_stores_user_base_url_active")
            .OnTable("stores")
            .OnColumn("user_id").Ascending()
            .OnColumn("base_url").Ascending()
            .OnColumn("is_active").Ascending()
            .WithOptions().Unique();
    }

    public override void Down()
    {
        Delete.Table("stores");
    }
}
