using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RewardService.Data.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Criteria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    TriggerEvent = table.Column<string>(type: "text", nullable: false),
                    PointsPerUnit = table.Column<int>(type: "integer", nullable: false),
                    MinimumAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    BonusPoints = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Criteria", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_Transactions", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Redemptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PointsRedeemed = table.Column<int>(type: "integer", nullable: false),
                    MoneyAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_Redemptions", x => x.Id));

            migrationBuilder.CreateIndex("IX_Transactions_UserId", "Transactions", "UserId");
            migrationBuilder.CreateIndex("IX_Transactions_IdempotencyKey", "Transactions", "IdempotencyKey",
                unique: true, filter: "\"IdempotencyKey\" IS NOT NULL");
            migrationBuilder.CreateIndex("IX_Redemptions_IdempotencyKey", "Redemptions", "IdempotencyKey",
                unique: true, filter: "\"IdempotencyKey\" IS NOT NULL");

            // Seed default reward criteria
            migrationBuilder.InsertData(
                table: "Criteria",
                columns: new[] { "Id", "Name", "TriggerEvent", "PointsPerUnit", "MinimumAmount", "BonusPoints", "IsActive", "CreatedAt" },
                values: new object[,]
                {
                    { Guid.Parse("00000000-0000-0000-0000-000000000001"), "Purchase points", "purchase", 10, 1.00m, null, true, DateTime.UtcNow },
                    { Guid.Parse("00000000-0000-0000-0000-000000000002"), "Referral bonus", "referral", 0, null, 500, true, DateTime.UtcNow },
                    { Guid.Parse("00000000-0000-0000-0000-000000000003"), "Daily login bonus", "daily_login", 0, null, 10, true, DateTime.UtcNow }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Criteria");
            migrationBuilder.DropTable(name: "Transactions");
            migrationBuilder.DropTable(name: "Redemptions");
        }
    }
}
