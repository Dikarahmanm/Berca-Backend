using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Berca_Backend.Migrations
{
    /// <summary>
    /// Migration to add Sale-Credit integration support for POS credit transactions
    /// </summary>
    public partial class AddSaleCreditIntegration : Migration
    {
        /// <summary>
        /// Adds credit transaction fields to Sales table and creates foreign key relationships
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add credit transaction fields to Sales table
            migrationBuilder.AddColumn<decimal>(
                name: "CreditAmount",
                table: "Sales",
                type: "decimal(18,2)",
                nullable: true,
                comment: "Amount paid using member credit");

            migrationBuilder.AddColumn<bool>(
                name: "IsCreditTransaction",
                table: "Sales",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "Indicates if this sale used member credit");

            migrationBuilder.AddColumn<int>(
                name: "CreditTransactionId",
                table: "Sales",
                type: "int",
                nullable: true,
                comment: "Reference to the member credit transaction");

            // Update PaymentMethod column to support new enum values
            // Note: This assumes PaymentMethod is stored as int
            // If it's stored as string, we would need to handle differently
            
            // Create foreign key constraint to MemberCreditTransactions
            migrationBuilder.CreateIndex(
                name: "IX_Sales_CreditTransactionId",
                table: "Sales",
                column: "CreditTransactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_MemberCreditTransactions_CreditTransactionId",
                table: "Sales",
                column: "CreditTransactionId",
                principalTable: "MemberCreditTransactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Create index for performance on credit transactions lookup
            migrationBuilder.CreateIndex(
                name: "IX_Sales_MemberId_IsCreditTransaction",
                table: "Sales",
                columns: new[] { "MemberId", "IsCreditTransaction" });

            // Create index for payment method queries
            migrationBuilder.CreateIndex(
                name: "IX_Sales_PaymentMethod",
                table: "Sales",
                column: "PaymentMethod");

            // Update any existing data if needed
            // Set default PaymentMethod to Cash (0) for existing records if NULL
            migrationBuilder.Sql(@"
                UPDATE Sales 
                SET PaymentMethod = 0 
                WHERE PaymentMethod IS NULL OR PaymentMethod = '';
            ");
        }

        /// <summary>
        /// Removes Sale-Credit integration changes
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop foreign key constraint
            migrationBuilder.DropForeignKey(
                name: "FK_Sales_MemberCreditTransactions_CreditTransactionId",
                table: "Sales");

            // Drop indexes
            migrationBuilder.DropIndex(
                name: "IX_Sales_CreditTransactionId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_MemberId_IsCreditTransaction",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_PaymentMethod",
                table: "Sales");

            // Remove credit transaction columns
            migrationBuilder.DropColumn(
                name: "CreditAmount",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "IsCreditTransaction",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "CreditTransactionId",
                table: "Sales");
        }
    }
}