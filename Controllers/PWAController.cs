using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Berca_Backend.DTOs;
using Berca_Backend.Services.Interfaces;
using System.Reflection;
using System.Security.Claims;

namespace Berca_Backend.Controllers
{
    /// <summary>
    /// Controller for PWA (Progressive Web App) functionality
    /// Handles cache management, offline sync, and app versioning
    /// Indonesian business context with POS-specific features
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PWAController : ControllerBase
    {
        private readonly ILogger<PWAController> _logger;
        private readonly IConfiguration _configuration;
        public PWAController(
            ILogger<PWAController> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        // ==================== APP VERSION & CACHE MANAGEMENT ==================== //

        /// <summary>
        /// Get current app version for cache busting
        /// </summary>
        /// <returns>App version information</returns>
        [HttpGet("version")]
        [AllowAnonymous]
        public IActionResult GetAppVersion()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version?.ToString() ?? "1.0.0.0";
                var buildDate = GetBuildDate();

                // Get critical resources that need to be cached
                var criticalResources = GetCriticalResourcesList();

                // Check if force update is required
                var forceUpdate = _configuration.GetValue<bool>("PWA:ForceUpdate", false);
                var updateMessage = _configuration["PWA:UpdateMessage"];

                var versionInfo = new PWAVersionDto
                {
                    Version = version,
                    BuildNumber = GetBuildNumber(),
                    BuildDate = buildDate,
                    CriticalResources = criticalResources,
                    ForceUpdate = forceUpdate,
                    UpdateMessage = updateMessage
                };

                return Ok(new 
                { 
                    success = true,
                    version = versionInfo,
                    timestamp = DateTime.UtcNow,
                    serverTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    timeZone = "Asia/Jakarta"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting app version");
                return StatusCode(500, new { success = false, message = "Terjadi kesalahan sistem" });
            }
        }

        /// <summary>
        /// Get list of critical resources for offline caching
        /// </summary>
        /// <returns>List of critical resource URLs</returns>
        [HttpGet("critical-resources")]
        [AllowAnonymous]
        public IActionResult GetCriticalResources()
        {
            try
            {
                var resources = GetCriticalResourcesList();
                
                return Ok(new 
                { 
                    success = true,
                    resources = resources,
                    cacheDuration = _configuration.GetValue<int>("PWA:CacheDurationHours", 24),
                    lastUpdated = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting critical resources");
                return StatusCode(500, new { success = false, message = "Terjadi kesalahan sistem" });
            }
        }

        /// <summary>
        /// Check for app updates
        /// </summary>
        /// <param name="currentVersion">Client's current version</param>
        /// <returns>Update information</returns>
        [HttpGet("check-update")]
        [AllowAnonymous]
        public IActionResult CheckForUpdates([FromQuery] string? currentVersion = null)
        {
            try
            {
                var serverVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";
                var updateAvailable = !string.IsNullOrEmpty(currentVersion) && 
                                    IsNewerVersion(serverVersion, currentVersion);

                var forceUpdate = _configuration.GetValue<bool>("PWA:ForceUpdate", false);
                var updateMessage = _configuration["PWA:UpdateMessage"] ?? "Pembaruan aplikasi tersedia";

                return Ok(new 
                { 
                    success = true,
                    updateAvailable = updateAvailable,
                    forceUpdate = forceUpdate,
                    currentVersion = serverVersion,
                    clientVersion = currentVersion,
                    updateMessage = updateMessage,
                    releaseNotes = GetReleaseNotes()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for updates");
                return StatusCode(500, new { success = false, message = "Terjadi kesalahan sistem" });
            }
        }

        // ==================== OFFLINE SYNC MANAGEMENT ==================== //

        /// <summary>
        /// Process offline sync queue from PWA
        /// </summary>
        /// <param name="request">Offline sync request with queued operations</param>
        /// <returns>Sync processing results</returns>
        [HttpPost("sync")]
        [Authorize]
        public async Task<IActionResult> ProcessOfflineSync([FromBody] OfflineSyncRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var branchId = GetCurrentUserBranchId();
                
                if (userId == null)
                {
                    return Unauthorized("User ID tidak ditemukan");
                }

                _logger.LogInformation("Processing offline sync for user {UserId} with {ItemCount} items", 
                    userId, request.Items.Count);

                var results = new List<object>();
                var successCount = 0;
                var errorCount = 0;

                foreach (var item in request.Items)
                {
                    try
                    {
                        var result = await ProcessSyncItem(item, userId.Value, branchId);
                        results.Add(new 
                        { 
                            clientId = item.ClientId,
                            entityType = item.EntityType,
                            action = item.Action,
                            success = result.Success,
                            message = result.Message,
                            serverId = result.ServerId
                        });

                        if (result.Success)
                            successCount++;
                        else
                            errorCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing sync item {ClientId}", item.ClientId);
                        results.Add(new 
                        { 
                            clientId = item.ClientId,
                            entityType = item.EntityType,
                            action = item.Action,
                            success = false,
                            message = ex.Message
                        });
                        errorCount++;
                    }
                }

                _logger.LogInformation("Offline sync completed for user {UserId}: {Success} success, {Errors} errors", 
                    userId, successCount, errorCount);

                return Ok(new 
                { 
                    success = true,
                    message = $"Sinkronisasi selesai: {successCount} berhasil, {errorCount} gagal",
                    results = results,
                    processed = request.Items.Count,
                    successCount = successCount,
                    errorCount = errorCount,
                    syncTimestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing offline sync");
                return StatusCode(500, new { success = false, message = "Terjadi kesalahan saat sinkronisasi" });
            }
        }

        /// <summary>
        /// Get offline-capable configuration for POS functions
        /// </summary>
        /// <returns>Offline configuration</returns>
        [HttpGet("offline-config")]
        [Authorize]
        public IActionResult GetOfflineConfig()
        {
            try
            {
                var config = new 
                {
                    maxOfflineItems = _configuration.GetValue<int>("PWA:MaxOfflineItems", 100),
                    maxOfflineDays = _configuration.GetValue<int>("PWA:MaxOfflineDays", 7),
                    syncInterval = _configuration.GetValue<int>("PWA:SyncIntervalMinutes", 30),
                    supportedOperations = new[]
                    {
                        "CREATE_SALE",
                        "UPDATE_PRODUCT_STOCK",
                        "CREATE_MEMBER_TRANSACTION",
                        "RECORD_PAYMENT"
                    },
                    criticalData = new[]
                    {
                        "products",
                        "members", 
                        "payment-methods",
                        "tax-rates"
                    }
                };

                return Ok(new { success = true, config = config });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting offline config");
                return StatusCode(500, new { success = false, message = "Terjadi kesalahan sistem" });
            }
        }

        // ==================== DATA SYNCHRONIZATION ==================== //

        /// <summary>
        /// Get essential data for offline operation
        /// </summary>
        /// <param name="lastSync">Last sync timestamp</param>
        /// <returns>Essential data package</returns>
        [HttpGet("essential-data")]
        [Authorize]
        public Task<IActionResult> GetEssentialData([FromQuery] DateTime? lastSync = null)
        {
            try
            {
                var branchId = GetCurrentUserBranchId();
                var data = new Dictionary<string, object>();

                // Get products (essential for POS)
                // This would integrate with IProductService when available
                data["products"] = new List<object>(); // Placeholder

                // Add other essential data
                data["timestamp"] = DateTime.UtcNow;
                data["branchId"] = branchId ?? 0;
                data["syncVersion"] = GetBuildNumber();

                _logger.LogInformation("Essential data requested for branch {BranchId}, last sync: {LastSync}", 
                    branchId, lastSync);

                return Task.FromResult<IActionResult>(Ok(new 
                { 
                    success = true,
                    data = data,
                    lastSync = lastSync,
                    currentTime = DateTime.UtcNow
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting essential data");
                return Task.FromResult<IActionResult>(StatusCode(500, new { success = false, message = "Terjadi kesalahan sistem" }));
            }
        }

        /// <summary>
        /// Upload offline transaction data
        /// </summary>
        /// <param name="transactions">Offline transactions</param>
        /// <returns>Upload results</returns>
        [HttpPost("upload-offline-data")]
        [Authorize]
        public async Task<IActionResult> UploadOfflineData([FromBody] List<Dictionary<string, object>> transactions)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized("User ID tidak ditemukan");
                }

                var results = new List<object>();
                
                foreach (var transaction in transactions)
                {
                    try
                    {
                        // Process offline transaction
                        // This would integrate with existing services
                        var result = await ProcessOfflineTransaction(transaction, userId.Value);
                        results.Add(result);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing offline transaction");
                        results.Add(new 
                        { 
                            success = false, 
                            error = ex.Message 
                        });
                    }
                }

                return Ok(new 
                { 
                    success = true,
                    message = "Data offline berhasil diupload",
                    results = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading offline data");
                return StatusCode(500, new { success = false, message = "Terjadi kesalahan sistem" });
            }
        }

        // ==================== PWA MANIFEST & SERVICE WORKER ==================== //

        /// <summary>
        /// Get PWA manifest configuration
        /// </summary>
        /// <returns>PWA manifest</returns>
        [HttpGet("manifest")]
        [AllowAnonymous]
        public IActionResult GetManifest()
        {
            try
            {
                var manifest = new 
                {
                    name = "Toko Eniwan POS",
                    short_name = "Toko Eniwan",
                    description = "Sistem Point of Sale untuk Toko Eniwan",
                    start_url = "/",
                    display = "standalone",
                    theme_color = "#2563eb",
                    background_color = "#ffffff",
                    orientation = "portrait-primary",
                    icons = GetAppIcons(),
                    categories = new[] { "business", "productivity", "finance" },
                    lang = "id-ID",
                    dir = "ltr",
                    scope = "/"
                };

                return Ok(manifest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PWA manifest");
                return StatusCode(500, new { success = false, message = "Terjadi kesalahan sistem" });
            }
        }

        // ==================== HELPER METHODS ==================== //

        private async Task<(bool Success, string Message, int? ServerId)> ProcessSyncItem(
            OfflineSyncItem item, int userId, int? branchId)
        {
            // This would integrate with existing services based on entity type
            // For now, return a placeholder implementation
            
            switch (item.EntityType.ToUpper())
            {
                case "SALE":
                    return await ProcessOfflineSale(item, userId, branchId);
                
                case "PRODUCT":
                    return await ProcessOfflineProduct(item, userId, branchId);
                
                default:
                    return (false, $"Entity type '{item.EntityType}' not supported", null);
            }
        }

        private async Task<(bool Success, string Message, int? ServerId)> ProcessOfflineSale(
            OfflineSyncItem item, int userId, int? branchId)
        {
            // Integration with SaleService would go here
            // For now, return placeholder
            await Task.Delay(100); // Simulate processing
            
            return (true, "Transaksi penjualan berhasil disinkronisasi", new Random().Next(1000, 9999));
        }

        private async Task<(bool Success, string Message, int? ServerId)> ProcessOfflineProduct(
            OfflineSyncItem item, int userId, int? branchId)
        {
            // Integration with ProductService would go here
            // For now, return placeholder
            await Task.Delay(50); // Simulate processing
            
            return (true, "Data produk berhasil disinkronisasi", null);
        }

        private async Task<object> ProcessOfflineTransaction(Dictionary<string, object> transaction, int userId)
        {
            // Process individual offline transaction
            // This would integrate with existing transaction services
            await Task.Delay(100); // Simulate processing
            
            return new 
            { 
                success = true, 
                message = "Transaksi berhasil diproses",
                transactionId = new Random().Next(1000, 9999)
            };
        }

        private List<string> GetCriticalResourcesList()
        {
            return new List<string>
            {
                "/",
                "/login",
                "/dashboard",
                "/pos",
                "/products",
                "/css/app.css",
                "/js/app.js",
                "/icons/icon-192x192.png",
                "/icons/icon-512x512.png",
                "/offline.html"
            };
        }

        private DateTime GetBuildDate()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fileInfo = new FileInfo(assembly.Location);
            return fileInfo.LastWriteTime;
        }

        private string GetBuildNumber()
        {
            return DateTime.Now.ToString("yyyyMMddHH");
        }

        private bool IsNewerVersion(string serverVersion, string clientVersion)
        {
            try
            {
                var serverVer = new Version(serverVersion);
                var clientVer = new Version(clientVersion);
                return serverVer > clientVer;
            }
            catch
            {
                return true; // Assume update needed if version parsing fails
            }
        }

        private List<string> GetReleaseNotes()
        {
            return new List<string>
            {
                "Peningkatan performa sistem",
                "Perbaikan bug pada modul penjualan",
                "Fitur notifikasi push baru",
                "Optimisasi penggunaan offline"
            };
        }

        private List<object> GetAppIcons()
        {
            return new List<object>
            {
                new { src = "/icons/icon-72x72.png", sizes = "72x72", type = "image/png" },
                new { src = "/icons/icon-96x96.png", sizes = "96x96", type = "image/png" },
                new { src = "/icons/icon-128x128.png", sizes = "128x128", type = "image/png" },
                new { src = "/icons/icon-144x144.png", sizes = "144x144", type = "image/png" },
                new { src = "/icons/icon-152x152.png", sizes = "152x152", type = "image/png" },
                new { src = "/icons/icon-192x192.png", sizes = "192x192", type = "image/png" },
                new { src = "/icons/icon-384x384.png", sizes = "384x384", type = "image/png" },
                new { src = "/icons/icon-512x512.png", sizes = "512x512", type = "image/png" }
            };
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        private int? GetCurrentUserBranchId()
        {
            var branchIdClaim = User.FindFirst("BranchId")?.Value;
            return int.TryParse(branchIdClaim, out var branchId) ? branchId : null;
        }
    }
}