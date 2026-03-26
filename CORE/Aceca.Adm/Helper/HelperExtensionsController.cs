using Aceca.Adm.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

namespace Aceca.Adm.Helper;

public class HelperExtensionsController(AppDbContext db, IWebHostEnvironment env) : Controller
{

    #region variaveis

    private readonly ILogger<HelperExtensionsController> _logger;

    #endregion

    #region Combos Marcas

    public async Task<IEnumerable<SelectListItem>> AsyncCmb_Variante()
    {
        var enumData = new List<SelectListItem>();

        try
        {
            enumData = (Enum.GetValues(typeof(ESimNao))
                .Cast<ESimNao>()
                .Select(e => new SelectListItem()
                {
                    Text = GetEnumDescription((ESimNao)e),
                    Value = Convert.ToInt32(e).ToString(),
                }))
            .ToList();
        }
        catch (Exception ex)
        {
            var msg = !string.IsNullOrEmpty(ex.InnerException?.Message) ? ex.InnerException?.Message : ex.Message;

            throw;
        }

        return enumData;
    }
    public async Task<IEnumerable<SelectListItem>> AsyncCmb_MarcaFase()
    {
        var lst = new List<SelectListItem>();

        try
        {
            var lstModel = await db.MarcaFase
                    ?.Where(s => s.Ativo == true)
                   .OrderBy(m => m.Ordem)
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
    public async Task<IEnumerable<SelectListItem>> AsyncCmb_MarcaFinalidade()
    {
        var lst = new List<SelectListItem>();

        try
        {
            var lstModel = await db.MarcaFinalidade
                    ?.Where(s => s.Ativo == true)
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
            var lstModel = await db.MarcaFabrica
                    ?.Where(s => s.Ativo == true)
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
    public async Task<IEnumerable<SelectListItem>> AsyncCmb_MarcaDimensao()
    {
        var lst = new List<SelectListItem>();

        try
        {
            var lstModel = await db.MarcaDimensao
                    ?.Where(s => s.Ativo == true)
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
    public async Task<IEnumerable<SelectListItem>> AsyncCmb_MarcaTipo()
    {
        var lst = new List<SelectListItem>();

        try
        {
            var lstModel = await db.MarcaTipo
                  ?.Where(s => s.Ativo == true)
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
            var lstModel = await db.MarcaSubTipo
                  ?.Where(s => s.Ativo == true)
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
            var lstModel = await db.MarcaSubTipo
                  ?.Where(s => s.MarcaTipoId.Equals(id))
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
    public async Task<IEnumerable<SelectListItem>> AsyncCmb_MarcaImpressora()
    {
        var lst = new List<SelectListItem>();

        try
        {
            var lstModel = await db.MarcaImpressora
                    ?.Where(s => s.Ativo == true)
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
    public async Task<IEnumerable<SelectListItem>> AsyncCmb_MarcaQualidadeImagem()
    {
        var lst = new List<SelectListItem>();

        try
        {
            var lstModel = await db.MarcaQualidadeImagem
                    ?.Where(s => s.Ativo == true)
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

    #endregion

    public async Task<IEnumerable<SelectListItem>> AsyncCmb_FabricaFase()
    {
        var lst = new List<SelectListItem>();

        try
        {
            var lstModel = await db.FabricaFase
                   ?.Where(s => s.Ativo == true)
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

    public async Task<IEnumerable<SelectListItem>> AsyncCmb_PaisCategoria()
    {
        var lst = new List<SelectListItem>();

        try
        {
            var lstModel = await db.PaisCategoria
                   ?.Where(s => s.Ativo == true)
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

    public async Task<IEnumerable<SelectListItem>> AsyncCmb_Socio()
    {
        var lst = new List<SelectListItem>();

        try
        {
            var lstModel = await db.Socio
                    ?.Where(s => s.Ativo == true)
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

    public async Task<IEnumerable<SelectListItem>> AsyncCmb_SocioPerfil()
    {
        var lst = new List<SelectListItem>();

        try
        {
            var lstModel = await db.SocioPerfil
                    ?.Where(s => s.Ativo == true)
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


    #region Enums Functions
    public static string GetEnumDescription(Enum value)
    {
        var fi = value.GetType().GetField(value.ToString());

        var attributes = fi.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];

        if (attributes != null && attributes.Any())
        {
            return attributes.First().Description;
        }

        return value.ToString();
    }

    #endregion

    #region Enums
    public enum ESimNao
    {
        [Description("Não")] Nao = 0,
        Sim = 1
    }

    public enum EPerfil
    {
        Fundador = 1,
        MembroHonra = 2,
        InMemoria = 3,
        Administracao = 4,
        Socio = 5
    }

    #endregion

}