// POS.Application/Features/Dashboard/DashboardInfo.cs
using System;
using System.Collections.Generic;

namespace POS.Application.Features.Dashboard
{
    public class DashboardInfo
    {
        // Total Books Card
        public int TotalBooks { get; set; }
        public decimal TotalBooksPercentageChange { get; set; }

        // Active Members Card
        public int ActiveMembers { get; set; }
        public decimal ActiveMembersPercentageChange { get; set; }

        // Books Borrowed Card
        public int BooksBorrowed { get; set; }
        public decimal BooksBorrowedPercentageChange { get; set; }

        // Categories Card
        public int TotalCategories { get; set; }

        // Pending Returns Card
        public int PendingReturns { get; set; }
        public int DueToday { get; set; }

        // Monthly Borrowing Statistics
        public List<MonthlyStatistic> MonthlyBorrowingStatistics { get; set; } = new();

        // Return Trends
        public List<DailyStatistic> ReturnTrends { get; set; } = new();
    }

    public class MonthlyStatistic
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class DailyStatistic
    {
        public DateTime Date { get; set; }
        public string DayLabel { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}