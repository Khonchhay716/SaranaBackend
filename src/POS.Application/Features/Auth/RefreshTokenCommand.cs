// // POS.Application/Features/Auth/RefreshTokenCommand.cs
// using FluentValidation;
// using MediatR;
// using Microsoft.EntityFrameworkCore;
// using POS.Application.Common.Dto;
// using POS.Application.Common.Interfaces;
// using System;
// using System.Linq;
// using System.Threading;
// using System.Threading.Tasks;

// namespace POS.Application.Features.Auth
// {
//     public record RefreshTokenCommand : IRequest<ApiResponse<AuthResponse>>
//     {
//         public string RefreshToken { get; set; } = string.Empty;
//     }

//     public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
//     {
//         public RefreshTokenCommandValidator()
//         {
//             RuleFor(x => x.RefreshToken)
//                 .NotEmpty().WithMessage("Refresh token is required");
//         }
//     }

//     public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, ApiResponse<AuthResponse>>
//     {
//         private readonly IMyAppDbContext _context;
//         private readonly IJwtService _jwtService;

//         public RefreshTokenCommandHandler(
//             IMyAppDbContext context,
//             IJwtService jwtService)
//         {
//             _context = context;
//             _jwtService = jwtService;
//         }

//         public async Task<ApiResponse<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
//         {
//             var storedToken = await _context.RefreshTokens
//                 .Include(rt => rt.Person)
//                     .ThenInclude(p => p.PersonRoles)
//                         .ThenInclude(pr => pr.Role)
//                             .ThenInclude(r => r.RolePermissions)
//                 .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

//             if (storedToken == null || storedToken.IsRevoked || storedToken.ExpiryDate < DateTime.UtcNow)
//             {
//                 return ApiResponse<AuthResponse>.Unauthorized("Invalid or expired refresh token");
//             }

//             var person = storedToken.Person;

//             if (!person.IsActive || person.IsDeleted)
//             {
//                 return ApiResponse<AuthResponse>.BadRequest("Account is inactive or deleted");
//             }

//             var permissions = person.PersonRoles
//                 .Where(pr => !pr.Role.IsDeleted)
//                 .SelectMany(pr => pr.Role.RolePermissions)
//                 .Select(rp => rp.PermissionName)
//                 .Distinct()
//                 .ToList();

//             var newAccessToken = _jwtService.GenerateAccessToken(person.Id, person.Username, permissions);
//             var newRefreshToken = _jwtService.GenerateRefreshToken();

//             storedToken.IsRevoked = true;

//             var userRefreshToken = new Domain.Entities.RefreshToken
//             {
//                 Token = newRefreshToken,
//                 PersonId = person.Id,
//                 ExpiryDate = DateTime.UtcNow.AddDays(7),
//                 CreatedDate = DateTime.UtcNow
//             };

//             _context.RefreshTokens.Add(userRefreshToken);
//             await _context.SaveChangesAsync(cancellationToken);

//             var response = new AuthResponse
//             {
//                 AccessToken = newAccessToken,
//                 RefreshToken = newRefreshToken,
//                 UserId = person.Id,
//                 Username = person.Username,
//                 Email = person.Email,
//                 Permissions = permissions
//             };

//             return ApiResponse<AuthResponse>.Ok(response);
//         }
//     }
// }



using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Application.Features.Role;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.Auth
{
    public record RefreshTokenCommand : IRequest<ApiResponse<AuthResponse>>
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, ApiResponse<AuthResponse>>
    {
        private readonly IMyAppDbContext _context;
        private readonly IJwtService _jwtService;

        public RefreshTokenCommandHandler(IMyAppDbContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        public async Task<ApiResponse<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            // ១. ទាញយក RefreshToken ជាមួយគ្រប់ Data ដែលពាក់ព័ន្ធ (Eager Loading)
            var storedToken = await _context.RefreshTokens
                .Include(rt => rt.Person)
                    .ThenInclude(p => p.Staff)
                .Include(rt => rt.Person)
                    .ThenInclude(p => p.Customer)
                .Include(rt => rt.Person)
                    .ThenInclude(p => p.PersonRoles)
                        .ThenInclude(pr => pr.Role)
                            .ThenInclude(r => r.RolePermissions)
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

            // ២. ផ្ទៀងផ្ទាត់ថាតើ Token ត្រឹមត្រូវ ឬ Expired ឬនៅ
            if (storedToken == null || storedToken.IsRevoked || storedToken.ExpiryDate < DateTime.UtcNow)
            {
                return ApiResponse<AuthResponse>.Unauthorized("Invalid or expired refresh token");
            }

            var person = storedToken.Person;

            // ៣. ទាញយកបញ្ជី Permissions ចេញពី Roles
            var permissions = person.PersonRoles
                .SelectMany(pr => pr.Role.RolePermissions)
                .Select(rp => rp.PermissionName)
                .Distinct()
                .ToList();

            // ៤. ទាញយកបញ្ជី Roles
            var roles = person.PersonRoles
                .Where(pr => !pr.Role.IsDeleted)
                .Select(pr => new RoleInfos { Id = pr.Role.Id, Name = pr.Role.Name, Description = pr.Role.Description })
                .Distinct().ToList();

            // ៥. បង្កើត Access Token និង Refresh Token ថ្មី
            var newAccessToken = _jwtService.GenerateAccessToken(person.Id, person.Username, permissions);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            // ៦. បោះបង់ Token ចាស់ រួចបញ្ចូល Token ថ្មីទៅក្នុង Database
            storedToken.IsRevoked = true;

            _context.RefreshTokens.Add(new Domain.Entities.RefreshToken
            {
                Token = newRefreshToken,
                PersonId = person.Id,
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                CreatedDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync(cancellationToken);

            // ៧. បញ្ជូន Response ត្រឡប់ទៅវិញ
            return ApiResponse<AuthResponse>.Ok(new AuthResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                UserId = person.Id,
                Username = person.Username,
                Email = person.Email,
                Permissions = permissions, 
                Roles = roles
            });
        }
    }
}