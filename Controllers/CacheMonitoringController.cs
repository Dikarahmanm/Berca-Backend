using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Berca_Backend.Services;

namespace Berca_Backend.Controllers
{
    /// <summary>
    /// Cache monitoring and management controller
    /// Provides endpoints for monitoring cache performance and managing cache operations
    /// </summary>
    [ApiController]
    [Route("api/cache")]
    [Authorize(Policy = "Admin.Cache")] // Only admins can access cache monitoring
    public class CacheMonitoringController : ControllerBase
    {
        private readonly ICacheInvalidationService _cacheInvalidation;
        private readonly ICacheWarmupService _cacheWarmup;
        private readonly ILogger<CacheMonitoringController> _logger;

        public CacheMonitoringController(
            ICacheInvalidationService cacheInvalidation,
            ICacheWarmupService cacheWarmup,
            ILogger<CacheMonitoringController> logger)
        {
            _cacheInvalidation = cacheInvalidation;
            _cacheWarmup = cacheWarmup;
            _logger = logger;
        }

        /// <summary>
        /// Get cache statistics and performance metrics
        /// </summary>
        [HttpGet("stats")]
        public IActionResult GetCacheStatistics()
        {
            try
            {
                var invalidationStats = _cacheInvalidation.GetCacheStats();
                var warmupStats = _cacheWarmup.GetWarmupStats();

                var stats = new
                {
                    Cache = new
                    {
                        TotalKeys = invalidationStats.TotalCacheKeys,
                        ActiveKeys = invalidationStats.CacheKeys.Take(50).ToList() // Show first 50 keys
                    },
                    Warmup = new
                    {
                        LastWarmupTime = warmupStats.LastWarmupTime,
                        LastWarmupDuration = warmupStats.LastWarmupDuration.TotalMilliseconds,
                        TotalWarmupsPerformed = warmupStats.TotalWarmupsPerformed,
                        CacheEntriesWarmed = warmupStats.CacheEntriesWarmed,
                        LastError = warmupStats.LastWarmupError
                    },
                    SystemInfo = new
                    {
                        ServerTime = DateTime.UtcNow,
                        MachineName = Environment.MachineName,
                        ProcessorCount = Environment.ProcessorCount
                    }
                };

                return Ok(new
                {
                    success = true,
                    data = stats,
                    message = "Cache statistics retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cache statistics");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Manually trigger cache warmup
        /// </summary>
        [HttpPost("warmup")]
        public async Task<IActionResult> TriggerCacheWarmup([FromQuery] string? type = "all")
        {
            try
            {
                _logger.LogInformation("🔥 Manual cache warmup triggered by admin for type: {WarmupType}", type);

                switch (type?.ToLower())
                {
                    case "dashboard":
                        await _cacheWarmup.WarmupDashboardCachesAsync();
                        break;
                    case "pos":
                        await _cacheWarmup.WarmupPOSCachesAsync();
                        break;
                    case "reference":
                        await _cacheWarmup.WarmupReferenceDataCachesAsync();
                        break;
                    case "ml":
                        await _cacheWarmup.WarmupMLPredictionCachesAsync();
                        break;
                    case "all":
                    default:
                        await _cacheWarmup.WarmupStartupCachesAsync();
                        break;
                }

                _logger.LogInformation("🔥 Manual cache warmup completed successfully");

                return Ok(new
                {
                    success = true,
                    message = $"Cache warmup completed successfully for type: {type}",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during manual cache warmup");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Cache warmup failed",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Invalidate cache by pattern
        /// </summary>
        [HttpPost("invalidate")]
        public IActionResult InvalidateCacheByPattern([FromBody] CacheInvalidationRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Pattern))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Pattern is required"
                    });
                }

                _logger.LogInformation("🗑️ Manual cache invalidation triggered by admin for pattern: {Pattern}", request.Pattern);

                _cacheInvalidation.InvalidateByPattern(request.Pattern);

                return Ok(new
                {
                    success = true,
                    message = $"Cache invalidated successfully for pattern: {request.Pattern}",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during manual cache invalidation");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Cache invalidation failed",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get cache keys by pattern for debugging
        /// </summary>
        [HttpGet("keys")]
        public IActionResult GetCacheKeys([FromQuery] string? pattern = null)
        {
            try
            {
                var stats = _cacheInvalidation.GetCacheStats();
                var allKeys = stats.CacheKeys;

                if (!string.IsNullOrWhiteSpace(pattern))
                {
                    var regex = new System.Text.RegularExpressions.Regex(
                        "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*") + "$",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                    allKeys = allKeys.Where(key => regex.IsMatch(key)).ToList();
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        pattern = pattern ?? "all",
                        totalKeys = stats.TotalCacheKeys,
                        matchingKeys = allKeys.Take(100).ToList(), // Limit to 100 keys
                        hasMore = allKeys.Count > 100
                    },
                    message = $"Found {allKeys.Count} cache keys"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cache keys");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error"
                });
            }
        }
    }

    /// <summary>
    /// Request model for cache invalidation
    /// </summary>
    public class CacheInvalidationRequest
    {
        public string Pattern { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }
}