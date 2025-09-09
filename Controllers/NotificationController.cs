using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Berca_Backend.DTOs;
using Berca_Backend.Services.Interfaces;
using System.Security.Claims;

namespace Berca_Backend.Controllers
{
    /// <summary>
    /// Unified notification controller with multi-branch support
    /// Provides comprehensive notification system for POS Toko Eniwan
    /// (Formerly MultiBranchNotificationController)
    /// </summary>
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly IMultiBranchNotificationService _notificationService;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(
            IMultiBranchNotificationService notificationService,
            ILogger<NotificationController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Get notifications with filtering and pagination
        /// </summary>
        /// <param name="filters">Filter parameters</param>
        /// <returns>Paginated list of notifications</returns>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PaginatedNotificationResponse>>> GetNotifications([FromQuery] NotificationFilters filters)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                _logger.LogInformation("Getting notifications for user {UserId} with filters", currentUserId);

                var result = await _notificationService.GetNotificationsAsync(filters, currentUserId);

                return Ok(new ApiResponse<PaginatedNotificationResponse>
                {
                    Success = true,
                    Data = result,
                    Message = $"Retrieved {result.Data.Count} notifications from {result.TotalCount} total",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications");
                return StatusCode(500, new ApiResponse<PaginatedNotificationResponse>
                {
                    Success = false,
                    Message = "Failed to retrieve notifications",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get notification by ID
        /// </summary>
        /// <param name="id">Notification ID</param>
        /// <returns>Notification details</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<NotificationDto>>> GetNotificationById(string id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                _logger.LogInformation("Getting notification {Id} for user {UserId}", id, currentUserId);

                var notification = await _notificationService.GetNotificationByIdAsync(id, currentUserId);

                if (notification == null)
                {
                    return NotFound(new ApiResponse<NotificationDto>
                    {
                        Success = false,
                        Message = "Notification not found or access denied"
                    });
                }

                return Ok(new ApiResponse<NotificationDto>
                {
                    Success = true,
                    Data = notification,
                    Message = "Notification retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification {Id}", id);
                return StatusCode(500, new ApiResponse<NotificationDto>
                {
                    Success = false,
                    Message = "Failed to retrieve notification",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Create new notification
        /// </summary>
        /// <param name="request">Notification creation request</param>
        /// <returns>Created notification</returns>
        [HttpPost]
        [Authorize(Policy = "Notification.Create")]
        public async Task<ActionResult<ApiResponse<NotificationDto>>> CreateNotification([FromBody] CreateNotificationRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                _logger.LogInformation("Creating notification for user {UserId}", currentUserId);

                // Validate request
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<NotificationDto>
                    {
                        Success = false,
                        Message = "Invalid request data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                    });
                }

                // Validate notification type and severity
                if (!IsValidNotificationType(request.Type))
                {
                    return BadRequest(new ApiResponse<NotificationDto>
                    {
                        Success = false,
                        Message = "Invalid notification type"
                    });
                }

                if (!IsValidNotificationSeverity(request.Severity))
                {
                    return BadRequest(new ApiResponse<NotificationDto>
                    {
                        Success = false,
                        Message = "Invalid notification severity"
                    });
                }

                if (!IsValidNotificationPriority(request.Priority))
                {
                    return BadRequest(new ApiResponse<NotificationDto>
                    {
                        Success = false,
                        Message = "Invalid notification priority"
                    });
                }

                // Check branch access if specified
                if (request.BranchId.HasValue)
                {
                    var canAccessBranch = await _notificationService.CanUserAccessBranchAsync(request.BranchId.Value, currentUserId);
                    if (!canAccessBranch)
                    {
                        return Forbid("Access denied to specified branch");
                    }
                }

                var notification = await _notificationService.CreateNotificationAsync(request, currentUserId);

                return CreatedAtAction(
                    nameof(GetNotificationById),
                    new { id = notification.Id },
                    new ApiResponse<NotificationDto>
                    {
                        Success = true,
                        Data = notification,
                        Message = "Notification created successfully"
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification");
                return StatusCode(500, new ApiResponse<NotificationDto>
                {
                    Success = false,
                    Message = "Failed to create notification",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Update existing notification
        /// </summary>
        /// <param name="id">Notification ID</param>
        /// <param name="request">Update request</param>
        /// <returns>Success status</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateNotification(string id, [FromBody] UpdateNotificationRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                _logger.LogInformation("Updating notification {Id} for user {UserId}", id, currentUserId);

                var success = await _notificationService.UpdateNotificationAsync(id, request, currentUserId);

                if (!success)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Notification not found or access denied"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Notification updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notification {Id}", id);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to update notification",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Mark notification as read
        /// </summary>
        /// <param name="id">Notification ID</param>
        /// <returns>Success status</returns>
        [HttpPut("{id}/mark-read")]
        public async Task<ActionResult<ApiResponse<bool>>> MarkAsRead(string id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                _logger.LogInformation("Marking notification {Id} as read for user {UserId}", id, currentUserId);

                var success = await _notificationService.MarkAsReadAsync(id, currentUserId);

                if (!success)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Notification not found or access denied"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Notification marked as read"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {Id} as read", id);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to mark notification as read",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Mark multiple notifications as read
        /// </summary>
        /// <param name="request">Bulk read request</param>
        /// <returns>Success status</returns>
        [HttpPut("mark-read-bulk")]
        public async Task<ActionResult<ApiResponse<bool>>> MarkBulkAsRead([FromBody] BulkNotificationRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                _logger.LogInformation("Marking {Count} notifications as read for user {UserId}", request.Ids.Count, currentUserId);

                if (!ModelState.IsValid || !request.Ids.Any())
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid request data"
                    });
                }

                var success = await _notificationService.MarkBulkAsReadAsync(request.Ids, currentUserId);

                return Ok(new ApiResponse<bool>
                {
                    Success = success,
                    Data = success,
                    Message = success 
                        ? $"{request.Ids.Count} notifications marked as read"
                        : "Failed to mark some notifications as read"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking bulk notifications as read");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to mark notifications as read",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Mark all notifications as read
        /// </summary>
        /// <param name="branchId">Optional branch ID filter</param>
        /// <returns>Success status</returns>
        [HttpPut("mark-all-read")]
        public async Task<ActionResult<ApiResponse<bool>>> MarkAllAsRead([FromQuery] int? branchId = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                _logger.LogInformation("Marking all notifications as read for user {UserId} in branch {BranchId}", currentUserId, branchId);

                // Check branch access if specified
                if (branchId.HasValue)
                {
                    var canAccessBranch = await _notificationService.CanUserAccessBranchAsync(branchId.Value, currentUserId);
                    if (!canAccessBranch)
                    {
                        return Forbid("Access denied to specified branch");
                    }
                }

                var success = await _notificationService.MarkAllAsReadAsync(branchId, currentUserId);

                return Ok(new ApiResponse<bool>
                {
                    Success = success,
                    Data = success,
                    Message = "All notifications marked as read"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to mark all notifications as read",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Archive notification
        /// </summary>
        /// <param name="id">Notification ID</param>
        /// <returns>Success status</returns>
        [HttpPut("{id}/archive")]
        public async Task<ActionResult<ApiResponse<bool>>> ArchiveNotification(string id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                _logger.LogInformation("Archiving notification {Id} for user {UserId}", id, currentUserId);

                var success = await _notificationService.ArchiveNotificationAsync(id, currentUserId);

                if (!success)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Notification not found or access denied"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Notification archived successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving notification {Id}", id);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to archive notification",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Delete notification
        /// </summary>
        /// <param name="id">Notification ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = "Notification.Delete")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteNotification(string id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                _logger.LogInformation("Deleting notification {Id} for user {UserId}", id, currentUserId);

                var success = await _notificationService.DeleteNotificationAsync(id, currentUserId);

                if (!success)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Notification not found or access denied"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Notification deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification {Id}", id);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to delete notification",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get notification statistics
        /// </summary>
        /// <param name="branchId">Optional branch ID filter</param>
        /// <param name="dateFrom">Start date filter</param>
        /// <param name="dateTo">End date filter</param>
        /// <returns>Notification statistics</returns>
        [HttpGet("stats")]
        public async Task<ActionResult<ApiResponse<NotificationStatsDto>>> GetNotificationStats(
            [FromQuery] int? branchId = null,
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                _logger.LogInformation("Getting notification stats for user {UserId}", currentUserId);

                // Check branch access if specified
                if (branchId.HasValue)
                {
                    var canAccessBranch = await _notificationService.CanUserAccessBranchAsync(branchId.Value, currentUserId);
                    if (!canAccessBranch)
                    {
                        return Forbid("Access denied to specified branch");
                    }
                }

                var stats = await _notificationService.GetNotificationStatsAsync(branchId, dateFrom, dateTo, currentUserId);

                return Ok(new ApiResponse<NotificationStatsDto>
                {
                    Success = true,
                    Data = stats,
                    Message = "Statistics retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification stats");
                return StatusCode(500, new ApiResponse<NotificationStatsDto>
                {
                    Success = false,
                    Message = "Failed to retrieve statistics",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Export notifications
        /// </summary>
        /// <param name="request">Export request parameters</param>
        /// <returns>Excel or CSV file</returns>
        [HttpGet("export")]
        public async Task<IActionResult> ExportNotifications([FromQuery] ExportNotificationRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                _logger.LogInformation("Exporting notifications for user {UserId}", currentUserId);

                // Check branch access if specified
                if (request.BranchId.HasValue)
                {
                    var canAccessBranch = await _notificationService.CanUserAccessBranchAsync(request.BranchId.Value, currentUserId);
                    if (!canAccessBranch)
                    {
                        return Forbid("Access denied to specified branch");
                    }
                }

                var fileBytes = await _notificationService.ExportNotificationsAsync(request, currentUserId);
                var fileName = $"notifications_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{request.Format}";
                var contentType = request.Format.ToLower() == "csv" 
                    ? "text/csv" 
                    : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting notifications");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to export notifications",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get system health
        /// </summary>
        /// <returns>System health metrics</returns>
        [HttpGet("health")]
        public async Task<ActionResult<ApiResponse<NotificationHealthDto>>> GetSystemHealth()
        {
            try
            {
                _logger.LogInformation("Getting notification system health");

                var health = await _notificationService.GetSystemHealthAsync();

                return Ok(new ApiResponse<DTOs.NotificationHealthDto>
                {
                    Success = true,
                    Data = health,
                    Message = $"System is {health.Status}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system health");
                return StatusCode(500, new ApiResponse<NotificationHealthDto>
                {
                    Success = false,
                    Message = "Failed to get system health",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get unread notification count
        /// </summary>
        /// <param name="branchId">Optional branch ID filter</param>
        /// <returns>Unread count</returns>
        [HttpGet("unread-count")]
        public async Task<ActionResult<ApiResponse<int>>> GetUnreadCount([FromQuery] int? branchId = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                // Check branch access if specified
                if (branchId.HasValue)
                {
                    var canAccessBranch = await _notificationService.CanUserAccessBranchAsync(branchId.Value, currentUserId);
                    if (!canAccessBranch)
                    {
                        return Forbid("Access denied to specified branch");
                    }
                }

                var count = await _notificationService.GetUnreadCountAsync(currentUserId, branchId);

                return Ok(new ApiResponse<int>
                {
                    Success = true,
                    Data = count,
                    Message = $"{count} unread notifications"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count");
                return StatusCode(500, new ApiResponse<int>
                {
                    Success = false,
                    Message = "Failed to get unread count",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get recent notifications for real-time updates
        /// </summary>
        /// <param name="branchId">Optional branch ID filter</param>
        /// <param name="limit">Maximum number of notifications to return</param>
        /// <returns>Recent notifications</returns>
        [HttpGet("recent")]
        public async Task<ActionResult<ApiResponse<List<NotificationDto>>>> GetRecentNotifications(
            [FromQuery] int? branchId = null,
            [FromQuery] int limit = 10)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                // Check branch access if specified
                if (branchId.HasValue)
                {
                    var canAccessBranch = await _notificationService.CanUserAccessBranchAsync(branchId.Value, currentUserId);
                    if (!canAccessBranch)
                    {
                        return Forbid("Access denied to specified branch");
                    }
                }

                // Limit the maximum number of notifications
                limit = Math.Min(limit, 50);

                var notifications = await _notificationService.GetRecentNotificationsAsync(currentUserId, branchId, limit);

                return Ok(new ApiResponse<List<NotificationDto>>
                {
                    Success = true,
                    Data = notifications,
                    Message = $"Retrieved {notifications.Count} recent notifications"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent notifications");
                return StatusCode(500, new ApiResponse<List<NotificationDto>>
                {
                    Success = false,
                    Message = "Failed to get recent notifications",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // === HELPER METHODS ===

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private static bool IsValidNotificationType(string type)
        {
            var validTypes = new[]
            {
                NotificationType.System,
                NotificationType.Transfer,
                NotificationType.Alert,
                NotificationType.User,
                NotificationType.Branch,
                NotificationType.Coordination,
                NotificationType.Inventory,
                NotificationType.Sales,
                NotificationType.Financial,
                NotificationType.Maintenance
            };

            return validTypes.Contains(type);
        }

        private static bool IsValidNotificationSeverity(string severity)
        {
            var validSeverities = new[]
            {
                NotificationSeverity.Info,
                NotificationSeverity.Warning,
                NotificationSeverity.Error,
                NotificationSeverity.Success
            };

            return validSeverities.Contains(severity);
        }

        private static bool IsValidNotificationPriority(string priority)
        {
            return Enum.TryParse<Models.NotificationPriority>(priority, true, out _);
        }
    }
}