using Microsoft.IdentityModel.Tokens;
using MyBookStore.Data.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace WebAPI.Services.Helper
{
    public interface IJwtService
    {
        string GenerateToken(Account account, int userId, string name);
    }

    public class JwtService : IJwtService
    {
        private readonly IConfiguration _cfg;

        public JwtService(IConfiguration cfg)
        {
            _cfg = cfg;
        }

        public string GenerateToken(Account account, int userId, string name)
        {
            var key = Encoding.UTF8.GetBytes(_cfg["Jwt:SecretKey"]!);

            var claims = new List<Claim>
            {
                new Claim("accountId", account.AccountId.ToString()),
                new Claim("userId", userId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, account.AccountId.ToString()),
                new Claim(ClaimTypes.Name, name),
                new Claim(ClaimTypes.Email, account.Email ?? ""),
                new Claim(ClaimTypes.Role, account.IsAdmin ? "Admin" : "Customer")
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(2),
                
                Issuer = _cfg["Jwt:Issuer"],
                Audience = _cfg["Jwt:Audience"],

                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), 
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}
