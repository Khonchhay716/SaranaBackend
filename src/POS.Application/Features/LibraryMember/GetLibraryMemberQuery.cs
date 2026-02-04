// POS.Application/Features/LibraryMember/GetLibraryMemberQuery.cs
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using Org.BouncyCastle.Asn1.Misc;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Typebase;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.LibraryMember
{
    public record GetLibraryMemberQuery : IRequest<ApiResponse<LibraryMemberInfo>>
    {
        public int Id { get; set; }
    }

    public class GetLibraryMemberQueryHandler : IRequestHandler<GetLibraryMemberQuery, ApiResponse<LibraryMemberInfo>>
    {
        private readonly IMyAppDbContext _context;

        public GetLibraryMemberQueryHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<LibraryMemberInfo>> Handle(GetLibraryMemberQuery request, CancellationToken cancellationToken)
        {
            var member = await _context.LibraryMembers
                .Include(m => m.Person)
                .FirstOrDefaultAsync(m => m.Id == request.Id && !m.IsDeleted, cancellationToken);

            if (member == null)
            {
                return ApiResponse<LibraryMemberInfo>.NotFound($"Library member with id {request.Id} not found");
            }

            // var result = new LibraryMemberInfo
            // {
            //     Id = member.Id,
            //     PersonId = member.PersonId,
            //     PersonName = $"{member.Person.FirstName} {member.Person.LastName}",
            //     MembershipNo = member.MembershipNo,
            //     MembershipType = member.MembershipType,
            //     Email = member.Email,
            //     IsActive = member.IsActive,
            //     MaxBooksAllowed = member.MaxBooksAllowed,
            //     Address = member.Address,
            //     PhoneNumber = member.PhoneNumber
            // };

            var data = member.Adapt<LibraryMemberInfo>();
            if (data.PersonId != null)
            {
                var person = await _context.Persons
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == data.PersonId && !c.IsDeleted, cancellationToken);

                if (person != null)
                {
                    data.Person = new TypeNamebase
                    {
                        Id = person.Id,
                        Name = person.Username
                    };
                }
            }


            return ApiResponse<LibraryMemberInfo>.Ok(data);
        }
    }
}