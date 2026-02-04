// POS.Application/Features/Dashboard/DashboardQuery.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.Dashboard
{
    public class DashboardQuery : IRequest<DashboardInfo>
    {
    }

    public class DashboardQueryHandler : IRequestHandler<DashboardQuery, DashboardInfo>
    {
        private readonly IMyAppDbContext _context;

        public DashboardQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardInfo> Handle(DashboardQuery request, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            var today = now.Date;
            var thisWeekStart = now.AddDays(-(int)now.DayOfWeek);
            var thisMonthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, now.Offset);
            var lastMonthStart = thisMonthStart.AddMonths(-1);

            // Total Books
            var totalBooks = await _context.Books
                .Where(b => !b.IsDeleted)
                .CountAsync(cancellationToken);

            var booksLastMonth = await _context.Books
                .Where(b => !b.IsDeleted && b.CreatedDate < thisMonthStart)
                .CountAsync(cancellationToken);

            var booksPercentageChange = CalculatePercentageChange(totalBooks, booksLastMonth);

            // Active Members
            var activeMembers = await _context.LibraryMembers
                .Where(m => !m.IsDeleted && m.IsActive)
                .CountAsync(cancellationToken);

            var activeMembersLastMonth = await _context.LibraryMembers
                .Where(m => !m.IsDeleted && m.IsActive && m.CreatedDate < thisMonthStart)
                .CountAsync(cancellationToken);

            var activeMembersPercentageChange = CalculatePercentageChange(activeMembers, activeMembersLastMonth);

            // Books Borrowed (Currently borrowed)
            var booksBorrowed = await _context.BookIssues
                .Where(bi => !bi.IsDeleted && bi.ReturnDate == null && (bi.Status == "Issued" || bi.Status == "Renew"))
                .CountAsync(cancellationToken);

            var booksBorrowedLastWeek = await _context.BookIssues
                .Where(bi => !bi.IsDeleted && bi.IssueDate < thisWeekStart && bi.ReturnDate == null && (bi.Status == "Issued" || bi.Status == "Renew"))
                .CountAsync(cancellationToken);

            var booksBorrowedPercentageChange = CalculatePercentageChange(booksBorrowed, booksBorrowedLastWeek);

            // Categories
            var totalCategories = await _context.Categories
                .Where(c => !c.IsDeleted)
                .CountAsync(cancellationToken);

            // Pending Returns (Overdue books)
            var pendingReturns = await _context.BookIssues
                .Where(bi => !bi.IsDeleted && bi.ReturnDate == null && bi.DueDate < now)
                .CountAsync(cancellationToken);

            var dueToday = await _context.BookIssues
                .Where(bi => !bi.IsDeleted && bi.ReturnDate == null && bi.DueDate.Date == today)
                .CountAsync(cancellationToken);

            // Monthly Borrowing Statistics (last 6 months)
            var monthlyStats = await GetMonthlyBorrowingStatistics(cancellationToken);

            // Return Trends (last 30 days)
            var returnTrends = await GetReturnTrends(cancellationToken);

            return new DashboardInfo
            {
                TotalBooks = totalBooks,
                TotalBooksPercentageChange = booksPercentageChange,
                ActiveMembers = activeMembers,
                ActiveMembersPercentageChange = activeMembersPercentageChange,
                BooksBorrowed = booksBorrowed,
                BooksBorrowedPercentageChange = booksBorrowedPercentageChange,
                TotalCategories = totalCategories,
                PendingReturns = pendingReturns,
                DueToday = dueToday,
                MonthlyBorrowingStatistics = monthlyStats,
                ReturnTrends = returnTrends
            };
        }

        private async Task<List<MonthlyStatistic>> GetMonthlyBorrowingStatistics(CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            var sixMonthsAgo = now.AddMonths(-6);

            var stats = await _context.BookIssues
                .Where(bi => !bi.IsDeleted && bi.IssueDate >= sixMonthsAgo)
                .GroupBy(bi => new { bi.IssueDate.Year, bi.IssueDate.Month })
                .Select(g => new MonthlyStatistic
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count()
                })
                .OrderBy(s => s.Year)
                .ThenBy(s => s.Month)
                .ToListAsync(cancellationToken);

            // Fill in missing months with zero counts
            var result = new List<MonthlyStatistic>();
            for (int i = 5; i >= 0; i--)
            {
                var date = now.AddMonths(-i);
                var existing = stats.FirstOrDefault(s => s.Year == date.Year && s.Month == date.Month);
                
                result.Add(new MonthlyStatistic
                {
                    Year = date.Year,
                    Month = date.Month,
                    MonthName = date.ToString("MMM"),
                    Count = existing?.Count ?? 0
                });
            }

            return result;
        }

        private async Task<List<DailyStatistic>> GetReturnTrends(CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            var thirtyDaysAgo = now.AddDays(-30);

            var stats = await _context.BookIssues
                .Where(bi => !bi.IsDeleted && bi.ReturnDate.HasValue && bi.ReturnDate >= thirtyDaysAgo)
                .GroupBy(bi => bi.ReturnDate.Value.Date)
                .Select(g => new DailyStatistic
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(s => s.Date)
                .ToListAsync(cancellationToken);

            // Fill in missing days with zero counts
            var result = new List<DailyStatistic>();
            for (int i = 29; i >= 0; i--)
            {
                var date = now.AddDays(-i).Date;
                var existing = stats.FirstOrDefault(s => s.Date == date);
                
                result.Add(new DailyStatistic
                {
                    Date = date,
                    DayLabel = date.ToString("MMM dd"),
                    Count = existing?.Count ?? 0
                });
            }

            return result;
        }

        private decimal CalculatePercentageChange(int current, int previous)
        {
            if (previous == 0)
            {
                return current > 0 ? 100 : 0;
            }

            return Math.Round(((decimal)(current - previous) / previous) * 100, 2);
        }
    }
}