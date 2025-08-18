// Services/IMemberService.cs - Sprint 2 Member Service Interface
using Berca_Backend.DTOs;
using Berca_Backend.Models;
namespace Berca_Backend.Services
{
    public interface IMemberService
    {
        // CRUD Operations
        Task<MemberSearchResponse> SearchMembersAsync(string? search = null, bool? isActive = null, int page = 1, int pageSize = 20);
        Task<MemberDto?> GetMemberByIdAsync(int id);
        Task<MemberDto?> GetMemberByPhoneAsync(string phone);
        Task<MemberDto?> GetMemberByNumberAsync(string memberNumber);
        Task<MemberDto> CreateMemberAsync(CreateMemberRequest request, string createdBy);
        Task<MemberDto> UpdateMemberAsync(int id, UpdateMemberRequest request, string updatedBy);
        Task<bool> DeleteMemberAsync(int id);

        // Point Management
        Task<bool> AddPointsAsync(int memberId, int points, string description, int? saleId = null, string? referenceNumber = null, string? createdBy = null);
        Task<bool> RedeemPointsAsync(int memberId, int points, string description, string? referenceNumber = null, string? createdBy = null);
        Task<List<MemberPointDto>> GetPointHistoryAsync(int memberId, int page = 1, int pageSize = 20);
        Task<int> GetAvailablePointsAsync(int memberId);

        // Member Analytics
        Task<MemberStatsDto> GetMemberStatsAsync(int memberId);
        Task<List<TopMemberDto>> GetTopMembersAsync(int count = 10, DateTime? startDate = null, DateTime? endDate = null);

        // ✅ ADDED: Member Statistics Update
        Task<bool> UpdateMemberStatsAsync(int memberId, decimal transactionAmount, int transactionCount = 1);

        // Validation
        Task<bool> IsPhoneExistsAsync(string phone, int? excludeId = null);
        Task<bool> IsMemberNumberExistsAsync(string memberNumber);

        // Tier Management
        Task<bool> UpdateMemberTierAsync(int memberId);
        Task<MembershipTier> CalculateMemberTierAsync(decimal totalSpent);

        // ==================== CREDIT MANAGEMENT ==================== //

        /// <summary>
        /// Grant credit to member for purchase (creates debt)
        /// </summary>
        /// <param name="memberId">Member ID</param>
        /// <param name="amount">Credit amount in IDR</param>
        /// <param name="description">Transaction description</param>
        /// <param name="saleId">Reference to sale transaction</param>
        /// <returns>Success status</returns>
        Task<bool> GrantCreditAsync(int memberId, decimal amount, string description, int saleId);

        /// <summary>
        /// Record debt payment from member (reduces debt)
        /// </summary>
        /// <param name="memberId">Member ID</param>
        /// <param name="amount">Payment amount in IDR</param>
        /// <param name="paymentMethod">Payment method (cash, transfer, etc.)</param>
        /// <param name="reference">Payment reference number</param>
        /// <returns>Success status</returns>
        Task<bool> RecordPaymentAsync(int memberId, decimal amount, string paymentMethod, string? reference = null);

        /// <summary>
        /// Get member's credit transaction history
        /// </summary>
        /// <param name="memberId">Member ID</param>
        /// <param name="days">Number of days to look back</param>
        /// <returns>List of credit transactions</returns>
        Task<List<MemberCreditTransactionDto>> GetCreditHistoryAsync(int memberId, int days = 90);

        /// <summary>
        /// Get comprehensive credit summary for member
        /// </summary>
        /// <param name="memberId">Member ID</param>
        /// <returns>Complete credit summary with analytics</returns>
        Task<MemberCreditSummaryDto> GetCreditSummaryAsync(int memberId);

        // ==================== RISK & COLLECTIONS ==================== //

        /// <summary>
        /// Get members with overdue payments for collections
        /// </summary>
        /// <param name="branchId">Optional branch filter</param>
        /// <returns>List of members with overdue debt</returns>
        Task<List<MemberDebtDto>> GetOverdueMembersAsync(int? branchId = null);

        /// <summary>
        /// Get members approaching their credit limit
        /// </summary>
        /// <param name="thresholdPercentage">Credit utilization threshold (default 80%)</param>
        /// <returns>List of members near credit limit</returns>
        Task<List<MemberDebtDto>> GetMembersApproachingLimitAsync(decimal thresholdPercentage = 80);

        /// <summary>
        /// Update member credit status based on payment behavior
        /// </summary>
        /// <param name="memberId">Member ID</param>
        /// <returns>Success status</returns>
        Task<bool> UpdateCreditStatusAsync(int memberId);

        /// <summary>
        /// Check if member is eligible for requested credit amount
        /// </summary>
        /// <param name="memberId">Member ID</param>
        /// <param name="requestedAmount">Requested credit amount</param>
        /// <returns>Eligibility assessment with details</returns>
        Task<CreditEligibilityDto> CheckCreditEligibilityAsync(int memberId, decimal requestedAmount);

        // ==================== PAYMENT REMINDERS ==================== //

        /// <summary>
        /// Get members requiring payment reminders
        /// </summary>
        /// <param name="reminderDate">Date to check reminders for (default today)</param>
        /// <returns>List of members needing reminders</returns>
        Task<List<PaymentReminderDto>> GetPaymentRemindersAsync(DateTime? reminderDate = null);

        /// <summary>
        /// Send payment reminder to member
        /// </summary>
        /// <param name="memberId">Member ID</param>
        /// <returns>Success status</returns>
        Task<bool> SendPaymentReminderAsync(int memberId);

        /// <summary>
        /// Calculate member credit score (300-850 range)
        /// </summary>
        /// <param name="memberId">Member ID</param>
        /// <returns>Credit score value</returns>
        Task<int> CalculateCreditScoreAsync(int memberId);

        // ==================== CREDIT ANALYTICS ==================== //

        /// <summary>
        /// Get credit analytics for branch or system-wide
        /// </summary>
        /// <param name="branchId">Optional branch filter</param>
        /// <param name="startDate">Start date for analysis</param>
        /// <param name="endDate">End date for analysis</param>
        /// <returns>Credit analytics summary</returns>
        Task<CreditAnalyticsDto> GetCreditAnalyticsAsync(int? branchId = null, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Update member credit limit based on tier and payment history
        /// </summary>
        /// <param name="memberId">Member ID</param>
        /// <returns>New credit limit amount</returns>
        Task<decimal> UpdateCreditLimitAsync(int memberId);
    }
}