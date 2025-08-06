// Data/SampleDataSeeder.cs - Sample data seeder for dashboard demo (FIXED)
using Berca_Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Berca_Backend.Data
{
    public static class SampleDataSeeder
    {
        public static async Task SeedSampleDataAsync(AppDbContext context)
        {
            // Only seed if no sales exist
            if (await context.Sales.AnyAsync())
                return;

            // Create sample users first
            var adminUser = new User
            {
                Id = 1,
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Role = "Admin",
                IsActive = true
                // ✅ Remove CreatedAt and UpdatedAt - they have default values
            };

            var cashierUser = new User
            {
                Id = 2,
                Username = "cashier",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("cashier123"),
                Role = "User",
                IsActive = true
                // ✅ Remove CreatedAt and UpdatedAt - they have default values
            };

            context.Users.AddRange(adminUser, cashierUser);

            // Create sample members
            var members = new List<Member>
            {
                new Member
                {
                    Id = 1,
                    Name = "John Doe",
                    Phone = "081234567890",
                    Email = "john@example.com",
                    MemberNumber = "M0001",
                    TotalPoints = 150, // ✅ Fixed: Use TotalPoints instead of Points
                    TotalSpent = 500000,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-30),
                    UpdatedAt = DateTime.UtcNow.AddDays(-30)
                },
                new Member
                {
                    Id = 2,
                    Name = "Jane Smith",
                    Phone = "081234567891",
                    Email = "jane@example.com",
                    MemberNumber = "M0002",
                    TotalPoints = 250, // ✅ Fixed: Use TotalPoints instead of Points
                    TotalSpent = 800000,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-25),
                    UpdatedAt = DateTime.UtcNow.AddDays(-25)
                }
            };

            context.Members.AddRange(members);

            // Create sample sales for the last 30 days
            var sales = new List<Sale>();
            var saleItems = new List<SaleItem>();
            var random = new Random();

            var products = await context.Products.ToListAsync();
            if (!products.Any())
            {
                await context.SaveChangesAsync();
                return; // No products to create sales for
            }

            for (int day = 0; day < 30; day++)
            {
                var saleDate = DateTime.UtcNow.AddDays(-day);
                int dailySalesCount = random.Next(5, 15); // 5-15 sales per day

                for (int i = 0; i < dailySalesCount; i++)
                {
                    var saleId = (day * 100) + i + 1;
                    var sale = new Sale
                    {
                        Id = saleId,
                        SaleNumber = $"TRX{saleDate:yyyyMMdd}{(i + 1):D3}",
                        SaleDate = saleDate.AddHours(random.Next(8, 20)).AddMinutes(random.Next(0, 59)),
                        CashierId = random.Next(1, 3), // admin or cashier
                        MemberId = random.Next(1, 4) == 1 ? members[random.Next(0, members.Count)].Id : null,
                        CustomerName = random.Next(1, 3) == 1 ? "Guest Customer" : null,
                        PaymentMethod = GetRandomPaymentMethod(random),
                        Status = SaleStatus.Completed,
                        ReceiptPrinted = true,
                        ReceiptPrintedAt = saleDate,
                        CreatedAt = saleDate,
                        UpdatedAt = saleDate
                    };

                    // Add random products to this sale
                    int itemCount = random.Next(1, 6); // 1-5 items per sale
                    decimal subtotal = 0;

                    for (int j = 0; j < itemCount; j++)
                    {
                        var product = products[random.Next(0, products.Count)];
                        var quantity = random.Next(1, 4); // 1-3 quantity
                        var unitPrice = product.SellPrice;
                        var unitCost = product.BuyPrice;
                        var itemSubtotal = quantity * unitPrice;

                        var saleItem = new SaleItem
                        {
                            Id = (saleId * 10) + j + 1,
                            SaleId = saleId,
                            ProductId = product.Id,
                            ProductName = product.Name,
                            ProductBarcode = product.Barcode,
                            Quantity = quantity,
                            UnitPrice = unitPrice,
                            UnitCost = unitCost, // ✅ Fixed: Set UnitCost
                            Subtotal = itemSubtotal, // ✅ Fixed: Use Subtotal instead of TotalPrice
                            DiscountAmount = 0,
                            Unit = product.Unit,
                            CreatedAt = saleDate
                            // ✅ Fixed: Removed TotalProfit assignment (it's computed)
                        };

                        saleItems.Add(saleItem);
                        subtotal += itemSubtotal;
                    }

                    // Calculate totals
                    var discountAmount = subtotal > 50000 ? subtotal * 0.05m : 0; // 5% discount if > 50k
                    var taxAmount = subtotal * 0.10m; // 10% tax
                    var total = subtotal - discountAmount + taxAmount;
                    var amountPaid = total + (random.Next(0, 10) * 1000); // Round up payment
                    var changeAmount = amountPaid - total;

                    sale.Subtotal = subtotal;
                    sale.DiscountAmount = discountAmount;
                    sale.TaxAmount = taxAmount;
                    sale.Total = total;
                    sale.AmountPaid = amountPaid;
                    sale.ChangeAmount = changeAmount;
                    // ✅ Fixed: Removed TotalItems and TotalProfit assignments (they're computed)

                    sales.Add(sale);
                }
            }

            context.Sales.AddRange(sales);
            await context.SaveChangesAsync();

            context.SaleItems.AddRange(saleItems);
            await context.SaveChangesAsync();

            // Create some notifications
            var notifications = new List<Notification>
            {
                new Notification
                {
                    Type = "low_stock",
                    Title = "Stok Produk Rendah",
                    Message = "Beberapa produk memiliki stok yang rendah",
                    Priority = NotificationPriority.High,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddHours(-2),
                    CreatedBy = "System"
                },
                new Notification
                {
                    Type = "daily_sales",
                    Title = "Laporan Penjualan Harian",
                    Message = $"Penjualan hari ini mencapai {sales.Where(s => s.SaleDate.Date == DateTime.UtcNow.Date).Sum(s => s.Total):C0}",
                    Priority = NotificationPriority.Normal,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddHours(-1),
                    CreatedBy = "System"
                }
            };

            context.Notifications.AddRange(notifications);
            await context.SaveChangesAsync();
        }

        private static string GetRandomPaymentMethod(Random random)
        {
            var paymentMethods = new[] { "Cash", "Debit Card", "Credit Card", "E-Wallet", "Bank Transfer" };
            return paymentMethods[random.Next(0, paymentMethods.Length)];
        }
    }
}