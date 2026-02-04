// POS.API/Controllers/DashboardController.cs
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Application.Common.Dto;
using POS.Application.Features.BookIssue;
using POS.Application.Features.Dashboard;
using System.Threading.Tasks;

namespace POS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Add if you have authentication
    public class DashboardController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DashboardController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Get complete dashboard with all statistics and charts
        /// </summary>
        /// <returns>Complete dashboard information</returns>
        [HttpGet]
        [ProducesResponseType(typeof(DashboardInfo), 200)]
        public async Task<IActionResult> GetDashboard()
        {
            var query = new DashboardQuery();
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Get quick statistics only (for dashboard cards)
        /// </summary>
        /// <returns>Quick statistics without charts</returns>
        [HttpGet("quick-stats")]
        [ProducesResponseType(typeof(QuickStatsInfo), 200)]
        public async Task<IActionResult> GetQuickStats()
        {
            var query = new GetQuickStatsQuery();
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Get books that are due today
        /// </summary>
        /// <param name="query">Pagination and search parameters</param>
        /// <returns>Paginated list of books due today</returns>
        [HttpGet("due-today")]
        [ProducesResponseType(typeof(PaginatedResult<BookIssueInfo>), 200)]
        public async Task<IActionResult> GetDueToday([FromQuery] GetTodayDueBooksQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }
    }
}