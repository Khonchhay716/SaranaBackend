// // POS.Application/Features/User/DeleteUserCommand.cs
// using FluentValidation;
// using MediatR;
// using Microsoft.EntityFrameworkCore;
// using POS.Application.Common.Dto;
// using POS.Application.Common.Interfaces;
// using System;
// using System.Threading;
// using System.Threading.Tasks;

// namespace POS.Application.Features.User
// {
//     public record DeleteUserCommand(int UserId) : IRequest<ApiResponse>;

//     public class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
//     {
//         public DeleteUserCommandValidator()
//         {
//             RuleFor(x => x.UserId)
//                 .GreaterThan(0).WithMessage("Valid user ID is required");
//         }
//     }

//     public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, ApiResponse>
//     {
//         private readonly IMyAppDbContext _context;

//         public DeleteUserCommandHandler(IMyAppDbContext context)
//         {
//             _context = context;
//         }

//         public async Task<ApiResponse> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
//         {
//             var person = await _context.Persons
//                 .FirstOrDefaultAsync(p => p.Id == request.UserId && !p.IsDeleted, cancellationToken);

//             if (person == null)
//             {
//                 return ApiResponse.NotFound("User not found");
//             }

//             person.IsDeleted = true;
//             person.DeletedDate = DateTime.UtcNow;

//             await _context.SaveChangesAsync(cancellationToken);

//             return ApiResponse.Ok("User deleted successfully");
//         }
//     }
// }




// POS.Application/Features/User/DeleteUserCommand.cs
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Features.User
{
    public record DeleteUserCommand(int UserId) : IRequest<ApiResponse>;

    public class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
    {
        public DeleteUserCommandValidator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("Valid user ID is required");
        }
    }

    public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, ApiResponse>
    {
        private readonly IMyAppDbContext _context;

        public DeleteUserCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            var person = await _context.Persons
                .FirstOrDefaultAsync(p => p.Id == request.UserId && !p.IsDeleted, cancellationToken);

            if (person == null)
                return ApiResponse.NotFound("User not found");

            person.IsDeleted = true;
            person.DeletedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse.Ok("User deleted successfully");
        }
    }
}