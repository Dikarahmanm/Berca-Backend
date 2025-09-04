using System;
using System.Linq;
using System.Threading.Tasks;
using Berca_Backend.Data;
using Berca_Backend.DTOs;
using Berca_Backend.Models;
using Berca_Backend.Services;
using Berca_Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Berca_Backend.Tests
{
    public class CreditLedgerTests
    {
        private static AppDbContext NewContext(string name)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(name)
                .Options;
            return new AppDbContext(options);
        }

        private class TestTimezoneService : ITimezoneService
        {
            public DateTime UtcToLocal(DateTime utcDateTime) => utcDateTime;
            public DateTime LocalToUtc(DateTime localDateTime) => localDateTime;
            public DateTime Now => DateTime.UtcNow;
            public DateTime Today => Now.Date;
            public DateOnly TodayDate => DateOnly.FromDateTime(Today);
            public TimeZoneInfo IndonesiaTimeZone => TimeZoneInfo.Utc;
            public string FormatIndonesiaTime(DateTime dateTime, string format = "dd/MM/yyyy HH:mm:ss") => dateTime.ToString(format);
        }

        [Fact]
        public async Task Reconcile_Marks_Oldest_Sales_As_Completed_When_Paid()
        {
            // Arrange
            var ctx = NewContext(nameof(Reconcile_Marks_Oldest_Sales_As_Completed_When_Paid));
            var logger = Mock.Of<ILogger<MemberService>>();
            var tz = new TestTimezoneService();
            var member = new Member { Name = "M", Phone = "1", MemberNumber = "X", CreditLimit = 100000, CurrentDebt = 500 };
            ctx.Members.Add(member);
            await ctx.SaveChangesAsync();

            var due1 = DateTime.UtcNow.AddDays(-5);
            var due2 = DateTime.UtcNow.AddDays(10);
            ctx.MemberCreditTransactions.AddRange(
                new MemberCreditTransaction { MemberId = member.Id, Type = CreditTransactionType.CreditSale, Amount = 1000, DueDate = due1, Status = CreditTransactionStatus.Pending, CreatedBy = 1 },
                new MemberCreditTransaction { MemberId = member.Id, Type = CreditTransactionType.CreditSale, Amount = 500, DueDate = due2, Status = CreditTransactionStatus.Pending, CreatedBy = 1 }
            );
            await ctx.SaveChangesAsync();

            var svc = new MemberService(ctx, logger, tz);

            // Act
            var ok = await svc.ReconcileCreditLedgerAsync(member.Id);

            // Assert
            Assert.True(ok);
            var txs = await ctx.MemberCreditTransactions.Where(t => t.MemberId == member.Id).OrderBy(t => t.DueDate).ToListAsync();
            Assert.Equal(CreditTransactionStatus.Completed, txs[0].Status);
            Assert.Equal(CreditTransactionStatus.Pending, txs[1].Status);

            var updatedMember = await ctx.Members.FindAsync(member.Id);
            Assert.Equal(due2.Date, updatedMember!.NextPaymentDueDate!.Value.Date);
        }

        [Fact]
        public async Task HasOverdue_IsFalse_When_DueDate_In_Future()
        {
            // Arrange
            var ctx = NewContext(nameof(HasOverdue_IsFalse_When_DueDate_In_Future));
            var logger = Mock.Of<ILogger<MemberService>>();
            var tz = new TestTimezoneService();
            var member = new Member { Name = "M", Phone = "1", MemberNumber = "X", CreditLimit = 100000, CurrentDebt = 1000, NextPaymentDueDate = DateTime.UtcNow.AddDays(7) };
            ctx.Members.Add(member);
            await ctx.SaveChangesAsync();
            var svc = new MemberService(ctx, logger, tz);

            // Act
            var overdue = await svc.HasOverduePaymentsAsync(member.Id);

            // Assert
            Assert.False(overdue);
        }

        [Fact]
        public async Task ManagerOverride_Bypasses_Overdue_During_Validation()
        {
            // Arrange
            var ctx = NewContext(nameof(ManagerOverride_Bypasses_Overdue_During_Validation));
            var posLogger = Mock.Of<ILogger<POSService>>();

            var member = new Member { Name = "M", Phone = "1", MemberNumber = "X", CreditLimit = 100000, CurrentDebt = 1000 };
            ctx.Members.Add(member);
            await ctx.SaveChangesAsync();

            var ms = new Mock<IMemberService>();
            ms.Setup(m => m.GetCreditSummaryAsync(member.Id))
              .ReturnsAsync(new MemberCreditSummaryDto { CreditLimit = 100000, IsEligible = true, TotalDelayedPayments = 0 });
            ms.Setup(m => m.CalculateAvailableCreditAsync(member.Id)).ReturnsAsync(100000);
            ms.Setup(m => m.CalculateCreditUtilizationAsync(member.Id)).ReturnsAsync(0);
            ms.Setup(m => m.CalculateCreditScoreAsync(member.Id)).ReturnsAsync(700);
            ms.Setup(m => m.CalculateMaxTransactionAmountAsync(member.Id)).ReturnsAsync(100000);
            ms.Setup(m => m.HasOverduePaymentsAsync(member.Id)).ReturnsAsync(true); // Simulate overdue

            // Unused dependencies can be mocked as well
            var prod = new Mock<IProductService>().Object;
            var notif = new Mock<INotificationService>().Object;
            var dash = new Mock<IDashboardService>().Object;

            var tz = new TestTimezoneService();
            var svc = new POSService(ctx, posLogger, prod, ms.Object, tz, notif, dash);

            var req = new CreditValidationRequestDto
            {
                MemberId = member.Id,
                RequestedAmount = 500,
                BranchId = 1,
                OverrideWarnings = true // Manager override
            };

            // Act
            var result = await svc.ValidateMemberCreditAsync(req);

            // Assert
            Assert.True(result.IsApproved);
            Assert.Contains(result.Warnings, w => w.Contains("Overdue payments overridden", StringComparison.OrdinalIgnoreCase));
        }
    }
}
