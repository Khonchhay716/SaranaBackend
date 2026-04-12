// using FluentValidation;
// using MediatR;
// using Microsoft.EntityFrameworkCore;
// using POS.Application.Common.Dto;
// using POS.Application.Common.Extensions;
// using POS.Application.Common.Interfaces;
// using POS.Application.Common.Typebase;

// namespace POS.Application.Features.Staff
// {
//     public class StaffListQuery : PaginationRequest, IRequest<PaginatedResult<StaffInfo>>
//     {
//         public string? Search { get; set; }
//         public string? Position { get; set; }
//         public bool? Status { get; set; }
//     }

//     public class StaffListQueryValidator : AbstractValidator<StaffListQuery>
//     {
//         public StaffListQueryValidator()
//         {
//             RuleFor(x => x.Page)
//                 .GreaterThan(0).WithMessage("Page must be greater than 0.");
//             RuleFor(x => x.PageSize)
//                 .GreaterThan(0).WithMessage("Page size must be greater than 0.")
//                 .LessThanOrEqualTo(100).WithMessage("Page size must not exceed 100.");
//         }
//     }

//     public class StaffListQueryHandler : IRequestHandler<StaffListQuery, PaginatedResult<StaffInfo>>
//     {
//         private readonly IMyAppDbContext _context;

//         public StaffListQueryHandler(IMyAppDbContext context)
//         {
//             _context = context;
//         }

//         public async Task<PaginatedResult<StaffInfo>> Handle(
//             StaffListQuery request,
//             CancellationToken cancellationToken)
//         {
//             var query = _context.Staffs
//                 .AsNoTracking()
//                 .Include(s => s.Person)
//                 .Where(s => !s.IsDeleted);

//             if (!string.IsNullOrWhiteSpace(request.Search))
//             {
//                 var search = request.Search.Trim().ToLower();
//                 query = query.Where(s =>
//                     s.FirstName.ToLower().Contains(search) ||
//                     s.LastName.ToLower().Contains(search) ||
//                     s.PhoneNumber.Contains(search) ||
//                     s.Position.ToLower().Contains(search));
//             }

//             if (request.Status.HasValue)
//                 query = query.Where(s => s.Status == request.Status.Value);

//             if (!string.IsNullOrWhiteSpace(request.Position))
//                 query = query.Where(s => s.Position == request.Position.Trim());

//             query = query.OrderByDescending(s => s.CreatedDate);
//             var projected = query.Select(s => new StaffInfo
//             {
//                 Id = s.Id,
//                 FirstName = s.FirstName,
//                 LastName = s.LastName,
//                 ImageProfile = s.ImageProfile,
//                 PhoneNumber = s.PhoneNumber,
//                 Position = s.Position,
//                 Salary = s.Salary,
//                 Status = s.Status,
//                 IsDeleted = s.IsDeleted,
//                 CreatedDate = s.CreatedDate,
//                 CreatedBy = s.CreatedBy,
//                 UpdatedDate = s.UpdatedDate,
//                 UpdatedBy = s.UpdatedBy,
//                 DeletedDate = s.DeletedDate,
//                 DeletedBy = s.DeletedBy,
//                 User = s.Person != null && !s.Person.IsDeleted
//                     ? new LinkedUserInfo
//                     {
//                         Id = s.Person.Id,
//                         Username = s.Person.Username,
//                         Email = s.Person.Email,
//                         IsActive = s.Person.IsActive
//                     }
//                     : null
//             });

//             return await projected.ToPaginatedResultAsync(request.Page, request.PageSize);
//         }
//     }
// }



using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;

namespace POS.Application.Features.Staff
{
    public class StaffListQuery : PaginationRequest, IRequest<PaginatedResult<StaffInfo>>
    {
        public string? Search { get; set; }
        public string? Position { get; set; }
        public bool? Status { get; set; }
    }

    public class StaffListQueryValidator : AbstractValidator<StaffListQuery>
    {
        public StaffListQueryValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThan(0).WithMessage("Page must be greater than 0.");
            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("Page size must be greater than 0.")
                .LessThanOrEqualTo(100).WithMessage("Page size must not exceed 100.");
        }
    }

    public class StaffListQueryHandler : IRequestHandler<StaffListQuery, PaginatedResult<StaffInfo>>
    {
        private readonly IMyAppDbContext _context;

        public StaffListQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<StaffInfo>> Handle(
            StaffListQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.Staffs
                .AsNoTracking()
                .Include(s => s.Person)
                .Include(s => s.Supervisor) // ✅ Load Supervisor
                .Where(s => !s.IsDeleted);

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim().ToLower();
                query = query.Where(s =>
                    s.FirstName.ToLower().Contains(search) ||
                    s.LastName.ToLower().Contains(search) ||
                    s.PhoneNumber.Contains(search) ||
                    s.Position.ToLower().Contains(search));
            }

            if (request.Status.HasValue)
                query = query.Where(s => s.Status == request.Status.Value);

            if (!string.IsNullOrWhiteSpace(request.Position))
                query = query.Where(s => s.Position == request.Position.Trim());

            query = query.OrderByDescending(s => s.CreatedDate);

            var projected = query.Select(s => new StaffInfo
            {
                Id           = s.Id,
                FirstName    = s.FirstName,
                LastName     = s.LastName,
                ImageProfile = s.ImageProfile,
                PhoneNumber  = s.PhoneNumber,
                Position     = s.Position,
                Salary       = s.Salary,
                Status       = s.Status,
                IsDeleted    = s.IsDeleted,
                SupervisorId = s.SupervisorId,
                Supervisor   = s.Supervisor != null && !s.Supervisor.IsDeleted
                    ? new SupervisorInfo
                    {
                        Id           = s.Supervisor.Id,
                        FullName     = s.Supervisor.FirstName + " " + s.Supervisor.LastName,
                        Position     = s.Supervisor.Position,
                        ImageProfile = s.Supervisor.ImageProfile,
                    } : null,
                CreatedDate  = s.CreatedDate,
                CreatedBy    = s.CreatedBy,
                UpdatedDate  = s.UpdatedDate,
                UpdatedBy    = s.UpdatedBy,
                DeletedDate  = s.DeletedDate,
                DeletedBy    = s.DeletedBy,
                User = s.Person != null && !s.Person.IsDeleted
                    ? new LinkedUserInfo
                    {
                        Id       = s.Person.Id,
                        Username = s.Person.Username,
                        Email    = s.Person.Email,
                        IsActive = s.Person.IsActive
                    }
                    : null
            });

            return await projected.ToPaginatedResultAsync(request.Page, request.PageSize);
        }
    }
}