using Microsoft.Extensions.Caching.Memory;
using Berca_Backend.Services;

namespace Berca_Backend.Extensions
{
    /// <summary>
    /// Extension methods for IMemoryCache to integrate with cache invalidation tracking
    /// </summary>
    public static class CacheExtensions
    {
        /// <summary>
        /// Set cache value and track the key for invalidation
        /// </summary>
        public static void SetAndTrack<T>(this IMemoryCache cache, string key, T value, MemoryCacheEntryOptions options, ICacheInvalidationService invalidationService)
        {
            cache.Set(key, value, options);
            invalidationService.TrackCacheKey(key);
        }

        /// <summary>
        /// Set cache value with expiration and track the key for invalidation
        /// </summary>
        public static void SetAndTrack<T>(this IMemoryCache cache, string key, T value, TimeSpan expiration, ICacheInvalidationService invalidationService)
        {
            cache.Set(key, value, expiration);
            invalidationService.TrackCacheKey(key);
        }

        /// <summary>
        /// Set cache value with absolute expiration and track the key for invalidation
        /// </summary>
        public static void SetAndTrack<T>(this IMemoryCache cache, string key, T value, DateTimeOffset absoluteExpiration, ICacheInvalidationService invalidationService)
        {
            cache.Set(key, value, absoluteExpiration);
            invalidationService.TrackCacheKey(key);
        }
    }
}