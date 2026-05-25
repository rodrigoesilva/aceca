using Aceca.Adm.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Net;
using System.Net.Mail;
using System.Reflection;
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
        public async Task<IEnumerable<SelectListItem>> AsyncCmb_MarcaAcervo()
        {
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
        public async Task<IEnumerable<SelectListItem>> AsyncCmb_MarcaFinalidade()
        {
            var lst = new List<SelectListItem>();

            try
            {
                var lstModel = await _db.MarcaFinalidade
                    .AsNoTracking()
                        ?.Where(s => s.Ativo == true)
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
        public async Task<IEnumerable<SelectListItem>> AsyncCmb_MarcaFabrica()
        {
            var lst = new List<SelectListItem>();

            try
            {
                var lstModel = await _db.MarcaFabrica
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
        public async Task<IEnumerable<SelectListItem>> AsyncCmb_MarcaDimensao()
        {
            var lst = new List<SelectListItem>();

            try
            {
                var lstModel = await _db.MarcaDimensao
                    .AsNoTracking()
                        ?.Where(s => s.Ativo == true)
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
        public async Task<IEnumerable<SelectListItem>> AsyncCmb_MarcaTipo()
        {
            var lst = new List<SelectListItem>();

            try
            {
                var lstModel = await _db.MarcaTipo
                    .AsNoTracking()
                      ?.Where(s => s.Ativo == true)
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
                    .AsNoTracking()
                      ?.Where(s => s.Ativo == true)
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
            var lst = new List<SelectListItem>();

            try
            {
                var lstModel = await _db.MarcaImpressora
                    .AsNoTracking()
                        ?.Where(s => s.Ativo == true)
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
        public async Task<IEnumerable<SelectListItem>> AsyncCmb_MarcaQualidadeImagem()
        {
            var lst = new List<SelectListItem>();

            try
            {
                var lstModel = await _db.MarcaQualidadeImagem
                    .AsNoTracking()
                        ?.Where(s => s.Ativo == true)
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

        public async Task<IEnumerable<SelectListItem>> AsyncCmb_MarcaRaridade()
        {
            var lst = new List<SelectListItem>();

            try
            {
                var lstModel = await _db.MarcaRaridade
                    .AsNoTracking()
                        ?.Where(s => s.Ativo == true)
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

        #endregion

        #region Combos
        public async Task<IEnumerable<SelectListItem>> AsyncCmb_AgendaImagem()
        {
            var lst = new List<SelectListItem>();

            try
            {
                var lstModel = await _db.AgendaImagem
                    .AsNoTracking()
                    ?.Where(s => s.Ativo == true)
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

        public async Task<IEnumerable<SelectListItem>> AsyncCmb_FabricaFase()
        {
            var lst = new List<SelectListItem>();

            try
            {
                var lstModel = await _db.FabricaFase
                    .AsNoTracking()
                    ?.Where(s => s.Ativo == true)
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

        public async Task<IEnumerable<SelectListItem>> AsyncCmb_PaisCategoria()
        {
            var lst = new List<SelectListItem>();

            try
            {
                var lstModel = await _db.PaisCategoria
                    .AsNoTracking()
                       ?.Where(s => s.Ativo == true)
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
            var lst = new List<SelectListItem>();

            try
            {
                var lstModel = await _db.SocioPerfil
                    .AsNoTracking()
                        ?.Where(s => s.Ativo == true)
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

        public async Task<IEnumerable<SelectListItem>> AsyncCmb_SocioTipoPagamento()
        {
            var lst = new List<SelectListItem>();

            try
            {
                var lstModel = await _db.TipoPagamento
                    .AsNoTracking()
                       ?.Where(s => s.Ativo == true)
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

        public enum ETipoEmail
        {
            Cadastro = 0,
            EsqueceuSenha = 1
        }

        #endregion

        // ──────────────────────────────────────────────
        // FUNÇÕES AUXILIARES
        // ──────────────────────────────────────────────


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

        public string GenerateSecuretToken()
        {
            // Gera token seguro
            var tokenBytes = RandomNumberGenerator.GetBytes(32);
            var token = Convert.ToBase64String(tokenBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");

            return token;
        }
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

            if (eTipoMail.Equals(ETipoEmail.EsqueceuSenha))
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
                Subject = "Redefinição de senha - ACECA Área do Sócio",
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

        #endregion


        #endregion
    }
}