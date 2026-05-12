using Aceca.Adm.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Aceca.Adm.Helper
{
    public class HelperExtensionsController : Controller
    {
        #region variaveis

        private readonly AppDbContext _db = new AppDbContext();
        private readonly ILogger<HelperExtensionsController> _logger;
        private readonly IConfiguration _appConfiguration;
        private readonly IWebHostEnvironment _appEnvironment;

        private static List<SelectListItem> _cacheMarcaFase;
        //

        #endregion

        public HelperExtensionsController(ILogger<HelperExtensionsController> logger, IConfiguration cfg,
            IWebHostEnvironment env)
        {
            _logger = logger;
            _appEnvironment = env;
            _appConfiguration = cfg;
        }


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
        public async Task<IEnumerable<SelectListItem>> AsyncCmb_MarcaFase_ot()
        {
            return await _db.MarcaFase
                .Where(x => (bool)x.Ativo)
                .OrderBy(x => x.Ordem)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.Descricao
                })
                .AsNoTracking()
                .ToListAsync();
        }
        public async Task<IEnumerable<SelectListItem>> AsyncCmb_MarcaFase()
        {
            if (_cacheMarcaFase != null)
                return _cacheMarcaFase;

            var data = await _db.MarcaFase
                .Where(x => x.Ativo)
                .OrderBy(x => x.Ordem)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.Descricao
                })
                .AsNoTracking()
                .ToListAsync();

            _cacheMarcaFase = data;

            return data;
        }

        public async Task<IEnumerable<SelectListItem>> AsyncCmb_MarcaFase1()
        {
            var lst = new List<SelectListItem>();

            try
            {
                var lstModel = await _db.MarcaFase
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
                var lstModel = await _db.MarcaFinalidade
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
                var lstModel = await _db.MarcaFabrica
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
                var lstModel = await _db.MarcaDimensao
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
                var lstModel = await _db.MarcaTipo
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

        public async Task<IEnumerable<SelectListItem>> AsyncCmb_MarcaTipoByFase(int id)
        {
            var lst = new List<SelectListItem>();

            try
            {
                var lstModelOrd = await _db.Marca
                    .AsNoTracking()
                    .Where(x => x.MarcaFaseId.Equals(id) && x.MarcaSubTipo.MarcaTipo != null)
                    .Select(x => x.MarcaSubTipo.MarcaTipo)
                    .Distinct()
                    .ToListAsync();

                var lstModel = lstModelOrd.OrderBy(x => x.Id);

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
                var lstModel = await _db.MarcaSubTipo
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
                var lstModel = await _db.MarcaSubTipo
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
                var lstModel = await _db.MarcaImpressora
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
                var lstModel = await _db.MarcaQualidadeImagem
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

        public async Task<IEnumerable<SelectListItem>> AsyncCmb_MarcaRaridade()
        {
            var lst = new List<SelectListItem>();

            try
            {
                var lstModel = await _db.MarcaRaridade
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

        #region Combos
        public async Task<IEnumerable<SelectListItem>> AsyncCmb_AgendaImagem()
        {
            var lst = new List<SelectListItem>();

            try
            {
                var lstModel = await _db.AgendaImagem
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

        public async Task<IEnumerable<SelectListItem>> AsyncCmb_FabricaFase()
        {
            var lst = new List<SelectListItem>();

            try
            {
                var lstModel = await _db.FabricaFase
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
                var lstModel = await _db.PaisCategoria
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
                var lstModel = await _db.Socio
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
                var lstModel = await _db.SocioPerfil
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

        public async Task<IEnumerable<SelectListItem>> AsyncCmb_SocioTipoPagamento()
        {
            var lst = new List<SelectListItem>();

            try
            {
                var lstModel = await _db.TipoPagamento
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
            Nenhum = 0,
            Fundador = 1,
            MembroHonra = 2,
            InMemoria = 3,
            Administracao = 4,
            Socio = 5
        }

        #endregion

        // ──────────────────────────────────────────────
        // FUNÇÕES AUXILIARES
        // ──────────────────────────────────────────────

        #region Validador Email
        public bool IsValidEmailUsingMailAddress(string emailAddress)
        {
            if (string.IsNullOrWhiteSpace(emailAddress))
                return false;
            try
            {
                // Simple pattern that checks for @ and a domain
                string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

                bool isValid = Regex.IsMatch(emailAddress, pattern, RegexOptions.IgnoreCase);

                return isValid;
            }
            catch (FormatException)
            {
                return false;
            }
        }
        #endregion

        #region Funções - MD5        

        public string GenerateMD5HashPassword(MD5 md5Hash, string input)
        {
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
                sBuilder.Append(data[i].ToString("x2"));

            var hash = sBuilder.ToString();

            return hash;
        }

        public bool VerifyMd5HashWithMySecurity(MD5 md5Hash, string input, string hash)
        {
            string hashOfInput = GenerateMD5HashPassword(md5Hash, input);
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;
            return comparer.Compare(hashOfInput, hash) == 0;
        }

        public static Guid GenerateGuidFromString(string input)
        {
            using MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            return new Guid(hashBytes);
        }
        public string GenerateStringPassword(int length)
        {
            const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            StringBuilder res = new StringBuilder();
            Random rnd = new Random();
            while (0 < length--)
            {
                res.Append(valid[rnd.Next(valid.Length)]);
            }
            return res.ToString();
        }

        #endregion
    }
}