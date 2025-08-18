using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Berca_Backend.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateComplete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BranchName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ParentBranchId = table.Column<int>(type: "int", nullable: true),
                    BranchType = table.Column<int>(type: "int", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ManagerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    City = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Province = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    OpeningDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StoreSize = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Medium"),
                    EmployeeCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Branches_Branches_ParentBranchId",
                        column: x => x.ParentBranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequiresExpiryDate = table.Column<bool>(type: "bit", nullable: false),
                    DefaultExpiryWarningDays = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LogActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogActivities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Members",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    MemberNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Tier = table.Column<int>(type: "int", nullable: false),
                    JoinDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TotalPoints = table.Column<int>(type: "int", nullable: false),
                    UsedPoints = table.Column<int>(type: "int", nullable: false),
                    TotalSpent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalTransactions = table.Column<int>(type: "int", nullable: false),
                    LastTransactionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Members", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "User"),
                    BranchId = table.Column<int>(type: "int", nullable: true),
                    CanAccessMultipleBranches = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AccessibleBranchIds = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BuyPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SellPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Stock = table.Column<int>(type: "int", nullable: false),
                    MinimumStock = table.Column<int>(type: "int", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "pcs"),
                    ImageUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryTransfers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransferNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    FromBranchId = table.Column<int>(type: "int", nullable: false),
                    ToBranchId = table.Column<int>(type: "int", nullable: false),
                    RequestReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    ActualCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: false),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ShippedByUserId = table.Column<int>(type: "int", nullable: true),
                    ShippedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReceivedByUserId = table.Column<int>(type: "int", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledByUserId = table.Column<int>(type: "int", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LogisticsProvider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrackingNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EstimatedDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DistanceKm = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryTransfers_Branches_FromBranchId",
                        column: x => x.FromBranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryTransfers_Branches_ToBranchId",
                        column: x => x.ToBranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryTransfers_Users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryTransfers_Users_CancelledByUserId",
                        column: x => x.CancelledByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryTransfers_Users_ReceivedByUserId",
                        column: x => x.ReceivedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryTransfers_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryTransfers_Users_ShippedByUserId",
                        column: x => x.ShippedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    ArchivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActionUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ActionText = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RelatedEntity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RelatedEntityId = table.Column<int>(type: "int", nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    EmailEnabled = table.Column<bool>(type: "bit", nullable: false),
                    EmailLowStock = table.Column<bool>(type: "bit", nullable: false),
                    EmailMonthlyReport = table.Column<bool>(type: "bit", nullable: false),
                    EmailSystemUpdates = table.Column<bool>(type: "bit", nullable: false),
                    InAppEnabled = table.Column<bool>(type: "bit", nullable: false),
                    InAppLowStock = table.Column<bool>(type: "bit", nullable: false),
                    InAppSales = table.Column<bool>(type: "bit", nullable: false),
                    InAppSystem = table.Column<bool>(type: "bit", nullable: false),
                    PushEnabled = table.Column<bool>(type: "bit", nullable: false),
                    PushToken = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    LowStockThreshold = table.Column<int>(type: "int", nullable: false),
                    QuietHoursStart = table.Column<TimeSpan>(type: "time", nullable: false),
                    QuietHoursEnd = table.Column<TimeSpan>(type: "time", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SaleNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SaleDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ChangeAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PaymentReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MemberId = table.Column<int>(type: "int", nullable: true),
                    CustomerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CashierId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReceiptPrinted = table.Column<bool>(type: "bit", nullable: false),
                    ReceiptPrintedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RefundedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RefundReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OriginalSaleId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    DiscountPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    RedeemedPoints = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sales_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Sales_Users_CashierId",
                        column: x => x.CashierId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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

            migrationBuilder.CreateTable(
                name: "UserNotificationSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    EmailEnabled = table.Column<bool>(type: "bit", nullable: false),
                    InAppEnabled = table.Column<bool>(type: "bit", nullable: false),
                    EmailLowStock = table.Column<bool>(type: "bit", nullable: false),
                    EmailMonthlyReport = table.Column<bool>(type: "bit", nullable: false),
                    EmailSystemUpdates = table.Column<bool>(type: "bit", nullable: false),
                    InAppLowStock = table.Column<bool>(type: "bit", nullable: false),
                    InAppSales = table.Column<bool>(type: "bit", nullable: false),
                    InAppSystem = table.Column<bool>(type: "bit", nullable: false),
                    LowStockThreshold = table.Column<int>(type: "int", nullable: false),
                    QuietHoursStart = table.Column<TimeSpan>(type: "time", nullable: false),
                    QuietHoursEnd = table.Column<TimeSpan>(type: "time", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotificationSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserNotificationSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PhotoUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Position = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Division = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Bio = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    BatchNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProductionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CurrentStock = table.Column<int>(type: "int", nullable: false),
                    InitialStock = table.Column<int>(type: "int", nullable: false),
                    CostPerUnit = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    SupplierName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PurchaseOrderNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsBlocked = table.Column<bool>(type: "bit", nullable: false),
                    BlockReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsExpired = table.Column<bool>(type: "bit", nullable: false),
                    IsDisposed = table.Column<bool>(type: "bit", nullable: false),
                    DisposalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DisposedByUserId = table.Column<int>(type: "int", nullable: true),
                    DisposalMethod = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BranchId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductBatches_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProductBatches_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductBatches_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductBatches_Users_DisposedByUserId",
                        column: x => x.DisposedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProductBatches_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryTransferItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InventoryTransferId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    SourceStockBefore = table.Column<int>(type: "int", nullable: false),
                    SourceStockAfter = table.Column<int>(type: "int", nullable: false),
                    DestinationStockBefore = table.Column<int>(type: "int", nullable: true),
                    DestinationStockAfter = table.Column<int>(type: "int", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BatchNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    QualityNotes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTransferItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryTransferItems_InventoryTransfers_InventoryTransferId",
                        column: x => x.InventoryTransferId,
                        principalTable: "InventoryTransfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryTransferItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryTransferStatusHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InventoryTransferId = table.Column<int>(type: "int", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: false),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    ChangedBy = table.Column<int>(type: "int", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTransferStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryTransferStatusHistories_InventoryTransfers_InventoryTransferId",
                        column: x => x.InventoryTransferId,
                        principalTable: "InventoryTransfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryTransferStatusHistories_Users_ChangedBy",
                        column: x => x.ChangedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryMutations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    StockBefore = table.Column<int>(type: "int", nullable: false),
                    StockAfter = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SaleId = table.Column<int>(type: "int", nullable: true),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryMutations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryMutations_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryMutations_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MemberPoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberId = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SaleId = table.Column<int>(type: "int", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TransactionAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PointRate = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsExpired = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberPoints_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MemberPoints_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SaleItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SaleId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProductBarcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "pcs"),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaleItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleItems_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Factures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierInvoiceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InternalReferenceNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: true),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SupplierPONumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveryNoteNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    Tax = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    Discount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReceivedBy = table.Column<int>(type: "int", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiedBy = table.Column<int>(type: "int", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedBy = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SupplierInvoiceFile = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReceiptFile = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SupportingDocs = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DisputeReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Factures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Factures_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Factures_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Factures_Users_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Factures_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Factures_Users_ReceivedBy",
                        column: x => x.ReceivedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Factures_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Factures_Users_VerifiedBy",
                        column: x => x.VerifiedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FactureItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FactureId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    SupplierItemCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SupplierItemDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    AcceptedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    TaxRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 0m),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    VerificationNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactureItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FactureItems_Factures_FactureId",
                        column: x => x.FactureId,
                        principalTable: "Factures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FactureItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FactureItems_Users_VerifiedBy",
                        column: x => x.VerifiedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "FacturePayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FactureId = table.Column<int>(type: "int", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OurPaymentReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SupplierAckReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BankAccount = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CheckNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TransferReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProcessedBy = table.Column<int>(type: "int", nullable: false),
                    ApprovedBy = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConfirmedBy = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DisputeReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PaymentReceiptFile = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ConfirmationFile = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ScheduledDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsRecurring = table.Column<bool>(type: "bit", nullable: false),
                    RecurrencePattern = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacturePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FacturePayments_Factures_FactureId",
                        column: x => x.FactureId,
                        principalTable: "Factures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FacturePayments_Users_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FacturePayments_Users_ConfirmedBy",
                        column: x => x.ConfirmedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FacturePayments_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FacturePayments_Users_ProcessedBy",
                        column: x => x.ProcessedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FacturePayments_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Color", "CreatedAt", "DefaultExpiryWarningDays", "Description", "Name", "RequiresExpiryDate", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "#FF6B35", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, "Mie instan, nasi instan, bubur instan - Indomie, Pop Mie, Sedaap, Sarimi", "Makanan Instan", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, "#FF8E53", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, "Kornet, sarden, buah kaleng, sayur kaleng - Pronas, ABC, Ayam Brand", "Makanan Kaleng", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, "#FFA726", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, "Chitato, Taro, Qtela, Lay's, keripik tradisional, kacang-kacangan", "Snacks & Keripik", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, "#FFB74D", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, "Roma, Monde, Khong Guan, Oreo, wafer Tanggo, Marie Regal", "Biskuit & Wafer", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, "#8D4E85", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, "Kopiko, Ricola, Cadbury, SilverQueen, permen lokal, Mentos", "Permen & Coklat", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 6, "#D2691E", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, "Kue kering, roti tawar, roti manis, donat, cake - Sari Roti, Breadtalk", "Kue & Roti", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 7, "#4FC3F7", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, "Nugget, sosis, bakso beku, frozen food - Fiesta, Bernardi, So Good", "Makanan Beku", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 8, "#29B6F6", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 60, "Aqua, VIT, Club, Pristine, Le Minerale, Cleo", "Air Mineral", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9, "#E53935", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 60, "Coca Cola, Sprite, Fanta, 7UP, Pepsi, Mirinda", "Minuman Ringan", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 10, "#6D4C41", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 60, "Teh Botol, Ultra Teh, Good Day, Kapal Api, Nescafe, Teh Pucuk", "Teh & Kopi Kemasan", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 11, "#FFF8E1", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 60, "Ultra Milk, Indomilk, Frisian Flag, susu kental manis - Carnation, Cap Enak", "Susu & Dairy", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 12, "#00BCD4", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 60, "Pocari Sweat, Mizone, Hydro Coco, Ion Water, Revive", "Minuman Isotonik", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 13, "#FF7043", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 60, "Buavita, SunTop, Minute Maid, Okky Jelly Drink, Frestea", "Jus & Minuman Buah", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 14, "#D32F2F", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 60, "Kratingdaeng, M-150, Extra Joss, Red Bull, Shark", "Minuman Energi", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 15, "#E1BEE7", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 60, "Walls, Aice, Diamond, Campina, Magnum, Cornetto", "Es Krim", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 16, "#4CAF50", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 90, "Paracetamol, Panadol, Bodrex, Paramex, Aspirin, Ibuprofen", "Obat Bebas", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 17, "#66BB6A", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 90, "Mixagrip, Neozep, Woods, Vicks, Komix, Actifed", "Obat Flu & Batuk", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 18, "#81C784", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 90, "Promag, Mylanta, Antasida, Norit, Entrostop, Diapet", "Obat Pencernaan", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 19, "#A5D6A7", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 90, "Redoxon, CDR, Enervon-C, Sangobion, Blackmores, Imboost", "Vitamin & Suplemen", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 20, "#C8E6C9", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 90, "Plester, perban, betadine, alkohol, kapas, hansaplast", "Perawatan Luka", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 21, "#E8F5E8", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 90, "Dettol, Antis, Lifebuoy, Nuvo, Mama Lime, Biore", "Hand Sanitizer & Antiseptik", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 22, "#2196F3", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 180, "Pantene, Head & Shoulders, Sunsilk, Clear, Tresemme, Makarizo", "Shampo & Hair Care", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 23, "#42A5F5", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 180, "Lux, Dove, Lifebuoy, Giv, Dettol, Biore, Citra", "Sabun Mandi", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 24, "#64B5F6", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 180, "Pepsodent, Close Up, Formula, Sensodyne, Systema, Enzim", "Pasta Gigi", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 25, "#90CAF9", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 180, "Vaseline, Nivea, Citra, Pond's, Olay, Garnier", "Body Lotion & Skin Care", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 26, "#BBDEFB", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 180, "Rexona, Dove Men, Gillette, Axe, Nivea Men, Adidas", "Deodorant", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 27, "#E91E63", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 180, "Wardah, Pixy, Maybelline, Revlon, L'Oreal, Make Over", "Kosmetik & Makeup", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 28, "#F06292", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 180, "Axe, Rexona, Body Shop, Calvin Klein, Hugo Boss, local brands", "Parfum & Cologne", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 29, "#FFE0B2", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 45, "SGM, Dancow, Bebelac, Lactogen, Nutrilon, Enfamil", "Susu Formula", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 30, "#FFCC80", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 45, "Cerelac, Milna, Promina, SUN, Heinz, Gerber", "Makanan Bayi", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 31, "#FFB74D", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 45, "Pampers, MamyPoko, Sweety, Merries, Goon, Huggies", "Popok & Diapers", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 32, "#FFA726", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 45, "Baby oil, powder, lotion, shampoo - Johnson's, Cussons, Zwitsal", "Baby Care Products", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 33, "#9C27B0", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 365, "Rinso, Attack, Surf, So Klin, Daia, Total", "Deterjen & Sabun Cuci", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 34, "#AB47BC", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 365, "Sunlight, Mama Lemon, Cream, Joy, Soklin, Economic", "Pembersih Piring", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 35, "#BA68C8", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 365, "Vixal, Super Pel, Wipol, Karbol, Kispray, Stella", "Pembersih Lantai", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 36, "#CE93D8", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 365, "Vixal, Harpic, Domestos, Duck, Toilet Duck, Cif", "Pembersih Kamar Mandi", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 37, "#E1BEE7", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 365, "Molto, Downy, Soklin, Comfort, Rapika, Stella", "Pelembut & Pewangi Pakaian", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 38, "#607D8B", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, "Powerbank, charger, earphone, speaker, flashdisk, mouse", "Elektronik & Gadget", false, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 39, "#78909C", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, "Case, screen protector, holder, cable, tempered glass, ring holder", "Aksesoris HP", false, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 40, "#8D6E63", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, "Gudang Garam, Djarum, Marlboro, Sampoerna, Bentoel, Lucky Strike", "Rokok & Tembakau", false, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 41, "#FF9800", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, "Pensil, pulpen, buku, penggaris, penghapus, spidol - Faber Castell, Pilot", "Alat Tulis", false, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 42, "#795548", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, "Tissue, toilet paper, kantong plastik, aluminum foil, plastic wrap", "Perlengkapan Rumah", false, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 43, "#F44336", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, "Kartu ucapan, gift wrap, balon, hiasan, mainan kecil, souvenir", "Seasonal & Gift Items", false, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 44, "#FFEB3B", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, "Baterai ABC, Energizer, Panasonic, lampu LED, senter, bohlam", "Baterai & Lampu", false, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Barcode", "BuyPrice", "CategoryId", "CreatedAt", "CreatedBy", "Description", "ImageUrl", "IsActive", "MinimumStock", "Name", "SellPrice", "Stock", "Unit", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1, "8886001001923", 2500m, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Mie instan rasa ayam bawang - Indofood", null, true, 10, "Indomie Ayam Bawang", 3500m, 50, "pcs", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 2, "8888001234567", 2300m, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Mie instan kuah rasa ayam bawang - Sarimi", null, true, 10, "Sarimi Ayam Bawang", 3200m, 40, "pcs", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 3, "8992843287654", 18000m, 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Kornet sapi kaleng 198g - Pronas", null, true, 5, "Pronas Kornet Sapi", 25000m, 24, "kaleng", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 4, "8999999876543", 8500m, 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Keripik kentang rasa BBQ - Chitato", null, true, 8, "Chitato Rasa BBQ", 12000m, 30, "pcs", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 5, "8992753147258", 4500m, 4, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Biskuit kelapa - Roma Mayora", null, true, 12, "Roma Kelapa", 6500m, 36, "bks", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 6, "8992787134567", 2500m, 8, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Air mineral kemasan botol 600ml - Aqua", null, true, 12, "Aqua 600ml", 3500m, 48, "btl", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 7, "8851013301234", 6000m, 9, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Minuman berkarbonasi rasa cola - Coca Cola", null, true, 6, "Coca Cola 330ml", 8500m, 30, "btl", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 8, "8991002101234", 4500m, 10, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Teh kemasan botol rasa manis - Sosro", null, true, 6, "Teh Botol Sosro 450ml", 6500m, 24, "btl", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 9, "8992761456789", 5500m, 11, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Susu UHT rasa coklat - Ultra Milk", null, true, 8, "Ultra Milk Coklat 250ml", 7500m, 30, "kotak", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 10, "8992696789012", 7000m, 12, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Minuman isotonik elektrolit - Pocari Sweat", null, true, 5, "Pocari Sweat 350ml", 10000m, 20, "btl", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 11, "8992832123456", 12000m, 16, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Obat pereda nyeri dan demam - Panadol", null, true, 5, "Panadol Tablet", 16000m, 25, "strip", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 12, "8992747369852", 8500m, 17, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Obat flu dan batuk - Mixagrip", null, true, 5, "Mixagrip Flu & Batuk", 12000m, 20, "strip", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 13, "8992888147258", 45000m, 19, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Vitamin C 1000mg - Redoxon", null, true, 3, "Redoxon Vitamin C", 65000m, 15, "btl", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 14, "8992777456789", 18000m, 22, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Shampo rambut total damage care - Pantene", null, true, 4, "Pantene Shampo 170ml", 25000m, 18, "btl", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 15, "8992556789012", 4500m, 23, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Sabun mandi soft touch - Lux", null, true, 8, "Lux Sabun Mandi", 6500m, 30, "pcs", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 16, "8992334567890", 12000m, 24, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Pasta gigi pencegah gigi berlubang - Pepsodent", null, true, 5, "Pepsodent 190g", 16000m, 20, "tube", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 17, "8992775987654", 15000m, 33, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Deterjen bubuk anti noda - Rinso", null, true, 5, "Rinso Anti Noda 800g", 21000m, 25, "bks", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 18, "8992775123456", 8500m, 34, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Sabun pencuci piring konsentrat - Sunlight", null, true, 5, "Sunlight 755ml", 12000m, 22, "btl", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 19, "6941059648208", 180000m, 38, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Powerbank portabel 10000mAh - Xiaomi", null, true, 2, "Powerbank Xiaomi 10000mAh", 250000m, 8, "pcs", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 20, "8992704987654", 18000m, 40, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Rokok kretek filter - Gudang Garam", null, true, 10, "Gudang Garam Surya 16", 20000m, 50, "bks", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 21, "8992804321098", 8000m, 44, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Baterai alkaline ukuran AA - ABC", null, true, 10, "Baterai ABC AA", 12000m, 40, "pack", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 22, "8999812345678", 1500m, 41, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Pulpen standard warna biru - Standard", null, true, 15, "Pulpen Standard AE7", 2500m, 50, "pcs", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Branches_BranchCode",
                table: "Branches",
                column: "BranchCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Branches_BranchType",
                table: "Branches",
                column: "BranchType");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_City",
                table: "Branches",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_IsActive",
                table: "Branches",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_ParentBranchId",
                table: "Branches",
                column: "ParentBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_Province",
                table: "Branches",
                column: "Province");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_Province_City",
                table: "Branches",
                columns: new[] { "Province", "City" });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Color",
                table: "Categories",
                column: "Color");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FactureItems_FactureId",
                table: "FactureItems",
                column: "FactureId");

            migrationBuilder.CreateIndex(
                name: "IX_FactureItems_IsVerified",
                table: "FactureItems",
                column: "IsVerified");

            migrationBuilder.CreateIndex(
                name: "IX_FactureItems_ProductId",
                table: "FactureItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_FactureItems_VerifiedBy",
                table: "FactureItems",
                column: "VerifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FacturePayments_ApprovedBy",
                table: "FacturePayments",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FacturePayments_ConfirmedBy",
                table: "FacturePayments",
                column: "ConfirmedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FacturePayments_CreatedBy",
                table: "FacturePayments",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FacturePayments_FactureId",
                table: "FacturePayments",
                column: "FactureId");

            migrationBuilder.CreateIndex(
                name: "IX_FacturePayments_PaymentDate",
                table: "FacturePayments",
                column: "PaymentDate");

            migrationBuilder.CreateIndex(
                name: "IX_FacturePayments_PaymentMethod",
                table: "FacturePayments",
                column: "PaymentMethod");

            migrationBuilder.CreateIndex(
                name: "IX_FacturePayments_ProcessedBy",
                table: "FacturePayments",
                column: "ProcessedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FacturePayments_Status",
                table: "FacturePayments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FacturePayments_Status_PaymentDate",
                table: "FacturePayments",
                columns: new[] { "Status", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_FacturePayments_UpdatedBy",
                table: "FacturePayments",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Factures_ApprovedBy",
                table: "Factures",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Factures_Branch_Status",
                table: "Factures",
                columns: new[] { "BranchId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Factures_BranchId",
                table: "Factures",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Factures_CreatedBy",
                table: "Factures",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Factures_DueDate",
                table: "Factures",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_Factures_InternalReferenceNumber",
                table: "Factures",
                column: "InternalReferenceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Factures_InvoiceDate",
                table: "Factures",
                column: "InvoiceDate");

            migrationBuilder.CreateIndex(
                name: "IX_Factures_ReceivedBy",
                table: "Factures",
                column: "ReceivedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Factures_Status",
                table: "Factures",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Factures_Status_DueDate",
                table: "Factures",
                columns: new[] { "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Factures_Supplier_InvoiceNumber",
                table: "Factures",
                columns: new[] { "SupplierId", "SupplierInvoiceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Factures_TotalAmount",
                table: "Factures",
                column: "TotalAmount");

            migrationBuilder.CreateIndex(
                name: "IX_Factures_UpdatedBy",
                table: "Factures",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Factures_VerifiedBy",
                table: "Factures",
                column: "VerifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMutations_CreatedAt",
                table: "InventoryMutations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMutations_ProductId",
                table: "InventoryMutations",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMutations_SaleId",
                table: "InventoryMutations",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMutations_Type",
                table: "InventoryMutations",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransferItems_InventoryTransferId",
                table: "InventoryTransferItems",
                column: "InventoryTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransferItems_ProductId",
                table: "InventoryTransferItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransfers_ApprovedByUserId",
                table: "InventoryTransfers",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransfers_CancelledByUserId",
                table: "InventoryTransfers",
                column: "CancelledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransfers_CreatedAt",
                table: "InventoryTransfers",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransfers_DestinationBranchId",
                table: "InventoryTransfers",
                column: "ToBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransfers_ReceivedByUserId",
                table: "InventoryTransfers",
                column: "ReceivedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransfers_RequestedByUserId",
                table: "InventoryTransfers",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransfers_ShippedByUserId",
                table: "InventoryTransfers",
                column: "ShippedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransfers_SourceBranchId",
                table: "InventoryTransfers",
                column: "FromBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransfers_Status",
                table: "InventoryTransfers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransfers_TransferNumber",
                table: "InventoryTransfers",
                column: "TransferNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransferStatusHistories_ChangedAt",
                table: "InventoryTransferStatusHistories",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransferStatusHistories_ChangedBy",
                table: "InventoryTransferStatusHistories",
                column: "ChangedBy");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransferStatusHistories_InventoryTransferId",
                table: "InventoryTransferStatusHistories",
                column: "InventoryTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberPoints_CreatedAt",
                table: "MemberPoints",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MemberPoints_MemberId",
                table: "MemberPoints",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberPoints_SaleId",
                table: "MemberPoints",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberPoints_Type",
                table: "MemberPoints",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Members_Email",
                table: "Members",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Members_IsActive",
                table: "Members",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Members_MemberNumber",
                table: "Members",
                column: "MemberNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Members_Phone",
                table: "Members",
                column: "Phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CreatedAt",
                table: "Notifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IsRead",
                table: "Notifications",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Type",
                table: "Notifications",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationSettings_UserId",
                table: "NotificationSettings",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductBatches_BatchNumber",
                table: "ProductBatches",
                column: "BatchNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBatches_BranchId",
                table: "ProductBatches",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBatches_BranchId_ExpiryDate",
                table: "ProductBatches",
                columns: new[] { "BranchId", "ExpiryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductBatches_CreatedByUserId",
                table: "ProductBatches",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBatches_DisposedByUserId",
                table: "ProductBatches",
                column: "DisposedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBatches_ExpiryDate",
                table: "ProductBatches",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBatches_IsDisposed",
                table: "ProductBatches",
                column: "IsDisposed");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBatches_IsExpired",
                table: "ProductBatches",
                column: "IsExpired");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBatches_ProductId",
                table: "ProductBatches",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBatches_ProductId_ExpiryDate",
                table: "ProductBatches",
                columns: new[] { "ProductId", "ExpiryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductBatches_UpdatedByUserId",
                table: "ProductBatches",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Barcode",
                table: "Products",
                column: "Barcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsActive",
                table: "Products",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Name",
                table: "Products",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_SaleItems_ProductId",
                table: "SaleItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleItems_SaleId",
                table: "SaleItems",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_CashierId",
                table: "Sales",
                column: "CashierId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_MemberId",
                table: "Sales",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_PaymentMethod",
                table: "Sales",
                column: "PaymentMethod");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_SaleDate",
                table: "Sales",
                column: "SaleDate");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_SaleNumber",
                table: "Sales",
                column: "SaleNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sales_Status",
                table: "Sales",
                column: "Status");

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

            migrationBuilder.CreateIndex(
                name: "IX_UserNotificationSettings_UserId",
                table: "UserNotificationSettings",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_Email",
                table: "UserProfiles",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_UserId",
                table: "UserProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_BranchId",
                table: "Users",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_BranchId_Role",
                table: "Users",
                columns: new[] { "BranchId", "Role" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Role",
                table: "Users",
                column: "Role");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FactureItems");

            migrationBuilder.DropTable(
                name: "FacturePayments");

            migrationBuilder.DropTable(
                name: "InventoryMutations");

            migrationBuilder.DropTable(
                name: "InventoryTransferItems");

            migrationBuilder.DropTable(
                name: "InventoryTransferStatusHistories");

            migrationBuilder.DropTable(
                name: "LogActivities");

            migrationBuilder.DropTable(
                name: "MemberPoints");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "NotificationSettings");

            migrationBuilder.DropTable(
                name: "ProductBatches");

            migrationBuilder.DropTable(
                name: "SaleItems");

            migrationBuilder.DropTable(
                name: "UserNotificationSettings");

            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.DropTable(
                name: "Factures");

            migrationBuilder.DropTable(
                name: "InventoryTransfers");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Sales");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Members");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Branches");
        }
    }
}
