// Services/MemberService.cs - Fixed: All timezone and enum issues
using Berca_Backend.DTOs;
using Berca_Backend.Models;
using Berca_Backend.Data;
using Microsoft.EntityFrameworkCore;
using Berca_Backend.Extensions;
using Berca_Backend.Services.Interfaces;

namespace Berca_Backend.Services
{
    public class MemberService : IMemberService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<MemberService> _logger;
        private readonly ITimezoneService _timezoneService;

        public MemberService(AppDbContext context, ILogger<MemberService> logger, ITimezoneService timezoneService)
        {
            _context = context;
            _logger = logger;
            _timezoneService = timezoneService; // ✅ FIXED: Remove nullable and throw
        }

        public async Task<MemberSearchResponse> SearchMembersAsync(string? search = null, bool? isActive = null, int page = 1, int pageSize = 20)
        {
            try
            {
                var query = _context.Members.AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(m =>
                       m.Name.Contains(search)
                       || m.Phone.Contains(search)
                       || m.MemberNumber.Contains(search)
                       || (m.Email != null && m.Email.Contains(search))
                    );
                }

                if (isActive.HasValue)
                {
                    query = query.Where(m => m.IsActive == isActive.Value);
                }

                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

                var members = await query
                    .OrderBy(m => m.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(m => new MemberDto
                    {
                        Id = m.Id,
                        MemberNumber = m.MemberNumber,
                        Name = m.Name,
                        Phone = m.Phone,
                        Email = m.Email,
                        DateOfBirth = m.DateOfBirth,
                        Gender = m.Gender,
                        Address = m.Address,
                        Tier = m.Tier.ToString(),
                        TotalSpent = m.TotalSpent,
                        IsActive = m.IsActive,
                        JoinDate = m.JoinDate,
                        LastTransactionDate = m.LastTransactionDate,
                        TotalPoints = m.TotalPoints,
                        UsedPoints = m.UsedPoints,
                        AvailablePoints = m.AvailablePoints,
                        TotalTransactions = m.TotalTransactions,
                        AverageTransactionValue = m.AverageTransactionValue,
                        CreatedAt = m.CreatedAt,
                        UpdatedAt = m.UpdatedAt
                    })
                    .ToListAsync();

                return new MemberSearchResponse
                {
                    Members = members,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    CurrentPage = page,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching members");
                throw;
            }
        }

        public async Task<MemberDto?> GetMemberByIdAsync(int id)
        {
            try
            {
                return await _context.Members
                    .Where(m => m.Id == id)
                    .Select(m => new MemberDto
                    {
                        Id = m.Id,
                        MemberNumber = m.MemberNumber,
                        Name = m.Name,
                        Phone = m.Phone,
                        Email = m.Email,
                        DateOfBirth = m.DateOfBirth,
                        Gender = m.Gender,
                        Address = m.Address,
                        Tier = m.Tier.ToString(),
                        TotalSpent = m.TotalSpent,
                        IsActive = m.IsActive,
                        JoinDate = m.JoinDate,
                        LastTransactionDate = m.LastTransactionDate,
                        TotalPoints = m.TotalPoints,
                        UsedPoints = m.UsedPoints,
                        AvailablePoints = m.AvailablePoints,
                        TotalTransactions = m.TotalTransactions,
                        AverageTransactionValue = m.AverageTransactionValue,
                        CreatedAt = m.CreatedAt,
                        UpdatedAt = m.UpdatedAt
                    })
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving member by ID: {MemberId}", id);
                throw;
            }
        }

        public async Task<MemberDto?> GetMemberByPhoneAsync(string phone)
        {
            try
            {
                return await _context.Members
                    .Where(m => m.Phone == phone && m.IsActive)
                    .Select(m => new MemberDto
                    {
                        Id = m.Id,
                        MemberNumber = m.MemberNumber,
                        Name = m.Name,
                        Phone = m.Phone,
                        Email = m.Email,
                        DateOfBirth = m.DateOfBirth,
                        Gender = m.Gender,
                        Address = m.Address,
                        Tier = m.Tier.ToString(),
                        TotalSpent = m.TotalSpent,
                        IsActive = m.IsActive,
                        JoinDate = m.JoinDate,
                        LastTransactionDate = m.LastTransactionDate,
                        TotalPoints = m.TotalPoints,
                        UsedPoints = m.UsedPoints,
                        AvailablePoints = m.AvailablePoints,
                        TotalTransactions = m.TotalTransactions,
                        AverageTransactionValue = m.AverageTransactionValue,
                        CreatedAt = m.CreatedAt,
                        UpdatedAt = m.UpdatedAt
                    })
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving member by phone: {Phone}", phone);
                throw;
            }
        }

        public async Task<MemberDto?> GetMemberByNumberAsync(string memberNumber)
        {
            try
            {
                return await _context.Members
                    .Where(m => m.MemberNumber == memberNumber && m.IsActive)
                    .Select(m => new MemberDto
                    {
                        Id = m.Id,
                        MemberNumber = m.MemberNumber,
                        Name = m.Name,
                        Phone = m.Phone,
                        Email = m.Email,
                        DateOfBirth = m.DateOfBirth,
                        Gender = m.Gender,
                        Address = m.Address,
                        Tier = m.Tier.ToString(),
                        TotalSpent = m.TotalSpent,
                        IsActive = m.IsActive,
                        JoinDate = m.JoinDate,
                        LastTransactionDate = m.LastTransactionDate,
                        TotalPoints = m.TotalPoints,
                        UsedPoints = m.UsedPoints,
                        AvailablePoints = m.AvailablePoints,
                        TotalTransactions = m.TotalTransactions,
                        AverageTransactionValue = m.AverageTransactionValue,
                        CreatedAt = m.CreatedAt,
                        UpdatedAt = m.UpdatedAt
                    })
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving member by number: {MemberNumber}", memberNumber);
                throw;
            }
        }

        public async Task<MemberDto> CreateMemberAsync(CreateMemberRequest request, string createdBy)
        {
            try
            {
                // Check if phone number already exists
                var existingMember = await _context.Members
                    .AnyAsync(m => m.Phone == request.Phone);

                if (existingMember)
                    throw new ArgumentException($"Phone number '{request.Phone}' already exists");

                // Check if email already exists (if provided)
                if (!string.IsNullOrEmpty(request.Email))
                {
                    var existingEmail = await _context.Members
                        .AnyAsync(m => m.Email == request.Email);

                    if (existingEmail)
                        throw new ArgumentException($"Email '{request.Email}' already exists");
                }

                var member = new Member
                {
                    MemberNumber = await GenerateMemberNumberAsync(),
                    Name = request.Name,
                    Phone = request.Phone,
                    Email = request.Email,
                    DateOfBirth = request.DateOfBirth,
                    Gender = request.Gender,
                    Address = request.Address,
                    JoinDate = _timezoneService.Now, // ✅ FIXED: Use Indonesia time
                    CreatedAt = _timezoneService.Now, // ✅ FIXED: Use Indonesia time
                    CreatedBy = createdBy,
                    IsActive = true,
                    Tier = MembershipTier.Bronze, // ✅ FIXED: Use MembershipTier instead of MemberTier
                    TotalPoints = 0,
                    UsedPoints = 0,
                    TotalSpent = 0,
                    TotalTransactions = 0
                };

                _context.Members.Add(member);
                await _context.SaveChangesAsync();

                // Beri poin bonus selamat datang
                await AddPointsAsync(member.Id, 100, "Welcome bonus", null, null, createdBy);

                return await GetMemberByIdAsync(member.Id)
                       ?? throw new Exception("Member created but not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating member: {MemberName}", request.Name);
                throw;
            }
        }

        public async Task<MemberDto> UpdateMemberAsync(int id, UpdateMemberRequest request, string updatedBy)
        {
            try
            {
                var member = await _context.Members.FindAsync(id);
                if (member == null)
                    throw new KeyNotFoundException($"Member with ID {id} not found");

                member.Name = request.Name;
                member.Phone = request.Phone;
                member.Email = request.Email;
                member.DateOfBirth = request.DateOfBirth;
                member.Gender = request.Gender;
                member.Address = request.Address;
                member.IsActive = request.IsActive;
                member.UpdatedAt = _timezoneService.Now; // ✅ FIXED: Use Indonesia time
                member.UpdatedBy = updatedBy;

                await _context.SaveChangesAsync();

                return await GetMemberByIdAsync(id) ?? throw new Exception("Member updated but not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating member: {MemberId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteMemberAsync(int id)
        {
            try
            {
                var member = await _context.Members.FindAsync(id);
                if (member == null) return false;

                // Soft delete - mark as inactive
                member.IsActive = false;
                member.UpdatedAt = _timezoneService.Now; // ✅ FIXED: Use Indonesia time

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting member: {MemberId}", id);
                throw;
            }
        }

        public async Task<bool> AddPointsAsync(int memberId, int points, string description, int? saleId = null, string? referenceNumber = null, string? createdBy = null)
        {
            try
            {
                var member = await _context.Members.FindAsync(memberId);
                if (member == null) return false;

                var memberPoint = new MemberPoint
                {
                    MemberId = memberId,
                    Points = points,
                    Type = PointTransactionType.Earn,
                    Description = description,
                    SaleId = saleId,
                    ReferenceNumber = referenceNumber,
                    CreatedAt = _timezoneService.Now, // ✅ FIXED: Use Indonesia time
                    CreatedBy = createdBy
                };

                _context.MemberPoints.Add(memberPoint);

                // Update member's last transaction date if this is from a sale
                if (saleId.HasValue)
                {
                    member.LastTransactionDate = _timezoneService.Now; // ✅ FIXED: Use Indonesia time
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding points for member: {MemberId}", memberId);
                throw;
            }
        }

        public async Task<bool> RedeemPointsAsync(int memberId, int points, string description, string? referenceNumber = null, string? createdBy = null)
        {
            try
            {
                var availablePoints = await GetAvailablePointsAsync(memberId);
                if (availablePoints < points)
                    throw new InvalidOperationException("Insufficient points");

                var memberPoint = new MemberPoint
                {
                    MemberId = memberId,
                    Points = -points,
                    Type = PointTransactionType.Redeem,
                    Description = description,
                    ReferenceNumber = referenceNumber,
                    CreatedAt = _timezoneService.Now, // ✅ FIXED: Use Indonesia time
                    CreatedBy = createdBy
                };

                _context.MemberPoints.Add(memberPoint);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error redeeming points for member: {MemberId}", memberId);
                throw;
            }
        }

        public async Task<List<MemberPointDto>> GetPointHistoryAsync(int memberId, int page = 1, int pageSize = 20)
        {
            try
            {
                return await _context.MemberPoints
                    .Where(mp => mp.MemberId == memberId)
                    .OrderByDescending(mp => mp.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(mp => new MemberPointDto
                    {
                        Id = mp.Id,
                        Points = mp.Points,
                        Type = mp.Type.ToString(),
                        Description = mp.Description,
                        ReferenceNumber = mp.ReferenceNumber,
                        CreatedAt = mp.CreatedAt,
                        IsEarning = mp.Points > 0,
                        IsRedemption = mp.Points < 0
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving point history for member: {MemberId}", memberId);
                throw;
            }
        }

        public async Task<int> GetAvailablePointsAsync(int memberId)
        {
            // selalu gunakan guard clause
            var memberExists = await _context.Members.AnyAsync(m => m.Id == memberId);
            if (!memberExists)
                throw new KeyNotFoundException($"Member {memberId} not found");

            return await _context.MemberPoints
                .Where(mp => mp.MemberId == memberId)
                .SumAsync(mp => mp.Points);
        }

        public async Task<MemberStatsDto> GetMemberStatsAsync(int memberId)
        {
            // guard clause
            var member = await _context.Members.FindAsync(memberId)
                         ?? throw new KeyNotFoundException($"Member {memberId} not found");

            var transactions = await _context.Sales
                .Where(s => s.MemberId == memberId && s.Status == SaleStatus.Completed)
                .ToListAsync();

            var totalPoints = await GetAvailablePointsAsync(memberId);

            return new MemberStatsDto
            {
                TotalTransactions = transactions.Count,
                TotalSpent = transactions.Sum(s => s.Total),
                AverageTransactionValue = transactions.Any() ? transactions.Average(s => s.Total) : 0,
                TotalPoints = await _context.MemberPoints
                    .Where(mp => mp.MemberId == memberId && mp.Points > 0)
                    .SumAsync(mp => mp.Points),
                AvailablePoints = totalPoints,
                LastTransactionDate = member.LastTransactionDate,
                MemberSince = member.JoinDate,
                CurrentTier = member.Tier.ToString()
            };
        }

        public async Task<List<TopMemberDto>> GetTopMembersAsync(int count = 10, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var query = _context.Sales
                    .Where(s => s.MemberId.HasValue && s.Status == SaleStatus.Completed);

                if (startDate.HasValue)
                    query = query.Where(s => s.SaleDate >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(s => s.SaleDate <= endDate.Value);

                return await query
                    .GroupBy(s => s.MemberId!.Value) // ✅ FIXED: Use ! operator to handle nullable warning
                    .Select(g => new TopMemberDto
                    {
                        MemberId = g.Key,
                        MemberName = g.First().Member!.Name, // ✅ FIXED: Use ! operator
                        MemberNumber = g.First().Member!.MemberNumber, // ✅ FIXED: Use ! operator
                        TransactionCount = g.Count(),
                        TotalSpent = g.Sum(s => s.Total),
                        AverageTransaction = g.Average(s => s.Total),
                        LastTransactionDate = g.Max(s => s.SaleDate)
                    })
                    .OrderByDescending(m => m.TotalSpent)
                    .Take(count)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top members");
                throw;
            }
        }

        public async Task<bool> IsPhoneExistsAsync(string phone, int? excludeId = null)
        {
            try
            {
                var query = _context.Members.Where(m => m.Phone == phone);

                if (excludeId.HasValue)
                    query = query.Where(m => m.Id != excludeId.Value);

                return await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking phone existence: {Phone}", phone);
                throw;
            }
        }

        public async Task<bool> IsMemberNumberExistsAsync(string memberNumber)
        {
            try
            {
                return await _context.Members.AnyAsync(m => m.MemberNumber == memberNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking member number existence: {MemberNumber}", memberNumber);
                throw;
            }
        }

        public async Task<bool> UpdateMemberTierAsync(int memberId)
        {
            try
            {
                var member = await _context.Members.FindAsync(memberId)
                    ?? throw new KeyNotFoundException($"Member {memberId} not found");

                var newTier = await CalculateMemberTierAsync(member.TotalSpent);
                if (member.Tier != newTier)
                {
                    member.Tier = newTier;
                    member.UpdatedAt = _timezoneService.Now; // ✅ FIXED: Use Indonesia time
                    await _context.SaveChangesAsync();

                    var bonusPoints = newTier switch
                    {
                        MembershipTier.Silver => 500,
                        MembershipTier.Gold => 1000,
                        MembershipTier.Platinum => 2000,
                        _ => 0
                    };
                    if (bonusPoints > 0)
                        await AddPointsAsync(memberId, bonusPoints,
                            $"Tier upgrade to {newTier}", null, null, "System");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating member tier: {MemberId}", memberId);
                throw;
            }
        }

        public async Task<MembershipTier> CalculateMemberTierAsync(decimal totalSpent)
        {
            // Add await to make it truly async (fixes warning)
            await Task.CompletedTask; // ✅ FIXED: Add await to avoid warning

            // langsung kembalikan enum, bukan string
            return totalSpent switch
            {
                >= 10_000_000 => MembershipTier.Platinum,
                >=  5_000_000 => MembershipTier.Gold,
                >=  1_000_000 => MembershipTier.Silver,
                _             => MembershipTier.Bronze
            };
        }

        // Private helper methods
        private async Task<string> GenerateMemberNumberAsync()
        {
            var year = _timezoneService.Now.Year; // ✅ FIXED: Use Indonesia time
            var memberCount = await _context.Members
                .Where(m => m.CreatedAt.Year == year)
                .CountAsync();

            return $"MBR{year}{(memberCount + 1):D6}";
        }
    }
}