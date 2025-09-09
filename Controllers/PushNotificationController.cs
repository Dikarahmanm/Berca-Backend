using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Berca_Backend.Services.Interfaces;
using Berca_Backend.DTOs;
using Berca_Backend.Models;
using System.Security.Claims;

namespace Berca_Backend.Controllers
{
    /// <summary>
    /// Controller for managing push notifications in PWA
    /// Handles subscription management and notification delivery
    /// Indonesian business context with proper authorization
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PushNotificationController : ControllerBase
    {
        private readonly IPushNotificationService _pushNotificationService;
        private readonly ILogger<PushNotificationController> _logger;

        public PushNotificationController(
            IPushNotificationService pushNotificationService,
            ILogger<PushNotificationController> logger)
        {
            _pushNotificationService = pushNotificationService;
            _logger = logger;
        }

        // ==================== SUBSCRIPTION MANAGEMENT ==================== //

        /// <summary>
        /// Subscribe current user to push notifications
        /// </summary>
        /// <param name="subscription">Push subscription data from browser</param>
        /// <returns>Success status</returns>
        [HttpPost("subscribe")]
        public async Task<IActionResult> SubscribeUser([FromBody] PushSubscriptionDto subscription)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized("User ID not found in token");
                }

                var success = await _pushNotificationService.SubscribeUserAsync(userId.Value, subscription);
                
                if (success)
                {
                    _logger.LogInformation("User {UserId} subscribed to push notifications successfully", userId);
                    return Ok(new { success = true, message = "Berhasil berlangganan notifikasi push" });
                }

                return BadRequest(new { success = false, message = "Gagal berlangganan notifikasi push" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing user to push notifications");
                return StatusCode(500, new { success = false, message = "Terjadi kesalahan sistem" });
            }
        }

        /// <summary>
        /// Unsubscribe current user from push notifications
        /// </summary>
        /// <param name="deviceId">Optional device ID for multi-device support</param>
        /// <returns>Success status</returns>
        [HttpDelete("unsubscribe")]
        public async Task<IActionResult> UnsubscribeUser([FromQuery] string? deviceId = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized("User ID not found in token");
                }

                var success = await _pushNotificationService.UnsubscribeUserAsync(userId.Value, deviceId);
                
                if (success)
                {
                    _logger.LogInformation("User {UserId} unsubscribed from push notifications", userId);
                    return Ok(new { success = true, message = "Berhasil berhenti berlangganan notifikasi push" });
                }

                return BadRequest(new { success = false, message = "Gagal berhenti berlangganan notifikasi push" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsubscribing user from push notifications");
                return StatusCode(500, new { success = false, message = "Terjadi kesalahan sistem" });
            }
        }

        /// <summary>
        /// Get current user's push subscription status
        /// </summary>
        /// <param name="deviceId">Optional device ID filter</param>
        /// <returns>Subscription status information</returns>
        [HttpGet("subscription")]
        public async Task<IActionResult> GetSubscriptionStatus([FromQuery] string? deviceId = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized("User ID not found in token");
                }

                var status = await _pushNotificationService.GetUserSubscriptionAsync(userId.Value, deviceId);
                
                if (status == null)
                {
                    return Ok(new { isSubscribed = false, message = "Status langganan tidak ditemukan" });
                }

                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscription status for user");
                return StatusCode(500, new { success = false, message = "Terjadi kesalahan sistem" });
            }
        }

        /// <summary>
        /// Update current user's push subscription
        /// </summary>
        /// <param name="subscription">Updated subscription data</param>
        /// <returns>Success status</returns>
        [HttpPut("subscription")]
        public async Task<IActionResult> UpdateSubscription([FromBody] PushSubscriptionDto subscription)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized("User ID not found in token");
                }

                var success = await _pushNotificationService.UpdateSubscriptionAsync(userId.Value, subscription);
                
                if (success)
                {
                    _logger.LogInformation("User {UserId} updated push subscription successfully", userId);
                    return Ok(new { success = true, message = "Berhasil memperbarui langganan notifikasi" });
                }

                return BadRequest(new { success = false, message = "Gagal memperbarui langganan notifikasi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user subscription");
                return StatusCode(500, new { success = false, message = "Terjadi kesalahan sistem" });
            }
        }

        // ==================== NOTIFICATION SENDING ==================== //

        /// <summary>
        /// Send push notification to specific user (Admin/Manager only)
        /// </summary>
        /// <param name="userId">Target user ID</param>
        /// <param name="payload">Notification payload</param>
        /// <returns>Delivery result</returns>
        [HttpPost("send/{userId}")]
        [Authorize(Policy = "Admin.Manage")]
        public async Task<IActionResult> SendToUser(int userId, [FromBody] NotificationPayload payload)
        {
            try
            {
                var result = await _pushNotificationService.SendNotificationAsync(userId, payload);
                
                if (result.Success)
                {
                    _logger.LogInformation("Push notification sent to user {UserId} successfully", userId);
                    return Ok(new 
                    { 
                        success = true, 
                        message = "Notifikasi berhasil dikirim",
                        delivered = result.SuccessCount,
                        failed = result.FailureCount
                    });
                }

                return BadRequest(new 
                { 
                    success = false, 
                    message = "Gagal mengirim notifikasi",
                    errors = result.Errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending push notification to user {UserId}", userId);
                return StatusCode(500, new { success = false, message = "Terjadi kesalahan sistem" });
            }
        }

        /// <summary>
        /// Send bulk push notifications (Admin/Manager only)
        /// </summary>
        /// <param name="request">Bulk notification request</param>
        /// <returns>Delivery result</returns>
        [HttpPost("send-bulk")]
        [Authorize(Policy = "Admin.Manage")]
        public async Task<IActionResult> SendBulkNotifications([FromBody] BulkPushNotificationRequest request)
        {
            try
            {
                PushNotificationResult result;

                if (request.UserIds?.Any() == true)
                {
                    // Send to specific users
                    result = await _pushNotificationService.SendBulkNotificationAsync(request.UserIds, request.Payload);
                }
                else if (request.Roles?.Any() == true)
                {
                    // Send to users with specific roles
                    result = await _pushNotificationService.SendToRolesAsync(request.Roles, request.Payload, request.BranchId);
                }
                else
                {
                    return BadRequest(new { success = false, message = "Target pengguna atau role harus ditentukan" });
                }

                if (result.Success)
                {
                    _logger.LogInformation("Bulk push notification sent: {Success}/{Total} successful", 
                        result.SuccessCount, result.TotalSent);
                    
                    return Ok(new 
                    { 
                        success = true, 
                        message = $"Notifikasi berhasil dikirim ke {result.SuccessCount} dari {result.TotalSent} pengguna",
                        delivered = result.SuccessCount,
                        failed = result.FailureCount,
                        errors = result.Errors.Take(5).ToList() // Limit error details
                    });
                }

                return BadRequest(new 
                { 
                    success = false, 
                    message = "Gagal mengirim notifikasi bulk",
                    delivered = result.SuccessCount,
                    failed = result.FailureCount,
                    errors = result.Errors.Take(10).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending bulk push notifications");
                return StatusCode(500, new { success = false, message = "Terjadi kesalahan sistem" });
            }
        }

        /// <summary>
        /// Send notification to users with specific roles (Manager+ only)
        /// </summary>
        /// <param name="roles">Target user roles</param>
        /// <param name="payload">Notification payload</param>
        /// <param name="branchId">Optional branch filter</param>
        /// <returns>Delivery result</returns>
        [HttpPost("send-to-roles")]
        [Authorize(Policy = "Manager.Manage")]
        public async Task<IActionResult> SendToRoles(
            [FromBody] List<string> roles, 
            [FromQuery] NotificationPayload payload,
            [FromQuery] int? branchId = null)
        {
            try
            {
                if (roles == null || !roles.Any())
                {
                    return BadRequest(new { success = false, message = "Role target harus ditentukan" });
                }

                var currentUserRole = GetCurrentUserRole();
                var currentUserBranchId = GetCurrentUserBranchId();

                // Validate permissions based on role hierarchy
                if (currentUserRole == "Manager" && roles.Contains("Admin"))
                {
                    return Forbid("Manager tidak dapat mengirim notifikasi ke Admin");
                }

                // If user is not Admin, restrict to their branch
                if (currentUserRole != "Admin" && branchId != currentUserBranchId)
                {
                    branchId = currentUserBranchId;
                }

                var result = await _pushNotificationService.SendToRolesAsync(roles ?? new List<string>(), payload, branchId);
                
                if (result.Success)
                {
                    _logger.LogInformation("Role-based push notification sent: {Success}/{Total} successful to roles {Roles}", 
                        result.SuccessCount, result.TotalSent, string.Join(", ", roles ?? new List<string>()));
                    
                    return Ok(new 
                    { 
                        success = true, 
                        message = $"Notifikasi berhasil dikirim ke {result.SuccessCount} pengguna dengan role {string.Join(", ", roles ?? new List<string>())}",
                        delivered = result.SuccessCount,
                        failed = result.FailureCount
                    });
                }

                return BadRequest(new 
                { 
                    success = false, 
                    message = "Gagal mengirim notifikasi berdasarkan role",
                    errors = result.Errors.Take(5).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending role-based push notifications");
                return StatusCode(500, new { success = false, message = "Terjadi kesalahan sistem" });
            }
        }

        // ==================== TESTING & UTILITIES ==================== //

        /// <summary>
        /// Test push notification service for current user
        /// </summary>
        /// <returns>Test result</returns>
        [HttpPost("test")]
        public async Task<IActionResult> TestPushService()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized("User ID not found in token");
                }

                var success = await _pushNotificationService.TestPushServiceAsync(userId.Value);
                
                if (success)
                {
                    return Ok(new { success = true, message = "Test notifikasi berhasil dikirim" });
                }

                return BadRequest(new { success = false, message = "Test notifikasi gagal - pastikan Anda sudah berlangganan" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing push service");
                return StatusCode(500, new { success = false, message = "Terjadi kesalahan sistem" });
            }
        }

        /// <summary>
        /// Get supported push services information
        /// </summary>
        /// <returns>List of supported services</returns>
        [HttpGet("supported-services")]
        [AllowAnonymous]
        public IActionResult GetSupportedServices()
        {
            try
            {
                var services = _pushNotificationService.GetSupportedPushServices();
                return Ok(new 
                { 
                    success = true, 
                    supportedServices = services,
                    message = "Layanan push notification yang didukung"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting supported services");
                return StatusCode(500, new { success = false, message = "Terjadi kesalahan sistem" });
            }
        }

        /// <summary>
        /// Validate push subscription data
        /// </summary>
        /// <param name="subscription">Subscription to validate</param>
        /// <returns>Validation result</returns>
        [HttpPost("validate-subscription")]
        public IActionResult ValidateSubscription([FromBody] PushSubscriptionDto subscription)
        {
            try
            {
                var isValid = _pushNotificationService.ValidateSubscription(subscription);
                
                return Ok(new 
                { 
                    success = true,
                    isValid = isValid,
                    message = isValid ? "Langganan valid" : "Langganan tidak valid"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating subscription");
                return StatusCode(500, new { success = false, message = "Terjadi kesalahan sistem" });
            }
        }

        // ==================== BUSINESS NOTIFICATIONS ==================== //

        /// <summary>
        /// Send product expiry notification (Manager+ only)
        /// </summary>
        [HttpPost("business/product-expiry")]
        [Authorize(Policy = "Manager.Manage")]
        public async Task<IActionResult> SendProductExpiryNotification(
            [FromQuery] int productId, 
            [FromQuery] DateTime expiryDate, 
            [FromQuery] int daysUntilExpiry,
            [FromQuery] int? branchId = null)
        {
            try
            {
                var result = await _pushNotificationService.SendProductExpiryNotificationAsync(
                    productId, expiryDate, daysUntilExpiry, branchId);
                
                return Ok(new 
                { 
                    success = result.Success, 
                    message = result.Success ? "Notifikasi kadaluarsa produk berhasil dikirim" : "Gagal mengirim notifikasi",
                    delivered = result.SuccessCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending product expiry notification");
                return StatusCode(500, new { success = false, message = "Terjadi kesalahan sistem" });
            }
        }

        /// <summary>
        /// Send facture due notification (Manager+ only)
        /// </summary>
        [HttpPost("business/facture-due")]
        [Authorize(Policy = "Manager.Manage")]
        public async Task<IActionResult> SendFactureDueNotification(
            [FromQuery] int factureId, 
            [FromQuery] DateTime dueDate, 
            [FromQuery] decimal amount,
            [FromQuery] string supplierName,
            [FromQuery] int? branchId = null)
        {
            try
            {
                var result = await _pushNotificationService.SendFactureDueNotificationAsync(
                    factureId, dueDate, amount, supplierName, branchId);
                
                return Ok(new 
                { 
                    success = result.Success, 
                    message = result.Success ? "Notifikasi jatuh tempo faktur berhasil dikirim" : "Gagal mengirim notifikasi",
                    delivered = result.SuccessCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending facture due notification");
                return StatusCode(500, new { success = false, message = "Terjadi kesalahan sistem" });
            }
        }

        // ==================== HELPER METHODS ==================== //

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        private string GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
        }

        private int? GetCurrentUserBranchId()
        {
            var branchIdClaim = User.FindFirst("BranchId")?.Value;
            return int.TryParse(branchIdClaim, out var branchId) ? branchId : null;
        }
    }
}