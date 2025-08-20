using Berca_Backend.Models;
using Berca_Backend.Services.Interfaces;

namespace Berca_Backend.Services
{
    /// <summary>
    /// Service for centralizing business rules and logic
    /// Provides consistent business rule evaluation across the application
    /// </summary>
    public class BusinessRulesService : IBusinessRulesService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<BusinessRulesService> _logger;

        public BusinessRulesService(IConfiguration configuration, ILogger<BusinessRulesService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Determines if a facture requires approval based on business rules
        /// </summary>
        public bool RequiresApproval(Facture facture)
        {
            var threshold = GetApprovalThreshold();
            
            return facture.Status == FactureStatus.Verified && 
                   facture.TotalAmount >= threshold;
        }

        /// <summary>
        /// Determines if a facture can be auto-approved based on business rules
        /// </summary>
        public bool CanAutoApprove(Facture facture)
        {
            var autoApprovalLimit = GetAutoApprovalLimit();
            
            return facture.Status == FactureStatus.Verified && 
                   facture.TotalAmount <= autoApprovalLimit;
        }

        /// <summary>
        /// Determines if a facture requires manager approval based on amount
        /// </summary>
        public bool RequiresManagerApproval(Facture facture)
        {
            var managerApprovalThreshold = GetManagerApprovalThreshold();
            
            return facture.TotalAmount >= managerApprovalThreshold;
        }

        /// <summary>
        /// Gets the approval reason for a facture
        /// </summary>
        public string GetApprovalReason(Facture facture)
        {
            if (RequiresManagerApproval(facture))
                return $"Amount {facture.TotalAmount:C} exceeds manager approval threshold";
                
            if (RequiresApproval(facture))
                return $"Amount {facture.TotalAmount:C} exceeds standard approval threshold";
                
            if (CanAutoApprove(facture))
                return "Eligible for auto-approval";
                
            return "No approval required";
        }

        /// <summary>
        /// Gets the approval threshold amount from configuration
        /// </summary>
        public decimal GetApprovalThreshold()
        {
            return _configuration.GetValue<decimal>("BusinessRules:Facture:ApprovalThreshold", 50000000m); // 50M IDR default
        }

        /// <summary>
        /// Gets the auto-approval limit from configuration
        /// </summary>
        public decimal GetAutoApprovalLimit()
        {
            return _configuration.GetValue<decimal>("BusinessRules:Facture:AutoApprovalLimit", 10000000m); // 10M IDR default
        }

        /// <summary>
        /// Gets the manager approval threshold from configuration
        /// </summary>
        public decimal GetManagerApprovalThreshold()
        {
            return _configuration.GetValue<decimal>("BusinessRules:Facture:RequireManagerApproval", 100000000m); // 100M IDR default
        }

        /// <summary>
        /// Determines if a facture is overdue based on business rules
        /// </summary>
        public bool IsOverdue(Facture facture)
        {
            var graceDays = _configuration.GetValue<int>("BusinessRules:Facture:OverdueGraceDays", 0);
            var overdueDate = facture.DueDate.AddDays(graceDays);
            
            return overdueDate < DateTime.UtcNow && 
                   facture.Status != FactureStatus.Paid && 
                   facture.Status != FactureStatus.Cancelled;
        }

        /// <summary>
        /// Gets the payment priority for a facture
        /// </summary>
        public PaymentPriority GetPaymentPriority(Facture facture)
        {
            if (facture.Status == FactureStatus.Paid || facture.Status == FactureStatus.Cancelled)
                return PaymentPriority.Normal;

            var daysUntilDue = (facture.DueDate.Date - DateTime.UtcNow.Date).Days;

            if (IsOverdue(facture) || daysUntilDue <= 1)
                return PaymentPriority.Urgent;

            if (daysUntilDue <= 7)
                return PaymentPriority.High;

            return PaymentPriority.Normal;
        }
    }
}