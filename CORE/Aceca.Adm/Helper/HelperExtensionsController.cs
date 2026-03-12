using Aceca.Adm.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Aceca.Adm.Helper;

public class HelperExtensionsController(AppDbContext db, IWebHostEnvironment env) : ControllerBase
{

    #region variaveis

    private readonly ILogger<HelperExtensionsController> _logger;

    #endregion

    #region Combos Marcas
    public async Task<IEnumerable<SelectListItem>> AsyncCmb_MarcaFase()
    {
        var lst = new List<SelectListItem>();

        try
        {
            var lstModel = await db.MarcaFases
                    ?.Where(s => s.Publica == true)
                   .OrderBy(m => m.OrdemExibir)
                   .AsNoTracking()
                   .ToListAsync();

            foreach (var element in lstModel)
                lst.Add(new SelectListItem
                {
                    Value = element.Id.ToString(),
                    Text = element.Titulo
                });
        }
        catch (Exception ex)
        {
            var msg = !string.IsNullOrEmpty(ex.InnerException?.Message) ? ex.InnerException?.Message : ex.Message;
            throw;
        }

        return lst;
    }
    public async Task<IEnumerable<SelectListItem>> AsyncCmb_MarcaTipo()
    {
        var lst = new List<SelectListItem>();

        try
        {
            var lstModel = await db.MarcaTipos
                  //?.Where(s => s.Ativo == true)
                  .OrderBy(m => m.Descricao)
                  .AsNoTracking()
                  .ToListAsync();

            foreach (var element in lstModel)
                lst.Add(new SelectListItem
                {
                    Value = element.Id.ToString(),
                    Text = element.Descricao
                });
        }
        catch (Exception ex)
        {
            var msg = !string.IsNullOrEmpty(ex.InnerException?.Message) ? ex.InnerException?.Message : ex.Message;
            throw;
        }

        return lst;
    }
    public async Task<IEnumerable<SelectListItem>> AsyncCmb_MarcaSubTipo()
    {
        var lst = new List<SelectListItem>();

        try
        {
            var lstModel = await db.MarcaSubTipos
                  //?.Where(s => s.Ativo == true)
                  .OrderBy(m => m.Descricao)
                  .AsNoTracking()
                  .ToListAsync();

            foreach (var element in lstModel)
                lst.Add(new SelectListItem
                {
                    Value = element.Id.ToString(),
                    Text = element.Descricao
                });
        }
        catch (Exception ex)
        {
            var msg = !string.IsNullOrEmpty(ex.InnerException?.Message) ? ex.InnerException?.Message : ex.Message;
            throw;
        }

        return lst;
    }
    public async Task<IEnumerable<SelectListItem>> AsyncCmb_MarcaSubTipoByTipo(int id)
    {
        var lst = new List<SelectListItem>();

        try
        {
            var lstModel = await db.MarcaSubTipos
                  ?.Where(s => s.TipoId.Equals(id))
                  .OrderBy(m => m.Descricao)
                  .AsNoTracking()
                  .ToListAsync();

            foreach (var element in lstModel)
                lst.Add(new SelectListItem
                {
                    Value = element.Id.ToString(),
                    Text = element.Descricao
                });
        }
        catch (Exception ex)
        {
            var msg = !string.IsNullOrEmpty(ex.InnerException?.Message) ? ex.InnerException?.Message : ex.Message;
            throw;
        }

        return lst;
    }
    public async Task<IEnumerable<SelectListItem>> AsyncCmb_MarcaFabrica()
    {
        var lst = new List<SelectListItem>();

        try
        {
            var lstModel = await db.MarcaFabricas
                    //?.Where(s => s.Ativo == true)
                   .OrderBy(m => m.Nome)
                   .AsNoTracking()
                   .ToListAsync();

            foreach (var element in lstModel)
                lst.Add(new SelectListItem
                {
                    Value = element.Id.ToString(),
                    Text = element.Nome
                });
        }
        catch (Exception ex)
        {
            var msg = !string.IsNullOrEmpty(ex.InnerException?.Message) ? ex.InnerException?.Message : ex.Message;
            throw;
        }

        return lst;
    }

    #endregion
}

public enum Permission
{
    Fundador,
    CanViewProducts,
    CanCreateProduct,
    CanDeleteProduct
}

public class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(Permission permission)
    {
        Policy = permission.ToString();
    }
}