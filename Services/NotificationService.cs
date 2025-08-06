// Services/NotificationService.cs - Fixed: Expression tree lambda cannot contain null propagating operator
using Berca_Backend.DTOs;
using Berca_Backend.Models;
using Berca_Backend.Data;
using Microsoft.EntityFrameworkCore;
using Berca_Backend.Extensions;
using Berca_Backend.Services.Interfaces;

namespace Berca_Backend.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<NotificationService> _logger;
        private readonly ITimezoneService _timezoneService;

        public NotificationService(AppDbContext context, ILogger<NotificationService> logger, ITimezoneService timezoneService)
        {
            _context = context;
            _logger = logger;
            _timezoneService = timezoneService;
        }

        public async Task<List<NotificationDto>> GetUserNotificationsAsync(int userId, bool? isRead = null, int page = 1, int pageSize = 20)
        {
            try
            {
                var query = _context.Notifications
                    .Where(n => n.UserId == userId || n.UserId == null);

                if (isRead.HasValue)
                {
                    query = query.Where(n => n.IsRead == isRead.Value);
                }

                return await query
                    .OrderByDescending(n => n.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(n => new NotificationDto
                    {
                        Id = n.Id,
                        Type = n.Type,
                        Title = n.Title,
                        Message = n.Message,
                        ActionUrl = n.ActionUrl,
                        IsRead = n.IsRead,
                        Priority = n.Priority != null ? n.Priority.ToString() : "Normal", // ✅ FIXED: Use != null instead of ?.
                        CreatedAt = n.CreatedAt,
                        ReadAt = n.ReadAt
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user notifications: {UserId}", userId);
                throw;
            }
        }

        public async Task<NotificationSummaryDto> GetNotificationSummaryAsync(int userId)
        {
            try
            {
                var query = _context.Notifications
                    .Where(n => n.UserId == userId || n.UserId == null);

                var unreadCount = await query.CountAsync(n => !n.IsRead);
                var totalCount = await query.CountAsync();

                var recentNotifications = await query
                    .Where(n => !n.IsRead)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(5)
                    .Select(n => new NotificationDto
                    {
                        Id = n.Id,
                        Type = n.Type,
                        Title = n.Title,
                        Message = n.Message,
                        ActionUrl = n.ActionUrl,
                        IsRead = n.IsRead,
                        Priority = n.Priority != null ? n.Priority.ToString() : "Normal", // ✅ FIXED: Use != null instead of ?.
                        CreatedAt = n.CreatedAt,
                        ReadAt = n.ReadAt
                    })
                    .ToListAsync();

                return new NotificationSummaryDto
                {
                    UnreadCount = unreadCount,
                    TotalCount = totalCount,
                    RecentNotifications = recentNotifications
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification summary: {UserId}", userId);
                throw;
            }
        }

        public async Task<NotificationDto> CreateNotificationAsync(CreateNotificationRequest request, string? createdBy = null)
        {
            try
            {
                var priorityEnum = NotificationPriority.Normal;
                if (!string.IsNullOrWhiteSpace(request.Priority) &&
                    Enum.TryParse<NotificationPriority>(request.Priority, true, out var parsedPriority))
                {
                    priorityEnum = parsedPriority;
                }

                var notification = new Notification
                {
                    UserId = request.UserId,
                    Type = request.Type,
                    Title = request.Title,
                    Message = request.Message,
                    ActionUrl = request.ActionUrl,
                    Priority = priorityEnum,
                    IsRead = false,
                    CreatedAt = _timezoneService.Now,
                    CreatedBy = createdBy,
                    ExpiryDate = request.ExpiryDate
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // ✅ FIXED: Handle nullable outside of expression tree
                var priorityString = notification.Priority?.ToString() ?? "Normal";

                return new NotificationDto
                {
                    Id = notification.Id,
                    Type = notification.Type,
                    Title = notification.Title,
                    Message = notification.Message,
                    ActionUrl = notification.ActionUrl,
                    ActionText = notification.ActionText,
                    IsRead = notification.IsRead,
                    Priority = priorityString, // ✅ Use pre-calculated string
                    CreatedAt = notification.CreatedAt,
                    ReadAt = notification.ReadAt,
                    TimeAgo = notification.TimeAgo,
                    IsExpired = notification.IsExpired
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification");
                throw;
            }
        }

        public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
        {
            try
            {
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == notificationId &&
                                            (n.UserId == userId || n.UserId == null));

                if (notification == null) return false;

                notification.IsRead = true;
                notification.ReadAt = _timezoneService.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read: {NotificationId}", notificationId);
                throw;
            }
        }

        public async Task<bool> MarkAllAsReadAsync(int userId)
        {
            try
            {
                var notifications = await _context.Notifications
                    .Where(n => (n.UserId == userId || n.UserId == null) && !n.IsRead)
                    .ToListAsync();

                var readTime = _timezoneService.Now;

                foreach (var notification in notifications)
                {
                    notification.IsRead = true;
                    notification.ReadAt = readTime;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> DeleteNotificationAsync(int notificationId, int userId)
        {
            try
            {
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == notificationId &&
                                            (n.UserId == userId || n.UserId == null));

                if (notification == null) return false;

                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification: {NotificationId}", notificationId);
                throw;
            }
        }

        public async Task<bool> CreateLowStockNotificationAsync(int productId, int currentStock)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.Id == productId);

                if (product == null) return false;

                var today = _timezoneService.Today;
                var todayStart = today;
                var todayEnd = today.AddDays(1).AddTicks(-1);

                var existingNotification = await _context.Notifications
                    .AnyAsync(n => n.Type == "low_stock" &&
                                 n.Message.Contains(product.Name) &&
                                 n.CreatedAt >= todayStart && n.CreatedAt <= todayEnd);

                if (existingNotification) return false;

                var notification = new Notification
                {
                    UserId = null,
                    Type = "low_stock",
                    Title = "Stok Produk Rendah",
                    Message = $"Stok {product.Name} tersisa {currentStock} unit. Segera lakukan restok.",
                    ActionUrl = $"/inventory/products/{productId}",
                    Priority = NotificationPriority.High,
                    IsRead = false,
                    CreatedAt = _timezoneService.Now,
                    CreatedBy = "System"
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating low stock notification for product: {ProductId}", productId);
                throw;
            }
        }

        public async Task<bool> CreateMonthlyRevenueNotificationAsync(decimal revenue, DateTime month)
        {
            try
            {
                var monthName = month.ToString("MMMM yyyy");
                var formattedRevenue = revenue.ToString("C0", new System.Globalization.CultureInfo("id-ID"));

                var notification = new Notification
                {
                    UserId = null,
                    Type = "monthly_revenue",
                    Title = "Laporan Pendapatan Bulanan",
                    Message = $"Pendapatan bulan {monthName}: {formattedRevenue}",
                    ActionUrl = $"/dashboard/reports?month={month:yyyy-MM}",
                    Priority = NotificationPriority.Normal,
                    IsRead = false,
                    CreatedAt = _timezoneService.Now,
                    CreatedBy = "System"
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating monthly revenue notification");
                throw;
            }
        }

        public async Task<bool> CreateInventoryAuditNotificationAsync()
        {
            try
            {
                var notification = new Notification
                {
                    UserId = null,
                    Type = "inventory_audit",
                    Title = "Audit Inventori Diperlukan",
                    Message = "Saatnya melakukan audit inventori bulanan. Silakan periksa dan sesuaikan stok fisik.",
                    ActionUrl = "/inventory/audit",
                    Priority = NotificationPriority.High,
                    IsRead = false,
                    CreatedAt = _timezoneService.Now,
                    CreatedBy = "System"
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating inventory audit notification");
                throw;
            }
        }

        public async Task<bool> CreateSystemMaintenanceNotificationAsync(DateTime scheduledTime, string message)
        {
            try
            {
                var formattedTime = scheduledTime.ToString("dd MMM yyyy HH:mm");

                var notification = new Notification
                {
                    UserId = null,
                    Type = "system_maintenance",
                    Title = "Pemeliharaan Sistem Terjadwal",
                    Message = $"Pemeliharaan sistem akan dilakukan pada {formattedTime}. {message}",
                    ActionUrl = null,
                    Priority = NotificationPriority.High,
                    IsRead = false,
                    CreatedAt = _timezoneService.Now,
                    CreatedBy = "System"
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating system maintenance notification");
                throw;
            }
        }

        public async Task<bool> BroadcastToAllUsersAsync(string type, string title, string message, string? actionUrl = null)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = null,
                    Type = type,
                    Title = title,
                    Message = message,
                    ActionUrl = actionUrl,
                    Priority = NotificationPriority.Normal,
                    IsRead = false,
                    CreatedAt = _timezoneService.Now,
                    CreatedBy = "System"
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting notification to all users");
                throw;
            }
        }

        public async Task<bool> BroadcastToRoleAsync(string role, string type, string title, string message, string? actionUrl = null)
        {
            try
            {
                var users = await _context.Users
                    .Where(u => u.Role == role && u.IsActive)
                    .Select(u => u.Id)
                    .ToListAsync();

                var createdAt = _timezoneService.Now;

                var notifications = users.Select(userId => new Notification
                {
                    UserId = userId,
                    Type = type,
                    Title = title,
                    Message = message,
                    ActionUrl = actionUrl,
                    Priority = NotificationPriority.Normal,
                    IsRead = false,
                    CreatedAt = createdAt,
                    CreatedBy = "System"
                }).ToList();

                _context.Notifications.AddRange(notifications);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting notification to role: {Role}", role);
                throw;
            }
        }

        public async Task<NotificationSettingsDto> GetUserSettingsAsync(int userId)
        {
            try
            {
                var settings = await _context.UserNotificationSettings
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (settings == null)
                {
                    return new NotificationSettingsDto
                    {
                        EmailEnabled = true,
                        EmailLowStock = true,
                        EmailMonthlyReport = true,
                        EmailSystemUpdates = true,
                        InAppEnabled = true,
                        InAppLowStock = true,
                        InAppSales = true,
                        InAppSystem = true,
                        LowStockThreshold = 10,
                        QuietHoursStart = new TimeSpan(22, 0, 0),
                        QuietHoursEnd = new TimeSpan(6, 0, 0)
                    };
                }

                return new NotificationSettingsDto
                {
                    EmailEnabled = settings.EmailEnabled,
                    EmailLowStock = settings.EmailLowStock,
                    EmailMonthlyReport = settings.EmailMonthlyReport,
                    EmailSystemUpdates = settings.EmailSystemUpdates,
                    InAppEnabled = settings.InAppEnabled,
                    InAppLowStock = settings.InAppLowStock,
                    InAppSales = settings.InAppSales,
                    InAppSystem = settings.InAppSystem,
                    LowStockThreshold = settings.LowStockThreshold,
                    QuietHoursStart = settings.QuietHoursStart,
                    QuietHoursEnd = settings.QuietHoursEnd
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user notification settings: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> UpdateUserSettingsAsync(int userId, UpdateNotificationSettingsRequest request)
        {
            try
            {
                var settings = await _context.UserNotificationSettings
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                var currentTime = _timezoneService.Now;

                if (settings == null)
                {
                    settings = new UserNotificationSettings
                    {
                        UserId = userId,
                        CreatedAt = currentTime
                    };
                    _context.UserNotificationSettings.Add(settings);
                }

                settings.EmailEnabled = request.EmailEnabled;
                settings.EmailLowStock = request.EmailLowStock;
                settings.EmailMonthlyReport = request.EmailMonthlyReport;
                settings.EmailSystemUpdates = request.EmailSystemUpdates;
                settings.InAppEnabled = request.InAppEnabled;
                settings.InAppLowStock = request.InAppLowStock;
                settings.InAppSales = request.InAppSales;
                settings.InAppSystem = request.InAppSystem;
                settings.LowStockThreshold = request.LowStockThreshold;
                settings.QuietHoursStart = request.QuietHoursStart;
                settings.QuietHoursEnd = request.QuietHoursEnd;
                settings.UpdatedAt = currentTime;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user notification settings: {UserId}", userId);
                throw;
            }
        }

        public async Task<int> CleanupExpiredNotificationsAsync()
        {
            try
            {
                var expiredDate = _timezoneService.Now.AddDays(-90);

                var expiredNotifications = await _context.Notifications
                    .Where(n => n.CreatedAt < expiredDate && n.IsRead)
                    .ToListAsync();

                _context.Notifications.RemoveRange(expiredNotifications);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cleaned up {Count} expired notifications", expiredNotifications.Count);
                return expiredNotifications.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired notifications");
                throw;
            }
        }

        public async Task<int> ArchiveOldNotificationsAsync(int daysOld = 30)
        {
            try
            {
                var archiveDate = _timezoneService.Now.AddDays(-daysOld);
                var archiveTime = _timezoneService.Now;

                var oldNotifications = await _context.Notifications
                    .Where(n => n.CreatedAt < archiveDate && n.IsRead)
                    .ToListAsync();

                foreach (var notification in oldNotifications)
                {
                    notification.IsArchived = true;
                    notification.ArchivedAt = archiveTime;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Archived {Count} old notifications", oldNotifications.Count);
                return oldNotifications.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving old notifications");
                throw;
            }
        }

        public async Task<bool> CreateOutOfStockNotificationAsync(int productId)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null) return false;

                var notification = new Notification
                {
                    Type = "OUT_OF_STOCK",
                    Title = $"Stok Habis: {product.Name}",
                    Message = $"Stok produk {product.Name} telah habis.",
                    Priority = NotificationPriority.High,
                    CreatedAt = _timezoneService.Now
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating out of stock notification for product: {ProductId}", productId);
                throw;
            }
        }

        public async Task<bool> CreateSaleCompletedNotificationAsync(int saleId, string saleNumber, decimal totalAmount)
        {
            try
            {
                var notification = new Notification
                {
                    Type = "SALE_COMPLETED",
                    Title = $"Penjualan Selesai: {saleNumber}",
                    Message = $"Transaksi penjualan #{saleNumber} telah selesai. Total: Rp{totalAmount:N0}",
                    Priority = NotificationPriority.Normal,
                    CreatedAt = _timezoneService.Now
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sale completed notification for sale: {SaleId}", saleId);
                throw;
            }
        }

        public async Task<bool> CreateStockAdjustmentNotificationAsync(int productId, int quantity, string notes)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null) return false;

                var notification = new Notification
                {
                    Type = "ADJUSTMENT",
                    Title = $"Penyesuaian Stok: {product.Name}",
                    Message = $"Stok produk {product.Name} disesuaikan sebanyak {quantity}. Catatan: {notes}",
                    Priority = NotificationPriority.Normal,
                    CreatedAt = _timezoneService.Now
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating stock adjustment notification for product: {ProductId}", productId);
                throw;
            }
        }

        // ✅ ADDED: Sale Cancelled Notification
        public async Task<bool> CreateSaleCancelledNotificationAsync(int saleId, string saleNumber, decimal totalAmount, string reason)
        {
            try
            {
                var notification = new Notification
                {
                    Type = "SALE_CANCELLED",
                    Title = $"Penjualan Dibatalkan: {saleNumber}",
                    Message = $"Transaksi penjualan #{saleNumber} senilai Rp{totalAmount:N0} telah dibatalkan. Alasan: {reason}",
                    Priority = NotificationPriority.High,
                    IsRead = false,
                    CreatedAt = _timezoneService.Now,
                    CreatedBy = "System",
                    UserId = null, // Broadcast to all users
                    ActionUrl = $"/sales/{saleId}"
                };
                
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sale cancelled notification for sale: {SaleId}", saleId);
                throw;
            }
        }

        // ✅ ADDED: Sale Refunded Notification
        public async Task<bool> CreateSaleRefundedNotificationAsync(int saleId, string saleNumber, decimal totalAmount, string reason)
        {
            try
            {
                var notification = new Notification
                {
                    Type = "SALE_REFUNDED",
                    Title = $"Penjualan Di-refund: {saleNumber}",
                    Message = $"Transaksi penjualan #{saleNumber} senilai Rp{totalAmount:N0} telah di-refund. Alasan: {reason}",
                    Priority = NotificationPriority.High,
                    IsRead = false,
                    CreatedAt = _timezoneService.Now,
                    CreatedBy = "System",
                    UserId = null, // Broadcast to all users
                    ActionUrl = $"/sales/{saleId}"
                };
                
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sale refunded notification for sale: {SaleId}", saleId);
                throw;
            }
        }
    }
}