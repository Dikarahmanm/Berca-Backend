using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Berca_Backend.Migrations
{
    /// <inheritdoc />
    public partial class FixPaymentMethodDataType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, convert string values to their corresponding enum integer values
            migrationBuilder.Sql(@"
                UPDATE Sales 
                SET PaymentMethod = CASE 
                    WHEN PaymentMethod = 'cash' THEN '2'
                    WHEN PaymentMethod = 'credit' THEN '5'  -- MemberCredit = 5
                    WHEN PaymentMethod = 'card' THEN '3'     -- CreditCard = 3
                    WHEN PaymentMethod = 'transfer' THEN '0' -- BankTransfer = 0
                    WHEN PaymentMethod = 'check' THEN '1'    -- Check = 1
                    WHEN PaymentMethod = 'digital' THEN '4'  -- DigitalPayment = 4
                    WHEN PaymentMethod = 'mixed' THEN '6'    -- Mixed = 6
                    WHEN PaymentMethod = 'points' THEN '7'   -- Points = 7
                    ELSE PaymentMethod  -- Keep existing numeric values
                END
                WHERE PaymentMethod IN ('cash', 'credit', 'card', 'transfer', 'check', 'digital', 'mixed', 'points');
            ");

            // Convert any remaining non-numeric values to default (Cash = 2)
            migrationBuilder.Sql(@"
                UPDATE Sales 
                SET PaymentMethod = '2'  -- Default to Cash
                WHERE PaymentMethod NOT LIKE '[0-9]%';
            ");

            // Now alter the column type from nvarchar to int
            migrationBuilder.AlterColumn<int>(
                name: "PaymentMethod",
                table: "Sales",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert column type back to string
            migrationBuilder.AlterColumn<string>(
                name: "PaymentMethod",
                table: "Sales",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            // Convert back to string values (optional - may lose precision)
            migrationBuilder.Sql(@"
                UPDATE Sales 
                SET PaymentMethod = CASE 
                    WHEN PaymentMethod = '0' THEN 'transfer'
                    WHEN PaymentMethod = '1' THEN 'check'
                    WHEN PaymentMethod = '2' THEN 'cash'
                    WHEN PaymentMethod = '3' THEN 'card'
                    WHEN PaymentMethod = '4' THEN 'digital'
                    WHEN PaymentMethod = '5' THEN 'credit'
                    WHEN PaymentMethod = '6' THEN 'mixed'
                    WHEN PaymentMethod = '7' THEN 'points'
                    ELSE 'cash'  -- Default fallback
                END;
            ");
        }
    }
}
