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

        public string GenerateJwtToken(User user)
        {
            // 1. Setup Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            // Add profile-specific IDs
            if (user.Role == UserRoles.Passenger && user.Passenger != null)
                claims.Add(new Claim("PassengerId", user.Passenger.PassengerId.ToString()));
            else if (user.Role == UserRoles.Company && user.Company != null)
                claims.Add(new Claim("CompanyId", user.Company.CompanyId.ToString()));

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