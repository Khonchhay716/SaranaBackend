// POS.Application/Features/Auth/LoginCommand.cs
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Application.Features.Role;
using POS.Domain.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.Auth
{
    public record LoginCommand : IRequest<ApiResponse<AuthResponse>>
    {
        public string UsernameOrEmail { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.UsernameOrEmail)
                .NotEmpty().WithMessage("Username or email is required");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required");
        }
    }

    public class LoginCommandHandler : IRequestHandler<LoginCommand, ApiResponse<AuthResponse>>
    {
        private readonly IMyAppDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtService _jwtService;

        public LoginCommandHandler(
            IMyAppDbContext context,
            IPasswordHasher passwordHasher,
            IJwtService jwtService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
        }

        public async Task<ApiResponse<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var person = await _context.Persons
                .Include(p => p.PersonRoles)
                    .ThenInclude(pr => pr.Role)
                        .ThenInclude(r => r.RolePermissions)
                .FirstOrDefaultAsync(p =>
                    (p.Username == request.UsernameOrEmail || p.Email == request.UsernameOrEmail) &&
                    !p.IsDeleted,
                    cancellationToken);

            if (person == null)
            {
                return ApiResponse<AuthResponse>.Unauthorized("Invalid username or password");
            }

            if (!person.IsActive)
            {
                return ApiResponse<AuthResponse>.BadRequest("Account is inactive");
            }

            if (!_passwordHasher.VerifyPassword(request.Password, person.PasswordHash))
            {
                return ApiResponse<AuthResponse>.Unauthorized("Invalid username or password");
            }

            var permissions = person.PersonRoles
                .Where(pr => !pr.Role.IsDeleted)
                .SelectMany(pr => pr.Role.RolePermissions)
                .Select(rp => rp.PermissionName)
                .Distinct()
                .ToList();

            var accessToken = _jwtService.GenerateAccessToken(person.Id, person.Username, permissions);
            var refreshToken = _jwtService.GenerateRefreshToken();

            // Revoke old refresh tokens
            var oldTokens = await _context.RefreshTokens
                .Where(rt => rt.PersonId == person.Id && !rt.IsRevoked)
                .ToListAsync(cancellationToken);

            foreach (var oldToken in oldTokens)
            {
                oldToken.IsRevoked = true;
            }

            var userRefreshToken = new RefreshToken
            {
                Token = refreshToken,
                PersonId = person.Id,
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                CreatedDate = DateTime.UtcNow
            };
            var roles = person.PersonRoles
                .Where(pr => !pr.Role.IsDeleted)
                .Select(pr => new RoleInfos
                {
                    Id = pr.Role.Id,
                    Name = pr.Role.Name,
                    Description = pr.Role.Description,
                })
                .Distinct()
                .ToList();

            _context.RefreshTokens.Add(userRefreshToken);
            await _context.SaveChangesAsync(cancellationToken);

            var response = new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                UserId = person.Id,
                Username = person.Username,
                Email = person.Email,
                FirstName = person.FirstName,
                LastName = person.LastName,
                Permissions = permissions,
                Roles = roles
            };

            return ApiResponse<AuthResponse>.Ok(response);
        }
    }
}
