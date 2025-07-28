// Controllers/MemberController.cs - Sprint 2 Member Controller Implementation (FIXED)
using Berca_Backend.DTOs;
using Berca_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
namespace Berca_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MemberController : ControllerBase
    {
        private readonly IMemberService _memberService;
        private readonly ILogger<MemberController> _logger;

        public MemberController(IMemberService memberService, ILogger<MemberController> logger)
        {
            _memberService = memberService;
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

                var response = await _memberService.SearchMembersAsync(search, isActive, page, pageSize);

                return Ok(new ApiResponse<MemberSearchResponse>
                {
                    Success = true,
                    Data = response,
                    Message = $"Retrieved {response.Members.Count} members"
                });
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
                var member = await _memberService.GetMemberByIdAsync(id);
                if (member == null)
                {
                    return NotFound(new ApiResponse<MemberDto>
                    {
                        Success = false,
                        Message = "Member not found"
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
}