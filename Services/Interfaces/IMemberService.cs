// Services/IMemberService.cs - Sprint 2 Member Service Interface
using Berca_Backend.DTOs;

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
        Task<string> CalculateMemberTierAsync(decimal totalSpent);
    }

    public class MemberPointDto
    {
        public int Id { get; set; }
        public int Points { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ReferenceNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsEarning { get; set; }
        public bool IsRedemption { get; set; }
    }

    public class MemberStatsDto
    {
        public int TotalTransactions { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal AverageTransactionValue { get; set; }
        public int TotalPoints { get; set; }
        public int AvailablePoints { get; set; }
        public DateTime? LastTransactionDate { get; set; }
        public string CurrentTier { get; set; } = string.Empty;
        public decimal NextTierRequirement { get; set; }
    }

    public class TopMemberDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string MemberNumber { get; set; } = string.Empty;
        public decimal TotalSpent { get; set; }
        public int TotalTransactions { get; set; }
        public string Tier { get; set; } = string.Empty;
    }
}