using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Berca_Backend.Migrations
{
	/// <inheritdoc />
	public partial class AddBranchSystem : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			// Create Branches table
			migrationBuilder.CreateTable(
				name: "Branches",
				columns: table => new
				{
					Id = table.Column<int>(type: "int", nullable: false)
						.Annotation("SqlServer:Identity", "1, 1"),
					BranchCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
					BranchName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
					ParentBranchId = table.Column<int>(type: "int", nullable: true), // Always null for flat structure
					BranchType = table.Column<int>(type: "int", nullable: false),
					Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
					ManagerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
					Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
					Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),

					// Location details
					City = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
					Province = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
					PostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),

					// Store details
					OpeningDate = table.Column<DateTime>(type: "datetime2", nullable: false),
					StoreSize = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
					EmployeeCount = table.Column<int>(type: "int", nullable: false),

					IsActive = table.Column<bool>(type: "bit", nullable: false),
					CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
					UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Branches", x => x.Id);
					table.ForeignKey(
						name: "FK_Branches_Branches_ParentBranchId",
						column: x => x.ParentBranchId,
						principalTable: "Branches",
						principalColumn: "Id");
				});

			// Add BranchId to Users table
			migrationBuilder.AddColumn<int>(
				name: "BranchId",
				table: "Users",
				type: "int",
				nullable: true);

			migrationBuilder.AddColumn<bool>(
				name: "CanAccessMultipleBranches",
				table: "Users",
				type: "bit",
				nullable: false,
				defaultValue: false);

			migrationBuilder.AddColumn<string>(
				name: "AccessibleBranchIds",
				table: "Users",
				type: "nvarchar(500)",
				maxLength: 500,
				nullable: true);

			// Create additional indexes for retail chain queries
			migrationBuilder.CreateIndex(
				name: "IX_Branches_ParentBranchId",
				table: "Branches",
				column: "ParentBranchId");

			migrationBuilder.CreateIndex(
				name: "IX_Branches_BranchCode",
				table: "Branches",
				column: "BranchCode",
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_Branches_City",
				table: "Branches",
				column: "City");

			migrationBuilder.CreateIndex(
				name: "IX_Branches_Province",
				table: "Branches",
				column: "Province");

			migrationBuilder.CreateIndex(
				name: "IX_Branches_BranchType",
				table: "Branches",
				column: "BranchType");

			migrationBuilder.CreateIndex(
				name: "IX_Users_BranchId",
				table: "Users",
				column: "BranchId");

			// Add foreign key constraint
			migrationBuilder.AddForeignKey(
				name: "FK_Users_Branches_BranchId",
				table: "Users",
				column: "BranchId",
				principalTable: "Branches",
				principalColumn: "Id");

			// Insert realistic retail chain sample data with proper timezone handling
			var nowUtc = DateTime.UtcNow;
			var jakartaNow = TimeZoneInfo.ConvertTimeFromUtc(nowUtc,
				TimeZoneInfo.FindSystemTimeZoneById("Asia/Jakarta"));

			migrationBuilder.InsertData(
				table: "Branches",
				columns: new[] { "Id", "BranchCode", "BranchName", "ParentBranchId", "BranchType", "Address", "ManagerName", "Phone", "Email", "City", "Province", "PostalCode", "OpeningDate", "StoreSize", "EmployeeCount", "IsActive", "CreatedAt", "UpdatedAt" },
				values: new object[,]
				{
					// Head Office - Using UTC for database storage but calculated from Jakarta time
					{ 1, "HQ", "Toko Eniwan Head Office", null, 0, "Jl. Merdeka No. 123, Jakarta Pusat, DKI Jakarta", "Maharaja Dika", "021-1234567", "admin@tokoeniwan.com", "Jakarta", "DKI Jakarta", "10110", TimeZoneInfo.ConvertTimeToUtc(jakartaNow.AddYears(-3), TimeZoneInfo.FindSystemTimeZoneById("Asia/Jakarta")), "Large", 25, true, nowUtc, nowUtc },
					
					// Retail Branches - All dates converted properly from Jakarta to UTC
					{ 2, "PWK001", "Toko Eniwan Purwakarta", null, 1, "Jl. Ahmad Yani No. 45, Purwakarta, Jawa Barat", "Budi Santoso", "0264-123456", "purwakarta@tokoeniwan.com", "Purwakarta", "Jawa Barat", "41115", TimeZoneInfo.ConvertTimeToUtc(jakartaNow.AddYears(-2), TimeZoneInfo.FindSystemTimeZoneById("Asia/Jakarta")), "Medium", 8, true, nowUtc, nowUtc },

					{ 3, "BDG001", "Toko Eniwan Bandung", null, 1, "Jl. Cihampelas No. 120, Bandung, Jawa Barat", "Sari Indrawati", "022-987654", "bandung@tokoeniwan.com", "Bandung", "Jawa Barat", "40131", TimeZoneInfo.ConvertTimeToUtc(jakartaNow.AddYears(-1), TimeZoneInfo.FindSystemTimeZoneById("Asia/Jakarta")), "Large", 15, true, nowUtc, nowUtc },

					{ 4, "SBY001", "Toko Eniwan Surabaya", null, 1, "Jl. Raya Darmo No. 88, Surabaya, Jawa Timur", "Ahmad Hidayat", "031-567890", "surabaya@tokoeniwan.com", "Surabaya", "Jawa Timur", "60265", TimeZoneInfo.ConvertTimeToUtc(jakartaNow.AddMonths(-8), TimeZoneInfo.FindSystemTimeZoneById("Asia/Jakarta")), "Large", 12, true, nowUtc, nowUtc },

					{ 5, "BKS001", "Toko Eniwan Bekasi", null, 1, "Jl. Cut Meutia No. 99, Bekasi, Jawa Barat", "Dewi Lestari", "021-8888999", "bekasi@tokoeniwan.com", "Bekasi", "Jawa Barat", "17112", TimeZoneInfo.ConvertTimeToUtc(jakartaNow.AddMonths(-6), TimeZoneInfo.FindSystemTimeZoneById("Asia/Jakarta")), "Medium", 10, true, nowUtc, nowUtc },

					{ 6, "BGR001", "Toko Eniwan Bogor", null, 1, "Jl. Pajajaran No. 77, Bogor, Jawa Barat", "Rini Setiawan", "0251-333444", "bogor@tokoeniwan.com", "Bogor", "Jawa Barat", "16129", TimeZoneInfo.ConvertTimeToUtc(jakartaNow.AddMonths(-3), TimeZoneInfo.FindSystemTimeZoneById("Asia/Jakarta")), "Small", 6, true, nowUtc, nowUtc },

					{ 7, "GUDANG-PUSAT", "Gudang - Jakarta Pusat", null, 2, "Jl. Sudirman No. 456 (Gudang)", "Supervisor Gudang", "021-2345678", null, "Jakarta", "DKI Jakarta", "10120", TimeZoneInfo.ConvertTimeToUtc(jakartaNow.AddYears(-2), TimeZoneInfo.FindSystemTimeZoneById("Asia/Jakarta")), "Large", 5, true, nowUtc, nowUtc }
				});
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			// Drop foreign key
			migrationBuilder.DropForeignKey(
				name: "FK_Users_Branches_BranchId",
				table: "Users");

			// Drop indexes
			migrationBuilder.DropIndex(
				name: "IX_Users_BranchId",
				table: "Users");

			migrationBuilder.DropIndex(
				name: "IX_Branches_BranchCode",
				table: "Branches");

			migrationBuilder.DropIndex(
				name: "IX_Branches_ParentBranchId",
				table: "Branches");

			// Drop columns from Users
			migrationBuilder.DropColumn(
				name: "BranchId",
				table: "Users");

			migrationBuilder.DropColumn(
				name: "CanAccessMultipleBranches",
				table: "Users");

			migrationBuilder.DropColumn(
				name: "AccessibleBranchIds",
				table: "Users");

			// Drop Branches table
			migrationBuilder.DropTable(
				name: "Branches");
		}
	}
}