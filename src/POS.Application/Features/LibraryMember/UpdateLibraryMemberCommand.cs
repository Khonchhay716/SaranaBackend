// POS.Application/Features/LibraryMember/UpdateLibraryMemberCommand.cs
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.LibraryMember
{
    public class UpdateLibraryMemberCommand : IRequest<ApiResponse<LibraryMemberInfo>>
    {
        public int Id { get; set; }
        public int PersonId { get; set; }
        public LibraryMemberType? MembershipType { get; set; }
        public string? Email { get; set; }
        public bool? IsActive { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class UpdateLibraryMemberCommandHandler : IRequestHandler<UpdateLibraryMemberCommand, ApiResponse<LibraryMemberInfo>>
    {
        private readonly IMyAppDbContext _context;

        public UpdateLibraryMemberCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<LibraryMemberInfo>> Handle(UpdateLibraryMemberCommand request, CancellationToken cancellationToken)
        {
            // Get member including person
            var member = await _context.LibraryMembers
                .Include(m => m.Person)
                .FirstOrDefaultAsync(m => m.Id == request.Id && !m.IsDeleted, cancellationToken);

            if (member == null)
            {
                return ApiResponse<LibraryMemberInfo>.NotFound($"Library member with id {request.Id} not found");
            }

            // Update PersonId if provided
            if (request.PersonId != 0 && request.PersonId != member.PersonId)
            {
                // Validate that the new person exists
                var personExists = await _context.Persons
                    .AnyAsync(p => p.Id == request.PersonId && !p.IsDeleted, cancellationToken);

                if (!personExists)
                {
                    return ApiResponse<LibraryMemberInfo>.NotFound($"Person with id {request.PersonId} not found");
                }

                // Check if the new person already has a library membership (excluding current member)
                var existingMember = await _context.LibraryMembers
                    .FirstOrDefaultAsync(m => m.PersonId == request.PersonId
                                           && m.Id != request.Id
                                           && !m.IsDeleted, cancellationToken);

                if (existingMember != null)
                {
                    return ApiResponse<LibraryMemberInfo>.BadRequest("The person has already requested a library card or a library membership");
                }

                member.PersonId = request.PersonId;
            }

            // Update email if provided
            if (!string.IsNullOrEmpty(request.Email) && member.Email != request.Email)
            {
                var emailExists = await _context.LibraryMembers
                    .AnyAsync(m => m.Email == request.Email && m.Id != request.Id && !m.IsDeleted, cancellationToken);

                if (emailExists)
                {
                    return ApiResponse<LibraryMemberInfo>.BadRequest("Email already exists");
                }

                member.Email = request.Email;
            }

            // Update MembershipType if provided
            if (request.MembershipType.HasValue)
            {
                member.MembershipType = (int)request.MembershipType;

                // Update MaxBooksAllowed based on type
                member.MaxBooksAllowed = request.MembershipType switch
                {
                    LibraryMemberType.Student => 3,
                    LibraryMemberType.Staff => 5,
                    LibraryMemberType.Teacher => 10,
                    _ => 3
                };
            }

            // Update other fields
            if (request.IsActive.HasValue)
            {
                member.IsActive = request.IsActive.Value;
            }

            if (request.Address != null)
            {
                member.Address = request.Address;
            }

            if (request.PhoneNumber != null)
            {
                member.PhoneNumber = request.PhoneNumber;
            }

            // Update timestamp
            member.UpdatedDate = DateTimeOffset.UtcNow;

            // Save changes
            await _context.SaveChangesAsync(cancellationToken);

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
                PhoneNumber = member.PhoneNumber
            };
            var person = await _context.Persons
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == member.PersonId && !p.IsDeleted, cancellationToken);

            if (person != null)
            {
                result.Person = new TypeNamebase
                {
                    Id = person.Id,
                    Name = person.Username 
                };
            }

            return ApiResponse<LibraryMemberInfo>.Ok(result, "Library member updated successfully");
        }
    }
}
