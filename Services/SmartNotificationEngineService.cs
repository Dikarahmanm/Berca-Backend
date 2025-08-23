using Berca_Backend.Data;
using Berca_Backend.DTOs;
using Berca_Backend.Models;
using Berca_Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Berca_Backend.Services
{
    /// <summary>
    /// Smart notification engine for intelligent alerts and escalation management
    /// Generates context-aware notifications based on business rules and urgency
    /// </summary>
    public interface ISmartNotificationEngineService
    {
        Task<List<SmartNotificationDto>> GenerateIntelligentNotificationsAsync(int? branchId = null);
        Task<bool> ProcessNotificationRulesAsync();
        Task<List<EscalationAlert>> ProcessEscalationAlertsAsync();
        Task<NotificationPreferencesDto> GetUserNotificationPreferencesAsync(int userId);
        Task<bool> SendCriticalExpiryAlertsAsync();
    }

    public class SmartNotificationEngineService : ISmartNotificationEngineService
    {
        private readonly AppDbContext _context;
        private readonly IExpiryManagementService _expiryService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<SmartNotificationEngineService> _logger;

        public SmartNotificationEngineService(
            AppDbContext context,
            IExpiryManagementService expiryService,
            INotificationService notificationService,
            ILogger<SmartNotificationEngineService> logger)
        {
            _context = context;
            _expiryService = expiryService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<List<SmartNotificationDto>> GenerateIntelligentNotificationsAsync(int? branchId = null)
        {
            try
            {
                var notifications = new List<SmartNotificationDto>();
                var currentDate = DateTime.UtcNow;

                // Critical expiry notifications
                var criticalBatches = await _context.ProductBatches
                    .Include(pb => pb.Product)
                    .Where(pb => pb.ExpiryDate <= currentDate.AddDays(3) && 
                                pb.CurrentStock > 0 && 
                                !pb.IsDisposed &&
                                (branchId == null || pb.BranchId == branchId))
                    .ToListAsync();

                foreach (var batch in criticalBatches)
                {
                    var daysLeft = (batch.ExpiryDate!.Value - currentDate).Days;
                    var stockValue = batch.CurrentStock * batch.CostPerUnit;

                    notifications.Add(new SmartNotificationDto
                    {
                        Type = NotificationTypes.CRITICAL_EXPIRY,
                        Priority = daysLeft <= 1 ? Models.NotificationPriority.Critical : Models.NotificationPriority.High,
                        Title = $"🚨 Critical Expiry Alert: {batch.Product.Name}",
                        Message = $"Batch {batch.BatchNumber} expires in {daysLeft} days. {batch.CurrentStock} units at risk (Rp {stockValue:N0})",
                        
                        PotentialLoss = stockValue,
                        ActionDeadline = batch.ExpiryDate.Value,
                        ActionUrl = $"/inventory/batch/{batch.Id}",
                        
                        ActionItems = new List<string>
                        {
                            $"Review pricing for immediate discount",
                            $"Check transfer opportunities to other branches",
                            $"Consider promotional campaigns",
                            daysLeft <= 1 ? "Prepare for disposal if unsold" : "Monitor daily"
                        },
                        
                        EscalationRule = new EscalationRule
                        {
                            EscalateAfterHours = daysLeft <= 1 ? 4 : 24,
                            EscalateToRoles = new List<string> { "Manager", "Admin" },
                            RequireAcknowledgment = daysLeft <= 1,
                            NotificationChannels = daysLeft <= 1 
                                ? new List<NotificationChannel> { NotificationChannel.PUSH, NotificationChannel.EMAIL, NotificationChannel.SMS }
                                : new List<NotificationChannel> { NotificationChannel.PUSH, NotificationChannel.EMAIL }
                        },
                        
                        BusinessImpact = new BusinessImpact
                        {
                            FinancialRisk = stockValue,
                            OperationalImpact = daysLeft <= 1 ? "High" : "Medium",
                            CustomerImpact = "Low",
                            ComplianceRisk = batch.Product.Category?.RequiresExpiryDate == true ? "High" : "Low"
                        },
                        
                        AffectedBatches = new List<AffectedBatch>
                        {
                            new AffectedBatch
                            {
                                BatchId = batch.Id,
                                BatchNumber = batch.BatchNumber,
                                Quantity = batch.CurrentStock,
                                Value = stockValue,
                                ExpiryDate = batch.ExpiryDate.Value
                            }
                        }
                    });
                }

                // Low stock with expiry risk notifications
                var lowStockWithExpiry = await _context.Products
                    .Include(p => p.ProductBatches)
                    .Where(p => p.Stock <= p.MinimumStock && 
                               p.ProductBatches.Any(pb => pb.ExpiryDate <= currentDate.AddDays(30)))
                    .ToListAsync();

                foreach (var product in lowStockWithExpiry)
                {
                    var nearestExpiry = product.ProductBatches
                        .Where(pb => pb.ExpiryDate.HasValue && pb.CurrentStock > 0)
                        .OrderBy(pb => pb.ExpiryDate)
                        .FirstOrDefault();

                    if (nearestExpiry != null)
                    {
                        notifications.Add(new SmartNotificationDto
                        {
                            Type = NotificationTypes.LOW_STOCK_EXPIRY_RISK,
                            Priority = Models.NotificationPriority.Normal,
                            Title = $"⚠️ Low Stock + Expiry Risk: {product.Name}",
                            Message = $"Stock: {product.Stock}/{product.MinimumStock} minimum. Nearest expiry: {nearestExpiry.ExpiryDate:dd/MM/yyyy}",
                            
                            ActionItems = new List<string>
                            {
                                "Consider reorder timing carefully",
                                "Evaluate FIFO opportunities",
                                "Check supplier lead times"
                            }
                        });
                    }
                }

                // Financial impact notifications
                var totalValueAtRisk = notifications.Sum(n => n.PotentialLoss);
                if (totalValueAtRisk > 10000000) // 10M IDR threshold
                {
                    notifications.Insert(0, new SmartNotificationDto
                    {
                        Type = NotificationTypes.FINANCIAL_RISK_SUMMARY,
                        Priority = Models.NotificationPriority.High,
                        Title = $"💰 High Financial Risk Alert",
                        Message = $"Total value at risk from expiring inventory: Rp {totalValueAtRisk:N0}",
                        
                        ActionItems = new List<string>
                        {
                            "Review all critical expiry alerts immediately",
                            "Implement emergency discount strategy",
                            "Consider inter-branch transfers",
                            "Schedule management review meeting"
                        },
                        
                        EscalationRule = new EscalationRule
                        {
                            EscalateAfterHours = 2,
                            EscalateToRoles = new List<string> { "Admin", "HeadManager" },
                            RequireAcknowledgment = true,
                            NotificationChannels = new List<NotificationChannel> { 
                                NotificationChannel.PUSH, 
                                NotificationChannel.EMAIL, 
                                NotificationChannel.SMS 
                            }
                        }
                    });
                }

                return notifications.OrderByDescending(n => (int)n.Priority).ThenBy(n => n.ActionDeadline).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating intelligent notifications for branch {BranchId}", branchId);
                throw;
            }
        }

        public async Task<bool> ProcessNotificationRulesAsync()
        {
            try
            {
                // Get all active notification rules
                var rules = await _context.NotificationRules
                    .Where(nr => nr.IsActive)
                    .ToListAsync();

                foreach (var rule in rules)
                {
                    await ProcessIndividualRuleAsync(rule);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing notification rules");
                return false;
            }
        }

        public async Task<List<EscalationAlert>> ProcessEscalationAlertsAsync()
        {
            try
            {
                var escalations = new List<EscalationAlert>();

                // Get unacknowledged critical notifications older than escalation threshold
                var overdueNotifications = await _context.Notifications
                    .Where(n => n.Priority == Models.NotificationPriority.Critical && 
                               !n.IsRead && 
                               n.CreatedAt <= DateTime.UtcNow.AddHours(-4))
                    .ToListAsync();

                foreach (var notification in overdueNotifications)
                {
                    // Send escalation to managers and admins
                    var managersAndAdmins = await _context.Users
                        .Where(u => u.IsActive && (u.Role == "Manager" || u.Role == "Admin"))
                        .ToListAsync();

                    foreach (var user in managersAndAdmins)
                    {
                        // Create escalation notification
                        var escalationNotification = new Notification
                        {
                            UserId = user.Id,
                            Type = "ESCALATION_ALERT",
                            Title = $"🚨 ESCALATED: {notification.Title}",
                            Message = $"Critical alert requires immediate attention. Original alert created {(DateTime.UtcNow - notification.CreatedAt).Hours} hours ago.",
                            Priority = Models.NotificationPriority.Critical,
                            ActionUrl = notification.ActionUrl,
                            CreatedAt = DateTime.UtcNow,
                            IsRead = false
                        };

                        _context.Notifications.Add(escalationNotification);
                    }

                    // Track escalation
                    escalations.Add(new EscalationAlert
                    {
                        OriginalNotificationId = notification.Id,
                        EscalatedAt = DateTime.UtcNow,
                        EscalationReason = "Unacknowledged critical alert",
                        EscalatedToUserCount = managersAndAdmins.Count,
                        Priority = "Critical"
                    });
                }

                await _context.SaveChangesAsync();
                return escalations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing escalation alerts");
                return new List<EscalationAlert>();
            }
        }

        public async Task<NotificationPreferencesDto> GetUserNotificationPreferencesAsync(int userId)
        {
            try
            {
                var preferences = await _context.UserNotificationPreferences
                    .FirstOrDefaultAsync(unp => unp.UserId == userId);

                if (preferences == null)
                {
                    // Return default preferences
                    return new NotificationPreferencesDto
                    {
                        UserId = userId,
                        EmailNotifications = true,
                        PushNotifications = true,
                        SmsNotifications = false,
                        ExpiryAlerts = true,
                        StockAlerts = true,
                        FinancialAlerts = true,
                        AlertFrequency = "Immediate",
                        QuietHours = new QuietHours { Start = "22:00", End = "06:00" }
                    };
                }

                return new NotificationPreferencesDto
                {
                    UserId = preferences.UserId,
                    EmailNotifications = preferences.EmailNotifications,
                    PushNotifications = preferences.PushNotifications,
                    SmsNotifications = preferences.SmsNotifications,
                    ExpiryAlerts = preferences.ExpiryAlerts,
                    StockAlerts = preferences.StockAlerts,
                    FinancialAlerts = preferences.FinancialAlerts,
                    AlertFrequency = preferences.AlertFrequency,
                    QuietHours = new QuietHours
                    {
                        Start = preferences.QuietHoursStart,
                        End = preferences.QuietHoursEnd
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification preferences for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> SendCriticalExpiryAlertsAsync()
        {
            try
            {
                var criticalBatches = await _context.ProductBatches
                    .Include(pb => pb.Product)
                    .Where(pb => pb.ExpiryDate <= DateTime.UtcNow.AddDays(1) && 
                                pb.CurrentStock > 0 && 
                                !pb.IsDisposed)
                    .ToListAsync();

                if (!criticalBatches.Any())
                    return true;

                var totalValueAtRisk = criticalBatches.Sum(b => b.CurrentStock * b.CostPerUnit);

                // Send to managers and admins
                var targetUsers = await _context.Users
                    .Where(u => u.IsActive && (u.Role == "Manager" || u.Role == "Admin"))
                    .ToListAsync();

                foreach (var user in targetUsers)
                {
                    var notification = new CreateNotificationDto
                    {
                        Title = "🚨 CRITICAL: Products Expiring Today",
                        Message = $"{criticalBatches.Count} batches expire today. Total value at risk: Rp {totalValueAtRisk:N0}",
                        Priority = "Critical",
                        Type = "CriticalExpiry",
                        ActionUrl = "/inventory/expiring",
                        Metadata = new { BatchCount = criticalBatches.Count, ValueAtRisk = totalValueAtRisk }
                    };

                    var request = new CreateNotificationRequest
                    {
                        UserId = user.Id,
                        Title = notification.Title,
                        Message = notification.Message,
                        Priority = notification.Priority,
                        Type = notification.Type,
                        ActionUrl = notification.ActionUrl
                    };
                    await _notificationService.CreateNotificationAsync(request, "System");
                }

                _logger.LogInformation("Sent critical expiry alerts for {Count} batches to {UserCount} users", 
                    criticalBatches.Count, targetUsers.Count);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending critical expiry alerts");
                return false;
            }
        }

        // Private helper methods
        private async Task ProcessIndividualRuleAsync(NotificationRule rule)
        {
            try
            {
                // Process rule based on type
                switch (rule.RuleType)
                {
                    case "ExpiryWarning":
                        await ProcessExpiryWarningRuleAsync(rule);
                        break;
                    case "LowStock":
                        await ProcessLowStockRuleAsync(rule);
                        break;
                    case "HighValue":
                        await ProcessHighValueRuleAsync(rule);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing notification rule {RuleId}", rule.Id);
            }
        }

        private async Task ProcessExpiryWarningRuleAsync(NotificationRule rule)
        {
            var thresholdDays = 7; // Default value - would parse JSON from Parameters if needed
            
            var expiringBatches = await _context.ProductBatches
                .Include(pb => pb.Product)
                .Where(pb => pb.ExpiryDate <= DateTime.UtcNow.AddDays(thresholdDays) &&
                            pb.ExpiryDate > DateTime.UtcNow &&
                            pb.CurrentStock > 0)
                .ToListAsync();

            // Create notifications based on rule criteria
            foreach (var batch in expiringBatches)
            {
                var notification = new CreateNotificationDto
                {
                    Title = $"Expiry Warning: {batch.Product.Name}",
                    Message = $"Batch {batch.BatchNumber} expires in {(batch.ExpiryDate!.Value - DateTime.UtcNow).Days} days",
                    Priority = "Medium",
                    Type = "ExpiryWarning",
                    ActionUrl = $"/inventory/batch/{batch.Id}"
                };

                // Send to relevant users based on rule
                var targetUsers = await GetTargetUsersForRuleAsync(rule);
                foreach (var userId in targetUsers)
                {
                    var request = new CreateNotificationRequest
                    {
                        UserId = userId,
                        Title = notification.Title,
                        Message = notification.Message,
                        Priority = notification.Priority,
                        Type = notification.Type,
                        ActionUrl = notification.ActionUrl
                    };
                    await _notificationService.CreateNotificationAsync(request, "System");
                }
            }
        }

        private async Task ProcessLowStockRuleAsync(NotificationRule rule)
        {
            var products = await _context.Products
                .Where(p => p.Stock <= p.MinimumStock && p.MinimumStock > 0)
                .ToListAsync();

            foreach (var product in products)
            {
                var notification = new CreateNotificationDto
                {
                    Title = $"Low Stock Alert: {product.Name}",
                    Message = $"Stock level: {product.Stock}/{product.MinimumStock} minimum",
                    Priority = "High",
                    Type = "LowStock",
                    ActionUrl = $"/inventory/product/{product.Id}"
                };

                var targetUsers = await GetTargetUsersForRuleAsync(rule);
                foreach (var userId in targetUsers)
                {
                    var request = new CreateNotificationRequest
                    {
                        UserId = userId,
                        Title = notification.Title,
                        Message = notification.Message,
                        Priority = notification.Priority,
                        Type = notification.Type,
                        ActionUrl = notification.ActionUrl
                    };
                    await _notificationService.CreateNotificationAsync(request, "System");
                }
            }
        }

        private async Task ProcessHighValueRuleAsync(NotificationRule rule)
        {
            var threshold = 5000000m; // Default value - would parse JSON from Parameters if needed
            
            var highValueBatches = await _context.ProductBatches
                .Include(pb => pb.Product)
                .Where(pb => pb.CurrentStock * pb.CostPerUnit >= threshold &&
                            pb.ExpiryDate <= DateTime.UtcNow.AddDays(7))
                .ToListAsync();

            foreach (var batch in highValueBatches)
            {
                var value = batch.CurrentStock * batch.CostPerUnit;
                var notification = new CreateNotificationDto
                {
                    Title = $"High Value Expiry Risk: {batch.Product.Name}",
                    Message = $"High value batch (Rp {value:N0}) expires soon",
                    Priority = "Critical",
                    Type = "HighValueRisk",
                    ActionUrl = $"/inventory/batch/{batch.Id}"
                };

                var targetUsers = await GetTargetUsersForRuleAsync(rule);
                foreach (var userId in targetUsers)
                {
                    var request = new CreateNotificationRequest
                    {
                        UserId = userId,
                        Title = notification.Title,
                        Message = notification.Message,
                        Priority = notification.Priority,
                        Type = notification.Type,
                        ActionUrl = notification.ActionUrl
                    };
                    await _notificationService.CreateNotificationAsync(request, "System");
                }
            }
        }

        private async Task<List<int>> GetTargetUsersForRuleAsync(NotificationRule rule)
        {
            var targetRoles = rule.TargetRoles?.Split(',').ToList() ?? new List<string> { "Manager", "Admin" };
            
            var users = await _context.Users
                .Where(u => u.IsActive && targetRoles.Contains(u.Role))
                .Select(u => u.Id)
                .ToListAsync();

            return users;
        }
    }

    // ==================== SUPPORTING DATA CLASSES ====================
    // DTOs moved to Berca_Backend.DTOs namespace to avoid conflicts
}