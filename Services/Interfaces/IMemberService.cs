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
        /// <param name="paymentTermDays">Payment terms in days (optional)</param>
        /// <param name="dueDate">Specific due date (optional, takes precedence over paymentTermDays)</param>
        /// <returns>Success status</returns>
        Task<bool> GrantCreditAsync(int memberId, decimal amount, string description, int saleId, int? paymentTermDays = null, DateTime? dueDate = null);

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
        /// Update member credit limit with new value and reason
        /// </summary>
        /// <param name="memberId">Member ID</param>
        /// <param name="newCreditLimit">New credit limit amount</param>
        /// <param name="reason">Reason for credit limit change</param>
        /// <param name="notes">Additional notes</param>
        /// <returns>Updated credit limit amount</returns>
        Task<decimal> UpdateCreditLimitAsync(int memberId, decimal newCreditLimit, string reason, string? notes = null);

        // ==================== MEMBER CREDIT INTEGRATION METHODS ==================== //

        /// <summary>
        /// Get member with comprehensive credit information
        /// </summary>
        /// <param name="memberId">Member ID</param>
        /// <returns>Member with complete credit details</returns>
        Task<MemberWithCreditDto?> GetMemberWithCreditAsync(int memberId);

        /// <summary>
        /// Search members with credit information and advanced filters
        /// </summary>
        /// <param name="filter">Search criteria including credit filters</param>
        /// <returns>Paginated result of members with credit info</returns>
        Task<PagedResult<MemberWithCreditDto>> SearchMembersWithCreditAsync(MemberSearchWithCreditDto filter);

        /// <summary>
        /// Get quick credit status for UI components
        /// </summary>
        /// <param name="memberId">Member ID</param>
        /// <returns>Concise credit status information</returns>
        Task<MemberCreditStatusDto?> GetMemberCreditStatusAsync(int memberId);

        /// <summary>
        /// Get member credit information optimized for POS display
        /// </summary>
        /// <param name="identifier">Phone, member number, or ID as string</param>
        /// <returns>POS-optimized member credit information</returns>
        Task<POSMemberCreditDto?> GetMemberCreditForPOSAsync(string identifier);

        /// <summary>
        /// Update member statistics after credit transaction
        /// </summary>
        /// <param name="memberId">Member ID</param>
        /// <param name="amount">Credit amount granted</param>
        /// <param name="saleId">Related sale ID</param>
        /// <returns>Success status</returns>
        Task<bool> UpdateMemberAfterCreditTransactionAsync(int memberId, decimal amount, int saleId);

        /// <summary>
        /// Find member by various identifiers (phone, member number, ID)
        /// </summary>
        /// <param name="identifier">Search identifier</param>
        /// <returns>Member or null if not found</returns>
        Task<Member?> FindMemberAsync(string identifier);

        /// <summary>
        /// Calculate available credit considering current debt and limits
        /// </summary>
        /// <param name="memberId">Member ID</param>
        /// <returns>Available credit amount</returns>
        Task<decimal> CalculateAvailableCreditAsync(int memberId);

        /// <summary>
        /// Get member credit utilization percentage
        /// </summary>
        /// <param name="memberId">Member ID</param>
        /// <returns>Credit utilization as percentage (0-100)</returns>
        Task<decimal> CalculateCreditUtilizationAsync(int memberId);

        /// <summary>
        /// Determine member credit status based on payment behavior
        /// </summary>
        /// <param name="memberId">Member ID</param>
        /// <returns>Credit status (Good, Warning, Bad)</returns>
        Task<string> DetermineCreditStatusAsync(int memberId);

        /// <summary>
        /// Check if member has any overdue payments
        /// </summary>
        /// <param name="memberId">Member ID</param>
        /// <returns>True if has overdue payments</returns>
        Task<bool> HasOverduePaymentsAsync(int memberId);

        /// <summary>
        /// Get days until next payment due
        /// </summary>
        /// <param name="memberId">Member ID</param>
        /// <returns>Days until next payment (negative if overdue)</returns>
        Task<int> GetDaysUntilNextPaymentAsync(int memberId);

        /// <summary>
        /// Calculate maximum allowed transaction for member
        /// </summary>
        /// <param name="memberId">Member ID</param>
        /// <returns>Maximum transaction amount</returns>
        Task<decimal> CalculateMaxTransactionAmountAsync(int memberId);

        /// <summary>
        /// Get top debtors regardless of overdue status
        /// </summary>
        /// <param name="branchId">Optional branch filter</param>
        /// <param name="limit">Maximum number of debtors to return (default 10)</param>
        /// <returns>List of members with highest debt amounts</returns>
        Task<List<MemberDebtDto>> GetTopDebtorsAsync(int? branchId = null, int limit = 10);

        /// <summary>
        /// Get member credit status color for UI display
        /// </summary>
        /// <param name="creditStatus">Credit status string</param>
        /// <returns>Color code (Green, Orange, Red)</returns>
        string GetCreditStatusColor(string creditStatus);

        /// <summary>
        /// Format credit amount for display with currency
        /// </summary>
        /// <param name="amount">Amount to format</param>
        /// <returns>Formatted currency string</returns>
        string FormatCreditAmount(decimal amount);

        /// <summary>
        /// Get member credit status message for POS display
        /// </summary>
        /// <param name="memberCredit">Member credit summary</param>
        /// <returns>User-friendly status message</returns>
        string GetCreditStatusMessage(MemberCreditSummaryDto memberCredit);

        /// <summary>
        /// Reconcile member credit ledger by allocating payments to oldest credit sales,
        /// updating transaction statuses, and syncing NextPaymentDueDate.
        /// </summary>
        /// <param name="memberId">Member ID</param>
        /// <returns>True if reconciliation completed</returns>
        Task<bool> ReconcileCreditLedgerAsync(int memberId);

        /// <summary>
        /// Backfill NextPaymentDueDate for members with outstanding debt by
        /// reconciling their ledger and setting the earliest pending due date.
        /// Returns the number of members processed.
        /// </summary>
        Task<int> BackfillNextPaymentDueDatesAsync();
    }
}
