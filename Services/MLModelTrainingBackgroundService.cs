using Berca_Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Berca_Backend.Data;

namespace Berca_Backend.Services
{
    /// <summary>
    /// Background service that automatically trains ML models on a schedule
    /// Ensures models stay current with latest data for optimal performance
    /// </summary>
    public class MLModelTrainingBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MLModelTrainingBackgroundService> _logger;
        private readonly TimeSpan _trainingInterval = TimeSpan.FromHours(24); // Train every 24 hours
        private readonly TimeSpan _healthCheckInterval = TimeSpan.FromHours(6); // Check health every 6 hours

        public MLModelTrainingBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<MLModelTrainingBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ML Model Training Background Service started");

            // Wait a bit before starting to let the application fully initialize
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

            // Initial training on startup (if needed)
            await PerformInitialTrainingIfNeeded(stoppingToken);

            // Main loop for scheduled training and health checks
            await ScheduledTrainingLoop(stoppingToken);
        }

        /// <summary>
        /// Check if initial training is needed and perform it
        /// </summary>
        private async Task PerformInitialTrainingIfNeeded(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var mlService = scope.ServiceProvider.GetRequiredService<IMLInventoryService>();

                _logger.LogInformation("Checking if initial ML model training is needed");

                var modelHealth = await mlService.GetModelHealthAsync();
                var needsTraining = modelHealth.OverallHealthScore < 50; // Train if health is poor

                if (needsTraining)
                {
                    _logger.LogInformation("Initial ML model training needed. Starting training process...");
                    
                    var success = await mlService.TrainModelsAsync();
                    
                    if (success)
                    {
                        _logger.LogInformation("Initial ML model training completed successfully");
                    }
                    else
                    {
                        _logger.LogError("Initial ML model training failed. Models may not perform optimally.");
                    }
                }
                else
                {
                    _logger.LogInformation("ML models are healthy. Initial training not needed.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during initial ML model training check");
            }
        }

        /// <summary>
        /// Main scheduled training and health monitoring loop
        /// </summary>
        private async Task ScheduledTrainingLoop(CancellationToken stoppingToken)
        {
            var lastTrainingTime = DateTime.UtcNow;
            var lastHealthCheckTime = DateTime.UtcNow;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.UtcNow;

                    // Health check every 6 hours
                    if (now - lastHealthCheckTime >= _healthCheckInterval)
                    {
                        await PerformHealthCheck();
                        lastHealthCheckTime = now;
                    }

                    // Training every 24 hours
                    if (now - lastTrainingTime >= _trainingInterval)
                    {
                        var shouldTrain = await ShouldPerformTraining();
                        
                        if (shouldTrain)
                        {
                            await PerformScheduledTraining();
                            lastTrainingTime = now;
                        }
                        else
                        {
                            _logger.LogInformation("Scheduled training skipped - conditions not met");
                            lastTrainingTime = now; // Reset timer anyway to avoid too frequent checks
                        }
                    }

                    // Wait 1 hour before next check
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in ML training background service main loop");
                    
                    // Wait 5 minutes before retrying after an error
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }

            _logger.LogInformation("ML Model Training Background Service stopped");
        }

        /// <summary>
        /// Perform health check on ML models
        /// </summary>
        private async Task PerformHealthCheck()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var mlService = scope.ServiceProvider.GetRequiredService<IMLInventoryService>();

                var modelHealth = await mlService.GetModelHealthAsync();
                
                _logger.LogInformation("ML Model Health Check: Overall Score {Score}%, Status: {Status}",
                    modelHealth.OverallHealthScore, modelHealth.OverallHealth);

                // Log warnings for unhealthy models
                if (modelHealth.OverallHealthScore < 70)
                {
                    _logger.LogWarning("ML Models showing poor health (Score: {Score}%). Consider retraining.",
                        modelHealth.OverallHealthScore);
                }

                // Log individual model health
                _logger.LogDebug("Demand Forecast Model: {Score}%, Anomaly Detection: {AnomalyScore}%, Clustering: {ClusterScore}%",
                    modelHealth.DemandForecastModel?.HealthScore ?? 0,
                    modelHealth.AnomalyDetectionModel?.HealthScore ?? 0,
                    modelHealth.ClusteringModel?.HealthScore ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during ML model health check");
            }
        }

        /// <summary>
        /// Determine if training should be performed based on various conditions
        /// </summary>
        private async Task<bool> ShouldPerformTraining()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var mlService = scope.ServiceProvider.GetRequiredService<IMLInventoryService>();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Check model health
                var modelHealth = await mlService.GetModelHealthAsync();
                if (modelHealth.OverallHealthScore < 70)
                {
                    _logger.LogInformation("Training triggered by low model health: {Score}%", modelHealth.OverallHealthScore);
                    return true;
                }

                // Check data freshness
                var latestSale = await context.Sales
                    .OrderByDescending(s => s.SaleDate)
                    .Select(s => s.SaleDate)
                    .FirstOrDefaultAsync();

                if (latestSale == default)
                {
                    _logger.LogWarning("No sales data found. Skipping training.");
                    return false;
                }

                var daysSinceLastData = (DateTime.UtcNow - latestSale).Days;
                if (daysSinceLastData <= 7) // Only train if we have recent data
                {
                    _logger.LogInformation("Training triggered by data freshness check. Latest data: {Days} days old", daysSinceLastData);
                    return true;
                }

                _logger.LogInformation("Training skipped - data is stale ({Days} days old) or model health is good ({Score}%)",
                    daysSinceLastData, modelHealth.OverallHealthScore);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if training should be performed");
                return false; // Don't train if we can't determine conditions
            }
        }

        /// <summary>
        /// Perform the actual ML model training
        /// </summary>
        private async Task PerformScheduledTraining()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var mlService = scope.ServiceProvider.GetRequiredService<IMLInventoryService>();

                _logger.LogInformation("Starting scheduled ML model training");
                var startTime = DateTime.UtcNow;

                var success = await mlService.TrainModelsAsync();
                
                var endTime = DateTime.UtcNow;
                var duration = endTime - startTime;

                if (success)
                {
                    _logger.LogInformation("Scheduled ML model training completed successfully in {Minutes:F1} minutes",
                        duration.TotalMinutes);

                    // Log model health after training
                    var modelHealth = await mlService.GetModelHealthAsync();
                    _logger.LogInformation("Post-training model health: Overall Score {Score}%, Status: {Status}",
                        modelHealth.OverallHealthScore, modelHealth.OverallHealth);
                }
                else
                {
                    _logger.LogError("Scheduled ML model training failed after {Minutes:F1} minutes",
                        duration.TotalMinutes);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during scheduled ML model training");
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ML Model Training Background Service stopping");
            await base.StopAsync(stoppingToken);
        }
    }
}