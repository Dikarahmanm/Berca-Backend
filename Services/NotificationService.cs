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
                        Priority = n.Priority != null ? n.Priority.Value.ToString() : "Normal",
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
                        Priority = n.Priority != null ? n.Priority.Value.ToString() : "Normal",
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

        public async Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto request, string? createdBy = null)
        {
            // Convert CreateNotificationDto to CreateNotificationRequest and call the base method
            var baseRequest = new CreateNotificationRequest
            {
                UserId = request.UserId,
                Type = request.Type,
                Title = request.Title,
                Message = request.Message,
                Priority = request.Priority,
                ActionUrl = request.ActionUrl,
                ActionText = request.ActionText,
                ExpiryDate = request.ExpiryDate
            };

            return await CreateNotificationAsync(baseRequest, createdBy);
        }

        public async Task<NotificationDto> CreateSystemNotificationAsync(CreateNotificationDto request, string? createdBy = null)
        {
            // System notifications go to all admins/managers
            request.UserId = null; // Broadcast notification
            return await CreateNotificationAsync(request, createdBy ?? "System");
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
                    ActionUrl = $"/sales/{saleId}", // ✅ ADDED: Link ke detail transaksi
                    ActionText = "Lihat Detail", // ✅ ADDED: Text untuk button
                    RelatedEntity = "Sale", // ✅ ADDED: Entity type
                    RelatedEntityId = saleId, // ✅ ADDED: Entity ID
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

        // ==================== EXPIRY NOTIFICATION METHODS ==================== //

        public async Task<bool> CreateExpiryWarningNotificationAsync(int productId, string productName, string batchNumber, DateTime expiryDate, int currentStock, int? branchId = null)
        {
            try
            {
                var formattedExpiryDate = expiryDate.ToString("dd MMM yyyy");
                var daysUntilExpiry = (expiryDate - _timezoneService.Today).Days;

                var notification = new Notification
                {
                    Type = ExpiryNotificationTypes.EXPIRY_WARNING,
                    Title = "Produk Akan Kedaluwarsa",
                    Message = $"Produk {productName} (Batch: {batchNumber}) akan kedaluwarsa dalam {daysUntilExpiry} hari ({formattedExpiryDate}). Stok tersisa: {currentStock} unit.",
                    Priority = NotificationPriority.High,
                    IsRead = false,
                    CreatedAt = _timezoneService.Now,
                    CreatedBy = "System",
                    UserId = null, // Broadcast to all users
                    ActionUrl = $"/inventory/products/{productId}",
                    ActionText = "Lihat Produk",
                    RelatedEntity = "Product",
                    RelatedEntityId = productId
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating expiry warning notification for product: {ProductId}, Batch: {BatchNumber}", productId, batchNumber);
                throw;
            }
        }

        public async Task<bool> CreateExpiryUrgentNotificationAsync(int productId, string productName, string batchNumber, DateTime expiryDate, int currentStock, int? branchId = null)
        {
            try
            {
                var formattedExpiryDate = expiryDate.ToString("dd MMM yyyy");
                var daysUntilExpiry = (expiryDate - _timezoneService.Today).Days;

                var notification = new Notification
                {
                    Type = ExpiryNotificationTypes.EXPIRY_URGENT,
                    Title = "URGENT: Produk Segera Kedaluwarsa",
                    Message = $"URGENT: Produk {productName} (Batch: {batchNumber}) akan kedaluwarsa dalam {daysUntilExpiry} hari ({formattedExpiryDate}). Stok tersisa: {currentStock} unit. Segera ambil tindakan!",
                    Priority = NotificationPriority.Critical,
                    IsRead = false,
                    CreatedAt = _timezoneService.Now,
                    CreatedBy = "System",
                    UserId = null, // Broadcast to all users
                    ActionUrl = $"/inventory/products/{productId}",
                    ActionText = "Tindakan Segera",
                    RelatedEntity = "Product",
                    RelatedEntityId = productId
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating expiry urgent notification for product: {ProductId}, Batch: {BatchNumber}", productId, batchNumber);
                throw;
            }
        }

        public async Task<bool> CreateExpiryExpiredNotificationAsync(int productId, string productName, string batchNumber, DateTime expiryDate, int currentStock, int? branchId = null)
        {
            try
            {
                var formattedExpiryDate = expiryDate.ToString("dd MMM yyyy");
                var daysExpired = (_timezoneService.Today - expiryDate).Days;

                var notification = new Notification
                {
                    Type = ExpiryNotificationTypes.EXPIRY_EXPIRED,
                    Title = "KRITIS: Produk Telah Kedaluwarsa",
                    Message = $"KRITIS: Produk {productName} (Batch: {batchNumber}) telah kedaluwarsa {daysExpired} hari yang lalu ({formattedExpiryDate}). Stok tersisa: {currentStock} unit. Segera lakukan pembuangan!",
                    Priority = NotificationPriority.Critical,
                    IsRead = false,
                    CreatedAt = _timezoneService.Now,
                    CreatedBy = "System",
                    UserId = null, // Broadcast to all users
                    ActionUrl = $"/inventory/products/{productId}",
                    ActionText = "Buang Segera",
                    RelatedEntity = "Product",
                    RelatedEntityId = productId
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating expiry expired notification for product: {ProductId}, Batch: {BatchNumber}", productId, batchNumber);
                throw;
            }
        }

        public async Task<bool> CreateExpiryRequiredNotificationAsync(int productId, string productName, string categoryName)
        {
            try
            {
                var notification = new Notification
                {
                    Type = ExpiryNotificationTypes.EXPIRY_REQUIRED,
                    Title = "Tanggal Kedaluwarsa Diperlukan",
                    Message = $"Produk {productName} dalam kategori {categoryName} memerlukan tanggal kedaluwarsa. Silakan tambahkan batch dengan tanggal kedaluwarsa.",
                    Priority = NotificationPriority.Normal,
                    IsRead = false,
                    CreatedAt = _timezoneService.Now,
                    CreatedBy = "System",
                    UserId = null, // Broadcast to all users
                    ActionUrl = $"/inventory/products/{productId}/batches",
                    ActionText = "Tambah Batch",
                    RelatedEntity = "Product",
                    RelatedEntityId = productId
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating expiry required notification for product: {ProductId}", productId);
                throw;
            }
        }

        public async Task<bool> CreateDisposalCompletedNotificationAsync(int disposedCount, decimal valueLost, string disposalMethod, int? branchId = null)
        {
            try
            {
                var formattedValueLost = valueLost.ToString("C0", new System.Globalization.CultureInfo("id-ID"));

                var notification = new Notification
                {
                    Type = ExpiryNotificationTypes.DISPOSAL_COMPLETED,
                    Title = "Pembuangan Produk Kedaluwarsa Selesai",
                    Message = $"Pembuangan produk kedaluwarsa telah selesai. {disposedCount} item dibuang dengan metode '{disposalMethod}'. Total nilai yang hilang: {formattedValueLost}.",
                    Priority = NotificationPriority.High,
                    IsRead = false,
                    CreatedAt = _timezoneService.Now,
                    CreatedBy = "System",
                    UserId = null, // Broadcast to all users
                    ActionUrl = "/inventory/expired",
                    ActionText = "Lihat Laporan",
                    RelatedEntity = "Disposal",
                    RelatedEntityId = null
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating disposal completed notification");
                throw;
            }
        }

        public async Task<bool> BroadcastDailyExpirySummaryAsync(int expiringCount, int expiredCount, decimal valueAtRisk, decimal valueLost, int? branchId = null)
        {
            try
            {
                var formattedValueAtRisk = valueAtRisk.ToString("C0", new System.Globalization.CultureInfo("id-ID"));
                var formattedValueLost = valueLost.ToString("C0", new System.Globalization.CultureInfo("id-ID"));
                var today = _timezoneService.Today.ToString("dd MMM yyyy");

                var notification = new Notification
                {
                    Type = ExpiryNotificationTypes.EXPIRY_DAILY_SUMMARY,
                    Title = $"Ringkasan Harian Kedaluwarsa - {today}",
                    Message = $"Ringkasan kedaluwarsa hari ini: {expiringCount} produk akan kedaluwarsa, {expiredCount} produk sudah kedaluwarsa. Nilai berisiko: {formattedValueAtRisk}, Nilai hilang: {formattedValueLost}.",
                    Priority = NotificationPriority.High,
                    IsRead = false,
                    CreatedAt = _timezoneService.Now,
                    CreatedBy = "System",
                    UserId = null, // Broadcast to managers and admins
                    ActionUrl = "/dashboard/expiry",
                    ActionText = "Lihat Detail",
                    RelatedEntity = "ExpirySummary",
                    RelatedEntityId = null
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Also send to specific roles (Manager and Admin)
                await BroadcastToRoleAsync("Manager", ExpiryNotificationTypes.EXPIRY_DAILY_SUMMARY, 
                    notification.Title, notification.Message, notification.ActionUrl);
                await BroadcastToRoleAsync("Admin", ExpiryNotificationTypes.EXPIRY_DAILY_SUMMARY, 
                    notification.Title, notification.Message, notification.ActionUrl);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting daily expiry summary");
                throw;
            }
        }

        public async Task<bool> CreateFifoRecommendationNotificationAsync(int productId, string productName, string recommendedAction, int? branchId = null)
        {
            try
            {
                var notification = new Notification
                {
                    Type = ExpiryNotificationTypes.FIFO_RECOMMENDATION,
                    Title = "Rekomendasi FIFO",
                    Message = $"Rekomendasi FIFO untuk produk {productName}: {recommendedAction}. Silakan prioritaskan penjualan batch yang akan segera kedaluwarsa.",
                    Priority = NotificationPriority.Normal,
                    IsRead = false,
                    CreatedAt = _timezoneService.Now,
                    CreatedBy = "System",
                    UserId = null, // Broadcast to all users
                    ActionUrl = $"/inventory/products/{productId}/fifo",
                    ActionText = "Lihat FIFO",
                    RelatedEntity = "Product",
                    RelatedEntityId = productId
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating FIFO recommendation notification for product: {ProductId}", productId);
                throw;
            }
        }

        // ==================== MEMBER CREDIT NOTIFICATION IMPLEMENTATIONS ==================== //

        public async Task<bool> CreateMemberDebtOverdueNotificationAsync(int memberId, string memberName, decimal overdueAmount, int daysOverdue)
        {
            try
            {
                var urgencyLevel = daysOverdue switch
                {
                    <= 7 => "⚠️",
                    <= 30 => "🔶",
                    _ => "🔴"
                };

                var formattedAmount = overdueAmount.ToString("C0", new System.Globalization.CultureInfo("id-ID"));
                var priority = daysOverdue > 30 ? NotificationPriority.Critical : NotificationPriority.High;

                var notification = new Notification
                {
                    Type = MemberCreditNotificationTypes.MEMBER_DEBT_OVERDUE,
                    Title = $"{urgencyLevel} Hutang Member Terlambat",
                    Message = $"{memberName} memiliki tunggakan {formattedAmount} yang sudah terlambat {daysOverdue} hari. Segera lakukan penagihan.",
                    Priority = priority,
                    IsRead = false,
                    CreatedAt = _timezoneService.Now,
                    CreatedBy = "System",
                    UserId = null, // Broadcast to managers
                    ActionUrl = $"/dashboard/member-credit/{memberId}",
                    ActionText = "Lihat Detail",
                    RelatedEntity = "Member",
                    RelatedEntityId = memberId
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Broadcast to relevant roles
                await BroadcastToRoleAsync("Admin", notification.Type, notification.Title, notification.Message, notification.ActionUrl);
                await BroadcastToRoleAsync("Manager", notification.Type, notification.Title, notification.Message, notification.ActionUrl);

                _logger.LogWarning("Created overdue debt notification for member {MemberName}: {Amount}, {Days} days", memberName, formattedAmount, daysOverdue);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create overdue debt notification for member {MemberId}", memberId);
                return false;
            }
        }

        public async Task<bool> CreateMemberCreditLimitWarningNotificationAsync(int memberId, string memberName, decimal currentDebt, decimal creditLimit)
        {
            try
            {
                var utilizationPercentage = (currentDebt / creditLimit) * 100;
                var formattedDebt = currentDebt.ToString("C0", new System.Globalization.CultureInfo("id-ID"));
                var formattedLimit = creditLimit.ToString("C0", new System.Globalization.CultureInfo("id-ID"));

                var notification = new Notification
                {
                    Type = MemberCreditNotificationTypes.MEMBER_CREDIT_LIMIT_WARNING,
                    Title = "⚠️ Batas Kredit Member Mendekati Limit",
                    Message = $"{memberName} telah menggunakan {utilizationPercentage:F1}% dari batas kredit ({formattedDebt} dari {formattedLimit}). Perhatikan pembayaran selanjutnya.",
                    Priority = NotificationPriority.Normal,
                    IsRead = false,
                    CreatedAt = _timezoneService.Now,
                    CreatedBy = "System",
                    UserId = null, // Broadcast to staff
                    ActionUrl = $"/dashboard/member-credit/{memberId}",
                    ActionText = "Lihat Detail",
                    RelatedEntity = "Member",
                    RelatedEntityId = memberId
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Broadcast to relevant roles
                await BroadcastToRoleAsync("Admin", notification.Type, notification.Title, notification.Message, notification.ActionUrl);
                await BroadcastToRoleAsync("Manager", notification.Type, notification.Title, notification.Message, notification.ActionUrl);
                await BroadcastToRoleAsync("User", notification.Type, notification.Title, notification.Message, notification.ActionUrl);

                _logger.LogInformation("Created credit limit warning for member {MemberName}: {Utilization:F1}% utilized", memberName, utilizationPercentage);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create credit limit warning for member {MemberId}", memberId);
                return false;
            }
        }

        public async Task<bool> CreatePaymentReceivedNotificationAsync(int memberId, string memberName, decimal paymentAmount)
        {
            try
            {
                var formattedAmount = paymentAmount.ToString("C0", new System.Globalization.CultureInfo("id-ID"));

                var notification = new Notification
                {
                    Type = MemberCreditNotificationTypes.PAYMENT_RECEIVED,
                    Title = "✅ Pembayaran Diterima",
                    Message = $"Pembayaran sebesar {formattedAmount} dari {memberName} telah diterima dan diproses.",
                    Priority = NotificationPriority.Normal,
                    IsRead = false,
                    CreatedAt = _timezoneService.Now,
                    CreatedBy = "System",
                    UserId = null, // Broadcast to managers
                    ActionUrl = $"/dashboard/member-credit/{memberId}",
                    ActionText = "Lihat Detail",
                    RelatedEntity = "Member",
                    RelatedEntityId = memberId
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created payment received notification for member {MemberName}: {Amount}", memberName, formattedAmount);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create payment received notification for member {MemberId}", memberId);
                return false;
            }
        }

        public async Task<bool> CreateCreditStatusChangeNotificationAsync(int memberId, string memberName, string oldStatus, string newStatus)
        {
            try
            {
                var statusIcon = newStatus switch
                {
                    "Good" => "✅",
                    "Warning" => "⚠️",
                    "Overdue" => "🔶",
                    "Suspended" => "🔴",
                    "Blacklisted" => "❌",
                    _ => "ℹ️"
                };

                var notification = new Notification
                {
                    Type = MemberCreditNotificationTypes.CREDIT_STATUS_CHANGE,
                    Title = $"{statusIcon} Status Kredit Member Berubah",
                    Message = $"Status kredit {memberName} telah berubah dari '{oldStatus}' menjadi '{newStatus}'. Silakan tinjau akun member ini.",
                    Priority = newStatus == "Suspended" || newStatus == "Blacklisted" ? NotificationPriority.High : NotificationPriority.Normal,
                    IsRead = false,
                    CreatedAt = _timezoneService.Now,
                    CreatedBy = "System",
                    UserId = null, // Broadcast to managers
                    ActionUrl = $"/dashboard/member-credit/{memberId}",
                    ActionText = "Lihat Detail",
                    RelatedEntity = "Member",
                    RelatedEntityId = memberId
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Broadcast to relevant roles
                await BroadcastToRoleAsync("Admin", notification.Type, notification.Title, notification.Message, notification.ActionUrl);
                await BroadcastToRoleAsync("Manager", notification.Type, notification.Title, notification.Message, notification.ActionUrl);

                _logger.LogInformation("Created credit status change notification for member {MemberName}: {OldStatus} -> {NewStatus}", memberName, oldStatus, newStatus);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create credit status change notification for member {MemberId}", memberId);
                return false;
            }
        }

        public async Task<bool> BroadcastDailyCreditSummaryAsync(int overdueMembers, decimal totalOverdueAmount, int newDefaulters)
        {
            try
            {
                var formattedAmount = totalOverdueAmount.ToString("C0", new System.Globalization.CultureInfo("id-ID"));
                var today = _timezoneService.Today.ToString("dd MMM yyyy");

                var summaryMessage = $"""
                    📅 Ringkasan Kredit Harian - {today}
                    
                    🔴 Member dengan tunggakan: {overdueMembers}
                    💰 Total tunggakan: {formattedAmount}
                    ⚡ Penunggak baru hari ini: {newDefaulters}
                    
                    {(overdueMembers > 0 ? "⚡ Perlu tindakan penagihan!" : "✅ Tidak ada tunggakan baru")}
                    """;

                var notification = new Notification
                {
                    Type = MemberCreditNotificationTypes.DAILY_CREDIT_SUMMARY,
                    Title = "📊 Ringkasan Kredit Harian",
                    Message = summaryMessage,
                    Priority = overdueMembers > 0 || newDefaulters > 0 ? NotificationPriority.High : NotificationPriority.Normal,
                    IsRead = false,
                    CreatedAt = _timezoneService.Now,
                    CreatedBy = "System",
                    UserId = null, // Broadcast to managers
                    ActionUrl = "/dashboard/member-credit",
                    ActionText = "Lihat Detail",
                    RelatedEntity = "CreditSummary",
                    RelatedEntityId = null
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Broadcast to relevant roles
                await BroadcastToRoleAsync("Admin", notification.Type, notification.Title, notification.Message, notification.ActionUrl);
                await BroadcastToRoleAsync("Manager", notification.Type, notification.Title, notification.Message, notification.ActionUrl);

                _logger.LogInformation("Broadcasted daily credit summary: {OverdueMembers} overdue, {TotalAmount} total", overdueMembers, formattedAmount);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast daily credit summary");
                return false;
            }
        }

        // ==================== SUPPLIER & FACTURE NOTIFICATION IMPLEMENTATIONS ==================== //

        public async Task<bool> CreateFactureDueTodayNotificationAsync(string factureNumber, string supplierName, decimal amount)
        {
            try
            {
                var formattedAmount = amount.ToString("C0", new System.Globalization.CultureInfo("id-ID"));

                var notification = new Notification
                {
                    Type = SupplierNotificationTypes.FACTURE_DUE_TODAY,
                    Title = "💰 Pembayaran Supplier Jatuh Tempo Hari Ini",
                    Message = $"Facture {factureNumber} dari {supplierName} senilai {formattedAmount} jatuh tempo hari ini. Segera proses pembayaran.",
                    Priority = NotificationPriority.High,
                    IsRead = false,
                    CreatedAt = _timezoneService.Now,
                    CreatedBy = "System",
                    UserId = null, // Broadcast to managers
                    ActionUrl = $"/dashboard/facture?search={factureNumber}",
                    ActionText = "Proses Pembayaran",
                    RelatedEntity = "Facture",
                    RelatedEntityId = null
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Broadcast to relevant roles
                await BroadcastToRoleAsync("Admin", notification.Type, notification.Title, notification.Message, notification.ActionUrl);
                await BroadcastToRoleAsync("Manager", notification.Type, notification.Title, notification.Message, notification.ActionUrl);

                _logger.LogInformation("Created due today notification for facture {FactureNumber} from {SupplierName}", factureNumber, supplierName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create due today notification for facture {FactureNumber}", factureNumber);
                return false;
            }
        }

        public async Task<bool> CreateFactureOverdueNotificationAsync(string factureNumber, string supplierName, decimal amount, int daysOverdue)
        {
            try
            {
                var formattedAmount = amount.ToString("C0", new System.Globalization.CultureInfo("id-ID"));

                var notification = new Notification
                {
                    Type = SupplierNotificationTypes.FACTURE_OVERDUE,
                    Title = "🔴 OVERDUE: Pembayaran Supplier Terlambat",
                    Message = $"Facture {factureNumber} dari {supplierName} senilai {formattedAmount} sudah terlambat {daysOverdue} hari. SEGERA bayar untuk menjaga hubungan supplier!",
                    Priority = NotificationPriority.Critical,
                    IsRead = false,
                    CreatedAt = _timezoneService.Now,
                    CreatedBy = "System",
                    UserId = null, // Broadcast to managers
                    ActionUrl = $"/dashboard/facture?search={factureNumber}",
                    ActionText = "Bayar Sekarang",
                    RelatedEntity = "Facture",
                    RelatedEntityId = null
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Broadcast to relevant roles
                await BroadcastToRoleAsync("Admin", notification.Type, notification.Title, notification.Message, notification.ActionUrl);
                await BroadcastToRoleAsync("Manager", notification.Type, notification.Title, notification.Message, notification.ActionUrl);

                _logger.LogError("Created overdue notification for facture {FactureNumber}: {Days} days late", factureNumber, daysOverdue);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create overdue notification for facture {FactureNumber}", factureNumber);
                return false;
            }
        }

        public async Task<bool> CreateSupplierPaymentCompletedNotificationAsync(string supplierName, decimal amount, string factureNumber)
        {
            try
            {
                var formattedAmount = amount.ToString("C0", new System.Globalization.CultureInfo("id-ID"));

                var notification = new Notification
                {
                    Type = SupplierNotificationTypes.SUPPLIER_PAYMENT_COMPLETED,
                    Title = "✅ Pembayaran Supplier Selesai",
                    Message = $"Pembayaran sebesar {formattedAmount} untuk {supplierName} (Facture: {factureNumber}) telah berhasil diproses.",
                    Priority = NotificationPriority.Normal,
                    IsRead = false,
                    CreatedAt = _timezoneService.Now,
                    CreatedBy = "System",
                    UserId = null, // Broadcast to managers
                    ActionUrl = $"/dashboard/facture?search={factureNumber}",
                    ActionText = "Lihat Detail",
                    RelatedEntity = "Facture",
                    RelatedEntityId = null
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created payment completed notification for supplier {SupplierName}: {Amount}", supplierName, formattedAmount);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create payment completed notification for supplier {SupplierName}", supplierName);
                return false;
            }
        }

        public async Task<bool> CreateFactureApprovalRequiredNotificationAsync(string factureNumber, string supplierName, decimal amount, int requestedByUserId)
        {
            try
            {
                var formattedAmount = amount.ToString("C0", new System.Globalization.CultureInfo("id-ID"));

                var notification = new Notification
                {
                    Type = SupplierNotificationTypes.FACTURE_APPROVAL_REQUIRED,
                    Title = "📋 Persetujuan Facture Diperlukan",
                    Message = $"Facture {factureNumber} dari {supplierName} senilai {formattedAmount} memerlukan persetujuan sebelum pembayaran.",
                    Priority = NotificationPriority.High,
                    IsRead = false,
                    CreatedAt = _timezoneService.Now,
                    CreatedBy = "System",
                    UserId = null, // Broadcast to managers
                    ActionUrl = $"/dashboard/facture?search={factureNumber}",
                    ActionText = "Review & Setujui",
                    RelatedEntity = "Facture",
                    RelatedEntityId = null
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Broadcast to relevant roles
                await BroadcastToRoleAsync("Admin", notification.Type, notification.Title, notification.Message, notification.ActionUrl);
                await BroadcastToRoleAsync("Manager", notification.Type, notification.Title, notification.Message, notification.ActionUrl);

                _logger.LogInformation("Created approval required notification for facture {FactureNumber}", factureNumber);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create approval required notification for facture {FactureNumber}", factureNumber);
                return false;
            }
        }

        public async Task<bool> CreateSupplierCreditWarningNotificationAsync(string supplierName, decimal currentOutstanding, decimal creditLimit)
        {
            try
            {
                var utilizationPercentage = (currentOutstanding / creditLimit) * 100;
                var formattedOutstanding = currentOutstanding.ToString("C0", new System.Globalization.CultureInfo("id-ID"));
                var formattedLimit = creditLimit.ToString("C0", new System.Globalization.CultureInfo("id-ID"));

                var notification = new Notification
                {
                    Type = SupplierNotificationTypes.SUPPLIER_CREDIT_WARNING,
                    Title = "⚠️ Batas Kredit Supplier Mendekati Limit",
                    Message = $"Outstanding dengan {supplierName} telah mencapai {utilizationPercentage:F1}% dari batas kredit ({formattedOutstanding} dari {formattedLimit}). Perhatikan pembayaran yang tertunda.",
                    Priority = NotificationPriority.Normal,
                    IsRead = false,
                    CreatedAt = _timezoneService.Now,
                    CreatedBy = "System",
                    UserId = null, // Broadcast to managers
                    ActionUrl = $"/dashboard/suppliers?search={supplierName}",
                    ActionText = "Lihat Detail",
                    RelatedEntity = "Supplier",
                    RelatedEntityId = null
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Broadcast to relevant roles
                await BroadcastToRoleAsync("Admin", notification.Type, notification.Title, notification.Message, notification.ActionUrl);
                await BroadcastToRoleAsync("Manager", notification.Type, notification.Title, notification.Message, notification.ActionUrl);

                _logger.LogInformation("Created credit warning for supplier {SupplierName}: {Utilization:F1}% utilized", supplierName, utilizationPercentage);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create credit warning for supplier {SupplierName}", supplierName);
                return false;
            }
        }
    }
}