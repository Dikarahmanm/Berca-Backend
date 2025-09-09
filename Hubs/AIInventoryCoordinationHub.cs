using Microsoft.AspNetCore.SignalR;
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
using Microsoft.AspNetCore.Authorization;
using Berca_Backend.Services;
using Berca_Backend.DTOs;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace Berca_Backend.Hubs
{
    /// <summary>
    /// SignalR hub for real-time AI inventory coordination
    /// Provides live updates, notifications, and coordination status
    /// </summary>
    [Authorize]
public class AIInventoryCoordinationHub : Hub
    {
        private readonly IAIInventoryCoordinationService _aiInventoryService;
        private readonly ILogger<AIInventoryCoordinationHub> _logger;
        
        // Connection tracking
        private static readonly ConcurrentDictionary<string, UserConnection> _connections = new();
        private static readonly ConcurrentDictionary<int, HashSet<string>> _branchSubscriptions = new();
        private static readonly Timer _statusUpdateTimer;

        static AIInventoryCoordinationHub()
        {
            // Initialize periodic status updates every 30 seconds
            _statusUpdateTimer = new Timer(async _ => await BroadcastSystemStatus(), 
                null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        public AIInventoryCoordinationHub(
            IAIInventoryCoordinationService aiInventoryService,
            ILogger<AIInventoryCoordinationHub> logger)
        {
            _aiInventoryService = aiInventoryService;
            _logger = logger;
        }

        // ==================== CONNECTION MANAGEMENT ==================== //

        public override async Task OnConnectedAsync()
        {
            try
            {
                var userId = GetCurrentUserId();
                var userName = GetCurrentUserName();
                var branchId = GetCurrentUserBranchId();

                var connection = new UserConnection
                {
                    UserId = userId,
                    UserName = userName,
                    BranchId = branchId,
                    ConnectedAt = DateTime.UtcNow,
                    LastActivity = DateTime.UtcNow
                };

                _connections[Context.ConnectionId] = connection;

                // Subscribe to branch-specific updates if user has a branch
                if (branchId.HasValue)
                {
                    await SubscribeToBranchUpdates(branchId.Value);
                }

                // Join general coordination updates group
                await Groups.AddToGroupAsync(Context.ConnectionId, "CoordinationUpdates");

                // Send current status to the newly connected user
                var currentStatus = await _aiInventoryService.GetRealtimeCoordinationStatusAsync();
                await Clients.Caller.SendAsync("ReceiveRealtimeStatus", currentStatus);

                _logger.LogInformation("User {UserName} (ID: {UserId}) connected to AI Inventory Coordination Hub", 
                    userName, userId);

                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during connection setup for user");
                throw;
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                if (_connections.TryRemove(Context.ConnectionId, out var connection))
                {
                    // Remove from branch subscriptions
                    if (connection.BranchId.HasValue)
                    {
                        if (_branchSubscriptions.TryGetValue(connection.BranchId.Value, out var connections))
                        {
                            connections.Remove(Context.ConnectionId);
                            if (!connections.Any())
                            {
                                _branchSubscriptions.TryRemove(connection.BranchId.Value, out _);
                            }
                        }
                    }

                    _logger.LogInformation("User {UserName} (ID: {UserId}) disconnected from AI Inventory Coordination Hub", 
                        connection.UserName, connection.UserId);
                }

                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during disconnection cleanup");
            }
        }

        // ==================== SUBSCRIPTION MANAGEMENT ==================== //

        /// <summary>
        /// Subscribe to branch-specific updates
        /// </summary>
        /// <param name="branchId">Branch ID to subscribe to</param>
        [HubMethodName("SubscribeToBranch")]
        public async Task SubscribeToBranchUpdates(int branchId)
        {
            try
            {
                var userId = GetCurrentUserId();
                
                // Verify user has access to this branch
                if (!await HasBranchAccess(userId, branchId))
                {
                    await Clients.Caller.SendAsync("Error", "Access denied to branch");
                    return;
                }

                // Add to branch group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Branch_{branchId}");
                
                // Track subscription
                _branchSubscriptions.AddOrUpdate(
                    branchId,
                    new HashSet<string> { Context.ConnectionId },
                    (key, existing) =>
                    {
                        existing.Add(Context.ConnectionId);
                        return existing;
                    });

                // Update user connection info
                if (_connections.TryGetValue(Context.ConnectionId, out var connection))
                {
                    connection.BranchId = branchId;
                    connection.LastActivity = DateTime.UtcNow;
                }

                _logger.LogInformation("User {UserId} subscribed to branch {BranchId} updates", userId, branchId);
                
                await Clients.Caller.SendAsync("SubscriptionConfirmed", $"Subscribed to Branch {branchId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing to branch {BranchId}", branchId);
                await Clients.Caller.SendAsync("Error", "Failed to subscribe to branch updates");
            }
        }

        /// <summary>
        /// Unsubscribe from branch updates
        /// </summary>
        /// <param name="branchId">Branch ID to unsubscribe from</param>
        [HubMethodName("UnsubscribeFromBranch")]
        public async Task UnsubscribeFromBranchUpdates(int branchId)
        {
            try
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Branch_{branchId}");
                
                if (_branchSubscriptions.TryGetValue(branchId, out var connections))
                {
                    connections.Remove(Context.ConnectionId);
                }

                _logger.LogInformation("User {UserId} unsubscribed from branch {BranchId}", GetCurrentUserId(), branchId);
                
                await Clients.Caller.SendAsync("UnsubscriptionConfirmed", $"Unsubscribed from Branch {branchId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsubscribing from branch {BranchId}", branchId);
                await Clients.Caller.SendAsync("Error", "Failed to unsubscribe from branch updates");
            }
        }

        // ==================== REAL-TIME DATA REQUESTS ==================== //

        /// <summary>
        /// Request current system status
        /// </summary>
        [HubMethodName("RequestSystemStatus")]
        public async Task RequestSystemStatus()
        {
            try
            {
                var status = await _aiInventoryService.GetRealtimeCoordinationStatusAsync();
                await Clients.Caller.SendAsync("ReceiveRealtimeStatus", status);
                
                _logger.LogDebug("System status sent to user {UserId}", GetCurrentUserId());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending system status to user");
                await Clients.Caller.SendAsync("Error", "Failed to get system status");
            }
        }

        /// <summary>
        /// Request AI insights for specific branch
        /// </summary>
        /// <param name="branchId">Branch ID to get insights for</param>
        [HubMethodName("RequestAIInsights")]
        public async Task RequestAIInsights(int? branchId = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                
                // Verify branch access if specified
                if (branchId.HasValue && !await HasBranchAccess(userId, branchId.Value))
                {
                    await Clients.Caller.SendAsync("Error", "Access denied to branch");
                    return;
                }

                var insights = await _aiInventoryService.GenerateAIInsightsAsync(branchId);
                await Clients.Caller.SendAsync("ReceiveAIInsights", insights);
                
                _logger.LogDebug("AI insights sent to user {UserId} for branch {BranchId}", userId, branchId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending AI insights to user for branch {BranchId}", branchId);
                await Clients.Caller.SendAsync("Error", "Failed to get AI insights");
            }
        }

        /// <summary>
        /// Request smart alerts
        /// </summary>
        /// <param name="branchId">Branch ID to filter alerts</param>
        [HubMethodName("RequestSmartAlerts")]
        public async Task RequestSmartAlerts(int? branchId = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                
                if (branchId.HasValue && !await HasBranchAccess(userId, branchId.Value))
                {
                    await Clients.Caller.SendAsync("Error", "Access denied to branch");
                    return;
                }

                var alerts = await _aiInventoryService.GenerateSmartAlertsAsync(branchId);
                await Clients.Caller.SendAsync("ReceiveSmartAlerts", alerts);
                
                _logger.LogDebug("Smart alerts sent to user {UserId} for branch {BranchId}", userId, branchId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending smart alerts to user for branch {BranchId}", branchId);
                await Clients.Caller.SendAsync("Error", "Failed to get smart alerts");
            }
        }

        // ==================== LIVE OPTIMIZATION MONITORING ==================== //

        /// <summary>
        /// Start monitoring optimization progress
        /// </summary>
        [HubMethodName("StartOptimizationMonitoring")]
        public async Task StartOptimizationMonitoring()
        {
            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "OptimizationMonitoring");
                
                if (_connections.TryGetValue(Context.ConnectionId, out var connection))
                {
                    connection.IsMonitoringOptimizations = true;
                }

                await Clients.Caller.SendAsync("OptimizationMonitoringStarted", "Now monitoring optimization progress");
                
                _logger.LogDebug("User {UserId} started optimization monitoring", GetCurrentUserId());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting optimization monitoring");
                await Clients.Caller.SendAsync("Error", "Failed to start optimization monitoring");
            }
        }

        /// <summary>
        /// Stop monitoring optimization progress
        /// </summary>
        [HubMethodName("StopOptimizationMonitoring")]
        public async Task StopOptimizationMonitoring()
        {
            try
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "OptimizationMonitoring");
                
                if (_connections.TryGetValue(Context.ConnectionId, out var connection))
                {
                    connection.IsMonitoringOptimizations = false;
                }

                await Clients.Caller.SendAsync("OptimizationMonitoringStopped", "Stopped monitoring optimization progress");
                
                _logger.LogDebug("User {UserId} stopped optimization monitoring", GetCurrentUserId());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping optimization monitoring");
            }
        }

        // ==================== BROADCASTING METHODS ==================== //

        /// <summary>
        /// Broadcast system status to all connected clients
        /// </summary>
        public static async Task BroadcastSystemStatus()
        {
            // This would be called by a background service
            // Implementation would get current status and broadcast
        }

        /// <summary>
        /// Broadcast critical alert to relevant users
        /// </summary>
        /// <param name="alert">Critical alert to broadcast</param>
        /// <param name="branchId">Branch ID if branch-specific</param>
        public async Task BroadcastCriticalAlert(SmartAlertDto alert, int? branchId = null)
        {
            try
            {
                if (branchId.HasValue)
                {
                    await Clients.Group($"Branch_{branchId}").SendAsync("ReceiveCriticalAlert", alert);
                }
                else
                {
                    await Clients.Group("CoordinationUpdates").SendAsync("ReceiveCriticalAlert", alert);
                }

                _logger.LogInformation("Critical alert broadcasted: {AlertTitle}", alert.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting critical alert");
            }
        }

        /// <summary>
        /// Broadcast optimization progress update
        /// </summary>
        /// <param name="optimizationId">Optimization ID</param>
        /// <param name="progress">Progress percentage</param>
        /// <param name="status">Current status</param>
        public async Task BroadcastOptimizationProgress(int optimizationId, decimal progress, string status)
        {
            try
            {
                var update = new
                {
                    OptimizationId = optimizationId,
                    Progress = progress,
                    Status = status,
                    Timestamp = DateTime.UtcNow
                };

                await Clients.Group("OptimizationMonitoring").SendAsync("ReceiveOptimizationProgress", update);
                
                _logger.LogDebug("Optimization progress broadcasted: {OptimizationId} - {Progress}%", 
                    optimizationId, progress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting optimization progress");
            }
        }

        /// <summary>
        /// Broadcast transfer recommendation update
        /// </summary>
        /// <param name="recommendation">New transfer recommendation</param>
        /// <param name="sourceBranchId">Source branch ID</param>
        /// <param name="targetBranchId">Target branch ID</param>
        public async Task BroadcastTransferRecommendation(
            IntelligentTransferRecommendationDto recommendation, 
            int sourceBranchId, 
            int targetBranchId)
        {
            try
            {
                // Send to both source and target branches
                await Clients.Group($"Branch_{sourceBranchId}")
                    .SendAsync("ReceiveTransferRecommendation", recommendation);
                    
                await Clients.Group($"Branch_{targetBranchId}")
                    .SendAsync("ReceiveTransferRecommendation", recommendation);

                _logger.LogDebug("Transfer recommendation broadcasted from branch {Source} to {Target}", 
                    sourceBranchId, targetBranchId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting transfer recommendation");
            }
        }

        // ==================== HELPER METHODS ==================== //

        private int GetCurrentUserId()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private string GetCurrentUserName()
        {
            return Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
        }

        private int? GetCurrentUserBranchId()
        {
            var branchClaim = Context.User?.FindFirst("BranchId")?.Value;
            return int.TryParse(branchClaim, out var branchId) ? branchId : null;
        }

        private async Task<bool> HasBranchAccess(int userId, int branchId)
        {
            // Implementation would check database for user branch access
            // For now, return true for demo purposes
            return true;
        }

        /// <summary>
        /// Get connection statistics
        /// </summary>
        [HubMethodName("GetConnectionStats")]
        public async Task GetConnectionStats()
        {
            try
            {
                var stats = new
                {
                    TotalConnections = _connections.Count,
                    BranchSubscriptions = _branchSubscriptions.ToDictionary(
                        kvp => kvp.Key, 
                        kvp => kvp.Value.Count),
                    ActiveUsers = _connections.Values
                        .Where(c => c.LastActivity > DateTime.UtcNow.AddMinutes(-5))
                        .Count()
                };

                await Clients.Caller.SendAsync("ReceiveConnectionStats", stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting connection stats");
                await Clients.Caller.SendAsync("Error", "Failed to get connection stats");
            }
        }
    }

    /// <summary>
    /// User connection tracking model
    /// </summary>
    public class UserConnection
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int? BranchId { get; set; }
        public DateTime ConnectedAt { get; set; }
        public DateTime LastActivity { get; set; }
        public bool IsMonitoringOptimizations { get; set; } = false;
    }
}
#pragma warning restore CS1998
