using System.Threading.Tasks;

namespace POS.Application.Common.Interfaces
{
    public interface ICurrentUserService
    {
        int? UserId { get; }
        Task<IEnumerable<string>> GetPermissionsAsync();
    }
}