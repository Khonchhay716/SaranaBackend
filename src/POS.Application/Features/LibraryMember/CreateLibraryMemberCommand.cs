// POS.Application/Features/LibraryMember/CreateLibraryMemberCommand.cs
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;
using POS.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.LibraryMember
{
    public class CreateLibraryMemberCommand : IRequest<ApiResponse<LibraryMemberInfo>>
    {
        public int PersonId { get; set; }
        public LibraryMemberType MembershipType { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class CreateLibraryMemberCommandValidator : AbstractValidator<CreateLibraryMemberCommand>
    {
        public CreateLibraryMemberCommandValidator()
        {
            RuleFor(x => x.PersonId)
                .GreaterThan(0).WithMessage("PersonId is required");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format");

            RuleFor(x => x.MembershipType)
                .IsInEnum().WithMessage("Invalid membership type");
        }
    }

    public class CreateLibraryMemberCommandHandler : IRequestHandler<CreateLibraryMemberCommand, ApiResponse<LibraryMemberInfo>>
    {
        private readonly IMyAppDbContext _context;

        public CreateLibraryMemberCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<LibraryMemberInfo>> Handle(CreateLibraryMemberCommand request, CancellationToken cancellationToken)
        {
            // Validate person exists
            var personExists = await _context.Persons
                .AnyAsync(p => p.Id == request.PersonId && !p.IsDeleted, cancellationToken);

            if (!personExists)
            {
                return ApiResponse<LibraryMemberInfo>.NotFound($"Person with id {request.PersonId} not found");
            }

            // Check if person already has a library membership
            var existingMember = await _context.LibraryMembers
                .FirstOrDefaultAsync(m => m.PersonId == request.PersonId && !m.IsDeleted, cancellationToken);

            if (existingMember != null)
            {
                return ApiResponse<LibraryMemberInfo>.BadRequest("The person has already requested a library card or a library membership");
            }

            // Check if email already exists
            var emailExists = await _context.LibraryMembers
                .AnyAsync(m => m.Email == request.Email && !m.IsDeleted, cancellationToken);

            if (emailExists)
            {
                return ApiResponse<LibraryMemberInfo>.BadRequest("Email already exists");
            }

            // Generate membership number
            var memberCount = await _context.LibraryMembers.CountAsync(cancellationToken);
            var membershipNo = $"LIB{DateTime.UtcNow.Year}{(memberCount + 1):D6}";

            // Determine max books allowed based on membership type
            var maxBooksAllowed = request.MembershipType switch
            {
                LibraryMemberType.Student => 3,
                LibraryMemberType.Staff => 5,
                LibraryMemberType.Teacher => 10,
                _ => 3
            };

            // Create new library member with Pending status
            var member = new Domain.Entities.LibraryMember
            {
                PersonId = request.PersonId,
                MembershipNo = membershipNo,
                MembershipType = (int)request.MembershipType,
                Email = request.Email,
                Address = request.Address,
                PhoneNumber = request.PhoneNumber,
                Status = (int)LibraryMemberStatus.Pending, // Set initial status to Pending
                IsActive = false, // Not active until approved
                MaxBooksAllowed = maxBooksAllowed,
                CreatedDate = DateTimeOffset.UtcNow
            };

            _context.LibraryMembers.Add(member);
            await _context.SaveChangesAsync(cancellationToken);

            // Get person details for response
            var person = await _context.Persons
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == member.PersonId && !p.IsDeleted, cancellationToken);

            // Map to DTO
            var result = new LibraryMemberInfo
            {
                Id = member.Id,
                PersonId = member.PersonId,
                MembershipNo = member.MembershipNo,
                MembershipType = member.MembershipType,
                Email = member.Email,
                IsActive = member.IsActive,
                MaxBooksAllowed = member.MaxBooksAllowed,
                Address = member.Address,
                PhoneNumber = member.PhoneNumber,
                Status = (LibraryMemberStatus)member.Status
            };

            if (person != null)
            {
                result.Person = new TypeNamebase
                {
                    Id = person.Id,
                    Name = person.Username
                };
            }

            return ApiResponse<LibraryMemberInfo>.Ok(result, "Library member created successfully with Pending status");
        }
    }
}