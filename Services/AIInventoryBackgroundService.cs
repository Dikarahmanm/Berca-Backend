using Microsoft.AspNetCore.SignalR;
using Berca_Backend.Data;
using Berca_Backend.Services;
using Berca_Backend.Hubs;
using Microsoft.EntityFrameworkCore;

namespace Berca_Backend.Services
{
    /// <summary>
    /// Background service for AI inventory coordination tasks
    /// Handles periodic AI model training, optimization, and real-time notifications
    /// </summary>
    public class AIInventoryBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AIInventoryBackgroundService> _logger;
        private readonly IHubContext<AIInventoryCoordinationHub> _hubContext;

        // Service intervals
        private readonly TimeSpan _systemStatusInterval = TimeSpan.FromMinutes(1);
        private readonly TimeSpan _aiInsightsInterval = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _smartAlertsInterval = TimeSpan.FromMinutes(2);
        private readonly TimeSpan _modelTrainingInterval = TimeSpan.FromHours(6);
        private readonly TimeSpan _autoOptimizationInterval = TimeSpan.FromHours(2);

        private DateTime _lastSystemStatusUpdate = DateTime.MinValue;
        private DateTime _lastAIInsightsUpdate = DateTime.MinValue;
        private DateTime _lastSmartAlertsCheck = DateTime.MinValue;
        private DateTime _lastModelTraining = DateTime.MinValue;
        private DateTime _lastAutoOptimization = DateTime.MinValue;

        public AIInventoryBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<AIInventoryBackgroundService> logger,
            IHubContext<AIInventoryCoordinationHub> hubContext)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AI Inventory Background Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessScheduledTasks();
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // Check every 30 seconds
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("AI Inventory Background Service is stopping");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in AI Inventory Background Service main loop");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Wait before retrying
                }
            }
        }

        private async Task ProcessScheduledTasks()
        {
            var now = DateTime.UtcNow;

            // System Status Updates (every minute)
            if (now - _lastSystemStatusUpdate >= _systemStatusInterval)
            {
                await BroadcastSystemStatus();
                _lastSystemStatusUpdate = now;
            }

            // AI Insights Updates (every 5 minutes)
            if (now - _lastAIInsightsUpdate >= _aiInsightsInterval)
            {
                await ProcessAIInsightsUpdates();
                _lastAIInsightsUpdate = now;
            }

            // Smart Alerts Check (every 2 minutes)
            if (now - _lastSmartAlertsCheck >= _smartAlertsInterval)
            {
                await ProcessSmartAlerts();
                _lastSmartAlertsCheck = now;
            }

            // Model Training (every 6 hours)
            if (now - _lastModelTraining >= _modelTrainingInterval)
            {
                await ProcessModelTraining();
                _lastModelTraining = now;
            }

            // Auto Optimization (every 2 hours)
            if (now - _lastAutoOptimization >= _autoOptimizationInterval)
            {
                await ProcessAutoOptimization();
                _lastAutoOptimization = now;
            }
        }

        private async Task BroadcastSystemStatus()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var aiService = scope.ServiceProvider.GetRequiredService<IAIInventoryCoordinationService>();

                var status = await aiService.GetRealtimeCoordinationStatusAsync();

                // Broadcast to all connected clients
                await _hubContext.Clients.Group("CoordinationUpdates")
                    .SendAsync("ReceiveRealtimeStatus", status);

                _logger.LogDebug("System status broadcasted to all connected clients");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting system status");
            }
        }

        private async Task ProcessAIInsightsUpdates()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var aiService = scope.ServiceProvider.GetRequiredService<IAIInventoryCoordinationService>();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Get all active branches
                var branches = await context.Branches
                    .Where(b => b.IsActive)
                    .Select(b => b.Id)
                    .ToListAsync();

                // Generate insights for each branch and broadcast
                foreach (var branchId in branches)
                {
                    var insights = await aiService.GenerateAIInsightsAsync(branchId);
                    
                    // Broadcast to subscribers of this branch
                    await _hubContext.Clients.Group($"Branch_{branchId}")
                        .SendAsync("ReceiveAIInsights", insights);

                    // Check for high-impact insights that need immediate attention
                    var criticalInsights = insights.Insights
                        .Where(i => i.ImpactScore >= 80 && i.IsActionable)
                        .ToList();

                    foreach (var criticalInsight in criticalInsights)
                    {
                        await _hubContext.Clients.Group($"Branch_{branchId}")
                            .SendAsync("ReceiveCriticalInsight", criticalInsight);
                    }
                }

                _logger.LogDebug("AI insights processed and broadcasted for {BranchCount} branches", branches.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing AI insights updates");
            }
        }

        private async Task ProcessSmartAlerts()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var aiService = scope.ServiceProvider.GetRequiredService<IAIInventoryCoordinationService>();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Generate system-wide smart alerts
                var systemAlerts = await aiService.GenerateSmartAlertsAsync();

                // Process critical alerts that need immediate broadcasting
                var criticalAlerts = systemAlerts
                    .Where(a => a.Severity == "Critical" || a.AIUrgencyScore >= 80)
                    .ToList();

                foreach (var alert in criticalAlerts)
                {
                    // Broadcast critical alerts to all coordination subscribers
                    await _hubContext.Clients.Group("CoordinationUpdates")
                        .SendAsync("ReceiveCriticalAlert", alert);

                    // Log critical alert for audit
                    _logger.LogWarning("Critical AI alert generated: {AlertTitle} - Urgency: {UrgencyScore}", 
                        alert.Title, alert.AIUrgencyScore);
                }

                // Generate branch-specific alerts
                var branches = await context.Branches
                    .Where(b => b.IsActive)
                    .Select(b => b.Id)
                    .ToListAsync();

                foreach (var branchId in branches)
                {
                    var branchAlerts = await aiService.GenerateSmartAlertsAsync(branchId);
                    var branchCriticalAlerts = branchAlerts
                        .Where(a => a.Severity == "Critical" || a.AIUrgencyScore >= 70)
                        .ToList();

                    if (branchCriticalAlerts.Any())
                    {
                        await _hubContext.Clients.Group($"Branch_{branchId}")
                            .SendAsync("ReceiveSmartAlerts", branchCriticalAlerts);
                    }
                }

                _logger.LogDebug("Smart alerts processed: {SystemAlerts} system-wide, {CriticalAlerts} critical", 
                    systemAlerts.Count, criticalAlerts.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing smart alerts");
            }
        }

        private async Task ProcessModelTraining()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var aiService = scope.ServiceProvider.GetRequiredService<IAIInventoryCoordinationService>();

                _logger.LogInformation("Starting scheduled AI model training");

                // Broadcast training start notification
                await _hubContext.Clients.Group("CoordinationUpdates")
                    .SendAsync("ReceiveSystemNotification", new
                    {
                        Type = "ModelTraining",
                        Message = "AI model training started",
                        Timestamp = DateTime.UtcNow,
                        Status = "InProgress"
                    });

                var trainingResult = await aiService.TrainPredictiveModelsAsync();

                var trainingStatus = new
                {
                    Type = "ModelTraining",
                    Message = trainingResult ? "AI model training completed successfully" : "AI model training failed",
                    Timestamp = DateTime.UtcNow,
                    Status = trainingResult ? "Completed" : "Failed"
                };

                // Broadcast training completion
                await _hubContext.Clients.Group("CoordinationUpdates")
                    .SendAsync("ReceiveSystemNotification", trainingStatus);

                _logger.LogInformation("Scheduled AI model training {Status}", 
                    trainingResult ? "completed successfully" : "failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in scheduled model training");
                
                // Broadcast training error
                await _hubContext.Clients.Group("CoordinationUpdates")
                    .SendAsync("ReceiveSystemNotification", new
                    {
                        Type = "ModelTraining",
                        Message = "AI model training encountered an error",
                        Timestamp = DateTime.UtcNow,
                        Status = "Error",
                        Error = ex.Message
                    });
            }
        }

        private async Task ProcessAutoOptimization()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var aiService = scope.ServiceProvider.GetRequiredService<IAIInventoryCoordinationService>();

                _logger.LogInformation("Starting scheduled auto-optimization");

                // Start with dry run to assess impact
                var dryRunResult = await aiService.ExecuteAutoOptimizationAsync(dryRun: true);

                // Broadcast dry run results
                await _hubContext.Clients.Group("CoordinationUpdates")
                    .SendAsync("ReceiveOptimizationPreview", new
                    {
                        Type = "AutoOptimizationPreview",
                        EstimatedSavings = dryRunResult.EstimatedSavings,
                        TotalActions = dryRunResult.TotalActions,
                        ConfidenceScore = dryRunResult.AIConfidenceScore,
                        Timestamp = DateTime.UtcNow
                    });

                // Execute actual optimization if confidence is high and potential savings are significant
                if (dryRunResult.AIConfidenceScore >= 75 && dryRunResult.EstimatedSavings >= 1000000) // 1M IDR threshold
                {
                    _logger.LogInformation("Executing auto-optimization: Confidence {Confidence}%, Savings Rp {Savings:N0}", 
                        dryRunResult.AIConfidenceScore, dryRunResult.EstimatedSavings);

                    var executionResult = await aiService.ExecuteAutoOptimizationAsync(dryRun: false);

                    // Broadcast execution results
                    await _hubContext.Clients.Group("CoordinationUpdates")
                        .SendAsync("ReceiveOptimizationResult", new
                        {
                            Type = "AutoOptimizationResult",
                            WasExecuted = true,
                            SuccessRate = executionResult.SuccessRate,
                            ActualSavings = executionResult.EstimatedSavings,
                            ActionsExecuted = executionResult.SuccessfulActions,
                            TotalActions = executionResult.TotalActions,
                            ExecutionTime = executionResult.ExecutionTime,
                            Timestamp = DateTime.UtcNow
                        });

                    _logger.LogInformation("Auto-optimization executed: {SuccessfulActions}/{TotalActions} successful, " +
                        "Success rate: {SuccessRate:F1}%, Estimated savings: Rp {Savings:N0}",
                        executionResult.SuccessfulActions, executionResult.TotalActions, 
                        executionResult.SuccessRate, executionResult.EstimatedSavings);
                }
                else
                {
                    _logger.LogInformation("Auto-optimization skipped: Confidence {Confidence}%, Savings Rp {Savings:N0} " +
                        "(below thresholds)", dryRunResult.AIConfidenceScore, dryRunResult.EstimatedSavings);

                    await _hubContext.Clients.Group("CoordinationUpdates")
                        .SendAsync("ReceiveOptimizationResult", new
                        {
                            Type = "AutoOptimizationResult",
                            WasExecuted = false,
                            Reason = "Below confidence or savings threshold",
                            ConfidenceScore = dryRunResult.AIConfidenceScore,
                            EstimatedSavings = dryRunResult.EstimatedSavings,
                            Timestamp = DateTime.UtcNow
                        });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in scheduled auto-optimization");
                
                await _hubContext.Clients.Group("CoordinationUpdates")
                    .SendAsync("ReceiveSystemNotification", new
                    {
                        Type = "AutoOptimizationError",
                        Message = "Auto-optimization encountered an error",
                        Timestamp = DateTime.UtcNow,
                        Status = "Error",
                        Error = ex.Message
                    });
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("AI Inventory Background Service is stopping");

            // Notify connected clients about service shutdown
            try
            {
                await _hubContext.Clients.Group("CoordinationUpdates")
                    .SendAsync("ReceiveSystemNotification", new
                    {
                        Type = "ServiceShutdown",
                        Message = "AI Inventory Background Service is shutting down",
                        Timestamp = DateTime.UtcNow,
                        Status = "Stopping"
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying clients about service shutdown");
            }

            await base.StopAsync(cancellationToken);
        }

        // ==================== UTILITY METHODS ==================== //

        /// <summary>
        /// Process transfer recommendations and broadcast them
        /// </summary>
        private async Task ProcessTransferRecommendations()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var aiService = scope.ServiceProvider.GetRequiredService<IAIInventoryCoordinationService>();

                var recommendations = await aiService.GetIntelligentTransferRecommendationsAsync();
                var highPriorityRecommendations = recommendations
                    .Where(r => r.Priority == "High" && r.AIConfidenceScore >= 80)
                    .ToList();

                foreach (var recommendation in highPriorityRecommendations)
                {
                    // Broadcast to affected branches
                    await _hubContext.Clients.Group($"Branch_{recommendation.SourceBranchId}")
                        .SendAsync("ReceiveTransferRecommendation", recommendation);
                        
                    await _hubContext.Clients.Group($"Branch_{recommendation.TargetBranchId}")
                        .SendAsync("ReceiveTransferRecommendation", recommendation);
                }

                _logger.LogDebug("Processed {Total} transfer recommendations, {HighPriority} high priority", 
                    recommendations.Count, highPriorityRecommendations.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing transfer recommendations");
            }
        }

        /// <summary>
        /// Monitor system health and broadcast warnings
        /// </summary>
        private async Task MonitorSystemHealth()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var aiService = scope.ServiceProvider.GetRequiredService<IAIInventoryCoordinationService>();

                var status = await aiService.GetRealtimeCoordinationStatusAsync();

                // Check for concerning metrics
                var concerns = new List<string>();

                if (status.SystemHealth.OverallScore < 70)
                    concerns.Add($"System health score is low: {status.SystemHealth.OverallScore}");

                if (status.SystemHealth.CriticalAlerts > 5)
                    concerns.Add($"High number of critical alerts: {status.SystemHealth.CriticalAlerts}");

                if (status.SystemHealth.CoordinationEfficiency < 60)
                    concerns.Add($"Coordination efficiency is low: {status.SystemHealth.CoordinationEfficiency}%");

                // Broadcast health warnings if any concerns
                if (concerns.Any())
                {
                    await _hubContext.Clients.Group("CoordinationUpdates")
                        .SendAsync("ReceiveHealthWarning", new
                        {
                            Type = "SystemHealthWarning",
                            Concerns = concerns,
                            OverallScore = status.SystemHealth.OverallScore,
                            Timestamp = DateTime.UtcNow,
                            RecommendedAction = "Review system performance and address critical alerts"
                        });

                    _logger.LogWarning("System health concerns detected: {Concerns}", string.Join(", ", concerns));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring system health");
            }
        }
    }
}