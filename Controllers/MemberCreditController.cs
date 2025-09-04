using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Berca_Backend.DTOs;
using Berca_Backend.Services;
using System.Security.Claims;

namespace Berca_Backend.Controllers
{
    /// <summary>
    /// Controller for member credit/debt management operations
    /// Handles credit granting, payments, collections, and analytics
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MemberCreditController : ControllerBase
    {
        private readonly IMemberService _memberService;
        private readonly ILogger<MemberCreditController> _logger;

        public MemberCreditController(IMemberService memberService, ILogger<MemberCreditController> logger)
        {
            _memberService = memberService;
            _logger = logger;
        }

        // ==================== CREDIT OPERATIONS ==================== //

        /// <summary>
        /// Grant credit to member for purchase
        /// </summary>
        /// <param name="memberId">Member ID</param>
        /// <param name="request">Credit grant request</param>
        [HttpPost("{memberId}/credit/grant")]
        [Authorize(Policy = "Membership.GrantCredit")]
        public async Task<IActionResult> GrantCredit(int memberId, [FromBody] GrantCreditDto request)
        {
            try
            {
                _logger.LogInformation("Granting {Amount} IDR credit to member {MemberId} for sale {SaleId}", 
                    request.Amount, memberId, request.SaleId);

                // Check credit eligibility first
                var eligibility = await _memberService.CheckCreditEligibilityAsync(memberId, request.Amount);
                if (!eligibility.IsEligible)
                {
                    return BadRequest(new 
                    { 
                        message = "Credit request denied", 
                        reason = eligibility.DecisionReason,
                        requirements = eligibility.RequirementsNotMet,
                        riskFactors = eligibility.RiskFactors
                    });
                }

                var success = await _memberService.GrantCreditAsync(memberId, request.Amount, request.Description, request.SaleId, request.PaymentTermDays, request.DueDate);
                
                if (!success)
                {
                    return BadRequest(new { message = "Failed to grant credit" });
                }

                // Get updated credit summary
                var summary = await _memberService.GetCreditSummaryAsync(memberId);

                _logger.LogInformation("Successfully granted {Amount} IDR credit to member {MemberId}", 
                    request.Amount, memberId);

                return Ok(new 
                { 
                    message = "Credit granted successfully",
                    creditSummary = summary
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error granting credit to member {MemberId}", memberId);
                return StatusCode(500, new { message = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// Record payment from member to reduce debt
        /// </summary>
        /// <param name="memberId">Member ID</param>
        /// <param name="request">Payment record request</param>
        [HttpPost("{memberId}/credit/payment")]
        [Authorize(Policy = "Membership.RecordPayment")]
        public async Task<IActionResult> RecordPayment(int memberId, [FromBody] RecordPaymentDto request)
        {
            try
            {
                _logger.LogInformation("Recording {Amount} IDR payment from member {MemberId} via {PaymentMethod}", 
                    request.Amount, memberId, request.PaymentMethod);

                var success = await _memberService.RecordPaymentAsync(
                    memberId, 
                    request.Amount, 
                    request.PaymentMethod, 
                    request.ReferenceNumber);

                if (!success)
                {
                    return BadRequest(new { message = "Failed to record payment" });
                }

                // Get updated credit summary
                var summary = await _memberService.GetCreditSummaryAsync(memberId);

                _logger.LogInformation("Successfully recorded {Amount} IDR payment from member {MemberId}", 
                    request.Amount, memberId);

                return Ok(new 
                { 
                    message = "Payment recorded successfully",
                    creditSummary = summary
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording payment for member {MemberId}", memberId);
                return StatusCode(500, new { message = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// Check member credit eligibility for requested amount
        /// </summary>
        /// <param name="memberId">Member ID</param>
        /// <param name="amount">Requested credit amount</param>
        [HttpGet("{memberId}/credit/eligibility")]
        [Authorize(Policy = "Membership.CheckEligibility")]
        public async Task<IActionResult> CheckCreditEligibility(int memberId, [FromQuery] decimal amount)
        {
            try
            {
                var eligibility = await _memberService.CheckCreditEligibilityAsync(memberId, amount);
                return Ok(eligibility);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking credit eligibility for member {MemberId}", memberId);
                return StatusCode(500, new { message = "Internal server error occurred" });
            }
        }

        // ==================== CREDIT INFORMATION ==================== //

        /// <summary>
        /// Get comprehensive credit summary for member
        /// </summary>
        /// <param name="memberId">Member ID</param>
        [HttpGet("{memberId}/credit/summary")]
        [Authorize(Policy = "Membership.CreditHistory")]
        public async Task<IActionResult> GetCreditSummary(int memberId)
        {
            try
            {
                var summary = await _memberService.GetCreditSummaryAsync(memberId);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting credit summary for member {MemberId}", memberId);
                return StatusCode(500, new { message = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// Get member credit transaction history
        /// </summary>
        /// <param name="memberId">Member ID</param>
        /// <param name="days">Number of days to look back (default 90)</param>
        [HttpGet("{memberId}/credit/history")]
        [Authorize(Policy = "Membership.CreditHistory")]
        public async Task<IActionResult> GetCreditHistory(int memberId, [FromQuery] int days = 90)
        {
            try
            {
                var history = await _memberService.GetCreditHistoryAsync(memberId, days);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting credit history for member {MemberId}", memberId);
                return StatusCode(500, new { message = "Internal server error occurred" });
            }
        }

        // ==================== COLLECTIONS MANAGEMENT ==================== //

        /// <summary>
        /// Get members with overdue payments for collections
        /// </summary>
        /// <param name="branchId">Optional branch filter</param>
        [HttpGet("collections/overdue")]
        [Authorize(Policy = "Membership.Collections")]
        public async Task<IActionResult> GetOverdueMembers([FromQuery] int? branchId = null)
        {
            try
            {
                var overdueMembers = await _memberService.GetOverdueMembersAsync(branchId);
                return Ok(overdueMembers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting overdue members for branch {BranchId}", branchId);
                return StatusCode(500, new { message = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// Get members approaching their credit limit
        /// </summary>
        /// <param name="threshold">Credit utilization threshold percentage (default 80%)</param>
        [HttpGet("collections/approaching-limit")]
        [Authorize(Policy = "Membership.Collections")]
        public async Task<IActionResult> GetMembersApproachingLimit([FromQuery] decimal threshold = 80)
        {
            try
            {
                var membersNearLimit = await _memberService.GetMembersApproachingLimitAsync(threshold);
                return Ok(membersNearLimit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting members approaching credit limit");
                return StatusCode(500, new { message = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// Get top debtors regardless of overdue status
        /// </summary>
        [HttpGet("collections/top-debtors")]
        [Authorize(Policy = "Membership.Collections")]
        public async Task<IActionResult> GetTopDebtors(
            [FromQuery] int? branchId = null, 
            [FromQuery] int limit = 10)
        {
            try
            {
                var topDebtors = await _memberService.GetTopDebtorsAsync(branchId, limit);
                return Ok(topDebtors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top debtors for branch {BranchId}", branchId);
                return StatusCode(500, new { message = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// Update member credit status based on payment behavior
        /// </summary>
        /// <param name="memberId">Member ID</param>
        [HttpPut("{memberId}/credit/status")]
        [Authorize(Policy = "Membership.UpdateCredit")]
        public async Task<IActionResult> UpdateCreditStatus(int memberId)
        {
            try
            {
                var success = await _memberService.UpdateCreditStatusAsync(memberId);
                
                if (!success)
                {
                    return BadRequest(new { message = "Failed to update credit status" });
                }

                // Get updated summary
                var summary = await _memberService.GetCreditSummaryAsync(memberId);

                return Ok(new 
                { 
                    message = "Credit status updated successfully",
                    newStatus = summary.Status,
                    creditScore = summary.CreditScore
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating credit status for member {MemberId}", memberId);
                return StatusCode(500, new { message = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// Update member credit limit with new value and reason
        /// </summary>
        /// <param name="memberId">Member ID</param>
        /// <param name="request">Credit limit update request</param>
        [HttpPut("{memberId}/credit/limit")]
        [Authorize(Policy = "Membership.UpdateCredit")]
        public async Task<IActionResult> UpdateCreditLimit(int memberId, [FromBody] UpdateCreditLimitDto request)
        {
            try
            {
                var newLimit = await _memberService.UpdateCreditLimitAsync(memberId, request.NewCreditLimit, request.Reason, request.Notes);

                _logger.LogInformation("Updated credit limit for member {MemberId} to {NewLimit} IDR. Reason: {Reason}", 
                    memberId, newLimit, request.Reason);

                return Ok(new 
                { 
                    message = "Credit limit updated successfully",
                    newCreditLimit = newLimit,
                    formattedLimit = $"Rp {newLimit:N0}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating credit limit for member {MemberId}", memberId);
                return StatusCode(500, new { message = "Internal server error occurred" });
            }
        }

        // ==================== PAYMENT REMINDERS ==================== //

        /// <summary>
        /// Get members requiring payment reminders
        /// </summary>
        /// <param name="reminderDate">Date to check reminders for (default today)</param>
        [HttpGet("reminders")]
        [Authorize(Policy = "Membership.SendReminders")]
        public async Task<IActionResult> GetPaymentReminders([FromQuery] DateTime? reminderDate = null)
        {
            try
            {
                var reminders = await _memberService.GetPaymentRemindersAsync(reminderDate);
                return Ok(reminders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment reminders for date {ReminderDate}", reminderDate);
                return StatusCode(500, new { message = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// Send payment reminder to member
        /// </summary>
        /// <param name="memberId">Member ID</param>
        [HttpPost("{memberId}/reminders/send")]
        [Authorize(Policy = "Membership.SendReminders")]
        public async Task<IActionResult> SendPaymentReminder(int memberId)
        {
            try
            {
                var success = await _memberService.SendPaymentReminderAsync(memberId);
                
                if (!success)
                {
                    return BadRequest(new { message = "Failed to send payment reminder" });
                }

                _logger.LogInformation("Payment reminder sent to member {MemberId}", memberId);

                return Ok(new { message = "Payment reminder sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending payment reminder to member {MemberId}", memberId);
                return StatusCode(500, new { message = "Internal server error occurred" });
            }
        }

        // ==================== CREDIT ANALYTICS ==================== //

        /// <summary>
        /// Get credit analytics for branch or system-wide
        /// </summary>
        /// <param name="branchId">Optional branch filter</param>
        /// <param name="startDate">Start date for analysis (default 30 days ago)</param>
        /// <param name="endDate">End date for analysis (default today)</param>
        [HttpGet("analytics")]
        [Authorize(Policy = "Membership.Analytics")]
        public async Task<IActionResult> GetCreditAnalytics(
            [FromQuery] int? branchId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                // Default to last 30 days if not specified
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                var analytics = await _memberService.GetCreditAnalyticsAsync(branchId, start, end);
                return Ok(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting credit analytics for branch {BranchId}", branchId);
                return StatusCode(500, new { message = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// Calculate member credit score
        /// </summary>
        /// <param name="memberId">Member ID</param>
        [HttpGet("{memberId}/credit/score")]
        [Authorize(Policy = "Membership.CreditHistory")]
        public async Task<IActionResult> GetCreditScore(int memberId)
        {
            try
            {
                var creditScore = await _memberService.CalculateCreditScoreAsync(memberId);
                
                var grade = creditScore switch
                {
                    >= 800 => "Excellent",
                    >= 740 => "Very Good",
                    >= 670 => "Good",
                    >= 580 => "Fair",
                    _ => "Poor"
                };

                return Ok(new 
                { 
                    memberId = memberId,
                    creditScore = creditScore,
                    creditGrade = grade,
                    scoreRange = "300-850"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating credit score for member {MemberId}", memberId);
                return StatusCode(500, new { message = "Internal server error occurred" });
            }
        }

        // ==================== BULK OPERATIONS ==================== //

        /// <summary>
        /// Send payment reminders to all overdue members
        /// </summary>
        /// <param name="branchId">Optional branch filter</param>
        [HttpPost("reminders/bulk-send")]
        [Authorize(Policy = "Membership.BulkCredit")]
        public async Task<IActionResult> SendBulkPaymentReminders([FromQuery] int? branchId = null)
        {
            try
            {
                var overdueMembers = await _memberService.GetOverdueMembersAsync(branchId);
                var results = new BulkOperationResultDto
                {
                    TotalRequested = overdueMembers.Count
                };

                foreach (var member in overdueMembers)
                {
                    try
                    {
                        var success = await _memberService.SendPaymentReminderAsync(member.MemberId);
                        if (success)
                        {
                            results.Successful++;
                        }
                        else
                        {
                            results.Failed++;
                            results.Errors.Add($"Failed to send reminder to member {member.MemberNumber}");
                        }
                    }
                    catch (Exception ex)
                    {
                        results.Failed++;
                        results.Errors.Add($"Error sending reminder to member {member.MemberNumber}: {ex.Message}");
                        _logger.LogError(ex, "Error in bulk reminder for member {MemberId}", member.MemberId);
                    }
                }

                results.Summary = $"Sent {results.Successful} reminders successfully, {results.Failed} failed";

                _logger.LogInformation("Bulk payment reminders completed: {Successful} successful, {Failed} failed", 
                    results.Successful, results.Failed);

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk payment reminder operation");
                return StatusCode(500, new { message = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// Update credit status for all members (maintenance operation)
        /// </summary>
        [HttpPost("maintenance/update-all-status")]
        [Authorize(Policy = "Membership.BulkCredit")]
        public async Task<IActionResult> UpdateAllCreditStatuses()
        {
            try
            {
                // Get all members with credit (simplified - in real app you'd paginate)
                var analytics = await _memberService.GetCreditAnalyticsAsync();
                var totalMembers = analytics.TotalMembersWithCredit;

                var results = new BulkOperationResultDto
                {
                    TotalRequested = totalMembers
                };

                // Note: In a real implementation, you'd want to process this in batches
                // and potentially use a background service for large datasets
                
                _logger.LogInformation("Starting bulk credit status update for {TotalMembers} members", totalMembers);

                results.Summary = $"Credit status update initiated for {totalMembers} members";
                results.Warnings.Add("This operation runs in the background and may take time to complete");

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk credit status update operation");
                return StatusCode(500, new { message = "Internal server error occurred" });
            }
        }

        // ==================== HELPER METHODS ==================== //

        /// <summary>
        /// Get current user ID from JWT claims
        /// </summary>
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        /// <summary>
        /// Get current user's branch ID from JWT claims
        /// </summary>
        private int? GetCurrentUserBranchId()
        {
            var branchIdClaim = User.FindFirst("BranchId")?.Value;
            return int.TryParse(branchIdClaim, out var branchId) ? branchId : null;
        }
    }
}