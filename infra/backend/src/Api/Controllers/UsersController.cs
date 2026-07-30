using Api.Data;
using Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto dto)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Email == dto.Email);
            if (userExists)
            {
                return BadRequest(new { message = "E-mail já cadastrado." });
            }

            var user = new User
            {
                Email = dto.Email,
                PasswordHash = dto.PasswordHash, // Recebe a senha enviada pelo front
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
            
            if (user == null || user.PasswordHash != dto.Password)
            {
                return Unauthorized(new { message = "E-mail ou senha incorretos." });
            }

            // Retorna os dados do usuário para o frontend salvar na sessão
            return Ok(new 
            { 
                email = user.Email, 
                fullName = user.FullName,
                cpf = user.Cpf,
                phone = user.Phone,
                cep = user.Cep,
                address = user.Address
            });
        }

        // --- ROTA DE DELETAR CONTA ---
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
    }

    // --- DTO DE CADASTRO ATUALIZADO (Com todos os campos do site) ---
    public class UserRegisterDto
    {
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
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