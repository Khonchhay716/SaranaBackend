using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;

namespace POS.Application.Features.Staff
{
    public class StaffTreeNode
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string ImageProfile { get; set; } = string.Empty;
        public bool Status { get; set; }
        public int? SupervisorId { get; set; }
        public LinkedUserInfo? User { get; set; }
        public List<StaffTreeNode> Subordinates { get; set; } = new();
    }

    public class StaffTreeQuery : IRequest<ApiResponse<List<StaffTreeNode>>>;

    public class StaffTreeQueryHandler : IRequestHandler<StaffTreeQuery, ApiResponse<List<StaffTreeNode>>>
    {
        private readonly IMyAppDbContext _context;

        public StaffTreeQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<List<StaffTreeNode>>> Handle(
            StaffTreeQuery request,
            CancellationToken cancellationToken)
        {
            var allStaff = await _context.Staffs
                .AsNoTracking()
                .Include(s => s.Person)
                .Where(s => !s.IsDeleted)
                .Select(s => new StaffTreeNode
                {
                    Id = s.Id,
                    FullName = s.FirstName + " " + s.LastName,
                    Position = s.Position,
                    ImageProfile = s.ImageProfile,
                    Status = s.Status,
                    SupervisorId = s.SupervisorId,
                    User = s.Person != null && !s.Person.IsDeleted
                        ? new LinkedUserInfo
                        {
                            Id = s.Person.Id,
                            Username = s.Person.Username,
                            Email = s.Person.Email,
                            IsActive = s.Person.IsActive
                        }
                        : null
                })
                .ToListAsync(cancellationToken);
            var lookup = allStaff.ToDictionary(s => s.Id);
            var roots = new List<StaffTreeNode>();

            foreach (var node in allStaff)
            {
                if (node.SupervisorId.HasValue && lookup.TryGetValue(node.SupervisorId.Value, out var parent))
                {
                    parent.Subordinates.Add(node);
                }
                else
                {
                    roots.Add(node);
                }
            }

            return ApiResponse<List<StaffTreeNode>>.Ok(roots, "Staff tree retrieved successfully.");
        }
    }
}