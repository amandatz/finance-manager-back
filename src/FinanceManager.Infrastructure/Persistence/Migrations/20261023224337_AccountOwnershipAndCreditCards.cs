using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AccountOwnershipAndCreditCards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "household_id",
                table: "financial_accounts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<List<Guid>>(
                name: "owner_user_ids",
                table: "financial_accounts",
                type: "uuid[]",
                nullable: false);

            migrationBuilder.CreateTable(
                name: "credit_cards",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    brand = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    credit_limit = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    statement_closing_day = table.Column<int>(type: "integer", nullable: false),
                    payment_due_day = table.Column<int>(type: "integer", nullable: false),
                    settlement_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    owner_user_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    household_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_credit_cards", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_financial_accounts_household_id",
                table: "financial_accounts",
                column: "household_id");

            migrationBuilder.CreateIndex(
                name: "ix_credit_cards_household_id",
                table: "credit_cards",
                column: "household_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "credit_cards");

            migrationBuilder.DropIndex(
                name: "ix_financial_accounts_household_id",
                table: "financial_accounts");

            migrationBuilder.DropColumn(
                name: "household_id",
                table: "financial_accounts");

            migrationBuilder.DropColumn(
                name: "owner_user_ids",
                table: "financial_accounts");
        }
    }
}
