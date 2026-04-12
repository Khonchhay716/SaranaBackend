// // POS.Application/Features/User/UserListQuery.cs
// using FluentValidation;
// using MediatR;
// using Microsoft.EntityFrameworkCore;
// using POS.Application.Common.Dto;
// using POS.Application.Common.Extensions;
// using POS.Application.Common.Interfaces;
// using System.Linq;
// using System.Threading;
// using System.Threading.Tasks;

// namespace POS.Application.Features.User
// {
//     public class UserListQuery : PaginationRequest, IRequest<PaginatedResult<UserInfo>>
//     {
//         public string? Search { get; set; }
//         public bool? IsActive { get; set; }
//     }

//     public class UserListQueryValidator : AbstractValidator<UserListQuery>
//     {
//         public UserListQueryValidator()
//         {
//             RuleFor(x => x.Page).GreaterThan(0);
//             RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
//         }
//     }

//     public class UserListQueryHandler : IRequestHandler<UserListQuery, PaginatedResult<UserInfo>>
//     {
//         private readonly IMyAppDbContext _context;

//         public UserListQueryHandler(IMyAppDbContext context)
//         {
//             _context = context;
//         }

//         public async Task<PaginatedResult<UserInfo>> Handle(UserListQuery request, CancellationToken cancellationToken)
//         {
//             var query = _context.Persons
//                 .AsNoTracking()
//                 .Include(p => p.PersonRoles)
//                     .ThenInclude(pr => pr.Role)
//                 .Where(p => !p.IsDeleted);

//             if (!string.IsNullOrWhiteSpace(request.Search))
//             {
//                 query = query.Where(p =>
//                     p.Username.Contains(request.Search) ||
//                     p.Email.Contains(request.Search) ||
//                     p.FirstName.Contains(request.Search) ||
//                     p.LastName.Contains(request.Search));
//             }

//             if (request.IsActive.HasValue)
//             {
//                 query = query.Where(p => p.IsActive == request.IsActive.Value);
//             }

//             query = query.OrderByDescending(p => p.CreatedDate);

//             var projectedQuery = query.Select(p => new UserInfo
//             {
//                 Id = p.Id,
//                 Username = p.Username,
//                 ImageProfile= p.ImageProfile,
//                 Email = p.Email,
//                 FirstName = p.FirstName,
//                 LastName = p.LastName,
//                 PhoneNumber = p.PhoneNumber,
//                 IsActive = p.IsActive,
//                 CreatedDate = p.CreatedDate,
//                 Roles = p.PersonRoles
//                     .Where(pr => !pr.Role.IsDeleted)
//                     .Select(pr => new RoleBasicInfo
//                     {
//                         Id = pr.Role.Id,
//                         Name = pr.Role.Name,
//                         Description = pr.Role.Description
//                     }).ToList()
//             });

//             return await projectedQuery.ToPaginatedResultAsync(request.Page, request.PageSize);
//         }
//     }
// }


// POS.Application/Features/User/UserListQuery.cs
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Extensions;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;

namespace POS.Application.Features.User
{
    public class UserListQuery : PaginationRequest, IRequest<PaginatedResult<UserInfo>>
    {
        public string? Search { get; set; }
        public bool? IsActive { get; set; } 
        public PersonType? Type { get; set; } 
    }

    public class UserListQueryValidator : AbstractValidator<UserListQuery>
    {
        public UserListQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThan(0);
            RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
            
            // Ensures the Type matches the Enum values if it is provided
            RuleFor(x => x.Type).IsInEnum().When(x => x.Type.HasValue);
        }
    }

    public class UserListQueryHandler : IRequestHandler<UserListQuery, PaginatedResult<UserInfo>>
    {
        private readonly IMyAppDbContext _context;

        public UserListQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<UserInfo>> Handle(UserListQuery request, CancellationToken cancellationToken)
        {
            // 1. Base Query
            var query = _context.Persons
                .AsNoTracking()
                .Include(p => p.Staff)
                .Include(p => p.Customer)
                .Include(p => p.PersonRoles)
                    .ThenInclude(pr => pr.Role)
                .Where(p => !p.IsDeleted);

            // 2. Filter by Search (Username, Email, FullName)
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim().ToLower();
                query = query.Where(p =>
                    p.Username.ToLower().Contains(search) ||
                    p.Email.ToLower().Contains(search) ||
                    (p.Staff != null && (p.Staff.FirstName.ToLower().Contains(search) || p.Staff.LastName.ToLower().Contains(search))) ||
                    (p.Customer != null && (p.Customer.FirstName.ToLower().Contains(search) || p.Customer.LastName.ToLower().Contains(search))));
            }

            // 3. Filter by Status (Active/Inactive)
            if (request.IsActive.HasValue)
            {
                query = query.Where(p => p.IsActive == request.IsActive.Value);
            }

            // 4. Filter by Person Type (Staff/Customer)
            if (request.Type.HasValue)
            {
                query = query.Where(p => p.Type == request.Type.Value);
            }

            // 5. Apply Default Ordering
            query = query.OrderByDescending(p => p.CreatedDate);

            // 6. Project to DTO
            var projectedQuery = query.Select(p => new UserInfo
            {
                Id = p.Id,
                Username = p.Username,
                Email = p.Email,
                IsActive = p.IsActive,
                Type = p.Type.ToString(),
                CreatedDate = p.CreatedDate,
                Roles = p.PersonRoles
                            .Where(pr => !pr.Role.IsDeleted)
                            .Select(pr => new RoleBasicInfo
                            {
                                Id = pr.Role.Id,
                                Name = pr.Role.Name,
                                Description = pr.Role.Description
                            }).ToList(),

                Staff = p.Type == PersonType.Staff && p.Staff != null
                    ? new StaffInfo
                    {
                        Id = p.Staff.Id,
                        FirstName = p.Staff.FirstName,
                        LastName = p.Staff.LastName,
                        PhoneNumber = p.Staff.PhoneNumber,
                        Position = p.Staff.Position,
                        Salary = p.Staff.Salary,
                        ImageProfile = p.Staff.ImageProfile
                    }
                    : null,

                Customer = p.Type == PersonType.Customer && p.Customer != null
                    ? new CustomerInfo
                    {
                        Id = p.Customer.Id,
                        FirstName = p.Customer.FirstName,
                        LastName = p.Customer.LastName,
                        PhoneNumber = p.Customer.PhoneNumber,
                        TotalPoint = p.Customer.TotalPoint,
                        ImageProfile = p.Customer.ImageProfile
                    }
                    : null
            });

            // 7. Paginate and Execute
            return await projectedQuery.ToPaginatedResultAsync(request.Page, request.PageSize);
        }
    }
}