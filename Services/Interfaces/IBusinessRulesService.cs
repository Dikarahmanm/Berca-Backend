using Berca_Backend.Models;

namespace Berca_Backend.Services.Interfaces
{
    /// <summary>
    /// Service for centralizing business rules and logic
    /// Provides consistent business rule evaluation across the application
    /// </summary>
    public interface IBusinessRulesService
    {
        /// <summary>
        /// Determines if a facture requires approval based on business rules
        /// </summary>
        bool RequiresApproval(Facture facture);

        /// <summary>
        /// Determines if a facture can be auto-approved based on business rules
        /// </summary>
        bool CanAutoApprove(Facture facture);

        /// <summary>
        /// Determines if a facture requires manager approval based on amount
        /// </summary>
        bool RequiresManagerApproval(Facture facture);

        /// <summary>
        /// Gets the approval reason for a facture
        /// </summary>
        string GetApprovalReason(Facture facture);

        /// <summary>
        /// Gets the approval threshold amount from configuration
        /// </summary>
        decimal GetApprovalThreshold();

        /// <summary>
        /// Gets the auto-approval limit from configuration
        /// </summary>
        decimal GetAutoApprovalLimit();

        /// <summary>
        /// Gets the manager approval threshold from configuration
        /// </summary>
        decimal GetManagerApprovalThreshold();

        /// <summary>
        /// Determines if a facture is overdue based on business rules
        /// </summary>
        bool IsOverdue(Facture facture);

        /// <summary>
        /// Gets the payment priority for a facture
        /// </summary>
        PaymentPriority GetPaymentPriority(Facture facture);
    }
}