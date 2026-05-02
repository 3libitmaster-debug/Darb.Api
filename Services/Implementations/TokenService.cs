using Darb.Api.Models;
using Darb.Api.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Darb.Api.Models.Enums;


namespace Darb.Api.Services.Implemention
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateJwtToken(Account Account)
        {
            // 1. Setup Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, Account.AccountId.ToString()),
                new Claim(ClaimTypes.Email, Account.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, Account.Role.ToString())
            };

            // Add profile-specific IDs
            if (Account.Role == AccountRoles.Passenger && Account.Passenger != null)
                claims.Add(new Claim("PassengerId", Account.Passenger.PassengerId.ToString()));
            else if (Account.Role == AccountRoles.Company && Account.Company != null)
                claims.Add(new Claim("CompanyId", Account.Company.CompanyId.ToString()));

            // 2. Fetch the Key from appsettings.json using the exact path
            // Note: We use "JwtSettings:Key" to match your appsettings structure
            var secretKey = _configuration["JwtSettings:Key"];

            if (string.IsNullOrEmpty(secretKey))
            {
                throw new InvalidOperationException("JWT Key is not configured. Please check JwtSettings:Key in appsettings.json");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // 3. Create Token
            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}