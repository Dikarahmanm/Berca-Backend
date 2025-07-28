// Services/IMemberService.cs - Sprint 2 Member Service Interface
using Berca_Backend.DTOs;
using Berca_Backend.Models;
namespace Berca_Backend.Services
{
    public interface IMemberService
    {
        // CRUD Operations
        Task<MemberSearchResponse> SearchMembersAsync(string? search = null, bool? isActive = null, int page = 1, int pageSize = 20);
        Task<MemberDto?> GetMemberByIdAsync(int id);
        Task<MemberDto?> GetMemberByPhoneAsync(string phone);
        Task<MemberDto?> GetMemberByNumberAsync(string memberNumber);
        Task<MemberDto> CreateMemberAsync(CreateMemberRequest request, string createdBy);
        Task<MemberDto> UpdateMemberAsync(int id, UpdateMemberRequest request, string updatedBy);
        Task<bool> DeleteMemberAsync(int id);

        // Point Management
        Task<bool> AddPointsAsync(int memberId, int points, string description, int? saleId = null, string? referenceNumber = null, string? createdBy = null);
        Task<bool> RedeemPointsAsync(int memberId, int points, string description, string? referenceNumber = null, string? createdBy = null);
        Task<List<MemberPointDto>> GetPointHistoryAsync(int memberId, int page = 1, int pageSize = 20);
        Task<int> GetAvailablePointsAsync(int memberId);

        // Member Analytics
        Task<MemberStatsDto> GetMemberStatsAsync(int memberId);
        Task<List<TopMemberDto>> GetTopMembersAsync(int count = 10, DateTime? startDate = null, DateTime? endDate = null);

        // Validation
        Task<bool> IsPhoneExistsAsync(string phone, int? excludeId = null);
        Task<bool> IsMemberNumberExistsAsync(string memberNumber);

        // Tier Management
        Task<bool> UpdateMemberTierAsync(int memberId);
        Task<MembershipTier> CalculateMemberTierAsync(decimal totalSpent);
    }
}