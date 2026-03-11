
using Microsoft.AspNetCore.Mvc;
using ACECA.Infrastructure.Data;
using ACECA.Core.Entities;

namespace ACECA.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsuariosController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsuariosController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Get() => Ok(_context.Usuarios.ToList());

    [HttpPost]
    public IActionResult Post(Usuario usuario)
    {
        _context.Usuarios.Add(usuario);
        _context.SaveChanges();
        return Ok(usuario);
    }
}
