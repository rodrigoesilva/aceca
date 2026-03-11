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
        if (string.IsNullOrWhiteSpace(form.Nome)||string.IsNullOrWhiteSpace(form.Email)||
            string.IsNullOrWhiteSpace(form.Assunto)||string.IsNullOrWhiteSpace(form.Mensagem))
            return BadRequest(new{msg="Campos obrigatórios."});
        var paths = new List<string>();
        if (form.Imagens?.Count>0)
        {
            var dir=Path.Combine(env.WebRootPath,"uploads");
            Directory.CreateDirectory(dir);
            var ok=new[]{".jpg",".jpeg",".png",".webp",".gif"};
            foreach(var img in form.Imagens.Take(3))
            {
                var ext=Path.GetExtension(img.FileName).ToLowerInvariant();
                if(!ok.Contains(ext))continue;
                var n=$"{Guid.NewGuid():N}{ext}";
                await using var fs=System.IO.File.Create(Path.Combine(dir,n));
                await img.CopyToAsync(fs);
                paths.Add($"/uploads/{n}");
            }
        }
        db.Contatos.Add(new Contato{Nome=form.Nome.Trim(),Email=form.Email.Trim().ToLower(),
            Telefone=form.Telefone?.Trim(),Assunto=form.Assunto,Mensagem=form.Mensagem.Trim(),
            Anexos=paths.Count>0?JsonSerializer.Serialize(paths):null});
        await db.SaveChangesAsync();
        return Ok(new{msg="Recebido!"});
    }
}
public class ContatoForm{
    public string Nome{get;set;}="";public string Email{get;set;}="";
    public string?Telefone{get;set;}public string Assunto{get;set;}="";
    public string Mensagem{get;set;}="";public List<IFormFile>?Imagens{get;set;}
}
