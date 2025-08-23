using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Berca_Backend.Services;
using Berca_Backend.DTOs;
using System.Security.Claims;

namespace Berca_Backend.Controllers
{
    /// <summary>
    /// Controller for Smart Notification Engine - Intelligent alerts and escalation management
    /// Provides context-aware notifications with advanced business rules
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SmartNotificationController : ControllerBase
    {
        private readonly ISmartNotificationEngineService _smartNotificationService;
        private readonly ILogger<SmartNotificationController> _logger;

        public SmartNotificationController(
            ISmartNotificationEngineService smartNotificationService,
            ILogger<SmartNotificationController> logger)
        {
            _smartNotificationService = smartNotificationService;
            _logger = logger;
        }

        /// <summary>
        /// Generate intelligent notifications with business context
        /// </summary>
        /// <param name="branchId">Optional branch ID filter</param>
        /// <returns>List of smart notifications with escalation rules</returns>
        [HttpGet("intelligent")]
        [Authorize(Policy = "Expiry.Notifications")]
        public async Task<ActionResult<ApiResponse<List<SmartNotificationDto>>>> GetIntelligentNotifications(
            [FromQuery] int? branchId = null)
        {
            try
            {
                _logger.LogInformation("Generating intelligent notifications for branch {BranchId}", branchId);

                var notifications = await _smartNotificationService.GenerateIntelligentNotificationsAsync(branchId);

                _logger.LogInformation("Generated {Count} intelligent notifications", notifications.Count);

                return Ok(new ApiResponse<List<SmartNotificationDto>>
                {
                    Success = true,
                    Data = notifications,
                    Message = $"Generated {notifications.Count} intelligent notifications"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating intelligent notifications for branch {BranchId}", branchId);
                return StatusCode(500, new ApiResponse<List<SmartNotificationDto>>
                {
                    Success = false,
                    Message = "Failed to generate intelligent notifications",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Process all active notification rules
        /// </summary>
        /// <returns>Success status of rule processing</returns>
        [HttpPost("process-rules")]
        [Authorize(Policy = "Expiry.BackgroundTasks")]
        public async Task<ActionResult<ApiResponse<bool>>> ProcessNotificationRules()
        {
            try
            {
                _logger.LogInformation("Processing notification rules");

                var success = await _smartNotificationService.ProcessNotificationRulesAsync();

                return Ok(new ApiResponse<bool>
                {
                    Success = success,
                    Data = success,
                    Message = success ? "Notification rules processed successfully" : "Failed to process some notification rules"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing notification rules");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to process notification rules",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Process escalation alerts for overdue notifications
        /// </summary>
        /// <returns>List of escalation alerts created</returns>
        [HttpPost("escalations")]
        [Authorize(Policy = "Expiry.BackgroundTasks")]
        public async Task<ActionResult<ApiResponse<List<EscalationAlert>>>> ProcessEscalationAlerts()
        {
            try
            {
                _logger.LogInformation("Processing escalation alerts");

                var escalations = await _smartNotificationService.ProcessEscalationAlertsAsync();

                _logger.LogInformation("Created {Count} escalation alerts", escalations.Count);

                return Ok(new ApiResponse<List<EscalationAlert>>
                {
                    Success = true,
                    Data = escalations,
                    Message = $"Created {escalations.Count} escalation alerts"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing escalation alerts");
                return StatusCode(500, new ApiResponse<List<EscalationAlert>>
                {
                    Success = false,
                    Message = "Failed to process escalation alerts",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get user notification preferences
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>User notification preferences</returns>
        [HttpGet("preferences/{userId}")]
        [Authorize(Policy = "Expiry.Notifications")]
        public async Task<ActionResult<ApiResponse<NotificationPreferencesDto>>> GetNotificationPreferences(int userId)
        {
            try
            {
                // Validate user access
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                
                if (currentUserId != userId && !new[] { "Admin", "HeadManager", "BranchManager" }.Contains(userRole))
                {
                    return Forbid("You can only access your own notification preferences");
                }

                var preferences = await _smartNotificationService.GetUserNotificationPreferencesAsync(userId);

                return Ok(new ApiResponse<NotificationPreferencesDto>
                {
                    Success = true,
                    Data = preferences,
                    Message = "Notification preferences retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification preferences for user {UserId}", userId);
                return StatusCode(500, new ApiResponse<NotificationPreferencesDto>
                {
                    Success = false,
                    Message = "Failed to get notification preferences",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Send critical expiry alerts immediately
        /// </summary>
        /// <returns>Success status of alert sending</returns>
        [HttpPost("critical-expiry-alerts")]
        [Authorize(Policy = "Expiry.BackgroundTasks")]
        public async Task<ActionResult<ApiResponse<bool>>> SendCriticalExpiryAlerts()
        {
            try
            {
                _logger.LogInformation("Sending critical expiry alerts");

                var success = await _smartNotificationService.SendCriticalExpiryAlertsAsync();

                return Ok(new ApiResponse<bool>
                {
                    Success = success,
                    Data = success,
                    Message = success ? "Critical expiry alerts sent successfully" : "Failed to send some critical expiry alerts"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending critical expiry alerts");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to send critical expiry alerts",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get summary of notification system health
        /// </summary>
        /// <returns>Notification system health metrics</returns>
        [HttpGet("health")]
        [Authorize(Policy = "Expiry.Analytics")]
        public async Task<ActionResult<ApiResponse<NotificationHealthDto>>> GetNotificationHealth()
        {
            try
            {
                _logger.LogInformation("Getting notification system health");

                // Generate intelligent notifications to get current state
                var notifications = await _smartNotificationService.GenerateIntelligentNotificationsAsync();
                
                var health = new NotificationHealthDto
                {
                    TotalActiveNotifications = notifications.Count,
                    CriticalNotifications = notifications.Count(n => n.Priority == Models.NotificationPriority.Critical),
                    HighNotifications = notifications.Count(n => n.Priority == Models.NotificationPriority.High),
                    MediumNotifications = notifications.Count(n => n.Priority == Models.NotificationPriority.Normal),
                    LowNotifications = notifications.Count(n => n.Priority == Models.NotificationPriority.Low),
                    TotalValueAtRisk = notifications.Sum(n => n.PotentialLoss),
                    OverdueNotifications = notifications.Count(n => n.ActionDeadline < DateTime.UtcNow),
                    CheckTimestamp = DateTime.UtcNow
                };

                return Ok(new ApiResponse<NotificationHealthDto>
                {
                    Success = true,
                    Data = health,
                    Message = "Notification system health retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification system health");
                return StatusCode(500, new ApiResponse<NotificationHealthDto>
                {
                    Success = false,
                    Message = "Failed to get notification system health",
                    Error = ex.Message
                });
            }
        }
    }

    /// <summary>
    /// DTO for notification system health metrics
    /// </summary>
    public class NotificationHealthDto
    {
        public int TotalActiveNotifications { get; set; }
        public int CriticalNotifications { get; set; }
        public int HighNotifications { get; set; }
        public int MediumNotifications { get; set; }
        public int LowNotifications { get; set; }
        public decimal TotalValueAtRisk { get; set; }
        public int OverdueNotifications { get; set; }
        public DateTime CheckTimestamp { get; set; }
    }
}