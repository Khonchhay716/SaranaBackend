// // POS.Application/Common/Interfaces/IJwtService.cs
// using System.Collections.Generic;

// namespace POS.Application.Common.Interfaces
// {
//     public interface IJwtService
//     {
//         string GenerateAccessToken(int userId, string username, IEnumerable<string> permissions);
//         string GenerateRefreshToken();
//         int? ValidateAccessToken(string token);
//     }
// }


// POS.Application/Common/Interfaces/IJwtService.cs
using System.Collections.Generic;

namespace POS.Application.Common.Interfaces
{
    public interface IJwtService
    {
        string GenerateAccessToken(int userId, string username, IEnumerable<string> permissions);
        string GenerateRefreshToken();
        bool ValidateAccessToken(string token);
    }
}