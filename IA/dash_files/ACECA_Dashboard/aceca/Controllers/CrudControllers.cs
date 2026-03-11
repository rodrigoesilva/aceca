using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Aceca.Api.Data;
using Aceca.Api.Models;

namespace Aceca.Api.Controllers;

// ── HELPER BASE ──
public abstract class CrudBase<T>(AppDbContext db) : ControllerBase where T : class
{
    protected readonly AppDbContext Db = db;
}

// ════════════════════════════════════════════════════════
// AGENDA
// ════════════════════════════════════════════════════════
[ApiController, Route("api/agendas")]
public class AgendasController(AppDbContext db) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Get() => Ok(await db.Agendas.OrderByDescending(a=>a.Data).ToListAsync());
    [HttpGet("{id}")] public async Task<IActionResult> Get(int id) { var r=await db.Agendas.FindAsync(id); return r==null?NotFound():Ok(r); }
    [HttpPost] public async Task<IActionResult> Post([FromBody] AgendaItem m) { db.Agendas.Add(m); await db.SaveChangesAsync(); return Ok(m); }
    [HttpPut("{id}")] public async Task<IActionResult> Put(int id, [FromBody] AgendaItem m) { m.Id=id; db.Entry(m).State=EntityState.Modified; await db.SaveChangesAsync(); return Ok(m); }
    [HttpDelete("{id}")] public async Task<IActionResult> Delete(int id) { var r=await db.Agendas.FindAsync(id); if(r==null)return NotFound(); db.Agendas.Remove(r); await db.SaveChangesAsync(); return Ok(); }
}

// ════════════════════════════════════════════════════════
// SÓCIOS  (READ + UPDATE — no create from admin, use auth)
// ════════════════════════════════════════════════════════
[ApiController, Route("api/socios")]
public class SociosController(AppDbContext db) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Get() => Ok(await db.Socios.Select(s=>new{s.Id,s.Nome,s.Email,s.Cargo,s.Ativo,s.CriadoEm}).ToListAsync());
    [HttpGet("{id}")] public async Task<IActionResult> Get(int id) { var s=await db.Socios.FindAsync(id); return s==null?NotFound():Ok(new{s.Id,s.Nome,s.Email,s.Cargo,s.Ativo}); }
    [HttpPut("{id}")] public async Task<IActionResult> Put(int id, [FromBody] UpdateSocioDto dto)
    {
        var s=await db.Socios.FindAsync(id); if(s==null)return NotFound();
        s.Nome=dto.Nome; s.Email=dto.Email; s.Cargo=dto.Cargo; s.Ativo=dto.Ativo;
        if(!string.IsNullOrEmpty(dto.Senha)) s.SenhaHash=BCrypt.Net.BCrypt.HashPassword(dto.Senha);
        await db.SaveChangesAsync(); return Ok(s);
    }
    [HttpDelete("{id}")] public async Task<IActionResult> Delete(int id) { var s=await db.Socios.FindAsync(id); if(s==null)return NotFound(); db.Socios.Remove(s); await db.SaveChangesAsync(); return Ok(); }
}
public record UpdateSocioDto(string Nome, string Email, string Cargo, bool Ativo, string? Senha);

// ════════════════════════════════════════════════════════
// PAÍSES
// ════════════════════════════════════════════════════════
[ApiController, Route("api/paises")]
public class PaisesController(AppDbContext db) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Get() => Ok(await db.Paises.OrderBy(p=>p.Nome).ToListAsync());
    [HttpGet("{id}")] public async Task<IActionResult> Get(int id) { var r=await db.Paises.FindAsync(id); return r==null?NotFound():Ok(r); }
    [HttpPost] public async Task<IActionResult> Post([FromBody] Pais m) { db.Paises.Add(m); await db.SaveChangesAsync(); return Ok(m); }
    [HttpPut("{id}")] public async Task<IActionResult> Put(int id, [FromBody] Pais m) { m.Id=id; db.Entry(m).State=EntityState.Modified; await db.SaveChangesAsync(); return Ok(m); }
    [HttpDelete("{id}")] public async Task<IActionResult> Delete(int id) { var r=await db.Paises.FindAsync(id); if(r==null)return NotFound(); db.Paises.Remove(r); await db.SaveChangesAsync(); return Ok(); }
}

// ════════════════════════════════════════════════════════
// FÁBRICAS
// ════════════════════════════════════════════════════════
[ApiController, Route("api/fabricas")]
public class FabricasController(AppDbContext db) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Get() => Ok(await db.Fabricas.Include(f=>f.Pais).Select(f=>new{f.Id,f.Nome,f.Cidade,f.PaisId,PaisNome=f.Pais!=null?f.Pais.Nome:null}).ToListAsync());
    [HttpGet("{id}")] public async Task<IActionResult> Get(int id) { var r=await db.Fabricas.Include(f=>f.Pais).FirstOrDefaultAsync(f=>f.Id==id); return r==null?NotFound():Ok(r); }
    [HttpPost] public async Task<IActionResult> Post([FromBody] Fabrica m) { db.Fabricas.Add(m); await db.SaveChangesAsync(); return Ok(m); }
    [HttpPut("{id}")] public async Task<IActionResult> Put(int id, [FromBody] Fabrica m) { m.Id=id; db.Entry(m).State=EntityState.Modified; await db.SaveChangesAsync(); return Ok(m); }
    [HttpDelete("{id}")] public async Task<IActionResult> Delete(int id) { var r=await db.Fabricas.FindAsync(id); if(r==null)return NotFound(); db.Fabricas.Remove(r); await db.SaveChangesAsync(); return Ok(); }
}

// ════════════════════════════════════════════════════════
// DIMENSÕES
// ════════════════════════════════════════════════════════
[ApiController, Route("api/dimensoes")]
public class DimensoesController(AppDbContext db) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Get() => Ok(await db.Dimensoes.ToListAsync());
    [HttpGet("{id}")] public async Task<IActionResult> Get(int id) { var r=await db.Dimensoes.FindAsync(id); return r==null?NotFound():Ok(r); }
    [HttpPost] public async Task<IActionResult> Post([FromBody] Dimensao m) { db.Dimensoes.Add(m); await db.SaveChangesAsync(); return Ok(m); }
    [HttpPut("{id}")] public async Task<IActionResult> Put(int id, [FromBody] Dimensao m) { m.Id=id; db.Entry(m).State=EntityState.Modified; await db.SaveChangesAsync(); return Ok(m); }
    [HttpDelete("{id}")] public async Task<IActionResult> Delete(int id) { var r=await db.Dimensoes.FindAsync(id); if(r==null)return NotFound(); db.Dimensoes.Remove(r); await db.SaveChangesAsync(); return Ok(); }
}

// ════════════════════════════════════════════════════════
// FASES
// ════════════════════════════════════════════════════════
[ApiController, Route("api/fases")]
public class FasesController(AppDbContext db) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Get() => Ok(await db.Fases.ToListAsync());
    [HttpGet("{id}")] public async Task<IActionResult> Get(int id) { var r=await db.Fases.FindAsync(id); return r==null?NotFound():Ok(r); }
    [HttpPost] public async Task<IActionResult> Post([FromBody] Fase m) { db.Fases.Add(m); await db.SaveChangesAsync(); return Ok(m); }
    [HttpPut("{id}")] public async Task<IActionResult> Put(int id, [FromBody] Fase m) { m.Id=id; db.Entry(m).State=EntityState.Modified; await db.SaveChangesAsync(); return Ok(m); }
    [HttpDelete("{id}")] public async Task<IActionResult> Delete(int id) { var r=await db.Fases.FindAsync(id); if(r==null)return NotFound(); db.Fases.Remove(r); await db.SaveChangesAsync(); return Ok(); }
}

// ════════════════════════════════════════════════════════
// IMPRESSORAS
// ════════════════════════════════════════════════════════
[ApiController, Route("api/impressoras")]
public class ImpressorasController(AppDbContext db) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Get() => Ok(await db.Impressoras.ToListAsync());
    [HttpGet("{id}")] public async Task<IActionResult> Get(int id) { var r=await db.Impressoras.FindAsync(id); return r==null?NotFound():Ok(r); }
    [HttpPost] public async Task<IActionResult> Post([FromBody] Impressora m) { db.Impressoras.Add(m); await db.SaveChangesAsync(); return Ok(m); }
    [HttpPut("{id}")] public async Task<IActionResult> Put(int id, [FromBody] Impressora m) { m.Id=id; db.Entry(m).State=EntityState.Modified; await db.SaveChangesAsync(); return Ok(m); }
    [HttpDelete("{id}")] public async Task<IActionResult> Delete(int id) { var r=await db.Impressoras.FindAsync(id); if(r==null)return NotFound(); db.Impressoras.Remove(r); await db.SaveChangesAsync(); return Ok(); }
}

// ════════════════════════════════════════════════════════
// TIPOS
// ════════════════════════════════════════════════════════
[ApiController, Route("api/tipos")]
public class TiposController(AppDbContext db) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Get() => Ok(await db.Tipos.ToListAsync());
    [HttpGet("{id}")] public async Task<IActionResult> Get(int id) { var r=await db.Tipos.FindAsync(id); return r==null?NotFound():Ok(r); }
    [HttpPost] public async Task<IActionResult> Post([FromBody] Tipo m) { db.Tipos.Add(m); await db.SaveChangesAsync(); return Ok(m); }
    [HttpPut("{id}")] public async Task<IActionResult> Put(int id, [FromBody] Tipo m) { m.Id=id; db.Entry(m).State=EntityState.Modified; await db.SaveChangesAsync(); return Ok(m); }
    [HttpDelete("{id}")] public async Task<IActionResult> Delete(int id) { var r=await db.Tipos.FindAsync(id); if(r==null)return NotFound(); db.Tipos.Remove(r); await db.SaveChangesAsync(); return Ok(); }
}

// ════════════════════════════════════════════════════════
// SUB-TIPOS
// ════════════════════════════════════════════════════════
[ApiController, Route("api/subtipos")]
public class SubTiposController(AppDbContext db) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Get() => Ok(await db.SubTipos.Include(s=>s.Tipo).Select(s=>new{s.Id,s.Nome,s.TipoId,TipoNome=s.Tipo!=null?s.Tipo.Nome:null}).ToListAsync());
    [HttpGet("{id}")] public async Task<IActionResult> Get(int id) { var r=await db.SubTipos.FindAsync(id); return r==null?NotFound():Ok(r); }
    [HttpPost] public async Task<IActionResult> Post([FromBody] SubTipo m) { db.SubTipos.Add(m); await db.SaveChangesAsync(); return Ok(m); }
    [HttpPut("{id}")] public async Task<IActionResult> Put(int id, [FromBody] SubTipo m) { m.Id=id; db.Entry(m).State=EntityState.Modified; await db.SaveChangesAsync(); return Ok(m); }
    [HttpDelete("{id}")] public async Task<IActionResult> Delete(int id) { var r=await db.SubTipos.FindAsync(id); if(r==null)return NotFound(); db.SubTipos.Remove(r); await db.SaveChangesAsync(); return Ok(); }
}
