using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditCardTransactionsAndSettlement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "financial_account_id",
                table: "transactions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "category_id",
                table: "transactions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "credit_card_id",
                table: "transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "settlement_group_id",
                table: "transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_transactions_credit_card_id",
                table: "transactions",
                column: "credit_card_id");

            migrationBuilder.CreateIndex(
                name: "ix_transactions_settlement_group_id",
                table: "transactions",
                column: "settlement_group_id");

            migrationBuilder.AddForeignKey(
                name: "fk_transactions_credit_cards_credit_card_id",
                table: "transactions",
                column: "credit_card_id",
                principalTable: "credit_cards",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_transactions_financial_accounts_financial_account_id",
                table: "transactions",
                column: "financial_account_id",
                principalTable: "financial_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_transactions_credit_cards_credit_card_id",
                table: "transactions");

            migrationBuilder.DropForeignKey(
                name: "fk_transactions_financial_accounts_financial_account_id",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "ix_transactions_credit_card_id",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "ix_transactions_settlement_group_id",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "credit_card_id",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "settlement_group_id",
                table: "transactions");

            migrationBuilder.AlterColumn<Guid>(
                name: "financial_account_id",
                table: "transactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "category_id",
                table: "transactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
