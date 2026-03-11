using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Aceca.Api.Data;
using Aceca.Api.Models;

namespace Aceca.Api.Controllers;

[ApiController, Route("api/marcas")]
public class MarcasController(AppDbContext db, IWebHostEnvironment env) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var list = await db.Marcas
            .Include(m => m.Fase)
            .Include(m => m.Fabrica)
            .Include(m => m.Tipo)
            .OrderByDescending(m => m.CriadoEm)
            .Select(m => new {
                m.Id, m.Nome, m.CodigoAceca, m.Descricao,
                m.ImagemUrl, m.ImagemDetalheUrl, m.IncluidoPor,
                m.FaseId,    FaseNome    = m.Fase    != null ? m.Fase.Nome    : null,
                m.FabricaId, FabricaNome = m.Fabrica != null ? m.Fabrica.Nome : null,
                m.TipoId,    TipoNome    = m.Tipo    != null ? m.Tipo.Nome    : null,
                m.CriadoEm, m.AtualizadoEm
            })
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var m = await db.Marcas.Include(x=>x.Fase).Include(x=>x.Fabrica).Include(x=>x.Tipo).FirstOrDefaultAsync(x=>x.Id==id);
        return m == null ? NotFound() : Ok(m);
    }

    [HttpPost]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Post([FromForm] MarcaForm form)
    {
        var m = new Marca {
            Nome         = form.Nome,
            CodigoAceca  = form.CodigoAceca,
            FaseId       = form.FaseId,
            FabricaId    = form.FabricaId,
            TipoId       = form.TipoId,
            Descricao    = form.Descricao,
            IncluidoPor  = form.IncluidoPor,
        };
        m.ImagemUrl        = await SaveFile(form.ImagemFile)        ?? m.ImagemUrl;
        m.ImagemDetalheUrl = await SaveFile(form.ImagemDetalheFile) ?? m.ImagemDetalheUrl;
        db.Marcas.Add(m);
        await db.SaveChangesAsync();
        return Ok(m);
    }

    [HttpPut("{id}")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Put(int id, [FromForm] MarcaForm form)
    {
        var m = await db.Marcas.FindAsync(id);
        if (m == null) return NotFound();
        m.Nome        = form.Nome;
        m.CodigoAceca = form.CodigoAceca;
        m.FaseId      = form.FaseId;
        m.FabricaId   = form.FabricaId;
        m.TipoId      = form.TipoId;
        m.Descricao   = form.Descricao;
        m.IncluidoPor = form.IncluidoPor;
        m.AtualizadoEm = DateTime.UtcNow;
        if (form.ImagemFile?.Length > 0)
            m.ImagemUrl = await SaveFile(form.ImagemFile) ?? m.ImagemUrl;
        if (form.ImagemDetalheFile?.Length > 0)
            m.ImagemDetalheUrl = await SaveFile(form.ImagemDetalheFile) ?? m.ImagemDetalheUrl;
        await db.SaveChangesAsync();
        return Ok(m);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var m = await db.Marcas.FindAsync(id);
        if (m == null) return NotFound();
        db.Marcas.Remove(m);
        await db.SaveChangesAsync();
        return Ok();
    }

    private async Task<string?> SaveFile(IFormFile? file)
    {
        if (file == null || file.Length == 0) return null;
        var ext  = Path.GetExtension(file.FileName).ToLowerInvariant();
        var safe = new[]{".jpg",".jpeg",".png",".webp",".gif"};
        if (!safe.Contains(ext)) return null;
        var dir  = Path.Combine(env.WebRootPath, "uploads");
        Directory.CreateDirectory(dir);
        var name = $"{Guid.NewGuid():N}{ext}";
        var path = Path.Combine(dir, name);
        await using var fs = System.IO.File.Create(path);
        await file.CopyToAsync(fs);
        return $"/uploads/{name}";
    }
}

public class MarcaForm
{
    public string   Nome              { get; set; } = "";
    public string?  CodigoAceca       { get; set; }
    public int?     FaseId            { get; set; }
    public int?     FabricaId         { get; set; }
    public int?     TipoId            { get; set; }
    public string?  Descricao         { get; set; }
    public string?  IncluidoPor       { get; set; }
    public IFormFile? ImagemFile        { get; set; }
    public IFormFile? ImagemDetalheFile { get; set; }
}
