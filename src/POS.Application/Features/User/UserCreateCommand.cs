

using CoreAuthBackend.Client.Core.Models;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Mapster;
using MediatR;
using POS.Application.Common.Dto;
using POS.Application.Common.Interfaces;
using TableUser = POS.Domain.Entities.User;

namespace POS.Application.Features.User
{
    public record UserCreateCommand : IRequest<ApiResponse<UserInfo>>
    {
        // /// where write column for create only it the same will file DTO also or where this for format data for input 
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        // public List<IdBase> Products { get; set; }
    }

    public class UserCreateCommandHandler : IRequestHandler<UserCreateCommand, ApiResponse<UserInfo>>
    {
        private readonly IMyAppDbContext _context;
        public UserCreateCommandHandler(IMyAppDbContext context)
        {
            _context = context;
        }
        public async Task<ApiResponse<UserInfo>> Handle(UserCreateCommand DtoCreate, CancellationToken cancellationToken)
        {
            var user = DtoCreate.Adapt<TableUser>();
            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);
            var info = user.Adapt<UserInfo>();

            return ApiResponse<UserInfo>.Created(info, "Unit created successfully");
        }
    }
}