using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Berca_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberCreditSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CreditLimit",
                table: "Members",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CreditScore",
                table: "Members",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CreditStatus",
                table: "Members",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentDebt",
                table: "Members",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPaymentDate",
                table: "Members",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LifetimeDebt",
                table: "Members",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextPaymentDueDate",
                table: "Members",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentDelays",
                table: "Members",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PaymentTerms",
                table: "Members",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "MemberCreditTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberCreditTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberCreditTransactions_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MemberCreditTransactions_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MemberCreditTransactions_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MemberPaymentReminders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberId = table.Column<int>(type: "int", nullable: false),
                    ReminderType = table.Column<int>(type: "int", nullable: false),
                    ReminderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DaysOverdue = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ResponseDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResponseAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NextReminderDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ContactMethod = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberPaymentReminders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberPaymentReminders_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MemberPaymentReminders_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MemberPaymentReminders_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemberCreditTransactions_BranchId",
                table: "MemberCreditTransactions",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberCreditTransactions_CreatedBy",
                table: "MemberCreditTransactions",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_MemberCreditTransactions_DueDate",
                table: "MemberCreditTransactions",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_MemberCreditTransactions_Member_Type_Status",
                table: "MemberCreditTransactions",
                columns: new[] { "MemberId", "Type", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberCreditTransactions_MemberId",
                table: "MemberCreditTransactions",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberCreditTransactions_Status",
                table: "MemberCreditTransactions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MemberCreditTransactions_TransactionDate",
                table: "MemberCreditTransactions",
                column: "TransactionDate");

            migrationBuilder.CreateIndex(
                name: "IX_MemberCreditTransactions_Type",
                table: "MemberCreditTransactions",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_MemberCreditTransactions_Type_DueDate",
                table: "MemberCreditTransactions",
                columns: new[] { "Type", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberPaymentReminders_BranchId",
                table: "MemberPaymentReminders",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberPaymentReminders_CreatedBy",
                table: "MemberPaymentReminders",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_MemberPaymentReminders_Member_Status",
                table: "MemberPaymentReminders",
                columns: new[] { "MemberId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberPaymentReminders_MemberId",
                table: "MemberPaymentReminders",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberPaymentReminders_NextReminder_Status",
                table: "MemberPaymentReminders",
                columns: new[] { "NextReminderDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberPaymentReminders_NextReminderDate",
                table: "MemberPaymentReminders",
                column: "NextReminderDate");

            migrationBuilder.CreateIndex(
                name: "IX_MemberPaymentReminders_Priority",
                table: "MemberPaymentReminders",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_MemberPaymentReminders_ReminderDate",
                table: "MemberPaymentReminders",
                column: "ReminderDate");

            migrationBuilder.CreateIndex(
                name: "IX_MemberPaymentReminders_ReminderType",
                table: "MemberPaymentReminders",
                column: "ReminderType");

            migrationBuilder.CreateIndex(
                name: "IX_MemberPaymentReminders_Status",
                table: "MemberPaymentReminders",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemberCreditTransactions");

            migrationBuilder.DropTable(
                name: "MemberPaymentReminders");

            migrationBuilder.DropColumn(
                name: "CreditLimit",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "CreditScore",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "CreditStatus",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "CurrentDebt",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "LastPaymentDate",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "LifetimeDebt",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "NextPaymentDueDate",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "PaymentDelays",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "PaymentTerms",
                table: "Members");
        }
    }
}
