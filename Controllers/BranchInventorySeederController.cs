using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Berca_Backend.Data;
using Berca_Backend.Models;

namespace Berca_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BranchInventorySeederController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<BranchInventorySeederController> _logger;

        public BranchInventorySeederController(AppDbContext context, ILogger<BranchInventorySeederController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("seed")]
        public async Task<IActionResult> SeedBranchInventory()
        {
            try
            {
                // Clear existing data
                var existingInventories = await _context.BranchInventories.ToListAsync();
                _context.BranchInventories.RemoveRange(existingInventories);
                await _context.SaveChangesAsync();

                var seedData = new List<BranchInventory>();

                // Get products and branches
                var products = await _context.Products.Take(20).ToListAsync();
                var branches = new int[] { 1, 2, 3 };

                foreach (var product in products)
                {
                    foreach (var branchId in branches)
                    {
                        int stock = branchId switch
                        {
                            1 => (int)(product.Stock * 0.6),  // Head Office - 60%
                            2 => (int)(product.Stock * 0.3),  // Branch 2 - 30%
                            3 => (int)(product.Stock * 0.25), // Branch 3 - 25%
                            _ => (int)(product.Stock * 0.2)
                        };

                        if (stock > 0)
                        {
                            seedData.Add(new BranchInventory
                            {
                                ProductId = product.Id,
                                BranchId = branchId,
                                Stock = stock,
                                MinimumStock = Math.Max(1, stock / 10),
                                MaximumStock = stock * 2,
                                BuyPrice = product.BuyPrice,
                                SellPrice = product.SellPrice,
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            });
                        }
                    }
                }

                await _context.BranchInventories.AddRangeAsync(seedData);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Successfully seeded {Count} branch inventory records", seedData.Count);

                return Ok(new {
                    success = true,
                    message = $"Successfully seeded {seedData.Count} branch inventory records",
                    data = seedData.GroupBy(x => x.BranchId).Select(g => new {
                        BranchId = g.Key,
                        ProductCount = g.Count()
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error seeding branch inventory");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("verify")]
        public async Task<IActionResult> VerifyBranchInventory()
        {
            try
            {
                var summary = await _context.BranchInventories
                    .GroupBy(bi => bi.BranchId)
                    .Select(g => new
                    {
                        BranchId = g.Key,
                        ProductCount = g.Count(),
                        TotalStock = g.Sum(x => x.Stock),
                        SampleProducts = g.Take(3).Select(x => new
                        {
                            ProductId = x.ProductId,
                            ProductName = x.Product.Name,
                            Stock = x.Stock
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = summary });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error verifying branch inventory");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}