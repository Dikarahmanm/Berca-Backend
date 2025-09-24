using Microsoft.Extensions.Caching.Memory;

namespace Berca_Backend.Services
{
    /// <summary>
    /// Centralized cache invalidation service for managing cache consistency
    /// Provides pattern-based and tag-based cache invalidation
    /// </summary>
    public interface ICacheInvalidationService
    {
        /// <summary>
        /// Track cache key for invalidation purposes
        /// </summary>
        void TrackCacheKey(string key);

        /// <summary>
        /// Invalidate cache entries by pattern matching
        /// </summary>
        void InvalidateByPattern(string pattern);

        /// <summary>
        /// Invalidate specific cache key
        /// </summary>
        void InvalidateKey(string key);

        /// <summary>
        /// Invalidate multiple cache keys
        /// </summary>
        void InvalidateKeys(params string[] keys);

        /// <summary>
        /// Invalidate cache entries by tags
        /// </summary>
        void InvalidateByTags(params string[] tags);

        /// <summary>
        /// Get cache statistics
        /// </summary>
        CacheStatistics GetCacheStats();
    }

    public class CacheInvalidationService : ICacheInvalidationService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CacheInvalidationService> _logger;

        // Track cache keys for pattern-based invalidation
        private readonly HashSet<string> _cacheKeys = new();
        private readonly object _lockObject = new();

        public CacheInvalidationService(IMemoryCache cache, ILogger<CacheInvalidationService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Track cache key for invalidation purposes
        /// Call this whenever you SET a cache key
        /// </summary>
        public void TrackCacheKey(string key)
        {
            lock (_lockObject)
            {
                _cacheKeys.Add(key);
            }
        }

        /// <summary>
        /// Invalidate cache entries by pattern matching
        /// Examples:
        /// - "product_*" invalidates all product caches
        /// - "*_123" invalidates all caches ending with _123
        /// </summary>
        public void InvalidateByPattern(string pattern)
        {
            var keysToRemove = new List<string>();

            lock (_lockObject)
            {
                var regex = PatternToRegex(pattern);
                keysToRemove = _cacheKeys.Where(key => regex.IsMatch(key)).ToList();
            }

            foreach (var key in keysToRemove)
            {
                InvalidateKey(key);
            }

            _logger.LogInformation("🗑️ Cache INVALIDATION: Removed {Count} cache entries matching pattern '{Pattern}'",
                keysToRemove.Count, pattern);
        }

        /// <summary>
        /// Invalidate specific cache key
        /// </summary>
        public void InvalidateKey(string key)
        {
            _cache.Remove(key);

            lock (_lockObject)
            {
                _cacheKeys.Remove(key);
            }

            _logger.LogDebug("🗑️ Cache INVALIDATION: Removed cache key '{Key}'", key);
        }

        /// <summary>
        /// Invalidate multiple cache keys
        /// </summary>
        public void InvalidateKeys(params string[] keys)
        {
            foreach (var key in keys)
            {
                InvalidateKey(key);
            }

            _logger.LogInformation("🗑️ Cache INVALIDATION: Removed {Count} specific cache keys", keys.Length);
        }

        /// <summary>
        /// Invalidate cache entries by tags
        /// This is a simplified tag-based invalidation using naming conventions
        /// </summary>
        public void InvalidateByTags(params string[] tags)
        {
            foreach (var tag in tags)
            {
                // Convert tag to pattern
                var pattern = $"*{tag}*";
                InvalidateByPattern(pattern);
            }

            _logger.LogInformation("🗑️ Cache INVALIDATION: Removed cache entries for tags: [{Tags}]",
                string.Join(", ", tags));
        }

        /// <summary>
        /// Get cache statistics
        /// </summary>
        public CacheStatistics GetCacheStats()
        {
            lock (_lockObject)
            {
                return new CacheStatistics
                {
                    TotalCacheKeys = _cacheKeys.Count,
                    CacheKeys = _cacheKeys.ToList()
                };
            }
        }

        /// <summary>
        /// Convert wildcard pattern to regex
        /// </summary>
        private static System.Text.RegularExpressions.Regex PatternToRegex(string pattern)
        {
            var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$";

            return new System.Text.RegularExpressions.Regex(regexPattern,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
    }

    /// <summary>
    /// Cache statistics for monitoring
    /// </summary>
    public class CacheStatistics
    {
        public int TotalCacheKeys { get; set; }
        public List<string> CacheKeys { get; set; } = new();
    }
}