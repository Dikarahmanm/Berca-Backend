// Controllers/NotificationController.cs - Sprint 2 Notification Controller Implementation
using Berca_Backend.DTOs;
using Berca_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Berca_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(INotificationService notificationService, ILogger<NotificationController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Get user notifications with filtering and pagination
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<NotificationDto>>>> GetUserNotifications(
            [FromQuery] bool? isRead = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new ApiResponse<List<NotificationDto>>
                    {
                        Success = false,
                        Message = "Invalid user authentication"
                    });
                }

                if (pageSize > 100) pageSize = 100; // Limit page size

                var notifications = await _notificationService.GetUserNotificationsAsync(userId, isRead, page, pageSize);

                return Ok(new ApiResponse<List<NotificationDto>>
                {
                    Success = true,
                    Data = notifications,
                    Message = $"Retrieved {notifications.Count} notifications"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user notifications");
                return StatusCode(500, new ApiResponse<List<NotificationDto>>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get notification summary (unread count, recent notifications)
        /// </summary>
        [HttpGet("summary")]
        public async Task<ActionResult<ApiResponse<NotificationSummaryDto>>> GetNotificationSummary()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new ApiResponse<NotificationSummaryDto>
                    {
                        Success = false,
                        Message = "Invalid user authentication"
                    });
                }

                var summary = await _notificationService.GetNotificationSummaryAsync(userId);

                return Ok(new ApiResponse<NotificationSummaryDto>
                {
                    Success = true,
                    Data = summary,
                    Message = "Notification summary retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification summary");
                return StatusCode(500, new ApiResponse<NotificationSummaryDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Create a new notification
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "Notifications.Write")]
        public async Task<ActionResult<ApiResponse<NotificationDto>>> CreateNotification([FromBody] CreateNotificationRequest request)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

                var notification = await _notificationService.CreateNotificationAsync(request, username);

                return CreatedAtAction(nameof(GetUserNotifications), new ApiResponse<NotificationDto>
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
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Mark notification as read
        /// </summary>
        [HttpPost("{id}/read")]
        public async Task<ActionResult<ApiResponse<bool>>> MarkAsRead(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid user authentication"
                    });
                }

                var result = await _notificationService.MarkAsReadAsync(id, userId);
                if (!result)
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
                _logger.LogError(ex, "Error marking notification as read: {NotificationId}", id);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Mark all notifications as read
        /// </summary>
        [HttpPost("read-all")]
        public async Task<ActionResult<ApiResponse<bool>>> MarkAllAsRead()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid user authentication"
                    });
                }

                var result = await _notificationService.MarkAllAsReadAsync(userId);

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = result,
                    Message = "All notifications marked as read"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Delete a notification
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteNotification(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid user authentication"
                    });
                }

                var result = await _notificationService.DeleteNotificationAsync(id, userId);
                if (!result)
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
                _logger.LogError(ex, "Error deleting notification: {NotificationId}", id);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Broadcast notification to all users
        /// </summary>
        [HttpPost("broadcast")]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult<ApiResponse<bool>>> BroadcastToAllUsers([FromBody] BroadcastNotificationRequest request)
        {
            try
            {
                var result = await _notificationService.BroadcastToAllUsersAsync(
                    request.Type,
                    request.Title,
                    request.Message,
                    request.ActionUrl);

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = result,
                    Message = "Notification broadcasted to all users"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting notification to all users");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Broadcast notification to specific role
        /// </summary>
        [HttpPost("broadcast/role")]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult<ApiResponse<bool>>> BroadcastToRole([FromBody] BroadcastToRoleRequest request)
        {
            try
            {
                var result = await _notificationService.BroadcastToRoleAsync(
                    request.Role,
                    request.Type,
                    request.Title,
                    request.Message,
                    request.ActionUrl);

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = result,
                    Message = $"Notification broadcasted to {request.Role} users"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting notification to role: {Role}", request.Role);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get user notification settings
        /// </summary>
        [HttpGet("settings")]
        public async Task<ActionResult<ApiResponse<NotificationSettingsDto>>> GetUserSettings()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new ApiResponse<NotificationSettingsDto>
                    {
                        Success = false,
                        Message = "Invalid user authentication"
                    });
                }

                var settings = await _notificationService.GetUserSettingsAsync(userId);

                return Ok(new ApiResponse<NotificationSettingsDto>
                {
                    Success = true,
                    Data = settings,
                    Message = "Notification settings retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user notification settings");
                return StatusCode(500, new ApiResponse<NotificationSettingsDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Update user notification settings
        /// </summary>
        [HttpPut("settings")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateUserSettings([FromBody] UpdateNotificationSettingsRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid user authentication"
                    });
                }

                var result = await _notificationService.UpdateUserSettingsAsync(userId, request);

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = result,
                    Message = "Notification settings updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user notification settings");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Trigger system notifications (for testing)
        /// </summary>
        [HttpPost("system/low-stock/{productId}")]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult<ApiResponse<bool>>> TriggerLowStockNotification(int productId, [FromQuery] int currentStock)
        {
            try
            {
                var result = await _notificationService.CreateLowStockNotificationAsync(productId, currentStock);

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = result,
                    Message = "Low stock notification created"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating low stock notification");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Trigger monthly revenue notification
        /// </summary>
        [HttpPost("system/monthly-revenue")]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult<ApiResponse<bool>>> TriggerMonthlyRevenueNotification([FromBody] MonthlyRevenueNotificationRequest request)
        {
            try
            {
                var result = await _notificationService.CreateMonthlyRevenueNotificationAsync(request.Revenue, request.Month);

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = result,
                    Message = "Monthly revenue notification created"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating monthly revenue notification");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Trigger inventory audit notification
        /// </summary>
        [HttpPost("system/inventory-audit")]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult<ApiResponse<bool>>> TriggerInventoryAuditNotification()
        {
            try
            {
                var result = await _notificationService.CreateInventoryAuditNotificationAsync();

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = result,
                    Message = "Inventory audit notification created"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating inventory audit notification");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Clean up expired notifications
        /// </summary>
        [HttpPost("cleanup/expired")]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult<ApiResponse<int>>> CleanupExpiredNotifications()
        {
            try
            {
                var count = await _notificationService.CleanupExpiredNotificationsAsync();

                return Ok(new ApiResponse<int>
                {
                    Success = true,
                    Data = count,
                    Message = $"Cleaned up {count} expired notifications"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired notifications");
                return StatusCode(500, new ApiResponse<int>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }
    }

    // Request DTOs
    public class BroadcastNotificationRequest
    {
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? ActionUrl { get; set; }
    }

    public class BroadcastToRoleRequest : BroadcastNotificationRequest
    {
        public string Role { get; set; } = string.Empty;
    }

    public class MonthlyRevenueNotificationRequest
    {
        public decimal Revenue { get; set; }
        public DateTime Month { get; set; }
    }
}