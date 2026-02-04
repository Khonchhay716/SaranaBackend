// using Microsoft.IdentityModel.Tokens;
// using POS.Application.Common.Interfaces;
// using System;
// using System.Collections.Generic;
// using System.IdentityModel.Tokens.Jwt;
// using System.Linq;
// using System.Security.Claims;
// using System.Security.Cryptography;
// using System.Text;
// using Microsoft.Extensions.Configuration;

// namespace POS.Infrastructure.Services
// {
//     public class JwtService : IJwtService
//     {
//         private readonly IConfiguration _configuration;
//         private readonly string _secretKey;
//         private readonly string _issuer;
//         private readonly string _audience;
//         private readonly int _accessTokenExpirationMinutes;
//         private readonly JwtSecurityTokenHandler _tokenHandler;

//         public JwtService(IConfiguration configuration)
//         {
//             _configuration = configuration;
//             _secretKey = _configuration["JwtSettings:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured.");
//             _issuer = _configuration["JwtSettings:Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured.");
//             _audience = _configuration["JwtSettings:Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured.");
//             _accessTokenExpirationMinutes = int.Parse(_configuration["JwtSettings:AccessTokenExpirationMinutes"] ?? "60");
//             _tokenHandler = new JwtSecurityTokenHandler();
//         }

//         public string GenerateAccessToken(int userId, string username, IEnumerable<string> permissions)
//         {
//             var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
//             var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

//             var claims = new List<Claim>
//             {
//                 new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
//                 new Claim(ClaimTypes.Name, username),
//                 new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
//             };

//             // ⭐ Add permissions as SEPARATE claims (not comma-separated)
//             claims.AddRange(permissions.Select(p => new Claim("permission", p)));

//             var token = new JwtSecurityToken(
//                 issuer: _issuer,
//                 audience: _audience,
//                 claims: claims,
//                 expires: DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
//                 signingCredentials: credentials
//             );

//             return _tokenHandler.WriteToken(token);
//         }

//         public string GenerateRefreshToken()
//         {
//             var randomNumber = new byte[64];
//             using var rng = RandomNumberGenerator.Create();
//             rng.GetBytes(randomNumber);
//             return Convert.ToBase64String(randomNumber);
//         }

//         public bool ValidateAccessToken(string token)
//         {
//             try
//             {
//                 var tokenValidationParameters = new TokenValidationParameters
//                 {
//                     ValidateIssuer = true,
//                     ValidateAudience = true,
//                     ValidateLifetime = true,
//                     ValidateIssuerSigningKey = true,
//                     ValidIssuer = _issuer,
//                     ValidAudience = _audience,
//                     IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey)),
//                     ClockSkew = TimeSpan.Zero
//                 };

//                 _tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken validatedToken);
//                 return true;
//             }
//             catch
//             {
//                 return false;
//             }
//         }
//     }
// }



using Microsoft.IdentityModel.Tokens;
using POS.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace POS.Infrastructure.Services
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly double _accessTokenExpirationSeconds; // ⭐ use seconds for testing
        private readonly JwtSecurityTokenHandler _tokenHandler;

        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
            _secretKey = _configuration["JwtSettings:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured.");
            _issuer = _configuration["JwtSettings:Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured.");
            _audience = _configuration["JwtSettings:Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured.");

            // ⭐ Read seconds from appsettings
            _accessTokenExpirationSeconds = double.Parse(_configuration["JwtSettings:AccessTokenExpirationSeconds"] ?? "3600");

            _tokenHandler = new JwtSecurityTokenHandler();
        }

        // Generate Access Token
        public string GenerateAccessToken(int userId, string username, IEnumerable<string> permissions)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Add permissions as separate claims
            claims.AddRange(permissions.Select(p => new Claim("permission", p)));

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddSeconds(_accessTokenExpirationSeconds), // ⭐ 15-second expiry
                signingCredentials: credentials
            );

            return _tokenHandler.WriteToken(token);
        }

        // Generate Refresh Token
        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        // Validate Access Token
        public bool ValidateAccessToken(string token)
        {
            try
            {
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _issuer,
                    ValidAudience = _audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey)),
                    ClockSkew = TimeSpan.Zero // ⭐ important for 15-second token
                };

                _tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken validatedToken);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
