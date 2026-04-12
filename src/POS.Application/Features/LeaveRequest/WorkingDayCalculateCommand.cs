using MediatR;
using POS.Application.Common.Dto;

namespace POS.Application.Features.Leave
{
    public record WorkingDayCalculateCommand : IRequest<ApiResponse<WorkingDayResult>>
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Session { get; set; } = "FullDay"; // FullDay | Morning | Afternoon
    }

    public class WorkingDayResult
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Session { get; set; } = string.Empty;
        public int TotalCalendarDays { get; set; }
        public decimal TotalWorkingDays { get; set; }
        public int TotalSundays { get; set; }
    }

    public class WorkingDayCalculateCommandHandler
        : IRequestHandler<WorkingDayCalculateCommand, ApiResponse<WorkingDayResult>>
    {
        public Task<ApiResponse<WorkingDayResult>> Handle(
            WorkingDayCalculateCommand request,
            CancellationToken cancellationToken)
        {
            // ✅ Validate dates
            if (request.EndDate.Date < request.StartDate.Date)
                return Task.FromResult(
                    ApiResponse<WorkingDayResult>.BadRequest(
                        "End date must be after or equal to start date."));

            // ✅ Validate session
            var validSessions = new[] { "FullDay", "Morning", "Afternoon" };
            if (!validSessions.Contains(request.Session))
                return Task.FromResult(
                    ApiResponse<WorkingDayResult>.BadRequest(
                        "Session must be 'FullDay', 'Morning', or 'Afternoon'."));

            var start = request.StartDate.Date;
            var end = request.EndDate.Date;
            var calendarDays = 0;
            var sundays = 0;
            decimal totalWorkingDays = 0;

            var isFirstWorkingDay = true; // ✅ track first working day

            for (var date = start; date <= end; date = date.AddDays(1))
            {
                calendarDays++;

                // ✅ Sunday = skip
                if (date.DayOfWeek == DayOfWeek.Sunday)
                {
                    sundays++;
                    continue;
                }

                if (request.Session == "FullDay")
                {
                    // ✅ All days = full day
                    totalWorkingDays += 1.0m;
                }
                else
                {
                    // ✅ Morning or Afternoon:
                    // First working day = 0.5
                    // Remaining days    = 1.0 (full day)
                    if (isFirstWorkingDay)
                    {
                        totalWorkingDays += 0.5m;
                        isFirstWorkingDay = false;
                    }
                    else
                    {
                        totalWorkingDays += 1.0m;
                    }
                }
            }

            return Task.FromResult(ApiResponse<WorkingDayResult>.Ok(
                new WorkingDayResult
                {
                    StartDate = start,
                    EndDate = end,
                    Session = request.Session,
                    TotalCalendarDays = calendarDays,
                    TotalWorkingDays = totalWorkingDays,
                    TotalSundays = sundays,
                },
                "Calculated successfully."));
        }
    }
}