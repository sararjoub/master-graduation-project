using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillsSwap.Migrations
{
    /// <inheritdoc />
    public partial class Phase06SubscriptionEntitlements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_UserId",
                table: "Subscriptions");

            migrationBuilder.AddColumn<int>(
                name: "BillingPeriod",
                table: "Subscriptions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PurchasedMonthlyCredits",
                table: "Subscriptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "AdditionalExpertPrice",
                table: "Plans",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ExpertSessionMinutes",
                table: "Plans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ExpertSharePercent",
                table: "Plans",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "ExpertCreditAllocation",
                columns: table => new
                {
                    SubscriptionId = table.Column<int>(type: "int", nullable: false),
                    StartsAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndsAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Allocated = table.Column<int>(type: "int", nullable: false),
                    Remaining = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpertCreditAllocation", x => new { x.SubscriptionId, x.StartsAtUtc });
                    table.CheckConstraint("CK_ExpertCredits_PeriodBalance", "[StartsAtUtc] < [EndsAtUtc] AND [Allocated] >= 0 AND [Remaining] BETWEEN 0 AND [Allocated]");
                    table.ForeignKey(
                        name: "FK_ExpertCreditAllocation_Subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                EXEC(N'UPDATE dbo.Plans SET AdditionalExpertPrice=5, ExpertSessionMinutes=30, ExpertSharePercent=70
                WHERE Code=N''PLUS'';');
                UPDATE dbo.Plans SET MonthlyPrice=14.99, YearlyPrice=149.99, MonthlyExpertCredits=2
                WHERE Code=N'PLUS' AND MonthlyPrice=9 AND YearlyPrice=84 AND MonthlyExpertCredits=1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_UserId_Status_EndsAtUtc",
                table: "Subscriptions",
                columns: new[] { "UserId", "Status", "EndsAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM dbo.Subscriptions WHERE BillingPeriod IS NOT NULL)
                    OR EXISTS (SELECT 1 FROM dbo.ExpertCreditAllocation)
                    THROW 51006, 'Rollback would remove purchased subscription or credit history.', 1;
                IF EXISTS (SELECT 1 FROM dbo.Plans WHERE Code=N'PLUS' AND
                    (AdditionalExpertPrice<>5 OR ExpertSessionMinutes<>30 OR ExpertSharePercent<>70))
                    THROW 51006, 'Rollback would remove custom expert finance configuration.', 1;
                """);
            migrationBuilder.DropTable(
                name: "ExpertCreditAllocation");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_UserId_Status_EndsAtUtc",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "BillingPeriod",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "PurchasedMonthlyCredits",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "AdditionalExpertPrice",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "ExpertSessionMinutes",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "ExpertSharePercent",
                table: "Plans");

            migrationBuilder.Sql("""
                UPDATE dbo.Plans SET MonthlyPrice=9, YearlyPrice=84, MonthlyExpertCredits=1
                WHERE Code=N'PLUS' AND MonthlyPrice=14.99 AND YearlyPrice=149.99 AND MonthlyExpertCredits=2;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_UserId",
                table: "Subscriptions",
                column: "UserId");
        }
    }
}
