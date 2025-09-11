

using CoreAuthBackend.Client.Core.Models;
using DocumentFormat.OpenXml.Spreadsheet;
using Mapster;
using MediatR;
using POS.Application.Common.Interfaces;
using TableUser = POS.Domain.Entities.User;

namespace POS.Application.Features.User
{
    public record UserCreateCommand : IRequest<ApiResponse<UserInfo>>
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class UserCreateCommandHandler : IRequestHandler<UserCreateCommand, ApiResponse<UserInfo>>
    {
        private readonly IMyAppDbContext _context;
        public UserCreateCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }
        public async Task<ApiResponse<UserInfo>> Handle(UserCreateCommand request, CancellationToken cancellationToken)
        {
            var user = request.Adapt<TableUser>();
            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);
            var info = user.Adapt<UserInfo>();

            return ApiResponse<UserInfo>.Created(info, "Unit created successfully");
        }
    }
}