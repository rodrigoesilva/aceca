using Aceca.Adm.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using static BCrypt.Net.BCrypt;

namespace Aceca.Adm.Helper
{
    public class HelperExtensionsController : Controller
    {
        #region variaveis

        //private readonly AppDbContext _db = new AppDbContext();
        private readonly ILogger<HelperExtensionsController> _logger;
        private readonly IConfiguration _appConfiguration;
        private readonly IWebHostEnvironment _appEnvironment;
        private readonly AppDbContext _db;
        private readonly IMemoryCache _cache;

        private static List<SelectListItem> _cacheMarcaFase;
        private static readonly ConcurrentDictionary<int, List<SelectListItem>> _cacheMarcaFaseByAcervo = new();

        // TTL dos combos de referência (AsyncCmb_*) cacheados via IMemoryCache: dados quase
        // estáticos (fases, tipos, categorias etc.), mas com expiração — não indefinido como
        // _cacheMarcaFase acima — para que um item recém-cadastrado pelo admin apareça nos
        // combos em poucos minutos, sem precisar reiniciar o processo.
        private static readonly TimeSpan _cacheComboTtl = TimeSpan.FromMinutes(15);
        //

        #endregion

        public HelperExtensionsController(ILogger<HelperExtensionsController> logger,
            IConfiguration cfg,
            AppDbContext db,
            IWebHostEnvironment env,
            IMemoryCache cache)
        {
            _logger = logger;
            _appEnvironment = env;
            _appConfiguration = cfg;
            _db = db;
            _cache = cache;
        }



        // ──────────────────────────────────────────────
        // ENUM
        // ──────────────────────────────────────────────


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



        #region Enum Acervo
        public enum EAcervo
        {
            Nenhum = 0,
            Geral = 1,
            Amostra = 2,
            Palheiro = 3,
            Cigarrilha = 4,
            Charutos = 5,
            FumosDiversos = 6,
            Afins = 7
        }
        #endregion

        #region Enum Fases

        public enum EFase
        {
            Pre = 10,
            Reis = 11,
            Pi1 = 12,
            Pi2 = 13,
            SA = 14,
            ams20 = 15,
            amc20 = 16,
            AM = 17,
            AMI = 18,
            Av6 = 19,
            Av5 = 20,
            Av9 = 21,
            AvDPF10 = 22,
            AvDS10 = 23,
            Av10 = 24,
            Av136 = 25,
            Frontal136 = 26,
            Palheiros = 27,
            Fumos_Cigarrilhas_RP = 28,
            Exportacao = 29,
            Cortadas = 32,
            Outros = 33,
            Quarentena = 34,
            Amarelo136 = 35,
            Comemorativas = 36,
            Vitrine = 38,
            Clandestinas = 39,
            Exterior = 40,
            MC = 41,
            QRCode136 = 42
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
            Socio = 5,
            Banido = 6
        }

        public enum ETipoEmail
        {
            Cadastro = 0,
            EsqueceuSenha = 1,
            ColecaoInteresse = 2,
            AcessoImagemIndevido = 3,
            FinanceiroPendente = 4
        }

        public enum EColecaoAcao
        {
            ColecaoDelete = 0,
            ColecaoIncluir = 1,
            ColecaoInteresse = 2,
            ColecaoNegociar = 3,
            ColecaoObs = 4,
        }

        public enum ENegociacaoAcao
        {
            NegociacaoMeusNegocios = 0,
            NegociacaoSocio = 1,
            NegociacaoAcervo = 2,
        }
        public enum EColecaoStatus
        {
            [Description("Minha Coleção")] Possui = 1,
            [Description("Meus Interesses")] Interesse = 2,
            [Description("Para Negociação")] DisponivelNegocio = 3,
        }
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
        public async Task<IEnumerable<SelectListItem>> AsyncCmb_MarcaAcervo()
        {
            return await _cache.GetOrCreateAsync("cmb_MarcaAcervo", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _cacheComboTtl;

                return await _db.MarcaAcervo
                    .AsNoTracking()
                    .Where(x => (bool)x.Ativo)
                    .OrderBy(x => x.Id)
                    .Select(x => new SelectListItem
                    {
                        Value = x.Id.ToString(),
                        Text = x.Descricao
                    })
                    .ToListAsync();
            });
        }
        public async Task<IEnumerable<SelectListItem>> AsyncCmb_MarcaFase()
        {
            if (_cacheMarcaFase != null)
                return _cacheMarcaFase;

            var data = await _db.MarcaFase
                .AsNoTracking()
                .Where(x => x.Ativo)
                .OrderBy(x => x.Ordem)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.Descricao
                })
                .ToListAsync();

            _cacheMarcaFase = data;

            return data;
        }

        // Retorna somente as fases que possuem ao menos um item de acervo (tabela marcas)
        // cadastrado para o idMarcaAcervo informado. Resultado é cacheado em memória por
        // acervo (poucos valores possíveis), evitando bater no banco a cada troca de combo.
        public async Task<IEnumerable<SelectListItem>> AsyncCmb_MarcaFaseByAcervo(int id)
        {
            if (_cacheMarcaFaseByAcervo.TryGetValue(id, out var cached))
                return cached;

            var lstModelOrd = await _db.Marca
                .AsNoTracking()
                .Where(x => x.MarcaAcervoId == id && x.MarcaFase != null && x.MarcaFase.Ativo == true)
                .Select(x => x.MarcaFase)
                .Distinct()
                .ToListAsync();

            var lst = lstModelOrd
                .OrderBy(x => x.Ordem)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.Descricao
                })
                .ToList();

            _cacheMarcaFaseByAcervo[id] = lst;

            return lst;
        }
        public async Task<IEnumerable<SelectListItem>> AsyncCmb_MarcaFinalidade()
        {
            return await _cache.GetOrCreateAsync("cmb_MarcaFinalidade", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _cacheComboTtl;

                var lstModel = await _db.MarcaFinalidade
                    .AsNoTracking()
                    .Where(s => s.Ativo == true)
                    .OrderBy(m => m.Descricao)
                    .ToListAsync();

                return lstModel.Select(element => new SelectListItem
                {
                    Value = element.Id.ToString(),
                    Text = element.Descricao
                }).ToList();
            });
        }
        public async Task<IEnumerable<SelectListItem>> AsyncCmb_MarcaFabrica()
        {
            return await _cache.GetOrCreateAsync("cmb_MarcaFabrica", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _cacheComboTtl;

                var lstModel = await _db.MarcaFabrica
                    .AsNoTracking()
                    .Where(s => s.Ativo == true)
                    .OrderBy(m => m.Nome)
                    .ToListAsync();

                return lstModel.Select(element => new SelectListItem
                {
                    Value = element.Id.ToString(),
                    Text = element.Nome
                }).ToList();
            });
        }
        public async Task<IEnumerable<SelectListItem>> AsyncCmb_MarcaDimensao()
        {
            return await _cache.GetOrCreateAsync("cmb_MarcaDimensao", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _cacheComboTtl;

                var lstModel = await _db.MarcaDimensao
                    .AsNoTracking()
                    .Where(s => s.Ativo == true)
                    .OrderBy(m => m.Descricao)
                    .ToListAsync();

                return lstModel.Select(element => new SelectListItem
                {
                    Value = element.Id.ToString(),
                    Text = element.Descricao
                }).ToList();
            });
        }
        public async Task<IEnumerable<SelectListItem>> AsyncCmb_MarcaTipo()
        {
            return await _cache.GetOrCreateAsync("cmb_MarcaTipo", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _cacheComboTtl;

                var lstModel = await _db.MarcaTipo
                    .AsNoTracking()
                    .Where(s => s.Ativo == true)
                    .OrderBy(m => m.Descricao)
                    .ToListAsync();

                return lstModel.Select(element => new SelectListItem
                {
                    Value = element.Id.ToString(),
                    Text = element.Descricao
                }).ToList();
            });
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
            return await _cache.GetOrCreateAsync("cmb_MarcaSubTipo", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _cacheComboTtl;

                var lstModel = await _db.MarcaSubTipo
                    .AsNoTracking()
                    .Where(s => s.Ativo == true)
                    .OrderBy(m => m.Descricao)
                    .ToListAsync();

                return lstModel.Select(element => new SelectListItem
                {
                    Value = element.Id.ToString(),
                    Text = element.Descricao
                }).ToList();
            });
        }
        public async Task<IEnumerable<SelectListItem>> AsyncCmb_MarcaSubTipoByTipo(int id)
        {
            var lst = new List<SelectListItem>();

            try
            {
                var lstModel = await _db.MarcaSubTipo
                    .AsNoTracking()
                      ?.Where(s => s.MarcaTipoId.Equals(id))
                      .OrderBy(m => m.Descricao)
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
            return await _cache.GetOrCreateAsync("cmb_MarcaImpressora", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _cacheComboTtl;

                var lstModel = await _db.MarcaImpressora
                    .AsNoTracking()
                    .Where(s => s.Ativo == true)
                    .OrderBy(m => m.Descricao)
                    .ToListAsync();

                return lstModel.Select(element => new SelectListItem
                {
                    Value = element.Id.ToString(),
                    Text = element.Descricao
                }).ToList();
            });
        }
        public async Task<IEnumerable<SelectListItem>> AsyncCmb_MarcaQualidadeImagem()
        {
            return await _cache.GetOrCreateAsync("cmb_MarcaQualidadeImagem", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _cacheComboTtl;

                var lstModel = await _db.MarcaQualidadeImagem
                    .AsNoTracking()
                    .Where(s => s.Ativo == true)
                    .OrderBy(m => m.Descricao)
                    .ToListAsync();

                return lstModel.Select(element => new SelectListItem
                {
                    Value = element.Id.ToString(),
                    Text = element.Descricao
                }).ToList();
            });
        }

        public async Task<IEnumerable<SelectListItem>> AsyncCmb_MarcaRaridade()
        {
            return await _cache.GetOrCreateAsync("cmb_MarcaRaridade", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _cacheComboTtl;

                var lstModel = await _db.MarcaRaridade
                    .AsNoTracking()
                    .Where(s => s.Ativo == true)
                    .OrderBy(m => m.Descricao)
                    .ToListAsync();

                return lstModel.Select(element => new SelectListItem
                {
                    Value = element.Id.ToString(),
                    Text = element.Descricao
                }).ToList();
            });
        }

        #endregion

        #region Combos
        public async Task<IEnumerable<SelectListItem>> AsyncCmb_AgendaImagem()
        {
            return await _cache.GetOrCreateAsync("cmb_AgendaImagem", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _cacheComboTtl;

                var lstModel = await _db.AgendaImagem
                    .AsNoTracking()
                    .Where(s => s.Ativo == true)
                    .OrderBy(m => m.Descricao)
                    .ToListAsync();

                return lstModel.Select(element => new SelectListItem
                {
                    Value = element.Id.ToString(),
                    Text = element.Descricao
                }).ToList();
            });
        }

        public async Task<IEnumerable<SelectListItem>> AsyncCmb_FabricaFase()
        {
            return await _cache.GetOrCreateAsync("cmb_FabricaFase", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _cacheComboTtl;

                var lstModel = await _db.FabricaFase
                    .AsNoTracking()
                    .Where(s => s.Ativo == true)
                    .OrderBy(m => m.Descricao)
                    .ToListAsync();

                return lstModel.Select(element => new SelectListItem
                {
                    Value = element.Id.ToString(),
                    Text = element.Descricao
                }).ToList();
            });
        }

        public async Task<IEnumerable<SelectListItem>> AsyncCmb_PaisCategoria()
        {
            return await _cache.GetOrCreateAsync("cmb_PaisCategoria", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _cacheComboTtl;

                var lstModel = await _db.PaisCategoria
                    .AsNoTracking()
                    .Where(s => s.Ativo == true)
                    .OrderBy(m => m.Descricao)
                    .ToListAsync();

                return lstModel.Select(element => new SelectListItem
                {
                    Value = element.Id.ToString(),
                    Text = element.Descricao
                }).ToList();
            });
        }

        public async Task<IEnumerable<SelectListItem>> AsyncCmb_Socio()
        {
            var lst = new List<SelectListItem>();

            try
            {
                var lstModel = await _db.Socio
                    .AsNoTracking()
                        ?.Where(s => s.Ativo == true)
                       .OrderBy(m => m.Nome)
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
            return await _cache.GetOrCreateAsync("cmb_SocioPerfil", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _cacheComboTtl;

                var lstModel = await _db.SocioPerfil
                    .AsNoTracking()
                    .Where(s => s.Ativo == true)
                    .OrderBy(m => m.Descricao)
                    .ToListAsync();

                return lstModel.Select(element => new SelectListItem
                {
                    Value = element.Id.ToString(),
                    Text = element.Descricao
                }).ToList();
            });
        }

        public async Task<IEnumerable<SelectListItem>> AsyncCmb_SocioTipoPagamento()
        {
            return await _cache.GetOrCreateAsync("cmb_SocioTipoPagamento", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _cacheComboTtl;

                var lstModel = await _db.TipoPagamento
                    .AsNoTracking()
                    .Where(s => s.Ativo == true)
                    .OrderBy(m => m.Descricao)
                    .ToListAsync();

                return lstModel.Select(element => new SelectListItem
                {
                    Value = element.Id.ToString(),
                    Text = element.Descricao
                }).ToList();
            });
        }

        #endregion

        #region Colecao

        public async Task<IEnumerable<SelectListItem>> AsyncCmb_ColecaoStatus()
        {
            var enumData = new List<SelectListItem>();

            try
            {
                enumData = (Enum.GetValues(typeof(EColecaoStatus))
                    .Cast<EColecaoStatus>()
                    .Select(e => new SelectListItem()
                    {
                        Text = GetEnumDescription((EColecaoStatus)e),
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
        #endregion

        // ──────────────────────────────────────────────
        // FUNÇÕES AUXILIARES
        // ──────────────────────────────────────────────


        #region Funções - Pass
        public string GenerateStringPassword(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

            return RandomNumberGenerator.GetString(chars, length);
        }

        public string GenerateSecuretToken()
        {
            // Gera token seguro
            var tokenBytes = RandomNumberGenerator.GetBytes(32);
            var token = Convert.ToBase64String(tokenBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");

            return token;
        }

        public bool IsMD5Hash(string input)
        {
            if (string.IsNullOrEmpty(input) || input.Length != 32) return false;

            return Regex.IsMatch(input, "^[0-9a-fA-F]{32}$", RegexOptions.Compiled);
        }

        #region MD5        

        public string GenerateMD5HashPassword(MD5 md5Hash, string input)
        {
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
                sBuilder.Append(data[i].ToString("x2"));

            var hash = sBuilder.ToString();

            return hash;
        }

        public bool VerifyMd5HashWithMySecurity(MD5 md5Hash, string inputPassword, string hashedPassword)
        {
            if (string.IsNullOrEmpty(inputPassword) || string.IsNullOrEmpty(hashedPassword)) return false;

            // O que decide o algoritmo é o formato do hash SALVO no banco, não da senha digitada
            // (que é sempre texto puro). Enquanto a migração para BCrypt não roda, hashedPassword
            // continua em MD5 (32 hex chars).
            if (!IsMD5Hash(hashedPassword))
                return VerifyHashPassword(inputPassword, hashedPassword);

            //somente se senha ainda MD5
            string hashOfInput = GenerateMD5HashPassword(md5Hash, inputPassword);

            StringComparer comparer = StringComparer.OrdinalIgnoreCase;

            return comparer.Compare(hashOfInput, hashedPassword) == 0;
        }
        #endregion

        #region BCCryp

        // 1. Hash a password (e.g., during User Registration)
        // Save this 'hashedPassword' string directly to your database
        public string GenerateHashPassword(string inputPassword)
        {
            const int WorkFactor = 12;
            var hashedPassword = HashPassword(inputPassword, WorkFactor);

            return hashedPassword;
        }

        // 2. Verify a password (e.g., during User Login)
        // Returns true if the password matches, false otherwise
        public bool VerifyHashPassword(string cleanPassword, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(storedHash))
                return false;

            try
            {
                return Verify(cleanPassword, storedHash);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                // storedHash nulo/vazio ou em formato que não é um BCrypt válido
                // (ex.: registro legado ainda não migrado) — trata como credencial inválida
                // em vez de deixar a exceção "Invalid salt version" vazar para o usuário.
                return false;
            }
        }
        #endregion

        #endregion

        // ──────────────────────────────────────────────
        // E-MAIL
        // ──────────────────────────────────────────────

        #region Email


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

        #region Envio Email

        /// <summary>
        /// Envia o e-mail de reset de senha via SMTP configurado no appsettings.json.
        /// Configure as chaves: Email:Host, Email:Port, Email:EnableSsl,
        ///                      Email:From, Email:User, Email:Password, Email:DisplayName
        /// </summary>
        public async Task<IActionResult> EnviarEmailAsync(ETipoEmail eTipoMail, string toEmail, string socioNome, string resetLink)
        {
            var smtpHost = _appConfiguration["Email:Host"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_appConfiguration["Email:Port"] ?? "587");
            var smtpSsl = bool.Parse(_appConfiguration["Email:EnableSsl"] ?? "true");
            var smtpFrom = _appConfiguration["Email:From"] ?? "noreply@aceca.com.br";
            var smtpUser = _appConfiguration["Email:User"] ?? smtpFrom;
            var smtpPassword = _appConfiguration["Email:Password"] ?? "";
            var displayName = _appConfiguration["Email:DisplayName"] ?? "ACECA - Área do Sócio";

            var strBody = string.Empty;
            var strSubject = "Redefinição de senha - ACECA Área do Sócio";

            if (eTipoMail.Equals(ETipoEmail.FinanceiroPendente))
            {
                strSubject = "Sua anuidade está a vencer - ACECA Área do Sócio";

                strBody = $@"
                    <!DOCTYPE html>
                    <html lang=""pt-BR"">
                        <head><meta charset=""UTF-8""></head>
                        <body style=""font-family:Arial,sans-serif;background:#f4f4f4;padding:30px;"">
                            <div style=""max-width:520px;margin:0 auto;background:#fff;border-radius:10px;padding:36px 40px;box-shadow:0 2px 12px rgba(0,0,0,.08);"">
                                <div style=""text-align:center;"">
                                    <img src=""https://www.aceca.com.br/img/logo/logo02.png"" alt=""ACECA"" width=""250"" style=""max-width:100%;"">
                                </div>
                                <h2 style=""color:#47007b;margin-top:0;"">Sua anuidade está a vencer</h2>
                                <p>Olá, {socioNome}</p>
                                <p>A sua associação está com o pagamento a vencer nos próximos <strong>7 dias</strong>.
                                   Não perca seu acesso, fazendo sua renovação com a ACECA.</p>
                                <p>Queremos que você esteja conosco desfrutando de todo o nosso acervo.</p>
                                <p><strong>Renove sua anuidade com a ACECA.</strong></p>
                                <hr style=""border:none;border-top:1px solid #eee;margin:24px 0;"">
                                <p style=""font-size:12px;color:#aaa;text-align:center;"">
                                  © ACECA - Associação dos Colecionadores de Embalagens de Cigarros e Afins
                                </p>
                            </div>
                        </body>
                    </html>";
            }
            else if (eTipoMail.Equals(ETipoEmail.Cadastro))
            {
                strSubject = "Seja bem-vindo(a) à ACECA - Complete seu cadastro";

                strBody = $@"
                    <!DOCTYPE html>
                    <html lang=""pt-BR"">
                        <head><meta charset=""UTF-8""></head>
                        <body style=""font-family:Arial,sans-serif;background:#f4f4f4;padding:30px;"">
                            <div style=""max-width:520px;margin:0 auto;background:#fff;border-radius:10px;padding:36px 40px;box-shadow:0 2px 12px rgba(0,0,0,.08);"">
                                <div style=""text-align:center;"">
                                    <img src=""https://www.aceca.com.br/img/logo/logo02.png"" alt=""ACECA"" width=""250"" style=""max-width:100%;"">
                                </div>
                                <h2 style=""color:#47007b;margin-top:0;"">Seja bem-vindo(a) à ACECA!</h2>
                                <p>Olá, {socioNome}</p>
                                <p>É um prazer ter você com a gente! Seu cadastro na <strong>ACECA Área do Sócio</strong> foi criado com sucesso.</p>
                                <p>Para concluir seu ingresso e liberar seu acesso, falta só um passo: confirme seus dados e crie sua senha de acesso clicando no botão abaixo.</p>
                                <p><em>Este link é válido por <strong>24 horas</strong>.</em></p>
                                <div style=""text-align:center;margin:32px 0;"">
                                    <a href=""{resetLink}"" style=""background:#47007b;color:#fff;padding:14px 32px;border-radius:6px; text-decoration:none;font-size:16px;display:inline-block;"">
                                        Completar meu cadastro
                                    </a>
                                </div>
                                <p style=""font-size:13px;color:#888;"">Você só conseguirá acessar a Área do Sócio depois de concluir esta etapa.
                                   Se você não reconhece este cadastro, entre em contato conosco.</p>
                                <hr style=""border:none;border-top:1px solid #eee;margin:24px 0;"">
                                <p style=""font-size:12px;color:#aaa;text-align:center;"">
                                  © ACECA - Associação dos Colecionadores de Embalagens de Cigarros e Afins
                                </p>
                            </div>
                        </body>
                    </html>";
            }
            else if (eTipoMail.Equals(ETipoEmail.EsqueceuSenha))
            {
                strBody = $@"
                    <!DOCTYPE html>
                    <html lang=""pt-BR"">
                        <head><meta charset=""UTF-8""></head>
                        <body style=""font-family:Arial,sans-serif;background:#f4f4f4;padding:30px;"">
                            <div style=""max-width:520px;margin:0 auto;background:#fff;border-radius:10px;padding:36px 40px;box-shadow:0 2px 12px rgba(0,0,0,.08);"">
                                <div style=""text-align:center;"">
                                    <img src=""https://www.aceca.com.br/img/logo/logo02.png"" alt=""ACECA"" width=""250"" style=""max-width:100%;"">
                                </div>
                                <h2 style=""color:#47007b;margin-top:0;"">Redefinição de Senha</h2>
                                <p>Olá, {socioNome}</p>
                                <p>Recebemos uma solicitação para redefinir a senha da sua conta na <strong>ACECA Área do Sócio</strong>.</p>
                                <p>Clique no botão abaixo para criar uma nova senha.<br>
                                   <em>Este link é válido por <strong>24 horas</strong>.</em>
                                </p>
                                <div style=""text-align:center;margin:32px 0;"">
                                    <a href=""{resetLink}"" style=""background:#47007b;color:#fff;padding:14px 32px;border-radius:6px; text-decoration:none;font-size:16px;display:inline-block;"">
                                        Redefinir minha senha
                                    </a>
                                </div>
                                <p style=""font-size:13px;color:#888;"">Se você não solicitou a redefinição, ignore este e-mail.
                                   Sua senha permanece inalterada.</p>
                                <hr style=""border:none;border-top:1px solid #eee;margin:24px 0;"">
                                <p style=""font-size:12px;color:#aaa;text-align:center;"">
                                  © ACECA - Associação dos Colecionadores de Embalagens de Cigarros e Afins
                                </p>
                            </div>
                        </body>
                    </html>";
            }
            else
            {
                strBody = $@"
                    <!DOCTYPE html>
                    <html lang=""pt-BR"">
                        <head><meta charset=""UTF-8""></head>
                        <body style=""font-family:Arial,sans-serif;background:#f4f4f4;padding:30px;"">
                            <div style=""max-width:520px;margin:0 auto;background:#fff;border-radius:10px;padding:36px 40px;box-shadow:0 2px 12px rgba(0,0,0,.08);"">
                                <div style=""text-align:center;"">
                                    <img src=""https://www.aceca.com.br/img/logo/logo02.png"" alt=""ACECA"" width=""250"" style=""max-width:100%;"">
                                </div>
                                <h2 style=""color:#47007b;margin-top:0;"">Cadastro de Sócio</h2>
                                <p>Olá, {socioNome}</p>
                                <p>Recebemos uma solicitação de seu cadastro na <strong>ACECA Área do Sócio</strong>.</p>
                                <p>Clique no botão abaixo para criar uma nova senha.<br>
                                   <em>Este link é válido por <strong>24 horas</strong>.</em>
                                </p>
                                <div style=""text-align:center;margin:32px 0;"">
                                    <a href=""{resetLink}"" style=""background:#47007b;color:#fff;padding:14px 32px;border-radius:6px; text-decoration:none;font-size:16px;display:inline-block;"">
                                        Realizar meu primeiro acesso
                                    </a>
                                </div>
                                <p style=""font-size:13px;color:#888;"">Se você não solicitou o seu cadastro, ignore este e-mail.</p>
                                <hr style=""border:none;border-top:1px solid #eee;margin:24px 0;"">
                                <p style=""font-size:12px;color:#aaa;text-align:center;"">
                                  © ACECA - Associação dos Colecionadores de Embalagens de Cigarros e Afins
                                </p>
                            </div>
                        </body>
                    </html>";
            }

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpFrom, displayName),
                Subject = strSubject,
                IsBodyHtml = true,
                Body = strBody
            };

            mailMessage.To.Add(toEmail);

            using var smtp = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPassword),
                EnableSsl = smtpSsl
            };

            // 3. Send asynchronously
            try
            {
                await smtp.SendMailAsync(mailMessage);
                // The task completes when the message is successfully sent
            }
            catch (SmtpException ex)
            {
                var mensagemErro = $"ERRO :: {MethodBase.GetCurrentMethod().Name} - {MethodBase.GetCurrentMethod().DeclaringType.Name} :: {ex?.Message}";
                _logger.LogError(mensagemErro);
                return BadRequest(new { bResult = false, type = "ERRO", message = mensagemErro });
            }

            return Ok(true);
        }

        /// <summary>
        /// Envia alerta silencioso para ti@aceca.com.br quando detectada
        /// tentativa de acesso indevido a uma imagem protegida.
        /// </summary>
        public async Task EnviarAlertaImagemAsync(
            string socioId, string socioNome,
            string codigoAceca, string imagemSrc, string urlAcesso,
            string acao, string timestamp)
        {
            var smtpHost     = _appConfiguration["Email:Host"]        ?? "smtp.gmail.com";
            var smtpPort     = int.Parse(_appConfiguration["Email:Port"]      ?? "587");
            var smtpSsl      = bool.Parse(_appConfiguration["Email:EnableSsl"] ?? "true");
            var smtpFrom     = _appConfiguration["Email:From"]        ?? "noreply@aceca.com.br";
            var smtpUser     = _appConfiguration["Email:User"]        ?? smtpFrom;
            var smtpPassword = _appConfiguration["Email:Password"]    ?? "";
            var displayName  = _appConfiguration["Email:DisplayName"] ?? "ACECA - Área do Sócio";

            // Timestamp enviado pelo browser (UTC) convertido para horário de Brasília (UTC-3)
            var timestampBrasil = timestamp;
            if (DateTimeOffset.TryParse(timestamp, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal, out var dtoTimestamp))
                timestampBrasil = dtoTimestamp.ToOffset(TimeSpan.FromHours(-3)).ToString("dd/MM/yyyy HH:mm:ss") + " (Brasília)";

            // Dados do último login do sócio, gravados por AuthController.LoginLog em SocioLogAcesso.
            Models.SocioLogAcesso? ultimoAcesso = null;

            if (int.TryParse(socioId, out var socioIdInt))
            {
                ultimoAcesso = await _db.SocioLogAcesso
                    .AsNoTracking()
                    .Where(x => x.SocioId == socioIdInt)
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync();
            }

            var linhasLogAcesso = "";

            if (ultimoAcesso != null)
            {
                linhasLogAcesso = $@"
                      <tr>
                        <td style=""padding:10px 14px;font-weight:bold;border:1px solid #e0d0f0;"">IP do Login</td>
                        <td style=""padding:10px 14px;border:1px solid #e0d0f0;"">{ultimoAcesso.IP}</td>
                      </tr>
                      <tr style=""background:#f9f0ff;"">
                        <td style=""padding:10px 14px;font-weight:bold;border:1px solid #e0d0f0;"">Sistema Operacional</td>
                        <td style=""padding:10px 14px;border:1px solid #e0d0f0;"">{ultimoAcesso.OS}</td>
                      </tr>
                      <tr>
                        <td style=""padding:10px 14px;font-weight:bold;border:1px solid #e0d0f0;"">Navegador</td>
                        <td style=""padding:10px 14px;border:1px solid #e0d0f0;"">{ultimoAcesso.Browser}</td>
                      </tr>
                      <tr style=""background:#f9f0ff;"">
                        <td style=""padding:10px 14px;font-weight:bold;border:1px solid #e0d0f0;"">Dispositivo</td>
                        <td style=""padding:10px 14px;border:1px solid #e0d0f0;"">{ultimoAcesso.Device}</td>
                      </tr>
                      <tr>
                        <td style=""padding:10px 14px;font-weight:bold;border:1px solid #e0d0f0;"">Operadora</td>
                        <td style=""padding:10px 14px;border:1px solid #e0d0f0;"">{ultimoAcesso.Operadora}</td>
                      </tr>
                      <tr style=""background:#f9f0ff;"">
                        <td style=""padding:10px 14px;font-weight:bold;border:1px solid #e0d0f0;"">Local do Login</td>
                        <td style=""padding:10px 14px;border:1px solid #e0d0f0;"">{ultimoAcesso.Cidade} / {ultimoAcesso.Estado}</td>
                      </tr>
                      <tr>
                        <td style=""padding:10px 14px;font-weight:bold;border:1px solid #e0d0f0;"">Coordenadas do Login</td>
                        <td style=""padding:10px 14px;border:1px solid #e0d0f0;"">{ultimoAcesso.Latitude}, {ultimoAcesso.Longitude}</td>
                      </tr>
                      <tr style=""background:#f9f0ff;"">
                        <td style=""padding:10px 14px;font-weight:bold;border:1px solid #e0d0f0;"">Último Login Registrado</td>
                        <td style=""padding:10px 14px;border:1px solid #e0d0f0;"">{ultimoAcesso.UltimoLogin?.ToString("dd/MM/yyyy HH:mm:ss")}</td>
                      </tr>";
            }

            var body = $@"
                <!DOCTYPE html>
                <html lang=""pt-BR"">
                <head><meta charset=""UTF-8""></head>
                <body style=""font-family:Arial,sans-serif;background:#f4f4f4;padding:30px;"">
                  <div style=""max-width:600px;margin:0 auto;background:#fff;border-radius:10px;
                               padding:36px 40px;box-shadow:0 2px 12px rgba(0,0,0,.08);"">
                    <div style=""text-align:center;"">
                      <img src=""https://www.aceca.com.br/img/logo/logo02.png""
                           alt=""ACECA"" width=""200"" style=""max-width:100%;"">
                    </div>
                    <h2 style=""color:#cc0000;margin-top:24px;"">
                      ⚠️ Tentativa de Acesso Indevido a Imagem
                    </h2>
                    <table style=""width:100%;border-collapse:collapse;margin-top:16px;"">
                      <tr style=""background:#f9f0ff;"">
                        <td style=""padding:10px 14px;font-weight:bold;width:40%;border:1px solid #e0d0f0;"">Timestamp</td>
                        <td style=""padding:10px 14px;border:1px solid #e0d0f0;"">{timestampBrasil}</td>
                      </tr>
                      <tr>
                        <td style=""padding:10px 14px;font-weight:bold;border:1px solid #e0d0f0;"">Sócio (Nome)</td>
                        <td style=""padding:10px 14px;border:1px solid #e0d0f0;"">{socioNome}</td>
                      </tr>
                      <tr style=""background:#f9f0ff;"">
                        <td style=""padding:10px 14px;font-weight:bold;border:1px solid #e0d0f0;"">Sócio (ID)</td>
                        <td style=""padding:10px 14px;border:1px solid #e0d0f0;"">{socioId}</td>
                      </tr>
                      <tr>
                        <td style=""padding:10px 14px;font-weight:bold;border:1px solid #e0d0f0;"">Código ACECA</td>
                        <td style=""padding:10px 14px;border:1px solid #e0d0f0;"">{codigoAceca}</td>
                      </tr>
                      <tr style=""background:#f9f0ff;"">
                        <td style=""padding:10px 14px;font-weight:bold;border:1px solid #e0d0f0;"">URL do Acesso</td>
                        <td style=""padding:10px 14px;border:1px solid #e0d0f0;word-break:break-all;"">{urlAcesso}</td>
                      </tr>
                      <tr>
                        <td style=""padding:10px 14px;font-weight:bold;border:1px solid #e0d0f0;"">URL da Imagem</td>
                        <td style=""padding:10px 14px;border:1px solid #e0d0f0;word-break:break-all;"">{imagemSrc}</td>
                      </tr>
                      <tr style=""background:#f9f0ff;"">
                        <td style=""padding:10px 14px;font-weight:bold;border:1px solid #e0d0f0;"">Ação Detectada</td>
                        <td style=""padding:10px 14px;border:1px solid #e0d0f0;color:#cc0000;
                                    font-weight:bold;"">{acao}</td>
                      </tr>
                      {linhasLogAcesso}
                    </table>
                    <hr style=""border:none;border-top:1px solid #eee;margin:28px 0;"">
                    <p style=""font-size:12px;color:#aaa;text-align:center;"">
                      © ACECA - Associação dos Colecionadores de Embalagens de Cigarros e Afins
                    </p>
                  </div>
                </body>
                </html>";

            var mailMessage = new MailMessage
            {
                From       = new MailAddress(smtpFrom, displayName),
                Subject    = $"⚠️ ALERTA: Acesso indevido do Sócio —  [{socioNome}]",
                IsBodyHtml = true,
                Body       = body
            };
            mailMessage.To.Add("ti@aceca.com.br");

            using var smtp = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPassword),
                EnableSsl   = smtpSsl
            };

            try   { await smtp.SendMailAsync(mailMessage); }
            catch (Exception ex)
            {
                _logger.LogError("ERRO EnviarAlertaImagemAsync :: {msg}", ex.Message);
            }
        }

        #endregion


        #endregion
    }
}