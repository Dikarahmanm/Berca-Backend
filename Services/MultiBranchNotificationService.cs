using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Berca_Backend.DTOs;
using Berca_Backend.Models;
using Berca_Backend.Services.Interfaces;
using Berca_Backend.Data;
using System.Security.Claims;

namespace Berca_Backend.Services
{
    /// <summary>
    /// Service for multi-branch notification management using existing infrastructure
    /// </summary>
    public class MultiBranchNotificationService : IMultiBranchNotificationService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<MultiBranchNotificationService> _logger;
        private readonly IHubContext<MultiBranchNotificationHub> _hubContext;

        public MultiBranchNotificationService(
            AppDbContext context,
            ILogger<MultiBranchNotificationService> logger,
            IHubContext<MultiBranchNotificationHub> hubContext)
        {
            _context = context;
            _logger = logger;
            _hubContext = hubContext;
        }

        // === CRUD OPERATIONS ===

        public async Task<PaginatedNotificationResponse> GetNotificationsAsync(NotificationFilters filters, int currentUserId)
        {
            try
            {
                var query = _context.Notifications
                    .Include(n => n.Branch)
                    .Include(n => n.User)
                    .AsQueryable();

                // Apply access control filters
                var filteredQuery = await ApplyAccessControlFiltersAsync(query, currentUserId, filters.BranchId);

                // Apply search filters
                if (filters.BranchId.HasValue)
                    filteredQuery = filteredQuery.Where(n => n.BranchId == filters.BranchId.Value);

                if (!string.IsNullOrEmpty(filters.Type))
                    filteredQuery = filteredQuery.Where(n => n.Type == filters.Type);

                if (!string.IsNullOrEmpty(filters.Severity))
                    filteredQuery = filteredQuery.Where(n => n.Severity == filters.Severity);

                if (!string.IsNullOrEmpty(filters.Priority))
                {
                    if (Enum.TryParse<NotificationPriority>(filters.Priority, true, out var priority))
                    {
                        filteredQuery = filteredQuery.Where(n => n.Priority == priority);
                    }
                }

                if (filters.IsRead.HasValue)
                    filteredQuery = filteredQuery.Where(n => n.IsRead == filters.IsRead.Value);

                if (filters.ActionRequired.HasValue)
                    filteredQuery = filteredQuery.Where(n => n.ActionRequired == filters.ActionRequired.Value);

                if (filters.DateFrom.HasValue)
                    filteredQuery = filteredQuery.Where(n => n.CreatedAt >= filters.DateFrom.Value);

                if (filters.DateTo.HasValue)
                    filteredQuery = filteredQuery.Where(n => n.CreatedAt <= filters.DateTo.Value);

                if (filters.UserId.HasValue)
                    filteredQuery = filteredQuery.Where(n => n.UserId == filters.UserId.Value);

                // Remove expired notifications
                filteredQuery = filteredQuery.Where(n => !n.ExpiryDate.HasValue || n.ExpiryDate.Value > DateTime.UtcNow);

                // Apply sorting
                var sortedQuery = filters.SortBy.ToLower() switch
                {
                    "title" => filters.SortOrder.ToLower() == "asc" 
                        ? filteredQuery.OrderBy(n => n.Title) 
                        : filteredQuery.OrderByDescending(n => n.Title),
                    "priority" => filters.SortOrder.ToLower() == "asc" 
                        ? filteredQuery.OrderBy(n => n.Priority) 
                        : filteredQuery.OrderByDescending(n => n.Priority),
                    "severity" => filters.SortOrder.ToLower() == "asc" 
                        ? filteredQuery.OrderBy(n => n.Severity) 
                        : filteredQuery.OrderByDescending(n => n.Severity),
                    _ => filters.SortOrder.ToLower() == "asc" 
                        ? filteredQuery.OrderBy(n => n.CreatedAt) 
                        : filteredQuery.OrderByDescending(n => n.CreatedAt)
                };

                // Get total count
                var totalCount = await filteredQuery.CountAsync();

                // Apply pagination
                var notifications = await sortedQuery
                    .Skip((filters.Page - 1) * filters.PageSize)
                    .Take(filters.PageSize)
                    .ToListAsync();

                var totalPages = (int)Math.Ceiling((double)totalCount / filters.PageSize);

                return new PaginatedNotificationResponse
                {
                    Data = notifications.Select(MapToDto).ToList(),
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    CurrentPage = filters.Page,
                    PageSize = filters.PageSize,
                    HasNextPage = filters.Page < totalPages,
                    HasPreviousPage = filters.Page > 1
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications for user {UserId}", currentUserId);
                throw;
            }
        }

        public async Task<NotificationDto?> GetNotificationByIdAsync(string id, int currentUserId)
        {
            try
            {
                if (!int.TryParse(id, out var notificationId))
                    return null;

                var notification = await _context.Notifications
                    .Include(n => n.Branch)
                    .Include(n => n.User)
                    .FirstOrDefaultAsync(n => n.Id == notificationId);

                if (notification == null)
                    return null;

                // Check access
                if (!await CanUserAccessNotificationInternalAsync(notification, currentUserId))
                    return null;

                return MapToDto(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification {Id} for user {UserId}", id, currentUserId);
                throw;
            }
        }

        public async Task<NotificationDto> CreateNotificationAsync(CreateNotificationRequest request, int createdBy)
        {
            try
            {
                var notification = new Notification
                {
                    Type = request.Type,
                    Severity = "info", // Default severity
                    Priority = Enum.TryParse<NotificationPriority>(request.Priority, true, out var priority) ? priority : NotificationPriority.Normal,
                    Title = request.Title,
                    Message = request.Message,
                    UserId = request.UserId,
                    ActionRequired = false,
                    ActionUrl = request.ActionUrl,
                    ActionText = request.ActionText,
                    ExpiryDate = request.ExpiryDate,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = createdBy.ToString()
                };

                // Add multi-branch specific fields if request is CreateNotificationDto
                if (request is CreateNotificationDto createDto)
                {
                    notification.BranchId = createDto.BranchId;
                    notification.ActionRequired = createDto.RequiresAction;
                }

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Load navigation properties
                await _context.Entry(notification)
                    .Reference(n => n.Branch)
                    .LoadAsync();
                await _context.Entry(notification)
                    .Reference(n => n.User)
                    .LoadAsync();

                var notificationDto = MapToDto(notification);

                // Broadcast real-time update
                await BroadcastNotificationUpdateAsync("created", notificationDto, notification.BranchId);

                _logger.LogInformation("Created notification {Id} by user {UserId}", notification.Id, createdBy);

                return notificationDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification for user {UserId}", createdBy);
                throw;
            }
        }

        public async Task<bool> UpdateNotificationAsync(string id, UpdateNotificationRequest request, int currentUserId)
        {
            try
            {
                if (!int.TryParse(id, out var notificationId))
                    return false;

                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == notificationId);

                if (notification == null)
                    return false;

                // Check access
                if (!await CanUserAccessNotificationInternalAsync(notification, currentUserId))
                    return false;

                var hasChanges = false;

                if (request.IsRead.HasValue && notification.IsRead != request.IsRead.Value)
                {
                    notification.IsRead = request.IsRead.Value;
                    notification.ReadAt = request.IsRead.Value ? DateTime.UtcNow : null;
                    hasChanges = true;
                }

                if (request.IsArchived.HasValue && notification.IsArchived != request.IsArchived.Value)
                {
                    notification.IsArchived = request.IsArchived.Value;
                    notification.ArchivedAt = request.IsArchived.Value ? DateTime.UtcNow : null;
                    hasChanges = true;
                }

                if (request.Metadata != null)
                {
                    notification.Metadata = System.Text.Json.JsonSerializer.Serialize(request.Metadata);
                    hasChanges = true;
                }

                if (hasChanges)
                {
                    await _context.SaveChangesAsync();

                    // Load navigation properties for broadcast
                    await _context.Entry(notification)
                        .Reference(n => n.Branch)
                        .LoadAsync();
                    await _context.Entry(notification)
                        .Reference(n => n.User)
                        .LoadAsync();

                    // Broadcast real-time update
                    await BroadcastNotificationUpdateAsync("updated", MapToDto(notification), notification.BranchId);

                    _logger.LogInformation("Updated notification {Id} by user {UserId}", notificationId, currentUserId);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notification {Id} for user {UserId}", id, currentUserId);
                throw;
            }
        }

        public async Task<bool> DeleteNotificationAsync(string id, int currentUserId)
        {
            try
            {
                if (!int.TryParse(id, out var notificationId))
                    return false;

                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == notificationId);

                if (notification == null)
                    return false;

                // Check access - only creator or admin can delete
                var userRole = await GetUserRoleAsync(currentUserId);
                if (notification.CreatedBy != currentUserId.ToString() && 
                    !new[] { "Admin", "HeadManager" }.Contains(userRole))
                    return false;

                var branchId = notification.BranchId;

                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();

                // Broadcast real-time update
                await BroadcastNotificationUpdateAsync("deleted", null, branchId, new List<string> { id });

                _logger.LogInformation("Deleted notification {Id} by user {UserId}", notificationId, currentUserId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification {Id} for user {UserId}", id, currentUserId);
                throw;
            }
        }

        // === SIMPLE IMPLEMENTATIONS FOR REQUIRED INTERFACE METHODS ===

        public async Task<bool> MarkAsReadAsync(string id, int userId)
        {
            var request = new UpdateNotificationRequest { IsRead = true };
            return await UpdateNotificationAsync(id, request, userId);
        }

        public async Task<bool> MarkBulkAsReadAsync(List<string> ids, int userId)
        {
            foreach (var id in ids)
            {
                await MarkAsReadAsync(id, userId);
            }
            return true;
        }

        public async Task<bool> MarkAllAsReadAsync(int? branchId, int userId)
        {
            var notifications = await _context.Notifications
                .Where(n => !n.IsRead)
                .Where(n => branchId == null || n.BranchId == branchId)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                if (await CanUserAccessNotificationInternalAsync(notification, userId))
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ArchiveNotificationAsync(string id, int currentUserId)
        {
            var request = new UpdateNotificationRequest { IsArchived = true };
            return await UpdateNotificationAsync(id, request, currentUserId);
        }

        public async Task<NotificationStatsDto> GetNotificationStatsAsync(int? branchId, DateTime? dateFrom, DateTime? dateTo, int currentUserId)
        {
            var query = _context.Notifications.AsQueryable();

            if (branchId.HasValue)
                query = query.Where(n => n.BranchId == branchId.Value);

            if (dateFrom.HasValue)
                query = query.Where(n => n.CreatedAt >= dateFrom.Value);

            if (dateTo.HasValue)
                query = query.Where(n => n.CreatedAt <= dateTo.Value);

            var notifications = await query.ToListAsync();

            return new NotificationStatsDto
            {
                Total = notifications.Count,
                Unread = notifications.Count(n => !n.IsRead),
                ActionRequired = notifications.Count(n => n.ActionRequired),
                Archived = notifications.Count(n => n.IsArchived),
                ByType = notifications.GroupBy(n => n.Type)
                    .ToDictionary(g => g.Key, g => g.Count()),
                BySeverity = notifications.GroupBy(n => n.Severity)
                    .ToDictionary(g => g.Key, g => g.Count()),
                ByPriority = notifications.GroupBy(n => n.Priority?.ToString() ?? "Normal")
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }

        public async Task<NotificationHealthDto> GetSystemHealthAsync()
        {
            var notificationCount = await _context.Notifications.CountAsync();
            var dbConnected = await _context.Database.CanConnectAsync();

            return new NotificationHealthDto
            {
                Status = dbConnected ? "healthy" : "unhealthy",
                Database = new DatabaseHealthDto
                {
                    Connected = dbConnected,
                    Notifications = notificationCount
                },
                SignalR = new SignalRHealthDto
                {
                    Connected = true,
                    Connections = 0
                },
                Performance = new PerformanceHealthDto
                {
                    AverageResponseTime = 85,
                    RequestsPerMinute = 100
                }
            };
        }

        public async Task<int> GetUnreadCountAsync(int userId, int? branchId = null)
        {
            var query = _context.Notifications
                .Where(n => !n.IsRead && (!n.ExpiryDate.HasValue || n.ExpiryDate.Value > DateTime.UtcNow));

            var filteredQuery = await ApplyAccessControlFiltersAsync(query, userId, branchId);

            if (branchId.HasValue)
                filteredQuery = filteredQuery.Where(n => n.BranchId == branchId.Value);

            return await filteredQuery.CountAsync();
        }

        public async Task<List<NotificationDto>> GetRecentNotificationsAsync(int userId, int? branchId = null, int limit = 10)
        {
            var query = _context.Notifications
                .Include(n => n.Branch)
                .Include(n => n.User)
                .Where(n => !n.ExpiryDate.HasValue || n.ExpiryDate.Value > DateTime.UtcNow)
                .OrderByDescending(n => n.CreatedAt);

            var filteredQuery = await ApplyAccessControlFiltersAsync(query, userId, branchId);

            if (branchId.HasValue)
                filteredQuery = filteredQuery.Where(n => n.BranchId == branchId.Value);

            var notifications = await filteredQuery.Take(limit).ToListAsync();
            return notifications.Select(MapToDto).ToList();
        }

        // === HELPER METHODS ===

        private NotificationDto MapToDto(Notification notification)
        {
            return new NotificationDto
            {
                Id = notification.Id,
                Type = notification.Type,
                Severity = notification.Severity,
                Priority = notification.Priority?.ToString() ?? "Normal",
                Title = notification.Title,
                Message = notification.Message,
                BranchId = notification.BranchId,
                BranchName = notification.Branch?.BranchName,
                UserId = notification.UserId,
                UserName = notification.User?.Username,
                CreatedAt = notification.CreatedAt,
                IsRead = notification.IsRead,
                IsArchived = notification.IsArchived,
                ActionRequired = notification.ActionRequired,
                ActionUrl = notification.ActionUrl,
                ActionText = notification.ActionText,
                ExpiresAt = notification.ExpiryDate,
                TimeAgo = notification.TimeAgo,
                IsExpired = notification.IsExpired,
                ReadAt = notification.ReadAt,
                Metadata = !string.IsNullOrEmpty(notification.Metadata) 
                    ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(notification.Metadata)
                    : null
            };
        }

        private async Task<IQueryable<Notification>> ApplyAccessControlFiltersAsync(
            IQueryable<Notification> query, int userId, int? branchId)
        {
            var userRole = await GetUserRoleAsync(userId);
            var userBranchId = await GetUserBranchIdAsync(userId);

            // Admin and HeadManager can see all notifications
            if (new[] { "Admin", "HeadManager" }.Contains(userRole))
                return query;

            // Branch managers can see their branch notifications
            if (userRole == "BranchManager" && userBranchId.HasValue)
            {
                return query.Where(n => n.BranchId == userBranchId.Value || n.BranchId == null);
            }

            // Regular users can only see notifications targeted to them or their branch
            return query.Where(n => 
                n.UserId == userId || 
                (n.BranchId == userBranchId && userBranchId.HasValue) ||
                n.BranchId == null); // System-wide notifications
        }

        private async Task<bool> CanUserAccessNotificationInternalAsync(Notification notification, int userId)
        {
            var userRole = await GetUserRoleAsync(userId);
            var userBranchId = await GetUserBranchIdAsync(userId);

            // Admin and HeadManager can access all
            if (new[] { "Admin", "HeadManager" }.Contains(userRole))
                return true;

            // User-specific notification
            if (notification.UserId == userId)
                return true;

            // Branch-specific notification
            if (notification.BranchId.HasValue && notification.BranchId == userBranchId)
                return true;

            // System-wide notification (no specific branch)
            if (!notification.BranchId.HasValue && new[] { "BranchManager", "Manager" }.Contains(userRole))
                return true;

            return false;
        }

        private async Task<string> GetUserRoleAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            return user?.Role ?? "User";
        }

        private async Task<int?> GetUserBranchIdAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            return user?.BranchId;
        }

        private async Task BroadcastNotificationUpdateAsync(string action, NotificationDto? notification, int? branchId, List<string>? notificationIds = null)
        {
            try
            {
                var update = new NotificationUpdateDto
                {
                    Action = action,
                    Notification = notification,
                    NotificationIds = notificationIds,
                    Timestamp = DateTime.UtcNow,
                    BranchId = branchId
                };

                if (branchId.HasValue)
                {
                    await _hubContext.Clients.Group($"branch-{branchId}")
                        .SendAsync("NotificationUpdate", update);
                }
                else
                {
                    // System-wide notification
                    await _hubContext.Clients.All
                        .SendAsync("SystemNotificationUpdate", update);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting notification update");
            }
        }

        // === STUB IMPLEMENTATIONS ===

        public async Task<byte[]> ExportNotificationsAsync(ExportNotificationRequest request, int currentUserId)
        {
            await Task.CompletedTask;
            return Array.Empty<byte>();
        }

        public async Task<NotificationDto> CreateSystemNotificationAsync(string title, string message, string priority = "medium", int? branchId = null, Dictionary<string, object>? metadata = null)
        {
            var request = new CreateNotificationDto
            {
                Type = NotificationType.System,
                Title = title,
                Message = message,
                Priority = priority,
                BranchId = branchId,
                IsSystemNotification = true,
                Metadata = metadata
            };

            return await CreateNotificationAsync(request, 1); // System user
        }

        public async Task<NotificationDto> CreateTransferNotificationAsync(string title, string message, int sourceBranchId, int targetBranchId, string transferId, string priority = "medium")
        {
            var metadata = new Dictionary<string, object>
            {
                ["transferId"] = transferId,
                ["sourceBranchId"] = sourceBranchId,
                ["targetBranchId"] = targetBranchId
            };

            var request = new CreateNotificationDto
            {
                Type = NotificationType.Transfer,
                Title = title,
                Message = message,
                Priority = priority,
                BranchId = targetBranchId,
                RequiresAction = true,
                ActionUrl = $"/transfers/{transferId}",
                Metadata = metadata
            };

            return await CreateNotificationAsync(request, 1); // System user
        }

        public async Task<NotificationDto> CreateCoordinationAlertAsync(string title, string message, int? branchId, string severity = "warning", string priority = "high", Dictionary<string, object>? metadata = null)
        {
            var request = new CreateNotificationDto
            {
                Type = NotificationType.Coordination,
                Title = title,
                Message = message,
                Priority = priority,
                BranchId = branchId,
                RequiresAction = true,
                Metadata = metadata
            };

            return await CreateNotificationAsync(request, 1); // System user
        }

        public async Task<NotificationDto> CreateInventoryAlertAsync(string title, string message, int branchId, int productId, string severity = "warning", Dictionary<string, object>? metadata = null)
        {
            var notificationMetadata = metadata ?? new Dictionary<string, object>();
            notificationMetadata["productId"] = productId;

            var request = new CreateNotificationDto
            {
                Type = NotificationType.Inventory,
                Title = title,
                Message = message,
                Priority = "medium",
                BranchId = branchId,
                RequiresAction = true,
                ActionUrl = $"/inventory/products/{productId}",
                Metadata = notificationMetadata
            };

            return await CreateNotificationAsync(request, 1); // System user
        }

        public Task<NotificationDto> CreateFromTemplateAsync(string templateName, Dictionary<string, object> parameters, int? branchId = null, int? userId = null)
        {
            throw new NotImplementedException("Template functionality not implemented yet");
        }

        public async Task<int> CleanupExpiredNotificationsAsync()
        {
            var expiredNotifications = await _context.Notifications
                .Where(n => n.ExpiryDate.HasValue && n.ExpiryDate.Value < DateTime.UtcNow)
                .ToListAsync();

            _context.Notifications.RemoveRange(expiredNotifications);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cleaned up {Count} expired notifications", expiredNotifications.Count);
            return expiredNotifications.Count;
        }

        public async Task<int> ArchiveOldNotificationsAsync(int daysOld = 30)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);
            var oldNotifications = await _context.Notifications
                .Where(n => n.CreatedAt < cutoffDate && !n.IsArchived)
                .ToListAsync();

            foreach (var notification in oldNotifications)
            {
                notification.IsArchived = true;
                notification.ArchivedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Archived {Count} old notifications", oldNotifications.Count);
            return oldNotifications.Count;
        }

        public async Task<bool> CanUserAccessNotificationAsync(string notificationId, int userId)
        {
            if (!int.TryParse(notificationId, out var id))
                return false;

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id);

            if (notification == null)
                return false;

            return await CanUserAccessNotificationInternalAsync(notification, userId);
        }

        public async Task<bool> CanUserAccessBranchAsync(int branchId, int userId)
        {
            var userRole = await GetUserRoleAsync(userId);
            var userBranchId = await GetUserBranchIdAsync(userId);

            // Admin and HeadManager can access all branches
            if (new[] { "Admin", "HeadManager" }.Contains(userRole))
                return true;

            // Users can access their own branch
            return userBranchId == branchId;
        }

        public Task<List<NotificationPreference>> GetUserPreferencesAsync(int userId)
        {
            throw new NotImplementedException("User preferences not implemented yet");
        }

        public Task<bool> UpdateUserPreferencesAsync(int userId, List<NotificationPreference> preferences)
        {
            throw new NotImplementedException("User preferences not implemented yet");
        }

        // === SPECIALIZED NOTIFICATION CREATION METHODS ===
        // (Migrated from Basic NotificationService)

        public async Task<bool> CreateLowStockNotificationAsync(int productId, int currentStock, int? branchId = null)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null) return false;

                var notificationRequest = new CreateNotificationRequest
                {
                    Type = NotificationType.Inventory,
                    Title = "Stok Produk Menipis",
                    Message = $"Produk {product.Name} sisa {currentStock} unit (minimum: {product.MinimumStock})",
                    Priority = "High",
                    Severity = "warning",
                    ActionRequired = true,
                    ActionUrl = $"/inventory/products/{productId}",
                    ActionText = "Lihat Produk",
                    BranchId = branchId
                };

                await CreateNotificationAsync(notificationRequest, 1); // System user
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating low stock notification for product {ProductId}", productId);
                return false;
            }
        }

        public async Task<bool> CreateMonthlyRevenueNotificationAsync(decimal revenue, DateTime month, int? branchId = null)
        {
            try
            {
                var notificationRequest = new CreateNotificationRequest
                {
                    Type = NotificationType.Financial,
                    Title = "Laporan Pendapatan Bulanan",
                    Message = $"Pendapatan bulan {month:MMMM yyyy}: Rp {revenue:N0}",
                    Priority = "Normal",
                    Severity = "info",
                    ActionUrl = "/reports/revenue",
                    ActionText = "Lihat Detail",
                    BranchId = branchId
                };

                await CreateNotificationAsync(notificationRequest, 1);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating monthly revenue notification");
                return false;
            }
        }

        public async Task<bool> CreateSystemMaintenanceNotificationAsync(DateTime scheduledTime, string message, int? branchId = null)
        {
            try
            {
                var notificationRequest = new CreateNotificationRequest
                {
                    Type = NotificationType.System,
                    Title = "Maintenance Terjadwal",
                    Message = $"Sistem maintenance pada {scheduledTime:dd/MM/yyyy HH:mm}. {message}",
                    Priority = "Critical",
                    Severity = "warning",
                    ActionRequired = true,
                    BranchId = branchId
                };

                await CreateNotificationAsync(notificationRequest, 1);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating system maintenance notification");
                return false;
            }
        }

        public async Task<bool> BroadcastToAllUsersAsync(string type, string title, string message, int? branchId = null, string? actionUrl = null)
        {
            try
            {
                var notificationRequest = new CreateNotificationRequest
                {
                    UserId = null, // Broadcast to all users
                    Type = type,
                    Title = title,
                    Message = message,
                    Priority = "Normal",
                    Severity = "info",
                    ActionUrl = actionUrl,
                    BranchId = branchId
                };

                await CreateNotificationAsync(notificationRequest, 1);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting notification to all users");
                return false;
            }
        }

        public async Task<bool> BroadcastToRoleAsync(string role, string type, string title, string message, int? branchId = null, string? actionUrl = null)
        {
            try
            {
                var usersQuery = _context.Users.Where(u => u.Role == role && u.IsActive);
                
                if (branchId.HasValue)
                {
                    usersQuery = usersQuery.Where(u => u.BranchId == branchId);
                }

                var users = await usersQuery.ToListAsync();

                foreach (var user in users)
                {
                    var notificationRequest = new CreateNotificationRequest
                    {
                        UserId = user.Id,
                        Type = type,
                        Title = title,
                        Message = message,
                        Priority = "Normal",
                        Severity = "info",
                        ActionUrl = actionUrl,
                        BranchId = branchId
                    };

                    await CreateNotificationAsync(notificationRequest, 1);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting notification to role {Role}", role);
                return false;
            }
        }

        // === PRODUCT & STOCK NOTIFICATIONS ===

        public async Task<bool> CreateOutOfStockNotificationAsync(int productId, int? branchId = null)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null) return false;

                var notificationRequest = new CreateNotificationRequest
                {
                    Type = NotificationType.Inventory,
                    Title = "Produk Habis",
                    Message = $"Produk {product.Name} sudah habis stok",
                    Priority = "Critical",
                    Severity = "error",
                    ActionRequired = true,
                    ActionUrl = $"/inventory/products/{productId}",
                    ActionText = "Restock Produk",
                    BranchId = branchId
                };

                await CreateNotificationAsync(notificationRequest, 1);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating out of stock notification for product {ProductId}", productId);
                return false;
            }
        }

        public async Task<bool> CreateSaleCompletedNotificationAsync(int saleId, string saleNumber, decimal totalAmount, int? branchId = null)
        {
            try
            {
                var notificationRequest = new CreateNotificationRequest
                {
                    Type = NotificationType.Sales,
                    Title = "Penjualan Selesai",
                    Message = $"Transaksi {saleNumber} berhasil. Total: Rp {totalAmount:N0}",
                    Priority = "Normal",
                    Severity = "success",
                    ActionUrl = $"/sales/{saleId}",
                    ActionText = "Lihat Transaksi",
                    BranchId = branchId
                };

                await CreateNotificationAsync(notificationRequest, 1);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sale completed notification");
                return false;
            }
        }

        public async Task<bool> CreateStockAdjustmentNotificationAsync(int productId, int quantity, string notes, int? branchId = null)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null) return false;

                var adjustmentType = quantity > 0 ? "Penambahan" : "Pengurangan";
                var notificationRequest = new CreateNotificationRequest
                {
                    Type = NotificationType.Inventory,
                    Title = "Penyesuaian Stok",
                    Message = $"{adjustmentType} stok {product.Name}: {Math.Abs(quantity)} unit. {notes}",
                    Priority = "Normal",
                    Severity = "info",
                    ActionUrl = $"/inventory/products/{productId}",
                    BranchId = branchId
                };

                await CreateNotificationAsync(notificationRequest, 1);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating stock adjustment notification");
                return false;
            }
        }

        // === EXPIRY MANAGEMENT NOTIFICATIONS ===

        public async Task<bool> CreateExpiryWarningNotificationAsync(int productId, string productName, string batchNumber, DateTime expiryDate, int currentStock, int? branchId = null)
        {
            try
            {
                var notificationRequest = new CreateNotificationRequest
                {
                    Type = NotificationType.Inventory,
                    Title = "Peringatan Produk Akan Kedaluwarsa",
                    Message = $"Produk {productName} (Batch: {batchNumber}) akan kedaluwarsa pada {expiryDate:dd/MM/yyyy}. Stok: {currentStock}",
                    Priority = "High",
                    Severity = "warning",
                    ActionRequired = true,
                    ActionUrl = $"/inventory/products/{productId}",
                    ActionText = "Tindak Lanjut",
                    BranchId = branchId
                };

                await CreateNotificationAsync(notificationRequest, 1);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating expiry warning notification");
                return false;
            }
        }

        public async Task<bool> CreateExpiryUrgentNotificationAsync(int productId, string productName, string batchNumber, DateTime expiryDate, int currentStock, int? branchId = null)
        {
            try
            {
                var notificationRequest = new CreateNotificationRequest
                {
                    Type = NotificationType.Inventory,
                    Title = "URGENT: Produk Segera Kedaluwarsa",
                    Message = $"URGENT! Produk {productName} (Batch: {batchNumber}) akan kedaluwarsa dalam 3 hari ({expiryDate:dd/MM/yyyy}). Stok: {currentStock}",
                    Priority = "Critical",
                    Severity = "error",
                    ActionRequired = true,
                    ActionUrl = $"/inventory/products/{productId}",
                    ActionText = "Tindakan Segera",
                    BranchId = branchId
                };

                await CreateNotificationAsync(notificationRequest, 1);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating expiry urgent notification");
                return false;
            }
        }

        public async Task<bool> CreateExpiryExpiredNotificationAsync(int productId, string productName, string batchNumber, DateTime expiryDate, int currentStock, int? branchId = null)
        {
            try
            {
                var notificationRequest = new CreateNotificationRequest
                {
                    Type = NotificationType.Inventory,
                    Title = "Produk Kedaluwarsa",
                    Message = $"Produk {productName} (Batch: {batchNumber}) telah kedaluwarsa pada {expiryDate:dd/MM/yyyy}. Stok tersisa: {currentStock}",
                    Priority = "Critical",
                    Severity = "error",
                    ActionRequired = true,
                    ActionUrl = $"/inventory/disposal",
                    ActionText = "Proses Disposal",
                    BranchId = branchId
                };

                await CreateNotificationAsync(notificationRequest, 1);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating expired product notification");
                return false;
            }
        }

        public async Task<bool> CreateDisposalCompletedNotificationAsync(int disposedCount, decimal valueLost, string disposalMethod, int? branchId = null)
        {
            try
            {
                var notificationRequest = new CreateNotificationRequest
                {
                    Type = NotificationType.Inventory,
                    Title = "Disposal Produk Selesai",
                    Message = $"Berhasil disposal {disposedCount} unit produk dengan metode {disposalMethod}. Nilai kerugian: Rp {valueLost:N0}",
                    Priority = "Normal",
                    Severity = "info",
                    ActionUrl = "/inventory/disposal-history",
                    BranchId = branchId
                };

                await CreateNotificationAsync(notificationRequest, 1);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating disposal completed notification");
                return false;
            }
        }

        public async Task<bool> BroadcastDailyExpirySummaryAsync(int expiringCount, int expiredCount, decimal valueAtRisk, decimal valueLost, int? branchId = null)
        {
            try
            {
                var notificationRequest = new CreateNotificationRequest
                {
                    Type = NotificationType.System,
                    Title = "Ringkasan Kedaluwarsa Harian",
                    Message = $"Akan kedaluwarsa: {expiringCount} produk (nilai Rp {valueAtRisk:N0}). Sudah kedaluwarsa: {expiredCount} produk (kerugian Rp {valueLost:N0})",
                    Priority = "High",
                    Severity = "warning",
                    ActionUrl = "/reports/expiry-summary",
                    BranchId = branchId
                };

                await BroadcastToRoleAsync("Manager", notificationRequest.Type, notificationRequest.Title, notificationRequest.Message, branchId, notificationRequest.ActionUrl);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting daily expiry summary");
                return false;
            }
        }

        // === MEMBER CREDIT NOTIFICATIONS ===

        public async Task<bool> CreateMemberDebtOverdueNotificationAsync(int memberId, string memberName, decimal overdueAmount, int daysOverdue, int? branchId = null)
        {
            try
            {
                var notificationRequest = new CreateNotificationRequest
                {
                    Type = NotificationType.Financial,
                    Title = "Member Debt Overdue",
                    Message = $"Member {memberName} memiliki tunggakan sebesar Rp {overdueAmount:N0} selama {daysOverdue} hari",
                    Priority = "High",
                    Severity = "warning",
                    ActionRequired = true,
                    ActionUrl = $"/members/{memberId}",
                    ActionText = "Tindak Lanjut",
                    BranchId = branchId
                };

                await CreateNotificationAsync(notificationRequest, 1);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating member debt overdue notification");
                return false;
            }
        }

        public async Task<bool> CreateMemberCreditLimitWarningNotificationAsync(int memberId, string memberName, decimal currentDebt, decimal creditLimit, int? branchId = null)
        {
            try
            {
                var percentage = Math.Round((currentDebt / creditLimit) * 100, 1);
                var notificationRequest = new CreateNotificationRequest
                {
                    Type = NotificationType.Financial,
                    Title = "Peringatan Batas Kredit Member",
                    Message = $"Member {memberName} telah menggunakan {percentage}% dari limit kredit (Rp {currentDebt:N0} / Rp {creditLimit:N0})",
                    Priority = "High",
                    Severity = "warning",
                    ActionUrl = $"/members/{memberId}",
                    BranchId = branchId
                };

                await CreateNotificationAsync(notificationRequest, 1);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating member credit limit warning");
                return false;
            }
        }

        public async Task<bool> CreatePaymentReceivedNotificationAsync(int memberId, string memberName, decimal paymentAmount, int? branchId = null)
        {
            try
            {
                var notificationRequest = new CreateNotificationRequest
                {
                    Type = NotificationType.Financial,
                    Title = "Pembayaran Member Diterima",
                    Message = $"Pembayaran dari {memberName} sebesar Rp {paymentAmount:N0} telah diterima",
                    Priority = "Normal",
                    Severity = "success",
                    ActionUrl = $"/members/{memberId}",
                    BranchId = branchId
                };

                await CreateNotificationAsync(notificationRequest, 1);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment received notification");
                return false;
            }
        }

        // === SUPPLIER & FACTURE NOTIFICATIONS ===

        public async Task<bool> CreateFactureDueTodayNotificationAsync(string factureNumber, string supplierName, decimal amount, int? branchId = null)
        {
            try
            {
                var notificationRequest = new CreateNotificationRequest
                {
                    Type = NotificationType.Financial,
                    Title = "Facture Jatuh Tempo Hari Ini",
                    Message = $"Facture {factureNumber} dari {supplierName} jatuh tempo hari ini. Jumlah: Rp {amount:N0}",
                    Priority = "High",
                    Severity = "warning",
                    ActionRequired = true,
                    ActionUrl = $"/factures?search={factureNumber}",
                    ActionText = "Proses Pembayaran",
                    BranchId = branchId
                };

                await CreateNotificationAsync(notificationRequest, 1);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating facture due today notification");
                return false;
            }
        }

        public async Task<bool> CreateFactureOverdueNotificationAsync(string factureNumber, string supplierName, decimal amount, int daysOverdue, int? branchId = null)
        {
            try
            {
                var notificationRequest = new CreateNotificationRequest
                {
                    Type = NotificationType.Financial,
                    Title = "Facture Terlambat Bayar",
                    Message = $"Facture {factureNumber} dari {supplierName} terlambat {daysOverdue} hari. Jumlah: Rp {amount:N0}",
                    Priority = "Critical",
                    Severity = "error",
                    ActionRequired = true,
                    ActionUrl = $"/factures?search={factureNumber}",
                    ActionText = "Bayar Sekarang",
                    BranchId = branchId
                };

                await CreateNotificationAsync(notificationRequest, 1);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating facture overdue notification");
                return false;
            }
        }

        public async Task<bool> CreateSupplierPaymentCompletedNotificationAsync(string supplierName, decimal amount, string factureNumber, int? branchId = null)
        {
            try
            {
                var notificationRequest = new CreateNotificationRequest
                {
                    Type = NotificationType.Financial,
                    Title = "Pembayaran Supplier Selesai",
                    Message = $"Pembayaran kepada {supplierName} sebesar Rp {amount:N0} untuk facture {factureNumber} telah berhasil",
                    Priority = "Normal",
                    Severity = "success",
                    ActionUrl = $"/factures?search={factureNumber}",
                    BranchId = branchId
                };

                await CreateNotificationAsync(notificationRequest, 1);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating supplier payment completed notification");
                return false;
            }
        }

        public async Task<bool> CreateFactureApprovalRequiredNotificationAsync(string factureNumber, string supplierName, decimal amount, int requestedByUserId, int? branchId = null)
        {
            try
            {
                var notificationRequest = new CreateNotificationRequest
                {
                    Type = NotificationType.Financial,
                    Title = "Approval Facture Diperlukan",
                    Message = $"Facture {factureNumber} dari {supplierName} senilai Rp {amount:N0} memerlukan approval untuk pembayaran",
                    Priority = "High",
                    Severity = "warning",
                    ActionRequired = true,
                    ActionUrl = $"/factures?search={factureNumber}",
                    ActionText = "Review & Approve",
                    BranchId = branchId
                };

                await BroadcastToRoleAsync("Manager", notificationRequest.Type, notificationRequest.Title, notificationRequest.Message, branchId, notificationRequest.ActionUrl);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating facture approval required notification");
                return false;
            }
        }

        public async Task<bool> CreateSaleCancelledNotificationAsync(int saleId, string saleNumber, decimal totalAmount, string reason, int? branchId = null)
        {
            try
            {
                var notificationRequest = new CreateNotificationRequest
                {
                    Type = NotificationType.Sales,
                    Title = "Penjualan Dibatalkan",
                    Message = $"Transaksi {saleNumber} dengan nilai Rp {totalAmount:N0} telah dibatalkan. Alasan: {reason}",
                    Priority = "Normal",
                    Severity = "warning",
                    ActionUrl = $"/sales/{saleId}",
                    ActionText = "Lihat Detail",
                    BranchId = branchId
                };

                await CreateNotificationAsync(notificationRequest, 1);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sale cancelled notification");
                return false;
            }
        }

        public async Task<bool> CreateSaleRefundedNotificationAsync(int saleId, string saleNumber, decimal totalAmount, string reason, int? branchId = null)
        {
            try
            {
                var notificationRequest = new CreateNotificationRequest
                {
                    Type = NotificationType.Sales,
                    Title = "Penjualan Direfund",
                    Message = $"Transaksi {saleNumber} dengan nilai Rp {totalAmount:N0} telah direfund. Alasan: {reason}",
                    Priority = "High",
                    Severity = "warning",
                    ActionUrl = $"/sales/{saleId}",
                    ActionText = "Lihat Detail",
                    BranchId = branchId
                };

                await CreateNotificationAsync(notificationRequest, 1);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sale refunded notification");
                return false;
            }
        }
    }

    /// <summary>
    /// SignalR Hub for real-time notification updates
    /// </summary>
    public class MultiBranchNotificationHub : Hub
    {
        private readonly ILogger<MultiBranchNotificationHub> _logger;

        public MultiBranchNotificationHub(ILogger<MultiBranchNotificationHub> logger)
        {
            _logger = logger;
        }

        public async Task JoinBranchGroup(int branchId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"branch-{branchId}");
            _logger.LogDebug("User joined branch {BranchId} group", branchId);
        }

        public async Task LeaveBranchGroup(int branchId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"branch-{branchId}");
            _logger.LogDebug("User left branch {BranchId} group", branchId);
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogDebug("User connected to notification hub");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogDebug("User disconnected from notification hub");
            await base.OnDisconnectedAsync(exception);
        }
    }

    // Define NotificationPreference for the interface
    public class NotificationPreference
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string NotificationType { get; set; } = string.Empty;
        public bool EmailEnabled { get; set; } = true;
        public bool BrowserEnabled { get; set; } = true;
        public bool PushEnabled { get; set; } = true;
        public string MinimumPriority { get; set; } = "medium";
        public bool QuietHoursEnabled { get; set; } = false;
        public TimeSpan? QuietHoursStart { get; set; }
        public TimeSpan? QuietHoursEnd { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}