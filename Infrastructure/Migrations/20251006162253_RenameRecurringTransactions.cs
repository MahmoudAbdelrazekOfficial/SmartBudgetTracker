using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameRecurringTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecurringTransaction_Accounts_AccountId",
                table: "RecurringTransaction");

            migrationBuilder.DropForeignKey(
                name: "FK_RecurringTransaction_AspNetUsers_UserId",
                table: "RecurringTransaction");

            migrationBuilder.DropForeignKey(
                name: "FK_RecurringTransaction_Categories_CategoryId",
                table: "RecurringTransaction");

            migrationBuilder.DropForeignKey(
                name: "FK_RecurringTransaction_Frequencies_FrequencyId",
                table: "RecurringTransaction");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_RecurringTransaction_RecurringTransactionId",
                table: "Transactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RecurringTransaction",
                table: "RecurringTransaction");

            migrationBuilder.RenameTable(
                name: "RecurringTransaction",
                newName: "RecurringTransactions");

            migrationBuilder.RenameIndex(
                name: "IX_RecurringTransaction_UserId",
                table: "RecurringTransactions",
                newName: "IX_RecurringTransactions_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_RecurringTransaction_FrequencyId",
                table: "RecurringTransactions",
                newName: "IX_RecurringTransactions_FrequencyId");

            migrationBuilder.RenameIndex(
                name: "IX_RecurringTransaction_CategoryId",
                table: "RecurringTransactions",
                newName: "IX_RecurringTransactions_CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_RecurringTransaction_AccountId",
                table: "RecurringTransactions",
                newName: "IX_RecurringTransactions_AccountId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RecurringTransactions",
                table: "RecurringTransactions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RecurringTransactions_Accounts_AccountId",
                table: "RecurringTransactions",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RecurringTransactions_AspNetUsers_UserId",
                table: "RecurringTransactions",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RecurringTransactions_Categories_CategoryId",
                table: "RecurringTransactions",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RecurringTransactions_Frequencies_FrequencyId",
                table: "RecurringTransactions",
                column: "FrequencyId",
                principalTable: "Frequencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_RecurringTransactions_RecurringTransactionId",
                table: "Transactions",
                column: "RecurringTransactionId",
                principalTable: "RecurringTransactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecurringTransactions_Accounts_AccountId",
                table: "RecurringTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_RecurringTransactions_AspNetUsers_UserId",
                table: "RecurringTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_RecurringTransactions_Categories_CategoryId",
                table: "RecurringTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_RecurringTransactions_Frequencies_FrequencyId",
                table: "RecurringTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_RecurringTransactions_RecurringTransactionId",
                table: "Transactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RecurringTransactions",
                table: "RecurringTransactions");

            migrationBuilder.RenameTable(
                name: "RecurringTransactions",
                newName: "RecurringTransaction");

            migrationBuilder.RenameIndex(
                name: "IX_RecurringTransactions_UserId",
                table: "RecurringTransaction",
                newName: "IX_RecurringTransaction_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_RecurringTransactions_FrequencyId",
                table: "RecurringTransaction",
                newName: "IX_RecurringTransaction_FrequencyId");

            migrationBuilder.RenameIndex(
                name: "IX_RecurringTransactions_CategoryId",
                table: "RecurringTransaction",
                newName: "IX_RecurringTransaction_CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_RecurringTransactions_AccountId",
                table: "RecurringTransaction",
                newName: "IX_RecurringTransaction_AccountId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RecurringTransaction",
                table: "RecurringTransaction",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RecurringTransaction_Accounts_AccountId",
                table: "RecurringTransaction",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RecurringTransaction_AspNetUsers_UserId",
                table: "RecurringTransaction",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RecurringTransaction_Categories_CategoryId",
                table: "RecurringTransaction",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RecurringTransaction_Frequencies_FrequencyId",
                table: "RecurringTransaction",
                column: "FrequencyId",
                principalTable: "Frequencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_RecurringTransaction_RecurringTransactionId",
                table: "Transactions",
                column: "RecurringTransactionId",
                principalTable: "RecurringTransaction",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
