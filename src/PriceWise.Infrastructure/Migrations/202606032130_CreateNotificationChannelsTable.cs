using FluentMigrator;

namespace PriceWise.Infrastructure.Migrations;

[Migration(202606032130)]
public sealed class CreateNotificationChannelsTable : Migration
{
    public override void Up()
    {
        Create.Table("notification_channels")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("user_id").AsGuid().NotNullable()
            .WithColumn("type").AsString(40).NotNullable()
            .WithColumn("name").AsString(120).NotNullable()
            .WithColumn("destination").AsString(2048).NotNullable()
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("created_at_utc").AsDateTime().NotNullable()
            .WithColumn("created_by").AsString(120).Nullable()
            .WithColumn("updated_at_utc").AsDateTime().Nullable()
            .WithColumn("updated_by").AsString(120).Nullable();

        Create.ForeignKey("fk_notification_channels_users")
            .FromTable("notification_channels").ForeignColumn("user_id")
            .ToTable("users").PrimaryColumn("id");

        Create.Index("ix_notification_channels_user_active")
            .OnTable("notification_channels")
            .OnColumn("user_id").Ascending()
            .OnColumn("is_active").Ascending();

        Create.Index("ix_notification_channels_user_type_destination_active")
            .OnTable("notification_channels")
            .OnColumn("user_id").Ascending()
            .OnColumn("type").Ascending()
            .OnColumn("destination").Ascending()
            .OnColumn("is_active").Ascending();
    }

    public override void Down()
    {
        Delete.Table("notification_channels");
    }
}
