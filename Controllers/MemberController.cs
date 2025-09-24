// Controllers/MemberController.cs - Enhanced with Member Credit Integration
using Berca_Backend.DTOs;
using Berca_Backend.Services;
using Berca_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
namespace Berca_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MemberController : ControllerBase
    {
        private readonly IMemberService _memberService;
        private readonly IMemoryCache _cache;
        private readonly ICacheInvalidationService _cacheInvalidation;
        // TODO: Add IMemberCreditService when implementation is complete
        private readonly ILogger<MemberController> _logger;

        public MemberController(IMemberService memberService, IMemoryCache cache, ICacheInvalidationService cacheInvalidation, ILogger<MemberController> logger)
        {
            _memberService = memberService;
            _cache = cache;
            _cacheInvalidation = cacheInvalidation;
            _logger = logger;
        }

        /// <summary>
        /// Search members with filtering and pagination
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "Membership.Read")]
        public async Task<ActionResult<ApiResponse<MemberSearchResponse>>> SearchMembers(
            [FromQuery] string? search = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (pageSize > 100) pageSize = 100; // Limit page size

                // ✅ CACHE ASIDE PATTERN: Check cache first
                var cacheKey = $"member_search_{search ?? "all"}_{isActive}_{page}_{pageSize}";

                if (_cache.TryGetValue(cacheKey, out ApiResponse<MemberSearchResponse>? cachedSearch))
                {
                    _logger.LogInformation("🔄 Cache HIT: Retrieved member search results from cache");
                    return Ok(cachedSearch);
                }

                _logger.LogInformation("🔄 Cache MISS: Performing member search in database");
                var response = await _memberService.SearchMembersAsync(search, isActive, page, pageSize);

                // Prepare the response object
                var searchResponse = new ApiResponse<MemberSearchResponse>
                {
                    Success = true,
                    Data = response,
                    Message = $"Retrieved {response.Members.Count} members"
                };

                // ✅ CACHE ASIDE PATTERN: Update cache after database fetch
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15), // Member search cache for 15 minutes
                    SlidingExpiration = TimeSpan.FromMinutes(5),
                    Priority = CacheItemPriority.Normal
                };

                _cache.Set(cacheKey, searchResponse, cacheOptions);
                _logger.LogInformation("💾 Cache UPDATED: Stored member search results in cache");

                return Ok(searchResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching members");
                return StatusCode(500, new ApiResponse<MemberSearchResponse>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get member by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Policy = "Membership.Read")]
        public async Task<ActionResult<ApiResponse<MemberDto>>> GetMember(int id)
        {
            try
            {
                // ✅ CACHE ASIDE PATTERN: Check cache first
                var cacheKey = $"member_by_id_{id}";

                if (_cache.TryGetValue(cacheKey, out ApiResponse<MemberDto>? cachedMember))
                {
                    _logger.LogInformation("🔄 Cache HIT: Retrieved member from cache for ID {MemberId}", id);
                    return Ok(cachedMember);
                }

                _logger.LogInformation("🔄 Cache MISS: Fetching member from database for ID {MemberId}", id);
                var member = await _memberService.GetMemberByIdAsync(id);
                if (member == null)
                {
                    return NotFound(new ApiResponse<MemberDto>
                    {
                        Success = false,
                        Message = "Member not found"
                    });
                }

                // Prepare the response object
                var memberResponse = new ApiResponse<MemberDto>
                {
                    Success = true,
                    Data = member
                };

                // ✅ CACHE ASIDE PATTERN: Update cache after database fetch
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30), // Member data cache for 30 minutes
                    SlidingExpiration = TimeSpan.FromMinutes(10),
                    Priority = CacheItemPriority.Normal
                };

                _cache.Set(cacheKey, memberResponse, cacheOptions);
                _logger.LogInformation("💾 Cache UPDATED: Stored member data in cache for ID {MemberId}", id);

                return Ok(memberResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving member: {MemberId}", id);
                return StatusCode(500, new ApiResponse<MemberDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get member by phone number (for POS lookup)
        /// </summary>
        [HttpGet("phone/{phone}")]
        [Authorize(Policy = "Membership.Read")]
        public async Task<ActionResult<ApiResponse<MemberDto>>> GetMemberByPhone(string phone)
        {
            try
            {
                var member = await _memberService.GetMemberByPhoneAsync(phone);
                if (member == null)
                {
                    return NotFound(new ApiResponse<MemberDto>
                    {
                        Success = false,
                        Message = "Member not found with this phone number"
                    });
                }

                return Ok(new ApiResponse<MemberDto>
                {
                    Success = true,
                    Data = member
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving member by phone: {Phone}", phone);
                return StatusCode(500, new ApiResponse<MemberDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get member by member number
        /// </summary>
        [HttpGet("number/{memberNumber}")]
        [Authorize(Policy = "Membership.Read")]
        public async Task<ActionResult<ApiResponse<MemberDto>>> GetMemberByNumber(string memberNumber)
        {
            try
            {
                var member = await _memberService.GetMemberByNumberAsync(memberNumber);
                if (member == null)
                {
                    return NotFound(new ApiResponse<MemberDto>
                    {
                        Success = false,
                        Message = "Member not found with this member number"
                    });
                }

                return Ok(new ApiResponse<MemberDto>
                {
                    Success = true,
                    Data = member
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving member by number: {MemberNumber}", memberNumber);
                return StatusCode(500, new ApiResponse<MemberDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Create a new member
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "Membership.Write")]
        public async Task<ActionResult<ApiResponse<MemberDto>>> CreateMember([FromBody] CreateMemberRequest request)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

                // Check if phone already exists
                if (await _memberService.IsPhoneExistsAsync(request.Phone))
                {
                    return Conflict(new ApiResponse<MemberDto>
                    {
                        Success = false,
                        Message = "A member with this phone number already exists"
                    });
                }

                var member = await _memberService.CreateMemberAsync(request, username);

                // ✅ CACHE INVALIDATION: Clear member-related caches after creation
                _cacheInvalidation.InvalidateByPattern("member_search_*");
                _cacheInvalidation.InvalidateByPattern("analytics_*"); // Member statistics might change

                _logger.LogInformation("🗑️ Cache invalidated after member creation: {MemberName} (ID: {MemberId})",
                    member.Name, member.Id);

                return CreatedAtAction(nameof(GetMember), new { id = member.Id }, new ApiResponse<MemberDto>
                {
                    Success = true,
                    Data = member,
                    Message = "Member created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating member: {MemberName}", request.Name);
                return StatusCode(500, new ApiResponse<MemberDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Update an existing member
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "Membership.Write")]
        public async Task<ActionResult<ApiResponse<MemberDto>>> UpdateMember(int id, [FromBody] UpdateMemberRequest request)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

                // Check if phone already exists for another member
                if (await _memberService.IsPhoneExistsAsync(request.Phone, id))
                {
                    return Conflict(new ApiResponse<MemberDto>
                    {
                        Success = false,
                        Message = "A member with this phone number already exists"
                    });
                }

                var member = await _memberService.UpdateMemberAsync(id, request, username);

                // ✅ CACHE INVALIDATION: Clear member-related caches after update
                _cacheInvalidation.InvalidateByPattern("member_search_*");
                _cacheInvalidation.InvalidateByPattern($"member_by_id_{id}");
                _cacheInvalidation.InvalidateByPattern("analytics_*"); // Member statistics might change

                _logger.LogInformation("🗑️ Cache invalidated after member update: {MemberId}", id);

                return Ok(new ApiResponse<MemberDto>
                {
                    Success = true,
                    Data = member,
                    Message = "Member updated successfully"
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ApiResponse<MemberDto>
                {
                    Success = false,
                    Message = "Member not found"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating member: {MemberId}", id);
                return StatusCode(500, new ApiResponse<MemberDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Delete a member (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "Membership.Delete")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteMember(int id)
        {
            try
            {
                var result = await _memberService.DeleteMemberAsync(id);
                if (!result)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Member not found"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Member deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting member: {MemberId}", id);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Add points to member account
        /// </summary>
        [HttpPost("{id}/points/add")]
        [Authorize(Policy = "Membership.Write")]
        public async Task<ActionResult<ApiResponse<bool>>> AddPoints(int id, [FromBody] AddPointsRequest request)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

                var result = await _memberService.AddPointsAsync(
                    id,
                    request.Points,
                    request.Description,
                    request.SaleId,
                    request.ReferenceNumber,
                    username);

                if (!result)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Member not found"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Points added successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding points for member: {MemberId}", id);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Redeem points from member account
        /// </summary>
        [HttpPost("{id}/points/redeem")]
        [Authorize(Policy = "Membership.Write")]
        public async Task<ActionResult<ApiResponse<bool>>> RedeemPoints(int id, [FromBody] RedeemPointsRequest request)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

                var result = await _memberService.RedeemPointsAsync(
                    id,
                    request.Points,
                    request.Description,
                    request.ReferenceNumber,
                    username);

                if (!result)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Member not found"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Points redeemed successfully"
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error redeeming points for member: {MemberId}", id);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get member point history
        /// </summary>
        [HttpGet("{id}/points/history")]
        [Authorize(Policy = "Membership.Read")]
        public async Task<ActionResult<ApiResponse<List<MemberPointDto>>>> GetPointHistory(
            int id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (pageSize > 100) pageSize = 100; // Limit page size

                var history = await _memberService.GetPointHistoryAsync(id, page, pageSize);

                return Ok(new ApiResponse<List<MemberPointDto>>
                {
                    Success = true,
                    Data = history,
                    Message = $"Retrieved {history.Count} point transactions"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving point history for member: {MemberId}", id);
                return StatusCode(500, new ApiResponse<List<MemberPointDto>>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get member available points balance
        /// </summary>
        [HttpGet("{id}/points/balance")]
        [Authorize(Policy = "Membership.Read")]
        public async Task<ActionResult<ApiResponse<int>>> GetAvailablePoints(int id)
        {
            try
            {
                var points = await _memberService.GetAvailablePointsAsync(id);

                return Ok(new ApiResponse<int>
                {
                    Success = true,
                    Data = points,
                    Message = "Available points retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available points for member: {MemberId}", id);
                return StatusCode(500, new ApiResponse<int>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get member statistics
        /// </summary>
        [HttpGet("{id}/stats")]
        [Authorize(Policy = "Membership.Read")]
        public async Task<ActionResult<ApiResponse<MemberStatsDto>>> GetMemberStats(int id)
        {
            try
            {
                var stats = await _memberService.GetMemberStatsAsync(id);

                return Ok(new ApiResponse<MemberStatsDto>
                {
                    Success = true,
                    Data = stats,
                    Message = "Member statistics retrieved successfully"
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ApiResponse<MemberStatsDto>
                {
                    Success = false,
                    Message = "Member not found"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting member stats: {MemberId}", id);
                return StatusCode(500, new ApiResponse<MemberStatsDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get top members by spending
        /// </summary>
        [HttpGet("reports/top-members")]
        [Authorize(Policy = "Reports.Read")]
        public async Task<ActionResult<ApiResponse<List<TopMemberDto>>>> GetTopMembers(
            [FromQuery] int count = 10,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                if (count > 100) count = 100; // Limit count

                var topMembers = await _memberService.GetTopMembersAsync(count, startDate, endDate);

                return Ok(new ApiResponse<List<TopMemberDto>>
                {
                    Success = true,
                    Data = topMembers,
                    Message = $"Retrieved top {topMembers.Count} members"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top members");
                return StatusCode(500, new ApiResponse<List<TopMemberDto>>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Update member tier based on spending
        /// </summary>
        [HttpPost("{id}/update-tier")]
        [Authorize(Policy = "Membership.Write")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateMemberTier(int id)
        {
            try
            {
                var result = await _memberService.UpdateMemberTierAsync(id);
                if (!result)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Member not found"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Member tier updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating member tier: {MemberId}", id);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Check if phone number exists
        /// </summary>
        [HttpGet("validate/phone/{phone}")]
        [Authorize(Policy = "Membership.Read")]
        public async Task<ActionResult<ApiResponse<bool>>> ValidatePhone(string phone, [FromQuery] int? excludeId = null)
        {
            try
            {
                var exists = await _memberService.IsPhoneExistsAsync(phone, excludeId);

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = exists,
                    Message = exists ? "Phone number already exists" : "Phone number is available"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating phone: {Phone}", phone);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        // ==================== MEMBER CREDIT INTEGRATION ENDPOINTS ==================== //

        /// <summary>
        /// Get member with credit information for membership module
        /// </summary>
        [HttpGet("{id}/with-credit")]
        [Authorize(Policy = "Member.CreditInfo")]
        public async Task<ActionResult<ApiResponse<MemberWithCreditDto>>> GetMemberWithCredit(int id)
        {
            try
            {
                var memberWithCredit = await _memberService.GetMemberWithCreditAsync(id);
                if (memberWithCredit == null)
                {
                    return NotFound(new ApiResponse<MemberWithCreditDto>
                    {
                        Success = false,
                        Message = "Member not found"
                    });
                }

                return Ok(new ApiResponse<MemberWithCreditDto>
                {
                    Success = true,
                    Data = memberWithCredit,
                    Message = "Member with credit information retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving member with credit: {MemberId}", id);
                return StatusCode(500, new ApiResponse<MemberWithCreditDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Search members with credit information and filters
        /// </summary>
        [HttpGet("search-with-credit")]
        [Authorize(Policy = "Member.CreditInfo")]
        public async Task<ActionResult<ApiResponse<PagedResult<MemberWithCreditDto>>>> SearchMembersWithCredit([FromQuery] MemberSearchWithCreditDto filter)
        {
            try
            {
                if (filter.PageSize > 100) filter.PageSize = 100; // Limit page size

                var result = await _memberService.SearchMembersWithCreditAsync(filter);

                return Ok(new ApiResponse<PagedResult<MemberWithCreditDto>>
                {
                    Success = true,
                    Data = result,
                    Message = $"Retrieved {result.Items.Count} members with credit information"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching members with credit");
                return StatusCode(500, new ApiResponse<PagedResult<MemberWithCreditDto>>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get quick credit status for UI components
        /// </summary>
        [HttpGet("{id}/credit-status")]
        [Authorize(Policy = "Member.CreditInfo")]
        public async Task<ActionResult<ApiResponse<MemberCreditStatusDto>>> GetMemberCreditStatus(int id)
        {
            try
            {
                var creditStatus = await _memberService.GetMemberCreditStatusAsync(id);
                if (creditStatus == null)
                {
                    return NotFound(new ApiResponse<MemberCreditStatusDto>
                    {
                        Success = false,
                        Message = "Member not found"
                    });
                }

                return Ok(new ApiResponse<MemberCreditStatusDto>
                {
                    Success = true,
                    Data = creditStatus,
                    Message = "Member credit status retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving member credit status: {MemberId}", id);
                return StatusCode(500, new ApiResponse<MemberCreditStatusDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get member credit info for POS lookup (supports phone, member number, or ID)
        /// </summary>
        [HttpGet("credit-lookup/{identifier}")]
        [Authorize(Policy = "POS.CreditValidation")]
        public async Task<ActionResult<ApiResponse<POSMemberCreditDto>>> GetMemberCreditForPOS(string identifier)
        {
            try
            {
                var memberCreditInfo = await _memberService.GetMemberCreditForPOSAsync(identifier);
                if (memberCreditInfo == null)
                {
                    return NotFound(new ApiResponse<POSMemberCreditDto>
                    {
                        Success = false,
                        Message = "Member not found with the provided identifier"
                    });
                }

                _logger.LogInformation("POS member credit lookup successful for identifier: {Identifier}", identifier);

                return Ok(new ApiResponse<POSMemberCreditDto>
                {
                    Success = true,
                    Data = memberCreditInfo,
                    Message = "Member credit information retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving member credit for POS lookup: {Identifier}", identifier);
                return StatusCode(500, new ApiResponse<POSMemberCreditDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Update member statistics after credit transaction
        /// </summary>
        [HttpPost("{id}/update-after-credit")]
        [Authorize(Policy = "POS.CreditTransaction")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateMemberAfterCreditTransaction(
            int id, 
            [FromBody] UpdateMemberAfterCreditRequest request)
        {
            try
            {
                var result = await _memberService.UpdateMemberAfterCreditTransactionAsync(
                    id, 
                    request.Amount, 
                    request.SaleId);

                if (!result)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Member not found"
                    });
                }

                _logger.LogInformation("Member updated after credit transaction. Member: {MemberId}, Amount: {Amount}, Sale: {SaleId}", 
                    id, request.Amount, request.SaleId);

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Member statistics updated successfully after credit transaction"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating member after credit transaction: {MemberId}", id);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }
    }

    // Request DTOs
    public class AddPointsRequest
    {
        public int Points { get; set; }
        public string Description { get; set; } = string.Empty;
        public int? SaleId { get; set; }
        public string? ReferenceNumber { get; set; }
    }

    public class RedeemPointsRequest
    {
        public int Points { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? ReferenceNumber { get; set; }
    }

    public class UpdateMemberAfterCreditRequest
    {
        public decimal Amount { get; set; }
        public int SaleId { get; set; }
    }
}