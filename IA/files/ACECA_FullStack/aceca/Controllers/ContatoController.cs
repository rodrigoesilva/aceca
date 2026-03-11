using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Aceca.Api.Data;
using Aceca.Api.Models;

namespace Aceca.Api.Controllers;

[ApiController, Route("api/contato")]
public class ContatoController(AppDbContext db, IWebHostEnvironment env) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(25_000_000)]
    public async Task<IActionResult> Post([FromForm] ContatoForm form)
    {
        if (string.IsNullOrWhiteSpace(form.Nome)    ||
            string.IsNullOrWhiteSpace(form.Email)   ||
            string.IsNullOrWhiteSpace(form.Assunto) ||
            string.IsNullOrWhiteSpace(form.Mensagem))
            return BadRequest(new { msg = "Campos obrigatórios ausentes." });

        var caminhos = new List<string>();
        if (form.Imagens?.Count > 0)
        {
            var dir = Path.Combine(env.WebRootPath, "uploads");
            Directory.CreateDirectory(dir);
            var extsOk = new[] { ".jpg",".jpeg",".png",".webp",".gif" };
            foreach (var img in form.Imagens.Take(3))
            {
                var ext = Path.GetExtension(img.FileName).ToLowerInvariant();
                if (!extsOk.Contains(ext)) continue;
                var nome = $"{Guid.NewGuid():N}{ext}";
                var path = Path.Combine(dir, nome);
                await using var fs = System.IO.File.Create(path);
                await img.CopyToAsync(fs);
                caminhos.Add($"/uploads/{nome}");
            }
        }

        db.Contatos.Add(new Contato {
            Nome     = form.Nome.Trim(),
            Email    = form.Email.Trim().ToLower(),
            Telefone = form.Telefone?.Trim(),
            Assunto  = form.Assunto,
            Mensagem = form.Mensagem.Trim(),
            Anexos   = caminhos.Count > 0
                       ? JsonSerializer.Serialize(caminhos)
                       : null
        });
        await db.SaveChangesAsync();
        return Ok(new { msg = "Mensagem recebida com sucesso!" });
    }
}

public class ContatoForm
{
    public string  Nome      { get; set; } = "";
    public string  Email     { get; set; } = "";
    public string? Telefone  { get; set; }
    public string  Assunto   { get; set; } = "";
    public string  Mensagem  { get; set; } = "";
    public List<IFormFile>? Imagens { get; set; }
}