using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Aceca.Api.Data;

namespace Aceca.Api.Controllers;

[ApiController, Route("api/auth")]
public class AuthController(AppDbContext db, IConfiguration cfg) : ControllerBase
{
    record LoginIn(string Email, string Senha);

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginIn dto)
    {
        var socio = await db.Socios
            .FirstOrDefaultAsync(s => s.Email == dto.Email.ToLower() && s.Ativo);

        if (socio is null || !BCrypt.Net.BCrypt.Verify(dto.Senha, socio.SenhaHash))
            return Unauthorized(new { msg = "Credenciais inválidas." });

        var k    = new SymmetricSecurityKey(
                       Encoding.UTF8.GetBytes(cfg["Jwt:Key"]!));
        var cred = new SigningCredentials(k, SecurityAlgorithms.HmacSha256);
        var tok  = new JwtSecurityToken(
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: cred,
            claims: [
                new(ClaimTypes.NameIdentifier, socio.Id.ToString()),
                new(ClaimTypes.Email, socio.Email),
                new(ClaimTypes.Name,  socio.Nome),
                new(ClaimTypes.Role,  socio.Cargo),
            ]);

        return Ok(new {
            token = new JwtSecurityTokenHandler().WriteToken(tok),
            nome  = socio.Nome,
            email = socio.Email,
            cargo = socio.Cargo
        });
    }
}