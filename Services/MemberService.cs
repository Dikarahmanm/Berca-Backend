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
                        // ✅ FIXED: Use database columns that are now properly maintained
                        TotalPoints = m.TotalPoints,
                        UsedPoints = m.UsedPoints,
                        AvailablePoints = m.TotalPoints - m.UsedPoints, // ✅ Computed property
                        TotalTransactions = m.TotalTransactions,
                        AverageTransactionValue = m.TotalTransactions > 0 ? m.TotalSpent / m.TotalTransactions : 0,
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
                        // ✅ FIXED: Use database columns that are now properly maintained
                        TotalPoints = m.TotalPoints,
                        UsedPoints = m.UsedPoints,
                        AvailablePoints = m.TotalPoints - m.UsedPoints, // ✅ Computed property
                        TotalTransactions = m.TotalTransactions,
                        AverageTransactionValue = m.TotalTransactions > 0 ? m.TotalSpent / m.TotalTransactions : 0,
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
                        // ✅ FIXED: Use database columns that are now properly maintained
                        TotalPoints = m.TotalPoints,
                        UsedPoints = m.UsedPoints,
                        AvailablePoints = m.TotalPoints - m.UsedPoints, // ✅ Computed property
                        TotalTransactions = m.TotalTransactions,
                        AverageTransactionValue = m.TotalTransactions > 0 ? m.TotalSpent / m.TotalTransactions : 0,
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
                        // ✅ FIXED: Use database columns that are now properly maintained
                        TotalPoints = m.TotalPoints,
                        UsedPoints = m.UsedPoints,
                        AvailablePoints = m.TotalPoints - m.UsedPoints, // ✅ Computed property
                        TotalTransactions = m.TotalTransactions,
                        AverageTransactionValue = m.TotalTransactions > 0 ? m.TotalSpent / m.TotalTransactions : 0,
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

        
        public async Task<bool> AddPointsAsync(int memberId, int points, string description, int? saleId = null, string? referenceNumber = null, string? createdBy = null)
        {
            try
            {
                var member = await _context.Members.FindAsync(memberId);
                if (member == null) return false;

                // ✅ FIXED: Validate SaleId exists if provided
                if (saleId.HasValue)
                {
                    var saleExists = await _context.Sales.AnyAsync(s => s.Id == saleId.Value);
                    if (!saleExists)
                    {
                        _logger.LogWarning("Sale {SaleId} not found when adding points, setting SaleId to null", saleId);
                        saleId = null;
                    }
                }

                // Create MemberPoint transaction record
                var memberPoint = new MemberPoint
                {
                    MemberId = memberId,
                    Points = points,
                    Type = PointTransactionType.Earn,
                    Description = description,
                    SaleId = saleId,
                    ReferenceNumber = referenceNumber,
                    CreatedAt = _timezoneService.Now,
                    CreatedBy = createdBy
                };

                _context.MemberPoints.Add(memberPoint);

                // ✅ CRITICAL FIX: Update Member table's point columns
                if (points > 0)
                {
                    // Adding points - update TotalPoints
                    member.TotalPoints += points;
                }
                else
                {
                    // Negative points (shouldn't happen in AddPoints, but defensive)
                    member.UsedPoints += Math.Abs(points);
                }

                // Update member's last transaction date if this is from a sale
                if (saleId.HasValue)
                {
                    member.LastTransactionDate = _timezoneService.Now;
                }

                member.UpdatedAt = _timezoneService.Now;

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

                var member = await _context.Members.FindAsync(memberId);
                if (member == null) return false;

                // Create MemberPoint transaction record
                var memberPoint = new MemberPoint
                {
                    MemberId = memberId,
                    Points = -points, // Negative for redemption
                    Type = PointTransactionType.Redeem,
                    Description = description,
                    ReferenceNumber = referenceNumber,
                    CreatedAt = _timezoneService.Now,
                    CreatedBy = createdBy
                };

                _context.MemberPoints.Add(memberPoint);

                // ✅ CRITICAL FIX: Update Member table's point columns
                member.UsedPoints += points;
                member.UpdatedAt = _timezoneService.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error redeeming points for member: {MemberId}", memberId);
                throw;
            }
        }

        // ✅ NEW METHOD: Update member spending statistics
        public async Task<bool> UpdateMemberStatsAsync(int memberId, decimal transactionAmount, int transactionCount = 1)
        {
            try
            {
                var member = await _context.Members.FindAsync(memberId);
                if (member == null) return false;

                member.TotalSpent += transactionAmount;
                member.TotalTransactions += transactionCount;
                member.LastTransactionDate = _timezoneService.Now;
                member.UpdatedAt = _timezoneService.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating member stats: {MemberId}", memberId);
                throw;
            }
        }

        // ==================== CREDIT MANAGEMENT IMPLEMENTATION ==================== //

        public async Task<bool> GrantCreditAsync(int memberId, decimal amount, string description, int saleId)
        {
            try
            {
                _logger.LogInformation("Granting credit: MemberId={MemberId}, Amount={Amount}, SaleId={SaleId}", 
                    memberId, amount, saleId);

                // 1. Get member with credit information
                var member = await _context.Members.FindAsync(memberId);
                if (member == null)
                {
                    _logger.LogWarning("Member not found: {MemberId}", memberId);
                    return false;
                }

                // 2. Check credit eligibility
                var eligibility = await CheckCreditEligibilityAsync(memberId, amount);
                if (!eligibility.IsEligible)
                {
                    _logger.LogWarning("Credit denied for member {MemberId}: {Reason}", memberId, eligibility.DecisionReason);
                    return false;
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 3. Calculate due date based on payment terms
                    var dueDate = _timezoneService.Now.AddDays(member.PaymentTerms);

                    // 4. Create credit transaction
                    var creditTransaction = new MemberCreditTransaction
                    {
                        MemberId = memberId,
                        Type = CreditTransactionType.CreditSale,
                        Amount = amount,
                        TransactionDate = _timezoneService.Now,
                        DueDate = dueDate,
                        Description = description,
                        ReferenceNumber = $"SALE-{saleId}",
                        Status = CreditTransactionStatus.Completed,
                        CreatedBy = 1, // TODO: Get from current user context
                        CreatedAt = _timezoneService.Now,
                        UpdatedAt = _timezoneService.Now
                    };

                    _context.MemberCreditTransactions.Add(creditTransaction);

                    // 5. Update member debt information
                    member.CurrentDebt += amount;
                    member.LifetimeDebt += amount;
                    member.NextPaymentDueDate = member.NextPaymentDueDate == null || dueDate < member.NextPaymentDueDate 
                        ? dueDate : member.NextPaymentDueDate;
                    member.UpdatedAt = _timezoneService.Now;

                    // 6. Update credit status if needed
                    await UpdateCreditStatusAsync(memberId);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Credit granted successfully: MemberId={MemberId}, Amount={Amount}", 
                        memberId, amount);
                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error granting credit: MemberId={MemberId}, Amount={Amount}", memberId, amount);
                throw;
            }
        }

        public async Task<bool> RecordPaymentAsync(int memberId, decimal amount, string paymentMethod, string? reference = null)
        {
            try
            {
                _logger.LogInformation("Recording payment: MemberId={MemberId}, Amount={Amount}, Method={PaymentMethod}", 
                    memberId, amount, paymentMethod);

                // 1. Validate payment
                if (amount <= 0)
                {
                    _logger.LogWarning("Invalid payment amount: {Amount}", amount);
                    return false;
                }

                var member = await _context.Members.FindAsync(memberId);
                if (member == null || member.CurrentDebt <= 0)
                {
                    _logger.LogWarning("Member not found or no debt: MemberId={MemberId}, CurrentDebt={CurrentDebt}", 
                        memberId, member?.CurrentDebt);
                    return false;
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 2. Create payment transaction
                    var paymentTransaction = new MemberCreditTransaction
                    {
                        MemberId = memberId,
                        Type = CreditTransactionType.Payment,
                        Amount = -amount, // Negative to reduce debt
                        TransactionDate = _timezoneService.Now,
                        Description = $"Payment via {paymentMethod}",
                        ReferenceNumber = reference,
                        Status = CreditTransactionStatus.Completed,
                        CreatedBy = 1, // TODO: Get from current user context
                        Notes = $"Payment method: {paymentMethod}",
                        CreatedAt = _timezoneService.Now,
                        UpdatedAt = _timezoneService.Now
                    };

                    _context.MemberCreditTransactions.Add(paymentTransaction);

                    // 3. Update member debt (cannot go below 0)
                    var actualPayment = Math.Min(amount, member.CurrentDebt);
                    member.CurrentDebt -= actualPayment;
                    member.LastPaymentDate = _timezoneService.Now;

                    // 4. Recalculate next payment due date
                    if (member.CurrentDebt > 0)
                    {
                        var oldestUnpaidTransaction = await _context.MemberCreditTransactions
                            .Where(t => t.MemberId == memberId && 
                                   t.Type == CreditTransactionType.CreditSale && 
                                   t.DueDate.HasValue)
                            .OrderBy(t => t.DueDate)
                            .FirstOrDefaultAsync();

                        member.NextPaymentDueDate = oldestUnpaidTransaction?.DueDate;
                    }
                    else
                    {
                        member.NextPaymentDueDate = null;
                    }

                    member.UpdatedAt = _timezoneService.Now;

                    // 5. Update credit status
                    await UpdateCreditStatusAsync(memberId);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Payment recorded successfully: MemberId={MemberId}, Amount={Amount}", 
                        memberId, actualPayment);
                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording payment: MemberId={MemberId}, Amount={Amount}", memberId, amount);
                throw;
            }
        }

        public async Task<List<MemberCreditTransactionDto>> GetCreditHistoryAsync(int memberId, int days = 90)
        {
            try
            {
                var fromDate = _timezoneService.Now.AddDays(-days);

                var transactions = await _context.MemberCreditTransactions
                    .Include(t => t.Member)
                    .Include(t => t.CreatedByUser)
                    .Include(t => t.Branch)
                    .Where(t => t.MemberId == memberId && t.TransactionDate >= fromDate)
                    .OrderByDescending(t => t.TransactionDate)
                    .Select(t => new MemberCreditTransactionDto
                    {
                        Id = t.Id,
                        MemberId = t.MemberId,
                        MemberName = t.Member.Name,
                        Type = t.Type,
                        TypeDescription = t.Type.ToString(),
                        Amount = t.Amount,
                        TransactionDate = t.TransactionDate,
                        DueDate = t.DueDate,
                        Description = t.Description,
                        ReferenceNumber = t.ReferenceNumber,
                        Status = t.Status,
                        StatusDescription = t.Status.ToString(),
                        BranchName = t.Branch != null ? t.Branch.BranchName : null,
                        CreatedByUserName = t.CreatedByUser.Username,
                        CreatedAt = t.CreatedAt,
                        IncreasesDebt = t.IncreasesDebt,
                        ReducesDebt = t.ReducesDebt,
                        DaysUntilDue = t.DaysUntilDue,
                        IsOverdue = t.IsOverdue,
                        FormattedAmount = $"IDR {t.Amount:N0}"
                    })
                    .ToListAsync();

                return transactions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting credit history: MemberId={MemberId}", memberId);
                throw;
            }
        }

        public async Task<MemberCreditSummaryDto> GetCreditSummaryAsync(int memberId)
        {
            try
            {
                var member = await _context.Members
                    .Include(m => m.CreditTransactions.OrderByDescending(t => t.TransactionDate).Take(10))
                    .Include(m => m.PaymentReminders.OrderByDescending(r => r.ReminderDate).Take(5))
                    .FirstOrDefaultAsync(m => m.Id == memberId);

                if (member == null)
                {
                    throw new ArgumentException($"Member with ID {memberId} not found");
                }

                // Get recent transactions
                var recentTransactions = member.CreditTransactions
                    .Select(t => new MemberCreditTransactionDto
                    {
                        Id = t.Id,
                        Type = t.Type,
                        Amount = t.Amount,
                        TransactionDate = t.TransactionDate,
                        Description = t.Description,
                        FormattedAmount = $"IDR {t.Amount:N0}"
                    })
                    .ToList();

                // Calculate overdue amount
                var overdueAmount = member.DaysOverdue > 0 ? member.CurrentDebt : 0;

                var summary = new MemberCreditSummaryDto
                {
                    MemberId = member.Id,
                    MemberName = member.Name,
                    MemberNumber = member.MemberNumber,
                    Phone = member.Phone,
                    Tier = member.Tier,
                    CreditLimit = member.CreditLimit,
                    CurrentDebt = member.CurrentDebt,
                    AvailableCredit = member.AvailableCredit,
                    CreditUtilization = member.CreditUtilization,
                    Status = member.CreditStatus,
                    StatusDescription = member.CreditStatus.ToString(),
                    PaymentTerms = member.PaymentTerms,
                    LastPaymentDate = member.LastPaymentDate,
                    NextPaymentDueDate = member.NextPaymentDueDate,
                    DaysOverdue = member.DaysOverdue,
                    OverdueAmount = overdueAmount,
                    CreditScore = member.CreditScore,
                    CreditGrade = GetCreditGrade(member.CreditScore),
                    PaymentSuccessRate = member.PaymentSuccessRate,
                    PaymentDelays = member.PaymentDelays,
                    LifetimeDebt = member.LifetimeDebt,
                    RecentTransactions = recentTransactions,
                    RemindersSent = member.PaymentReminders.Count,
                    LastReminderDate = member.PaymentReminders.FirstOrDefault()?.ReminderDate,
                    IsEligibleForCredit = member.CreditStatus == CreditStatus.Good || member.CreditStatus == CreditStatus.Warning,
                    RiskLevel = GetRiskLevel(member.CreditScore, member.DaysOverdue),
                    RequiresAttention = member.DaysOverdue > 0 || member.CreditUtilization > 80,
                    FormattedCreditLimit = $"IDR {member.CreditLimit:N0}",
                    FormattedCurrentDebt = $"IDR {member.CurrentDebt:N0}",
                    FormattedAvailableCredit = $"IDR {member.AvailableCredit:N0}"
                };

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting credit summary: MemberId={MemberId}", memberId);
                throw;
            }
        }

        public async Task<List<MemberDebtDto>> GetOverdueMembersAsync(int? branchId = null)
        {
            try
            {
                var query = _context.Members
                    .Where(m => m.CurrentDebt > 0 && 
                               m.NextPaymentDueDate.HasValue && 
                               m.NextPaymentDueDate.Value.Date < _timezoneService.Today);

                if (branchId.HasValue)
                {
                    // Filter by members who have transactions in the specific branch
                    query = query.Where(m => m.CreditTransactions.Any(t => t.BranchId == branchId));
                }

                var overdueMembers = await query
                    .Include(m => m.PaymentReminders)
                    .OrderByDescending(m => m.NextPaymentDueDate)
                    .Select(m => new MemberDebtDto
                    {
                        MemberId = m.Id,
                        MemberName = m.Name,
                        MemberNumber = m.MemberNumber,
                        Phone = m.Phone,
                        Email = m.Email,
                        Tier = m.Tier,
                        TotalDebt = m.CurrentDebt,
                        OverdueAmount = m.CurrentDebt, // All debt is overdue for these members
                        DaysOverdue = m.DaysOverdue,
                        LastPaymentDate = m.LastPaymentDate,
                        NextDueDate = m.NextPaymentDueDate,
                        Status = m.CreditStatus,
                        StatusDescription = m.CreditStatus.ToString(),
                        RemindersSent = m.PaymentReminders.Count,
                        LastReminderDate = m.PaymentReminders.OrderByDescending(r => r.ReminderDate).FirstOrDefault().ReminderDate,
                        RecommendedAction = GetRecommendedAction(m.DaysOverdue, m.PaymentReminders.Count),
                        CollectionPriority = GetCollectionPriority(m.DaysOverdue, m.CurrentDebt),
                        CreditLimit = m.CreditLimit,
                        AvailableCredit = m.AvailableCredit,
                        CreditScore = m.CreditScore,
                        IsHighRisk = m.DaysOverdue > 30 || m.CreditScore < 580,
                        RequiresUrgentAction = m.DaysOverdue > 60,
                        FormattedTotalDebt = $"IDR {m.CurrentDebt:N0}",
                        FormattedOverdueAmount = $"IDR {m.CurrentDebt:N0}"
                    })
                    .ToListAsync();

                return overdueMembers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting overdue members");
                throw;
            }
        }

        public async Task<List<MemberDebtDto>> GetMembersApproachingLimitAsync(decimal thresholdPercentage = 80)
        {
            try
            {
                var members = await _context.Members
                    .Where(m => m.CreditLimit > 0 && 
                               m.CurrentDebt > 0 && 
                               (m.CurrentDebt / m.CreditLimit * 100) >= thresholdPercentage)
                    .OrderByDescending(m => m.CreditUtilization)
                    .Select(m => new MemberDebtDto
                    {
                        MemberId = m.Id,
                        MemberName = m.Name,
                        MemberNumber = m.MemberNumber,
                        Phone = m.Phone,
                        Email = m.Email,
                        Tier = m.Tier,
                        TotalDebt = m.CurrentDebt,
                        DaysOverdue = m.DaysOverdue,
                        Status = m.CreditStatus,
                        CreditLimit = m.CreditLimit,
                        AvailableCredit = m.AvailableCredit,
                        CreditScore = m.CreditScore,
                        RecommendedAction = m.CreditUtilization > 90 ? "Immediate attention - near credit limit" : "Monitor closely",
                        FormattedTotalDebt = $"IDR {m.CurrentDebt:N0}"
                    })
                    .ToListAsync();

                return members;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting members approaching limit");
                throw;
            }
        }

        public async Task<bool> UpdateCreditStatusAsync(int memberId)
        {
            try
            {
                var member = await _context.Members.FindAsync(memberId);
                if (member == null) return false;

                var oldStatus = member.CreditStatus;
                var newStatus = CalculateCreditStatus(member);

                if (oldStatus != newStatus)
                {
                    member.CreditStatus = newStatus;
                    member.UpdatedAt = _timezoneService.Now;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Credit status updated: MemberId={MemberId}, Old={OldStatus}, New={NewStatus}", 
                        memberId, oldStatus, newStatus);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating credit status: MemberId={MemberId}", memberId);
                throw;
            }
        }

        public async Task<CreditEligibilityDto> CheckCreditEligibilityAsync(int memberId, decimal requestedAmount)
        {
            try
            {
                var member = await _context.Members.FindAsync(memberId);
                if (member == null)
                {
                    return new CreditEligibilityDto
                    {
                        MemberId = memberId,
                        IsEligible = false,
                        DecisionReason = "Member not found",
                        RequirementsNotMet = new List<string> { "Member does not exist" }
                    };
                }

                var result = new CreditEligibilityDto
                {
                    MemberId = memberId,
                    MemberName = member.Name,
                    RequestedAmount = requestedAmount,
                    CurrentUtilization = member.CreditUtilization,
                    CreditScore = member.CreditScore,
                    Status = member.CreditStatus,
                    AvailableCredit = member.AvailableCredit,
                    RequirementsNotMet = new List<string>(),
                    RiskFactors = new List<string>(),
                    Recommendations = new List<string>()
                };

                // Check eligibility criteria
                bool isEligible = true;

                // 1. Credit status check
                if (member.CreditStatus == CreditStatus.Suspended || member.CreditStatus == CreditStatus.Blacklisted)
                {
                    isEligible = false;
                    result.RequirementsNotMet.Add($"Credit status is {member.CreditStatus}");
                }

                // 2. Available credit check
                if (requestedAmount > member.AvailableCredit)
                {
                    isEligible = false;
                    result.RequirementsNotMet.Add($"Requested amount ({requestedAmount:C}) exceeds available credit ({member.AvailableCredit:C})");
                }

                // 3. Credit score check
                if (member.CreditScore < 580 && requestedAmount > 500000) // 500K IDR limit for poor credit
                {
                    isEligible = false;
                    result.RequirementsNotMet.Add("Credit score too low for large transactions");
                }

                // 4. Overdue check
                if (member.DaysOverdue > 0)
                {
                    isEligible = false;
                    result.RequirementsNotMet.Add($"Account is {member.DaysOverdue} days overdue");
                }

                // Set approval amount
                if (isEligible)
                {
                    result.ApprovedAmount = requestedAmount;
                }
                else
                {
                    // Calculate maximum approvable amount
                    result.ApprovedAmount = Math.Min(member.AvailableCredit, 
                        member.CreditScore < 580 ? 500000 : requestedAmount);
                }

                result.IsEligible = isEligible;
                result.DecisionReason = isEligible ? "Credit approved" : string.Join("; ", result.RequirementsNotMet);
                result.RiskLevel = GetRiskLevel(member.CreditScore, member.DaysOverdue);
                result.RequiresManagerApproval = requestedAmount > 2000000 || member.CreditScore < 650; // 2M IDR or Fair credit

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking credit eligibility: MemberId={MemberId}", memberId);
                throw;
            }
        }

        // ==================== HELPER METHODS ==================== //

        private CreditStatus CalculateCreditStatus(Member member)
        {
            // Business rules for credit status
            if (member.DaysOverdue >= 90) return CreditStatus.Blacklisted;
            if (member.DaysOverdue >= 15) return CreditStatus.Suspended;
            if (member.DaysOverdue > 0) return CreditStatus.Overdue;
            if (member.CreditUtilization > 90 || member.PaymentDelays > 3) return CreditStatus.Warning;
            return CreditStatus.Good;
        }

        private string GetCreditGrade(int creditScore)
        {
            return creditScore switch
            {
                >= 800 => "Excellent",
                >= 740 => "Very Good",
                >= 670 => "Good",
                >= 580 => "Fair",
                _ => "Poor"
            };
        }

        private string GetRiskLevel(int creditScore, int daysOverdue)
        {
            if (daysOverdue > 60 || creditScore < 500) return "Very High";
            if (daysOverdue > 30 || creditScore < 580) return "High";
            if (daysOverdue > 7 || creditScore < 670) return "Medium";
            return "Low";
        }

        private string GetRecommendedAction(int daysOverdue, int remindersSent)
        {
            return daysOverdue switch
            {
                > 90 => "Legal action / Collections agency",
                > 60 => "Manager intervention required",
                > 30 => "Phone call + in-person visit",
                > 15 => "Email + SMS reminder",
                > 7 => "SMS reminder",
                _ => "No action needed"
            };
        }

        private string GetCollectionPriority(int daysOverdue, decimal debtAmount)
        {
            if (daysOverdue > 90 || debtAmount > 5000000) return "Critical";
            if (daysOverdue > 60 || debtAmount > 2000000) return "High";
            if (daysOverdue > 30 || debtAmount > 1000000) return "Medium";
            return "Low";
        }

        // ==================== STUB IMPLEMENTATIONS (TO BE COMPLETED) ==================== //

        public async Task<List<PaymentReminderDto>> GetPaymentRemindersAsync(DateTime? reminderDate = null)
        {
            // TODO: Implement payment reminder logic
            await Task.CompletedTask;
            return new List<PaymentReminderDto>();
        }

        public async Task<bool> SendPaymentReminderAsync(int memberId)
        {
            // TODO: Implement reminder sending logic
            await Task.CompletedTask;
            return true;
        }

        public async Task<int> CalculateCreditScoreAsync(int memberId)
        {
            // TODO: Implement credit score calculation algorithm
            await Task.CompletedTask;
            return 600; // Default score
        }

        public async Task<CreditAnalyticsDto> GetCreditAnalyticsAsync(int? branchId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            // TODO: Implement credit analytics
            await Task.CompletedTask;
            return new CreditAnalyticsDto();
        }

        public async Task<decimal> UpdateCreditLimitAsync(int memberId)
        {
            // TODO: Implement credit limit update logic based on tier and payment history
            await Task.CompletedTask;
            return 0;
        }
    }
}