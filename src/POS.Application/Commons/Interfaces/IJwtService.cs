// using System.Collections.Generic;

// namespace POS.Application.Common.Interfaces
// {
//     public interface IJwtService
//     {
//         string GenerateAccessToken(int userId, string username, IEnumerable<string> permissions);
//         string GenerateRefreshToken();
//         bool ValidateAccessToken(string token);
//     }
// }


using System.Collections.Generic;

namespace POS.Application.Common.Interfaces
{
    public interface IJwtService
    {
        // REMOVE permissions from here
        string GenerateAccessToken(int userId, string username);
        string GenerateRefreshToken();
        bool ValidateAccessToken(string token);
    }
}