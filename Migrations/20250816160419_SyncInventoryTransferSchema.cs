using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Berca_Backend.Migrations
{
    /// <inheritdoc />
    public partial class SyncInventoryTransferSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /* Categories columns already exist
            migrationBuilder.AddColumn<int>(
                name: "DefaultExpiryWarningDays",
                table: "Categories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresExpiryDate",
                table: "Categories",
                type: "bit",
                nullable: false,
                defaultValue: false); */

            // InventoryTransfers table already exists, just add missing indexes and constraints
            /*migrationBuilder.CreateTable(
                name: "InventoryTransfers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransferNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    SourceBranchId = table.Column<int>(type: "int", nullable: false),
                    DestinationBranchId = table.Column<int>(type: "int", nullable: false),
                    RequestReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    ActualCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    RequestedBy = table.Column<int>(type: "int", nullable: false),
                    ApprovedBy = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ShippedBy = table.Column<int>(type: "int", nullable: true),
                    ShippedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReceivedBy = table.Column<int>(type: "int", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledBy = table.Column<int>(type: "int", nullable: true),
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
                        name: "FK_InventoryTransfers_Branches_DestinationBranchId",
                        column: x => x.DestinationBranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryTransfers_Branches_SourceBranchId",
                        column: x => x.SourceBranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryTransfers_Users_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InventoryTransfers_Users_CancelledBy",
                        column: x => x.CancelledBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InventoryTransfers_Users_ReceivedBy",
                        column: x => x.ReceivedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InventoryTransfers_Users_RequestedBy",
                        column: x => x.RequestedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryTransfers_Users_ShippedBy",
                        column: x => x.ShippedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                }); */

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

            /* InventoryTransferItems already exists
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
                }); */

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

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Color", "DefaultExpiryWarningDays", "Description", "Name", "RequiresExpiryDate" },
                values: new object[] { "#FF6B35", 30, "Mie instan, nasi instan, bubur instan - Indomie, Pop Mie, Sedaap, Sarimi", "Makanan Instan", true });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Color", "DefaultExpiryWarningDays", "Description", "Name", "RequiresExpiryDate" },
                values: new object[] { "#FF8E53", 30, "Kornet, sarden, buah kaleng, sayur kaleng - Pronas, ABC, Ayam Brand", "Makanan Kaleng", true });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Color", "DefaultExpiryWarningDays", "Description", "Name", "RequiresExpiryDate" },
                values: new object[] { "#FFA726", 30, "Chitato, Taro, Qtela, Lay's, keripik tradisional, kacang-kacangan", "Snacks & Keripik", true });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Color", "DefaultExpiryWarningDays", "Description", "Name", "RequiresExpiryDate" },
                values: new object[] { "#FFB74D", 30, "Roma, Monde, Khong Guan, Oreo, wafer Tanggo, Marie Regal", "Biskuit & Wafer", true });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Color", "DefaultExpiryWarningDays", "Description", "Name", "RequiresExpiryDate" },
                values: new object[] { "#8D4E85", 30, "Kopiko, Ricola, Cadbury, SilverQueen, permen lokal, Mentos", "Permen & Coklat", true });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Color", "CreatedAt", "DefaultExpiryWarningDays", "Description", "Name", "RequiresExpiryDate", "UpdatedAt" },
                values: new object[,]
                {
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

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1,
                column: "Description",
                value: "Mie instan rasa ayam bawang - Indofood");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Barcode", "BuyPrice", "CategoryId", "Description", "MinimumStock", "Name", "SellPrice", "Stock" },
                values: new object[] { "8888001234567", 2300m, 1, "Mie instan kuah rasa ayam bawang - Sarimi", 10, "Sarimi Ayam Bawang", 3200m, 40 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Barcode", "BuyPrice", "CategoryId", "Description", "Name", "SellPrice", "Stock", "Unit" },
                values: new object[] { "8992843287654", 18000m, 2, "Kornet sapi kaleng 198g - Pronas", "Pronas Kornet Sapi", 25000m, 24, "kaleng" });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Barcode", "CategoryId", "Description", "MinimumStock", "Name", "Stock" },
                values: new object[] { "8999999876543", 3, "Keripik kentang rasa BBQ - Chitato", 8, "Chitato Rasa BBQ", 30 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Barcode", "BuyPrice", "CategoryId", "Description", "MinimumStock", "Name", "SellPrice", "Stock", "Unit" },
                values: new object[] { "8992753147258", 4500m, 4, "Biskuit kelapa - Roma Mayora", 12, "Roma Kelapa", 6500m, 36, "bks" });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Barcode", "BuyPrice", "CategoryId", "CreatedAt", "CreatedBy", "Description", "ImageUrl", "IsActive", "MinimumStock", "Name", "SellPrice", "Stock", "Unit", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
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
                name: "IX_InventoryTransferItems_InventoryTransferId",
                table: "InventoryTransferItems",
                column: "InventoryTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransferItems_ProductId",
                table: "InventoryTransferItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransfers_ApprovedBy",
                table: "InventoryTransfers",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransfers_CancelledBy",
                table: "InventoryTransfers",
                column: "CancelledBy");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransfers_CreatedAt",
                table: "InventoryTransfers",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransfers_DestinationBranchId",
                table: "InventoryTransfers",
                column: "DestinationBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransfers_ReceivedBy",
                table: "InventoryTransfers",
                column: "ReceivedBy");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransfers_RequestedBy",
                table: "InventoryTransfers",
                column: "RequestedBy");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransfers_ShippedBy",
                table: "InventoryTransfers",
                column: "ShippedBy");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransfers_SourceBranchId",
                table: "InventoryTransfers",
                column: "SourceBranchId");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryTransferItems");

            migrationBuilder.DropTable(
                name: "InventoryTransferStatusHistories");

            migrationBuilder.DropTable(
                name: "ProductBatches");

            migrationBuilder.DropTable(
                name: "InventoryTransfers");

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 36);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 42);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 44);

            migrationBuilder.DropColumn(
                name: "DefaultExpiryWarningDays",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "RequiresExpiryDate",
                table: "Categories");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Color", "Description", "Name" },
                values: new object[] { "#FF914D", "Produk makanan dan snack", "Makanan" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Color", "Description", "Name" },
                values: new object[] { "#4BBF7B", "Minuman segar dan berenergi", "Minuman" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Color", "Description", "Name" },
                values: new object[] { "#E15A4F", "Perangkat elektronik dan aksesoris", "Elektronik" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Color", "Description", "Name" },
                values: new object[] { "#FFB84D", "Keperluan dan peralatan rumah tangga", "Rumah Tangga" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Color", "Description", "Name" },
                values: new object[] { "#6366F1", "Produk kesehatan dan perawatan", "Kesehatan" });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1,
                column: "Description",
                value: "Mie instan rasa ayam bawang");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Barcode", "BuyPrice", "CategoryId", "Description", "MinimumStock", "Name", "SellPrice", "Stock" },
                values: new object[] { "8851013301234", 6000m, 2, "Minuman berkarbonasi rasa cola", 5, "Coca Cola 330ml", 8000m, 30 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Barcode", "BuyPrice", "CategoryId", "Description", "Name", "SellPrice", "Stock", "Unit" },
                values: new object[] { "1234567890123", 12000m, 3, "Baterai alkaline ukuran AA", "Baterai AA Alkaline", 15000m, 20, "pcs" });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Barcode", "CategoryId", "Description", "MinimumStock", "Name", "Stock" },
                values: new object[] { "8992775123456", 4, "Sabun pencuci piring konsentrat", 5, "Sabun Cuci Piring Sunlight", 25 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Barcode", "BuyPrice", "CategoryId", "Description", "MinimumStock", "Name", "SellPrice", "Stock", "Unit" },
                values: new object[] { "8992832123456", 3000m, 5, "Obat pereda nyeri dan demam", 10, "Paracetamol 500mg", 5000m, 40, "tablet" });
        }
    }
}
