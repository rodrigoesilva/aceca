using Aceca.Adm.Data;
using Aceca.Adm.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aceca.Adm.Controllers;

[ApiController, Route("api/marcas")]
public class MarcasController(AppDbContext db, IWebHostEnvironment env) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            /*var list = await db.Marcas.Take(10).ToListAsync();*/
            
            string strUrlPath = "https://www.aceca.com.br/midia/geral";

            string strUrlImg = "https://www.aceca.com.br/midia/geral/10/pre0752_1.jpg";
            string strUrlImgDetalhe = "https://www.aceca.com.br/midia/geral/detalhes/dpre0752.jpg";

            //$varUrlImg = "".$urlBase.'/midia/geral/'.$linha['faseId'].'/'.$imagem."";
            //$varUrlImgDetalhe = "".$urlBase.'/midia/geral/detalhes/'.$detalhe."";

            var lstModel = await db.Marcas
                   .Include(m => m.MarcaFase)
                   //.Include(f => f.Pais).Select(f => new { f.Id, f.Nome, f.Cidade, f.PaisId, PaisNome = f.Pais != null ? f.Pais.Nome : null }
                   .Include(m => m.MarcaFabrica)
                   .Include(m => m.MarcaSubTipo)
                   .Include(m => m.MarcaTipo)
               .OrderBy(m => m.MarcaFaseId)
                   .ThenBy(m => m.NomeMarca)
                   .ThenBy(m => m.FabricaId)
                   .ThenBy(m => m.FabricaDesc)
                   .ThenBy(m => m.Descricao)
               .Select(m => new {
                   m.Id,
                   NomeFase = m.MarcaFase.Abre,
                   m.CodigoAceca,
                   Imagem = $"{strUrlPath}/{m.MarcaFaseId}/{(m.Imagem.Contains(".") ? m.Imagem : m.Imagem + m.ExtImagem)}",
                   ImagemDetalhe = $"{strUrlPath}/detalhes/{(m.ImagemDetalhe.Contains(".") ? m.ImagemDetalhe : m.ImagemDetalhe + m.ExtImagemDetalhe)}",
                   m.NomeMarca,
                   m.FabricaId,
                   FabricaNome = m.FabricaDesc != null ? m.FabricaDesc : null,
                   m.Descricao,
                   m.IncluidoPor,

                   /*
                   m.ImagemDetalheUrl,
                   m.FaseId,    FaseNome    = m.Fase    != null ? m.Fase.Nome    : null,
                  
                   m.TipoId,    TipoNome    = m.Tipo    != null ? m.Tipo.Nome    : null,
                   m.CriadoEm, m.AtualizadoEm
                   */
               })
               .Take(100)
               .AsNoTracking()
               .ToListAsync();

            if (lstModel.Count <= 0)
            {
                return Ok(new
                {
                    bResult = true,
                    type = "ERRO - VAZIO - lstResult",
                    message = "listagem em branco",
                    data = lstModel
                });
            }

            return Ok(new
            {
                bResult = true,
                type = "OK",
                message = "SUCESSO ::: ",
                data = lstModel,
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                bResult = false,
                type = "ERRO",
                message = ex?.Message
            });
        }

    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var m = new Marca();
        
        //var m = await db.Marcas.Include(x=>x.Fase).Include(x=>x.Fabrica).Include(x=>x.Tipo).FirstOrDefaultAsync(x=>x.Id==id);

        return m == null ? NotFound() : Ok(m);
    }

    [HttpPost]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Post([FromForm] MarcaForm form)
    {
        var m = new Marca();
        /*
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
        */
        await db.SaveChangesAsync();
        return Ok(m);
    }

    [HttpPut("{id}")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Put(int id, [FromForm] MarcaForm form)
    {
        var m = await db.Marcas.FindAsync(id);
        /*
        if (m == null) return NotFound();
        m.Nome        = form.Nome;
        m.CodigoAceca = form.CodigoAceca;
        m.FaseId      = form.FaseId;
        m.FabricaId   = form.FabricaId;
        m.TipoId      = form.TipoId;
        m.Descricao   = form.Descricao;
        m.IncluidoPor = form.IncluidoPor;
        //m.AtualizadoEm = DateTime.UtcNow;
        if (form.ImagemFile?.Length > 0)
            m.ImagemUrl = await SaveFile(form.ImagemFile) ?? m.ImagemUrl;
        if (form.ImagemDetalheFile?.Length > 0)
            m.ImagemDetalheUrl = await SaveFile(form.ImagemDetalheFile) ?? m.ImagemDetalheUrl;
        */
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
