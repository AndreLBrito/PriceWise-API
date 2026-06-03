using FluentMigrator;

namespace PriceWise.Infrastructure.Migrations;

[Migration(202606031930)]
public sealed class CreateAuthenticationTables : Migration
{
    public override void Up()
    {
        Create.Table("users")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("name").AsString(120).NotNullable()
            .WithColumn("email").AsString(254).NotNullable()
            .WithColumn("password_hash").AsString(500).NotNullable()
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("created_at_utc").AsDateTime().NotNullable()
            .WithColumn("created_by").AsString(120).Nullable()
            .WithColumn("updated_at_utc").AsDateTime().Nullable()
            .WithColumn("updated_by").AsString(120).Nullable();

        Create.Table("refresh_tokens")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("user_id").AsGuid().NotNullable()
            .WithColumn("token_hash").AsString(128).NotNullable()
            .WithColumn("expires_at_utc").AsDateTime().NotNullable()
            .WithColumn("revoked_at_utc").AsDateTime().Nullable()
            .WithColumn("created_at_utc").AsDateTime().NotNullable()
            .WithColumn("created_by").AsString(120).Nullable()
            .WithColumn("updated_at_utc").AsDateTime().Nullable()
            .WithColumn("updated_by").AsString(120).Nullable();

        Create.ForeignKey("fk_refresh_tokens_users")
            .FromTable("refresh_tokens").ForeignColumn("user_id")
            .ToTable("users").PrimaryColumn("id");

        Create.Index("ix_users_email")
            .OnTable("users")
            .OnColumn("email").Ascending()
            .WithOptions().Unique();

        Create.Index("ix_refresh_tokens_token_hash")
            .OnTable("refresh_tokens")
            .OnColumn("token_hash").Ascending()
            .WithOptions().Unique();
    }

    public override void Down()
    {
        Delete.Table("refresh_tokens");
        Delete.Table("users");
    }
}
