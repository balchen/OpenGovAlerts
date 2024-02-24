using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PoliticalAlertsWeb.Models;
using PoliticalAlertsWeb.Settings;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace PoliticalAlertsWeb.Controllers
{
    public class UserController : Controller
    {
        private readonly AlertsDbContext db;
        private readonly ILogger<UserController> logger;
        private readonly IOptions<AuthenticationSettings> settings;

        public UserController(AlertsDbContext db, ILogger<UserController> logger, IOptions<AuthenticationSettings> settings)
        {
            this.db = db;
            this.logger = logger;
            this.settings = settings;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AuthenticateRequest request)
        {
            logger.LogDebug("HttpPost Authenticate");

            if (!ModelState.IsValid)
            {
                return ValidationProblem();
            }

            // Perform login
            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == request.Email);

            if (user == null)
            {
                logger.LogInformation("Invalid login attempt for user {0}", request.Email);

                ModelState.AddModelError("email", "Feil passord eller e-post");
                return ValidationProblem();
            }

            if (!VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt))
            {
                logger.LogInformation("Invalid login attempt for user {0}", request.Email);

                ModelState.AddModelError("email", "Feil passord eller e-post");
                return ValidationProblem();
            }

            // Generate JWT token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(settings.Value.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, user.Id.ToString()),
                    new Claim(ClaimTypes.Role, user.Role.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            // return basic user info and authentication token
            var res = new AuthenticateResponse
            {
                Id = user.Id,
                Email = user.Username,
                Token = tokenString,
                Role = user.Role
            };

            logger.LogDebug("Login success for email {0}", request.Email);

            return Ok(res);
        }

        private int CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            passwordHash = null;
            passwordSalt = null;
            if (string.IsNullOrWhiteSpace(password))
            {
                return -1;
            }

            using var hmac = new System.Security.Cryptography.HMACSHA512();
            passwordSalt = hmac.Key;
            passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

            return 0;
        }

        private bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                // _logger.LogError("Cannot verify password hash. Password is empty or whitespace.");
                return false;
            }

            if (storedHash.Length != 64 || storedSalt.Length != 128)
            {
                // _logger.LogError("Cannot verify password hash. Hash or salt of invalid size");
                return false;
            }

            using var hmac = new System.Security.Cryptography.HMACSHA512(storedSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            for (int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != storedHash[i])
                {
                    // _logger.LogDebug("Password verification failed");
                    return false;
                }
            }

            return true;
        }
    }
}