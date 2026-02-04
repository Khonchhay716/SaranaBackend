// POS.Application/Features/Dashboard/GetQuickStatsQuery.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.Dashboard
{
    /// <summary>
    /// Get quick statistics for dashboard cards only (lightweight query)
    /// </summary>
    public class GetQuickStatsQuery : IRequest<QuickStatsInfo>
    {
    }

    public class GetQuickStatsQueryHandler : IRequestHandler<GetQuickStatsQuery, QuickStatsInfo>
    {
        private readonly IMyAppDbContext _context;

        public GetQuickStatsQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<QuickStatsInfo> Handle(GetQuickStatsQuery request, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            var today = now.Date;
            var thisWeekStart = now.AddDays(-(int)now.DayOfWeek);
            var thisMonthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, now.Offset);

            // Get all stats in parallel
            var totalBooksTask = _context.Books
                .Where(b => !b.IsDeleted)
                .CountAsync(cancellationToken);

            var booksLastMonthTask = _context.Books
                .Where(b => !b.IsDeleted && b.CreatedDate < thisMonthStart)
                .CountAsync(cancellationToken);

            var activeMembersTask = _context.LibraryMembers
                .Where(m => !m.IsDeleted && m.IsActive)
                .CountAsync(cancellationToken);

            var activeMembersLastMonthTask = _context.LibraryMembers
                .Where(m => !m.IsDeleted && m.IsActive && m.CreatedDate < thisMonthStart)
                .CountAsync(cancellationToken);

            var booksBorrowedTask = _context.BookIssues
                .Where(bi => !bi.IsDeleted && bi.ReturnDate == null && (bi.Status == "Issued" || bi.Status == "Renew"))
                .CountAsync(cancellationToken);

            var booksBorrowedLastWeekTask = _context.BookIssues
                .Where(bi => !bi.IsDeleted && bi.IssueDate < thisWeekStart && bi.ReturnDate == null && (bi.Status == "Issued" || bi.Status == "Renew"))
                .CountAsync(cancellationToken);

            var totalCategoriesTask = _context.Categories
                .Where(c => !c.IsDeleted)
                .CountAsync(cancellationToken);

            var pendingReturnsTask = _context.BookIssues
                .Where(bi => !bi.IsDeleted && bi.ReturnDate == null && bi.DueDate < now)
                .CountAsync(cancellationToken);

            var dueTodayTask = _context.BookIssues
                .Where(bi => !bi.IsDeleted && bi.ReturnDate == null && bi.DueDate.Date == today)
                .CountAsync(cancellationToken);

            // Wait for all tasks
            await Task.WhenAll(
                totalBooksTask,
                booksLastMonthTask,
                activeMembersTask,
                activeMembersLastMonthTask,
                booksBorrowedTask,
                booksBorrowedLastWeekTask,
                totalCategoriesTask,
                pendingReturnsTask,
                dueTodayTask
            );

            var totalBooks = totalBooksTask.Result;
            var booksLastMonth = booksLastMonthTask.Result;
            var activeMembers = activeMembersTask.Result;
            var activeMembersLastMonth = activeMembersLastMonthTask.Result;
            var booksBorrowed = booksBorrowedTask.Result;
            var booksBorrowedLastWeek = booksBorrowedLastWeekTask.Result;

            return new QuickStatsInfo
            {
                TotalBooks = totalBooks,
                TotalBooksPercentageChange = CalculatePercentageChange(totalBooks, booksLastMonth),
                ActiveMembers = activeMembers,
                ActiveMembersPercentageChange = CalculatePercentageChange(activeMembers, activeMembersLastMonth),
                BooksBorrowed = booksBorrowed,
                BooksBorrowedPercentageChange = CalculatePercentageChange(booksBorrowed, booksBorrowedLastWeek),
                TotalCategories = totalCategoriesTask.Result,
                PendingReturns = pendingReturnsTask.Result,
                DueToday = dueTodayTask.Result
            };
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

    public class QuickStatsInfo
    {
        public int TotalBooks { get; set; }
        public decimal TotalBooksPercentageChange { get; set; }
        public int ActiveMembers { get; set; }
        public decimal ActiveMembersPercentageChange { get; set; }
        public int BooksBorrowed { get; set; }
        public decimal BooksBorrowedPercentageChange { get; set; }
        public int TotalCategories { get; set; }
        public int PendingReturns { get; set; }
        public int DueToday { get; set; }
    }
}