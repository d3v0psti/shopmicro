using Api.Data;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // --- ROTA DE CADASTRO ---
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto dto)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Email == dto.Email);
            if (userExists)
            {
                return BadRequest(new { message = "E-mail já cadastrado." });
            }

            if (string.IsNullOrWhiteSpace(dto.Password) && string.IsNullOrWhiteSpace(dto.PasswordHash))
            {
                return BadRequest(new { message = "Senha é obrigatória." });
            }

            var rawPassword = string.IsNullOrWhiteSpace(dto.Password) ? dto.PasswordHash : dto.Password;
            var user = new User
            {
                Email = dto.Email,
                PasswordHash = SecurityService.HashPassword(rawPassword),
                FullName = dto.FullName,
                Cpf = dto.Cpf,
                Phone = dto.Phone,
                Cep = dto.Cep,
                Address = dto.Address
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return StatusCode(201, new { message = "Usuário cadastrado com sucesso!" });
        }

        // --- ROTA DE LOGIN (ESTAVA FALTANDO) ---
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            
            if (user == null || !SecurityService.VerifyPassword(dto.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "E-mail ou senha incorretos." });
            }

            var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "shopmicro_dev_secret_12345";
            var accessToken = SecurityService.GenerateJwtToken(user.Email, user.FullName, jwtSecret, TimeSpan.FromHours(1));
            var refreshToken = SecurityService.GenerateRefreshToken();
            var refreshTokenHash = SecurityService.HashRefreshToken(refreshToken);

            var tokenEntry = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = refreshTokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                Revoked = false
            };

            _context.RefreshTokens.Add(tokenEntry);
            await _context.SaveChangesAsync();

            Response.Cookies.Append("admin_refresh_token", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(30),
                Path = "/"
            });

            return Ok(new 
            { 
                token = accessToken,
                email = user.Email, 
                fullName = user.FullName,
                cpf = user.Cpf,
                phone = user.Phone,
                cep = user.Cep,
                address = user.Address
            });
        }

        // --- ROTA DE LISTAGEM DE USUÁRIOS ---
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .Select(u => new UserListDto
                {
                    Email = u.Email,
                    FullName = u.FullName,
                    Cpf = u.Cpf,
                    Phone = u.Phone,
                    Cep = u.Cep,
                    Address = u.Address
                })
                .ToListAsync();

            return Ok(users);
        }

        [Authorize]
        [HttpPut("{email}")]
        public async Task<IActionResult> UpdateUser(string email, [FromBody] UpdateUserDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return NotFound(new { message = "Usuário não encontrado." });

            user.FullName = dto.FullName;
            user.Email = dto.Email;
            user.Cpf = dto.Cpf;
            user.Phone = dto.Phone;
            user.Cep = dto.Cep;
            user.Address = dto.Address;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Perfil atualizado com sucesso." });
        }

        [Authorize]
        [HttpPut("{email}/password")]
        public async Task<IActionResult> ChangePassword(string email, [FromBody] ChangePasswordDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                return NotFound(new { message = "Usuário não encontrado." });
            }

            if (string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                return BadRequest(new { message = "Nova senha é obrigatória." });
            }

            var currentUserEmail = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (currentUserEmail == email)
            {
                if (string.IsNullOrWhiteSpace(dto.CurrentPassword))
                {
                    return BadRequest(new { message = "Senha atual é obrigatória para alteração da própria senha." });
                }

                if (!SecurityService.VerifyPassword(dto.CurrentPassword, user.PasswordHash))
                {
                    return Unauthorized(new { message = "Senha atual incorreta." });
                }
            }

            user.PasswordHash = SecurityService.HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Senha atualizada com sucesso." });
        }

        [Authorize]
        [HttpDelete("{email}")]
        public async Task<IActionResult> DeleteUser(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                return NotFound(new { message = "Usuário não encontrado." });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Conta deletada com sucesso." });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            if (Request.Cookies.TryGetValue("admin_refresh_token", out var refreshToken))
            {
                var refreshHash = SecurityService.HashRefreshToken(refreshToken);
                var tokenRecord = await _context.RefreshTokens
                    .FirstOrDefaultAsync(rt => rt.TokenHash == refreshHash && !rt.Revoked);

                if (tokenRecord != null)
                {
                    tokenRecord.Revoked = true;
                    await _context.SaveChangesAsync();
                }
            }

            Response.Cookies.Delete("admin_refresh_token", new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Strict,
                Path = "/"
            });

            return Ok(new { message = "Logout realizado com sucesso." });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            if (!Request.Cookies.TryGetValue("admin_refresh_token", out var refreshToken))
            {
                return Unauthorized(new { message = "Refresh token não encontrado." });
            }

            var refreshTokenHash = SecurityService.HashRefreshToken(refreshToken);
            var tokenRecord = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.TokenHash == refreshTokenHash && !rt.Revoked);

            if (tokenRecord == null || tokenRecord.ExpiresAt < DateTime.UtcNow)
            {
                return Unauthorized(new { message = "Refresh token inválido ou expirado." });
            }

            var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "shopmicro_dev_secret_12345";
            var accessToken = SecurityService.GenerateJwtToken(tokenRecord.User.Email, tokenRecord.User.FullName, jwtSecret, TimeSpan.FromHours(1));

            return Ok(new { token = accessToken });
        }

        [HttpGet("validate-session")]
        public async Task<IActionResult> ValidateSession()
        {
            if (!Request.Cookies.TryGetValue("admin_refresh_token", out var refreshToken))
            {
                return Unauthorized(new { message = "Sessão inválida." });
            }

            var refreshTokenHash = SecurityService.HashRefreshToken(refreshToken);
            var tokenRecord = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.TokenHash == refreshTokenHash && !rt.Revoked);

            if (tokenRecord == null || tokenRecord.ExpiresAt < DateTime.UtcNow || tokenRecord.User == null)
            {
                return Unauthorized(new { message = "Sessão inválida." });
            }

            return Ok(new { email = tokenRecord.User.Email, fullName = tokenRecord.User.FullName });
        }
    }

    // --- DTO DE CADASTRO ATUALIZADO (Com todos os campos do site) ---
    public class UserRegisterDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Cpf { get; set; }
        public string? Phone { get; set; }
        public string? Cep { get; set; }
        public string? Address { get; set; }
    }

    public class ChangePasswordDto
    {
        public string? CurrentPassword { get; set; }
        public string NewPassword { get; set; } = string.Empty;
    }

    public class UpdateUserDto
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Cpf { get; set; }
        public string? Phone { get; set; }
        public string? Cep { get; set; }
        public string? Address { get; set; }
    }

    public class UserListDto
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Cpf { get; set; }
        public string? Phone { get; set; }
        public string? Cep { get; set; }
        public string? Address { get; set; }
    }

    // --- DTO DE LOGIN ---
    public class UserLoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
