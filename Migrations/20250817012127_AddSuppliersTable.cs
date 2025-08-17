using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Berca_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddSuppliersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransfers_Branches_DestinationBranchId",
                table: "InventoryTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransfers_Branches_SourceBranchId",
                table: "InventoryTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransfers_Users_ApprovedBy",
                table: "InventoryTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransfers_Users_CancelledBy",
                table: "InventoryTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransfers_Users_ReceivedBy",
                table: "InventoryTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransfers_Users_RequestedBy",
                table: "InventoryTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransfers_Users_ShippedBy",
                table: "InventoryTransfers");

            migrationBuilder.RenameColumn(
                name: "SourceBranchId",
                table: "InventoryTransfers",
                newName: "FromBranchId");

            migrationBuilder.RenameColumn(
                name: "ShippedBy",
                table: "InventoryTransfers",
                newName: "ShippedByUserId");

            migrationBuilder.RenameColumn(
                name: "RequestedBy",
                table: "InventoryTransfers",
                newName: "RequestedByUserId");

            migrationBuilder.RenameColumn(
                name: "ReceivedBy",
                table: "InventoryTransfers",
                newName: "ReceivedByUserId");

            migrationBuilder.RenameColumn(
                name: "DestinationBranchId",
                table: "InventoryTransfers",
                newName: "ToBranchId");

            migrationBuilder.RenameColumn(
                name: "CancelledBy",
                table: "InventoryTransfers",
                newName: "CancelledByUserId");

            migrationBuilder.RenameColumn(
                name: "ApprovedBy",
                table: "InventoryTransfers",
                newName: "ApprovedByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryTransfers_ShippedBy",
                table: "InventoryTransfers",
                newName: "IX_InventoryTransfers_ShippedByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryTransfers_RequestedBy",
                table: "InventoryTransfers",
                newName: "IX_InventoryTransfers_RequestedByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryTransfers_ReceivedBy",
                table: "InventoryTransfers",
                newName: "IX_InventoryTransfers_ReceivedByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryTransfers_CancelledBy",
                table: "InventoryTransfers",
                newName: "IX_InventoryTransfers_CancelledByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryTransfers_ApprovedBy",
                table: "InventoryTransfers",
                newName: "IX_InventoryTransfers_ApprovedByUserId");

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ContactPerson = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PaymentTerms = table.Column<int>(type: "int", nullable: false),
                    CreditLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Suppliers_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Suppliers_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Suppliers_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Branch_Status",
                table: "Suppliers",
                columns: new[] { "BranchId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_CompanyName",
                table: "Suppliers",
                column: "CompanyName");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_CreatedBy",
                table: "Suppliers",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_CreditLimit",
                table: "Suppliers",
                column: "CreditLimit");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Email",
                table: "Suppliers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_PaymentTerms",
                table: "Suppliers",
                column: "PaymentTerms");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_SupplierCode",
                table: "Suppliers",
                column: "SupplierCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_UpdatedBy",
                table: "Suppliers",
                column: "UpdatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransfers_Branches_FromBranchId",
                table: "InventoryTransfers",
                column: "FromBranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransfers_Branches_ToBranchId",
                table: "InventoryTransfers",
                column: "ToBranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransfers_Users_ApprovedByUserId",
                table: "InventoryTransfers",
                column: "ApprovedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransfers_Users_CancelledByUserId",
                table: "InventoryTransfers",
                column: "CancelledByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransfers_Users_ReceivedByUserId",
                table: "InventoryTransfers",
                column: "ReceivedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransfers_Users_RequestedByUserId",
                table: "InventoryTransfers",
                column: "RequestedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransfers_Users_ShippedByUserId",
                table: "InventoryTransfers",
                column: "ShippedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransfers_Branches_FromBranchId",
                table: "InventoryTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransfers_Branches_ToBranchId",
                table: "InventoryTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransfers_Users_ApprovedByUserId",
                table: "InventoryTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransfers_Users_CancelledByUserId",
                table: "InventoryTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransfers_Users_ReceivedByUserId",
                table: "InventoryTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransfers_Users_RequestedByUserId",
                table: "InventoryTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransfers_Users_ShippedByUserId",
                table: "InventoryTransfers");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.RenameColumn(
                name: "ToBranchId",
                table: "InventoryTransfers",
                newName: "DestinationBranchId");

            migrationBuilder.RenameColumn(
                name: "ShippedByUserId",
                table: "InventoryTransfers",
                newName: "ShippedBy");

            migrationBuilder.RenameColumn(
                name: "RequestedByUserId",
                table: "InventoryTransfers",
                newName: "RequestedBy");

            migrationBuilder.RenameColumn(
                name: "ReceivedByUserId",
                table: "InventoryTransfers",
                newName: "ReceivedBy");

            migrationBuilder.RenameColumn(
                name: "FromBranchId",
                table: "InventoryTransfers",
                newName: "SourceBranchId");

            migrationBuilder.RenameColumn(
                name: "CancelledByUserId",
                table: "InventoryTransfers",
                newName: "CancelledBy");

            migrationBuilder.RenameColumn(
                name: "ApprovedByUserId",
                table: "InventoryTransfers",
                newName: "ApprovedBy");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryTransfers_ShippedByUserId",
                table: "InventoryTransfers",
                newName: "IX_InventoryTransfers_ShippedBy");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryTransfers_RequestedByUserId",
                table: "InventoryTransfers",
                newName: "IX_InventoryTransfers_RequestedBy");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryTransfers_ReceivedByUserId",
                table: "InventoryTransfers",
                newName: "IX_InventoryTransfers_ReceivedBy");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryTransfers_CancelledByUserId",
                table: "InventoryTransfers",
                newName: "IX_InventoryTransfers_CancelledBy");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryTransfers_ApprovedByUserId",
                table: "InventoryTransfers",
                newName: "IX_InventoryTransfers_ApprovedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransfers_Branches_DestinationBranchId",
                table: "InventoryTransfers",
                column: "DestinationBranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransfers_Branches_SourceBranchId",
                table: "InventoryTransfers",
                column: "SourceBranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransfers_Users_ApprovedBy",
                table: "InventoryTransfers",
                column: "ApprovedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransfers_Users_CancelledBy",
                table: "InventoryTransfers",
                column: "CancelledBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransfers_Users_ReceivedBy",
                table: "InventoryTransfers",
                column: "ReceivedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransfers_Users_RequestedBy",
                table: "InventoryTransfers",
                column: "RequestedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransfers_Users_ShippedBy",
                table: "InventoryTransfers",
                column: "ShippedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
