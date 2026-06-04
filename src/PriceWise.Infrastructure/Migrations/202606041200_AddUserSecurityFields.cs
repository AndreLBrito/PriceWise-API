using FluentMigrator;

namespace PriceWise.Infrastructure.Migrations;

[Migration(202606041200)]
public sealed class AddUserSecurityFields : Migration
{
    public override void Up()
    {
        Alter.Table("users")
            .AddColumn("role").AsString(30).NotNullable().WithDefaultValue("User")
            .AddColumn("failed_login_attempts").AsInt32().NotNullable().WithDefaultValue(0)
            .AddColumn("locked_until_utc").AsDateTime().Nullable();

        Create.Index("ix_users_role")
            .OnTable("users")
            .OnColumn("role").Ascending();
    }

    public override void Down()
    {
        Delete.Index("ix_users_role").OnTable("users");
        Delete.Column("locked_until_utc").FromTable("users");
        Delete.Column("failed_login_attempts").FromTable("users");
        Delete.Column("role").FromTable("users");
    }
}
