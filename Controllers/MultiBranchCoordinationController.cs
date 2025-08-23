using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Berca_Backend.Services;
using Berca_Backend.DTOs;
using System.Security.Claims;

namespace Berca_Backend.Controllers
{
    /// <summary>
    /// Controller for Multi-Branch Coordination - Advanced inter-branch operations
    /// Provides transfer recommendations, performance comparison, and cross-branch optimization
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MultiBranchCoordinationController : ControllerBase
    {
        private readonly IMultiBranchCoordinationService _coordinationService;
        private readonly ILogger<MultiBranchCoordinationController> _logger;

        public MultiBranchCoordinationController(
            IMultiBranchCoordinationService coordinationService,
            ILogger<MultiBranchCoordinationController> logger)
        {
            _coordinationService = coordinationService;
            _logger = logger;
        }

        /// <summary>
        /// Get inter-branch transfer recommendations based on expiry and demand
        /// </summary>
        /// <returns>List of recommended transfers between branches</returns>
        [HttpGet("transfer-recommendations")]
        [Authorize(Policy = "Transfer.Analytics")]
        public async Task<ActionResult<ApiResponse<List<InterBranchTransferRecommendationDto>>>> GetTransferRecommendations()
        {
            try
            {
                _logger.LogInformation("Getting inter-branch transfer recommendations");

                var recommendations = await _coordinationService.GetInterBranchTransferRecommendationsAsync();

                _logger.LogInformation("Generated {Count} transfer recommendations", recommendations.Count);

                return Ok(new ApiResponse<List<InterBranchTransferRecommendationDto>>
                {
                    Success = true,
                    Data = recommendations,
                    Message = $"Generated {recommendations.Count} transfer recommendations"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transfer recommendations");
                return StatusCode(500, new ApiResponse<List<InterBranchTransferRecommendationDto>>
                {
                    Success = false,
                    Message = "Failed to get transfer recommendations",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get comprehensive branch performance comparison
        /// </summary>
        /// <param name="startDate">Start date for comparison period</param>
        /// <param name="endDate">End date for comparison period</param>
        /// <returns>Branch performance comparison with metrics</returns>
        [HttpGet("branch-performance")]
        [Authorize(Policy = "Reports.CrossBranch")]
        public async Task<ActionResult<ApiResponse<BranchPerformanceComparisonDto>>> GetBranchPerformance(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                _logger.LogInformation("Getting branch performance comparison from {StartDate} to {EndDate}", start, end);

                var performance = await _coordinationService.GetBranchPerformanceComparisonAsync(start, end);

                return Ok(new ApiResponse<BranchPerformanceComparisonDto>
                {
                    Success = true,
                    Data = performance,
                    Message = "Branch performance comparison retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting branch performance comparison");
                return StatusCode(500, new ApiResponse<BranchPerformanceComparisonDto>
                {
                    Success = false,
                    Message = "Failed to get branch performance comparison",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Identify cross-branch optimization opportunities
        /// </summary>
        /// <returns>List of optimization opportunities across branches</returns>
        [HttpGet("optimization-opportunities")]
        [Authorize(Policy = "Reports.CrossBranch")]
        public async Task<ActionResult<ApiResponse<List<CrossBranchOpportunityDto>>>> GetOptimizationOpportunities()
        {
            try
            {
                _logger.LogInformation("Identifying cross-branch optimization opportunities");

                var opportunities = await _coordinationService.GetCrossBranchOptimizationOpportunitiesAsync();

                _logger.LogInformation("Identified {Count} optimization opportunities", opportunities.Count);

                return Ok(new ApiResponse<List<CrossBranchOpportunityDto>>
                {
                    Success = true,
                    Data = opportunities,
                    Message = $"Identified {opportunities.Count} optimization opportunities"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting optimization opportunities");
                return StatusCode(500, new ApiResponse<List<CrossBranchOpportunityDto>>
                {
                    Success = false,
                    Message = "Failed to get optimization opportunities",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Optimize inventory distribution across branches
        /// </summary>
        /// <param name="productId">Optional product ID to optimize</param>
        /// <returns>Inventory distribution optimization plan</returns>
        [HttpPost("optimize-distribution")]
        [Authorize(Policy = "MultiBranch.Access")]
        public async Task<ActionResult<ApiResponse<InventoryDistributionPlanDto>>> OptimizeInventoryDistribution(
            [FromQuery] int? productId = null)
        {
            try
            {
                _logger.LogInformation("Optimizing inventory distribution for product {ProductId}", productId);

                var plan = await _coordinationService.OptimizeInventoryDistributionAsync(productId);

                return Ok(new ApiResponse<InventoryDistributionPlanDto>
                {
                    Success = true,
                    Data = plan,
                    Message = "Inventory distribution optimization completed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error optimizing inventory distribution");
                return StatusCode(500, new ApiResponse<InventoryDistributionPlanDto>
                {
                    Success = false,
                    Message = "Failed to optimize inventory distribution",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get demand forecasting across branches
        /// </summary>
        /// <param name="forecastDays">Number of days to forecast</param>
        /// <param name="productId">Optional product ID filter</param>
        /// <returns>Demand forecast across branches</returns>
        [HttpGet("demand-forecast")]
        [Authorize(Policy = "Reports.Forecasting")]
        public async Task<ActionResult<ApiResponse<List<BranchDemandForecastDto>>>> GetDemandForecast(
            [FromQuery] int forecastDays = 30,
            [FromQuery] int? productId = null)
        {
            try
            {
                _logger.LogInformation("Getting demand forecast for {Days} days, product {ProductId}", forecastDays, productId);

                var forecast = await _coordinationService.GetDemandForecastAsync(forecastDays, productId);

                return Ok(new ApiResponse<List<BranchDemandForecastDto>>
                {
                    Success = true,
                    Data = forecast,
                    Message = $"Demand forecast generated for {forecastDays} days"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting demand forecast");
                return StatusCode(500, new ApiResponse<List<BranchDemandForecastDto>>
                {
                    Success = false,
                    Message = "Failed to get demand forecast",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get system-wide coordination health metrics
        /// </summary>
        /// <returns>Multi-branch coordination health status</returns>
        [HttpGet("coordination-health")]
        [Authorize(Policy = "Reports.CrossBranch")]
        public async Task<ActionResult<ApiResponse<CoordinationHealthDto>>> GetCoordinationHealth()
        {
            try
            {
                _logger.LogInformation("Getting coordination system health");

                // Get data for health metrics
                var transferRecommendations = await _coordinationService.GetInterBranchTransferRecommendationsAsync();
                var opportunities = await _coordinationService.GetCrossBranchOptimizationOpportunitiesAsync();

                var health = new CoordinationHealthDto
                {
                    TotalTransferRecommendations = transferRecommendations.Count,
                    HighPriorityTransfers = transferRecommendations.Count(t => t.Priority == "High"),
                    TotalOptimizationOpportunities = opportunities.Count,
                    HighImpactOpportunities = opportunities.Count(o => o.Impact == "High"),
                    TotalPotentialSavings = opportunities.Sum(o => o.PotentialSavings),
                    SystemEfficiencyScore = CalculateEfficiencyScore(transferRecommendations, opportunities),
                    LastUpdateTimestamp = DateTime.UtcNow
                };

                return Ok(new ApiResponse<CoordinationHealthDto>
                {
                    Success = true,
                    Data = health,
                    Message = "Coordination system health retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting coordination system health");
                return StatusCode(500, new ApiResponse<CoordinationHealthDto>
                {
                    Success = false,
                    Message = "Failed to get coordination system health",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Execute automatic optimization based on current opportunities
        /// </summary>
        /// <param name="dryRun">If true, only simulate the optimization without making changes</param>
        /// <returns>Optimization execution results</returns>
        [HttpPost("execute-optimization")]
        [Authorize(Policy = "MultiBranch.Access")]
        public async Task<ActionResult<ApiResponse<OptimizationExecutionResultDto>>> ExecuteOptimization(
            [FromQuery] bool dryRun = true)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                
                _logger.LogInformation("Executing optimization (dry run: {DryRun}) by user {UserId}", dryRun, currentUserId);

                var result = await _coordinationService.ExecuteAutomaticOptimizationAsync(dryRun, currentUserId);

                return Ok(new ApiResponse<OptimizationExecutionResultDto>
                {
                    Success = true,
                    Data = result,
                    Message = dryRun ? "Optimization simulation completed" : "Optimization executed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing optimization");
                return StatusCode(500, new ApiResponse<OptimizationExecutionResultDto>
                {
                    Success = false,
                    Message = "Failed to execute optimization",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Calculate system efficiency score based on recommendations and opportunities
        /// </summary>
        private static double CalculateEfficiencyScore(
            List<InterBranchTransferRecommendationDto> transfers, 
            List<CrossBranchOpportunityDto> opportunities)
        {
            if (!transfers.Any() && !opportunities.Any())
                return 100; // Perfect efficiency if no issues

            var urgentTransfers = transfers.Count(t => t.Priority == "High");
            var highImpactOpportunities = opportunities.Count(o => o.Impact == "High");
            var totalIssues = urgentTransfers + highImpactOpportunities;

            // Score inversely related to number of high-priority issues
            var efficiency = Math.Max(0, 100 - (totalIssues * 10));
            return Math.Min(100, efficiency);
        }
    }

    /// <summary>
    /// DTO for coordination system health metrics
    /// </summary>
    public class CoordinationHealthDto
    {
        public int TotalTransferRecommendations { get; set; }
        public int HighPriorityTransfers { get; set; }
        public int TotalOptimizationOpportunities { get; set; }
        public int HighImpactOpportunities { get; set; }
        public decimal TotalPotentialSavings { get; set; }
        public double SystemEfficiencyScore { get; set; }
        public DateTime LastUpdateTimestamp { get; set; }
    }
}