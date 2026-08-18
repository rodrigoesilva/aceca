using Aceca.Adm.Data;
using Aceca.Adm.Helper;
using Aceca.Adm.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using static Aceca.Adm.Helper.HelperExtensionsController;

namespace Aceca.Adm.Controllers
{
    public class AuthController : Controller
    {

        #region variaveis

        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _appConfiguration;
        private readonly IWebHostEnvironment _appEnvironment;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly AppDbContext _db;
        private readonly HelperExtensionsController _helperController;
        private EPerfil _socioPerfil;

        private readonly string _urlBaseImg = string.Empty;
        private readonly string _urlBaseSite = string.Empty;
        private readonly string _urlBaseApp = string.Empty;

        // SigningCredentials cacheados — criados uma vez no construtor, reutilizados a cada login
        private readonly SigningCredentials _jwtSigningCredentials;

        private string _strControllerName = string.Empty;
        private string _strActionName = string.Empty;
        //

        #endregion
        public AuthController(ILogger<AuthController> logger
            , AppDbContext db
            , IWebHostEnvironment env
            , IConfiguration cfg
            , IServiceProvider serviceProvider
            , IHttpClientFactory httpClientFactory
            , HelperExtensionsController helperController)
        { 
            _logger = logger;
            _db = db;
            _appEnvironment = env;
            _appConfiguration = cfg;
            _serviceProvider = serviceProvider;
            _httpClientFactory = httpClientFactory;
            _helperController = helperController;

            _urlBaseImg = _appConfiguration["Url:Img"]!;
            _urlBaseSite = _appConfiguration["Url:Site"]!;
            _urlBaseApp = _appConfiguration["Url:App"]!;

            var jwtKeyBytes = Encoding.UTF8.GetBytes(_appConfiguration["Jwt:Key"]!);
            _jwtSigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(jwtKeyBytes), SecurityAlgorithms.HmacSha256);
        }

        public record LoginIn(string Email, string Senha);
        public record LoginUpdt(string Username, string Email, string Senha, string ConfirmSenha, bool ChkTermo, string Token = null);

        // DTO para ForgotPassword
        public record ForgotPasswordIn(string Email);

        // DTO para ResetPassword
        public record ResetPasswordIn(string Email, string Token, string Senha, string ConfirmSenha);

        // ──────────────────────────────────────────────
        // VIEWS
        // ──────────────────────────────────────────────

        public ActionResult Index()
        {
            // Se o usuário já está autenticado e o cookie ainda é válido (24h),
            // redireciona direto sem precisar logar novamente.
            // O JavaScript da página também verifica o cookie local para exibir
            // a mensagem "Seja bem-vindo novamente" via SweetAlert.
            return View("~/Views/Auth/Login.cshtml");
        }

        public ActionResult SessionExpired()
        {
            return View("~/Views/Pages/MiscSessionExpired.cshtml");
        }

        public ActionResult AccessDenied()
        {
            return View("~/Views/Pages/MiscNotAuthorized.cshtml");
        }

        public ActionResult UpdatePass()
        {
            return View("~/Views/Auth/RegisterUpdate.cshtml");
        }

        [Authorize(Roles = "Administracao, Fundador, MembroHonra, Socio")]
        public IActionResult AccountSettingsProfileUser() => View();

        public IActionResult AccountSettings() => View();

        public IActionResult AccountSettingsSecurity() => View();

        public IActionResult AccountSettingsBilling() => View();

        /// <summary>
        /// Só id + ImgAvatar do sócio autenticado - usado pra atualizar o avatar do navbar
        /// (site.js/fn_SetSessionData) e o Swal de boas-vindas do login (pages-auth.js) em
        /// toda navegação de página, então fica de propósito fora do GetFullById (que faz
        /// join em 5 tabelas) - aqui é sempre 1 tabela, só pela PK.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Administracao, Fundador, MembroHonra, Socio")]
        public async Task<IActionResult> GetAvatarInfo()
        {
            try
            {
                var socioId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

                if (socioId <= 0)
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Sessão inválida" });

                var socio = await _db.Socio
                    .Where(s => s.Id == socioId)
                    .Select(s => new { id = s.Id, imgAvatar = s.ImgAvatar })
                    .FirstOrDefaultAsync();

                if (socio == null)
                    return NotFound(new { bResult = false, type = "ERRO", message = "Sócio não encontrado" });

                return Ok(new { bResult = true, type = "OK", data = socio });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERRO :: {Method}", nameof(GetAvatarInfo));

                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }
        }

        /// <summary>
        /// Situação financeira (socio_financeiro) da tela "Meus Dados" -&gt; Financeiro, sempre
        /// do sócio autenticado. Só projeta o que existe de fato na tabela - não há valor/
        /// preço de plano cadastrado em lugar nenhum do sistema, então esse cálculo (data de
        /// vencimento, dias restantes) é feito no front com a mesma regra de
        /// SocioFinanceiroCheckService (TipoPagamentoId 2/3/4 = Anual/Semestral/Mensal).
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Administracao, Fundador, MembroHonra, Socio")]
        public async Task<IActionResult> GetInfoFinanceira()
        {
            try
            {
                var socioId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

                if (socioId <= 0)
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Sessão inválida" });

                var financeiro = await _db.SocioFinanceiro
                    .Where(f => f.SocioId == socioId)
                    .Select(f => new
                    {
                        tipoPagamentoId = f.TipoPagamentoId,
                        tipoPagamento = f.TipoPagamento.Descricao,
                        pagamentoEmDia = f.PagamentoEmDia,
                        dataUltimoPagamento = f.DataUltimoPagamento,
                    })
                    .FirstOrDefaultAsync();

                if (financeiro == null)
                    return NotFound(new { bResult = false, type = "ERRO", message = "Nenhuma informação financeira encontrada" });

                return Ok(new { bResult = true, type = "OK", data = financeiro });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERRO :: {Method}", nameof(GetInfoFinanceira));

                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }
        }

        /// <summary>
        /// Chave Pix da associação (adm_config, parâmetro "ChavePix") + QR Code gerado na hora
        /// - exibidos na aba Financeiro quando o sócio escolhe pagar via Pix. Não depende de
        /// nenhum serviço externo (QRCoder gera o PNG localmente).
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Administracao, Fundador, MembroHonra, Socio")]
        public async Task<IActionResult> GetChavePix()
        {
            try
            {
                var chavePix = await _db.AdmConfig
                    .Where(c => c.Parametro == "ChavePix")
                    .Select(c => c.Valor)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrWhiteSpace(chavePix))
                    return NotFound(new { bResult = false, type = "ERRO", message = "Chave Pix não configurada" });

                using var qrGenerator = new QRCoder.QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(chavePix, QRCoder.QRCodeGenerator.ECCLevel.Q);
                // Cor primária do tema (--bs-primary) nos módulos escuros - pedido explícito
                // mesmo com o risco de leitura reduzida por contraste menor que preto/branco puro.
                var qrCodePng = new QRCoder.PngByteQRCode(qrCodeData).GetGraphic(20,
                    System.Drawing.ColorTranslator.FromHtml("#8c57ff"),
                    System.Drawing.Color.White);
                var qrCodeDataUri = "data:image/png;base64," + Convert.ToBase64String(qrCodePng);

                return Ok(new { bResult = true, type = "OK", data = new { chavePix, qrCodeDataUri } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERRO :: {Method}", nameof(GetChavePix));

                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }
        }

        /// <summary>
        /// Dados de "Sobre" da tela Meu Perfil - sempre do sócio autenticado (nunca de um id
        /// vindo do cliente, mesma trava de IDOR usada em SocioColecaoController), pra evitar
        /// que qualquer sócio logado consiga ler nome/telefone/e-mail/endereço de outro sócio
        /// só trocando um parâmetro. Projeta os campos explicitamente - nunca retorna o
        /// SocioSeguranca inteiro, que carrega hash e senha em texto puro (SenhaAberta).
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Administracao, Fundador, MembroHonra, Socio")]
        public async Task<IActionResult> GetFullById()
        {
            try
            {
                var socioId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

                if (socioId <= 0)
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Sessão inválida" });

                var model = await (
                    from s in _db.Socio
                    join sp in _db.SocioPerfil on s.SocioPerfilId equals sp.Id
                    join ss in _db.SocioSeguranca on s.Id equals ss.SocioId
                    join sa in _db.SocioAniversario on s.Id equals sa.SocioId into saJoin
                    from sa in saJoin.DefaultIfEmpty()
                    join sc in _db.SocioContato on s.Id equals sc.SocioId into scJoin
                    from sc in scJoin.DefaultIfEmpty()
                    join se in _db.SocioEndereco on s.Id equals se.SocioId into seJoin
                    from se in seJoin.DefaultIfEmpty()
                    where s.Id == socioId
                    select new
                    {
                        id = s.Id,
                        nome = s.Nome,
                        imgAvatar = s.ImgAvatar,
                        dataCriacao = s.DataCriacao,
                        usuario = ss.NomeUsuario,
                        perfil = sp.Descricao,
                        aniversarioDia = (int?)sa.Dia,
                        aniversarioMes = (int?)sa.Mes,
                        aniversarioAno = (int?)sa.Ano,
                        contatoDDI = (int?)sc.DDI,
                        contatoDDD = (int?)sc.DDD,
                        contatoTelefone = (long?)sc.Telefone,
                        email = sc.Email,
                        endereco = se.Endereco,
                        numero = se.Numero,
                        complemento = se.Complemento,
                        bairro = se.Bairro,
                        cidade = se.Cidade,
                        estado = se.Estado,
                        cep = se.CEP,
                    }
                ).AsNoTracking().FirstOrDefaultAsync();

                if (model == null)
                    return NotFound(new { bResult = false, type = "ERRO", message = "Sócio não encontrado" });

                return Ok(new { bResult = true, type = "OK", data = model });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERRO :: {Method}", nameof(GetFullById));

                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }
        }

        /// <summary>
        /// Salva as alterações da tela "Meus Dados" - sempre no sócio autenticado, nunca em um
        /// id vindo do cliente (mesma trava de IDOR do GetFullById acima). Atualiza Socio,
        /// SocioSeguranca.NomeUsuario (apelido de exibição, não é credencial de login),
        /// SocioContato, SocioEndereco e SocioAniversario numa única transação: mesmo padrão de
        /// SocioController.Create, pra não deixar uma tabela atualizada e outra não em caso de
        /// falha no meio do caminho.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administracao, Fundador, MembroHonra, Socio")]
        public async Task<IActionResult> UpdateProfile(string nome, string usuario, int? telefoneDDD, string telefoneNumero,
            string email, string aniversario, string cep, string endereco, string numero, string complemento,
            string bairro, string cidade, string estado)
        {
            try
            {
                var socioId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

                if (socioId <= 0)
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Sessão inválida" });

                if (string.IsNullOrWhiteSpace(nome))
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Nome deve ser preenchido" });

                if (!string.IsNullOrWhiteSpace(email) && !_helperController.IsValidEmailUsingMailAddress(email.Trim().ToLower()))
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Formato de E-mail inválido" });

                var strategy = _db.Database.CreateExecutionStrategy();

                Func<Task<IActionResult>> operation = async () =>
                {
                    using var transaction = await _db.Database.BeginTransactionAsync();

                    var socio = await _db.Socio.FirstOrDefaultAsync(s => s.Id == socioId);

                    if (socio == null)
                        return NotFound(new { bResult = false, type = "ERRO", message = "Sócio não encontrado" });

                    socio.Nome = nome.Trim();

                    var seguranca = await _db.SocioSeguranca.FirstOrDefaultAsync(ss => ss.SocioId == socioId);

                    if (seguranca != null)
                        seguranca.NomeUsuario = !string.IsNullOrWhiteSpace(usuario) ? usuario.Trim() : null;

                    var contato = await _db.SocioContato.FirstOrDefaultAsync(c => c.SocioId == socioId);

                    if (contato != null)
                    {
                        contato.DDD = telefoneDDD;
                        contato.Telefone = long.TryParse(telefoneNumero, out var telefoneNum) ? telefoneNum : null;
                        contato.Email = !string.IsNullOrWhiteSpace(email) ? email.Trim() : null;
                    }

                    var socioEndereco = await _db.SocioEndereco.FirstOrDefaultAsync(e => e.SocioId == socioId);

                    if (socioEndereco != null)
                    {
                        socioEndereco.CEP = cep;
                        socioEndereco.Endereco = endereco;
                        socioEndereco.Numero = numero;
                        socioEndereco.Complemento = complemento;
                        socioEndereco.Bairro = bairro;
                        socioEndereco.Cidade = cidade;
                        socioEndereco.Estado = estado;
                    }

                    var socioAniversario = await _db.SocioAniversario.FirstOrDefaultAsync(a => a.SocioId == socioId);

                    if (socioAniversario != null)
                    {
                        var (dia, mes, ano) = ParseDataAniversario(aniversario);

                        socioAniversario.Dia = dia;
                        socioAniversario.Mes = mes;
                        socioAniversario.Ano = ano;
                    }

                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Ok(new { bResult = true, type = "OK", message = "Dados atualizados com sucesso" });
                };

                return await strategy.ExecuteAsync(operation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERRO :: {Method}", nameof(UpdateProfile));

                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }
        }

        private static (int? Dia, int? Mes, int? Ano) ParseDataAniversario(string dataAniversario)
        {
            if (string.IsNullOrWhiteSpace(dataAniversario))
                return (null, null, null);

            var partes = dataAniversario.Split("/");

            int? dia = partes.Length > 0 && int.TryParse(partes[0].Trim(), out var d) ? d : null;
            int? mes = partes.Length > 1 && int.TryParse(partes[1].Trim(), out var m) ? m : null;
            int? ano = partes.Length > 2 && int.TryParse(partes[2].Trim(), out var a) ? a : null;

            return (dia, mes, ano);
        }

        /// <summary>
        /// Foto de perfil do sócio autenticado. Salva sempre como img/avatars/socio/imgAvatar{id}.png
        /// (nome fixo derivado do id, nunca do nome do arquivo enviado - sem risco de path
        /// traversal) e marca Socio.ImgAvatar, usado pelo front pra decidir entre essa imagem e o
        /// avatar padrão da ACECA.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administracao, Fundador, MembroHonra, Socio")]
        public async Task<IActionResult> UploadAvatar(IFormFile arquivo)
        {
            try
            {
                var socioId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

                if (socioId <= 0)
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Sessão inválida" });

                if (arquivo == null || arquivo.Length == 0)
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Nenhuma imagem enviada" });

                if (arquivo.Length > 800 * 1024)
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Imagem maior que 800K" });

                var extensao = Path.GetExtension(arquivo.FileName)?.ToLowerInvariant();
                var extensoesValidas = new[] { ".png", ".jpg", ".jpeg" };

                if (string.IsNullOrEmpty(extensao) || !extensoesValidas.Contains(extensao))
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Formato de imagem inválido - use PNG ou JPG" });

                using (var checkStream = arquivo.OpenReadStream())
                {
                    if (!IsValidImageContent(checkStream, extensao))
                        return BadRequest(new { bResult = false, type = "ERRO", message = "Conteúdo do arquivo não corresponde à extensão informada" });
                }

                var pastaAvatares = Path.Combine(_appEnvironment.WebRootPath, "img", "avatars", "socio");

                Directory.CreateDirectory(pastaAvatares);

                var nomeArquivo = $"imgAvatar{socioId}.png";
                var caminhoCompleto = Path.Combine(pastaAvatares, nomeArquivo);

                using (var stream = new FileStream(caminhoCompleto, FileMode.Create))
                {
                    await arquivo.CopyToAsync(stream);
                }

                var socio = await _db.Socio.FirstOrDefaultAsync(s => s.Id == socioId);

                if (socio != null)
                {
                    socio.ImgAvatar = nomeArquivo;
                    await _db.SaveChangesAsync();
                }

                return Ok(new { bResult = true, type = "OK", message = "Foto atualizada com sucesso", data = new { imgAvatar = nomeArquivo } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERRO :: {Method}", nameof(UploadAvatar));

                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }
        }

        private static bool IsValidImageContent(Stream stream, string extension)
        {
            Span<byte> header = stackalloc byte[12];

            stream.Position = 0;
            int read = stream.Read(header);
            stream.Position = 0;

            if (read < 4)
                return false;

            return extension switch
            {
                ".jpg" or ".jpeg" => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
                ".png" => header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47,
                _ => false
            };
        }

        /// <summary>
        /// Troca a senha do sócio autenticado (tela "Meus Dados" -&gt; Segurança). Sempre no
        /// sócio da claim, nunca de um id vindo do cliente. Confere a senha atual com o mesmo
        /// verificador usado no login (MD5 legado ou BCrypt, dependendo do que já está salvo) -
        /// sem o bypass de "Id == 39" que existe em LoginValidacao, que nunca deve ser copiado
        /// pra código novo. Só grava o hash (Senha); não grava SenhaAberta em texto puro.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administracao, Fundador, MembroHonra, Socio")]
        public async Task<IActionResult> UpdatePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            try
            {
                var socioId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

                if (socioId <= 0)
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Sessão inválida" });

                if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Preencha a senha atual e a nova senha" });

                // Mesmas 3 regras exibidas em tempo real na tela (fn_ValidarRequisitosSenha em
                // pages-auth-account-settings-seguranca.js) - reforçadas aqui pois validação de
                // front-end nunca é garantia.
                if (newPassword.Length < 8
                    || !Regex.IsMatch(newPassword, "[A-Z]")
                    || !Regex.IsMatch(newPassword, @"[0-9\W]"))
                    return BadRequest(new { bResult = false, type = "ERRO", message = "A nova senha não atende aos requisitos mínimos (8 caracteres, 1 maiúscula, 1 número/símbolo)" });

                if (newPassword != confirmPassword)
                    return BadRequest(new { bResult = false, type = "ERRO", message = "A nova senha e a confirmação não coincidem" });

                var seguranca = await _db.SocioSeguranca.FirstOrDefaultAsync(s => s.SocioId == socioId);

                if (seguranca == null)
                    return NotFound(new { bResult = false, type = "ERRO", message = "Sócio não encontrado" });

                using var md5Hash = MD5.Create();

                if (!_helperController.VerifyMd5HashWithMySecurity(md5Hash, currentPassword, seguranca.Senha))
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Senha atual incorreta" });

                seguranca.Senha = _helperController.GenerateHashPassword(newPassword);
                seguranca.SenhaAtualizada = true;

                await _db.SaveChangesAsync();

                return Ok(new { bResult = true, type = "OK", message = "Senha atualizada com sucesso" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERRO :: {Method}", nameof(UpdatePassword));

                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }
        }

        /// <summary>
        /// Últimos 5 acessos do sócio autenticado (tela "Meus Dados" -&gt; Segurança), sempre
        /// escopado pela claim - nunca por um socioId vindo do cliente.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Administracao, Fundador, MembroHonra, Socio")]
        public async Task<IActionResult> GetUltimosAcessos()
        {
            try
            {
                var socioId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

                if (socioId <= 0)
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Sessão inválida" });

                var acessos = await _db.SocioLogAcesso
                    .Where(a => a.SocioId == socioId)
                    .OrderByDescending(a => a.UltimoLogin)
                    .Take(5)
                    .Select(a => new
                    {
                        browser = a.Browser,
                        os = a.OS,
                        device = a.Device,
                        cidade = a.Cidade,
                        estado = a.Estado,
                        ultimoLogin = a.UltimoLogin,
                    })
                    .ToListAsync();

                return Ok(new { bResult = true, type = "OK", data = acessos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERRO :: {Method}", nameof(GetUltimosAcessos));

                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }
        }

        /// <summary>
        /// Combinações Fase/Tipo com mais quantidade na coleção do sócio autenticado
        /// (tela "Meu Perfil" -&gt; card Coleção), sempre escopado pela claim - nunca por um
        /// socioId vindo do cliente. O percentual da barra de progresso é a completude do
        /// catálogo: quantos itens dessa Fase+Tipo o sócio possui em relação ao total de
        /// marcas existentes para essa mesma combinação (não ao total da coleção do sócio).
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Administracao, Fundador, MembroHonra, Socio")]
        public async Task<IActionResult> GetTopFasesColecao()
        {
            try
            {
                var socioId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

                if (socioId <= 0)
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Sessão inválida" });

                var itensColecao = await _db.SocioColecao
                    .Where(c => c.SocioId == socioId && (c.Possui || c.Interesse))
                    .Join(_db.Marca, c => c.MarcaId, m => m.Id, (c, m) => new { c.Possui, c.Interesse, m.MarcaFaseId, m.MarcaSubTipoId })
                    .Join(_db.MarcaSubTipo, x => x.MarcaSubTipoId, st => st.Id, (x, st) => new { x.Possui, x.Interesse, x.MarcaFaseId, st.MarcaTipoId })
                    .ToListAsync();

                var catalogoPorGrupo = (await _db.Marca
                        .Where(m => m.MarcaFaseId != null && m.MarcaSubTipoId != null)
                        .Join(_db.MarcaSubTipo, m => m.MarcaSubTipoId, st => st.Id, (m, st) => new { m.MarcaFaseId, st.MarcaTipoId })
                        .ToListAsync())
                    .GroupBy(x => (x.MarcaFaseId, x.MarcaTipoId))
                    .ToDictionary(g => g.Key, g => g.Count());

                var fases = await _db.MarcaFase.AsNoTracking().ToDictionaryAsync(f => f.Id!.Value, f => f.Descricao);
                var tipos = await _db.MarcaTipo.AsNoTracking().ToDictionaryAsync(t => t.Id!.Value, t => t.Descricao);

                var topFases = itensColecao
                    .GroupBy(x => (x.MarcaFaseId, x.MarcaTipoId))
                    .Select(g => new
                    {
                        grupo = g.Key,
                        nomeFase = g.Key.MarcaFaseId.HasValue && fases.TryGetValue(g.Key.MarcaFaseId.Value, out var nf) ? nf : "-",
                        tipo = tipos.TryGetValue(g.Key.MarcaTipoId, out var nt) ? nt : "-",
                        qtdPossui = g.Count(x => x.Possui),
                        qtdInteresse = g.Count(x => x.Interesse),
                    })
                    .OrderByDescending(x => x.qtdPossui)
                    .Take(10)
                    .Select(x =>
                    {
                        var totalCatalogo = catalogoPorGrupo.TryGetValue(x.grupo, out var tot) ? tot : 0;

                        return new
                        {
                            x.nomeFase,
                            x.tipo,
                            x.qtdPossui,
                            x.qtdInteresse,
                            totalCatalogo,
                            percentPossui = totalCatalogo > 0 ? (int)Math.Round(100.0 * x.qtdPossui / totalCatalogo) : 0,
                        };
                    })
                    .ToList();

                return Ok(new { bResult = true, type = "OK", data = topFases });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERRO :: {Method}", nameof(GetTopFasesColecao));

                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }
        }

        /// <summary>
        /// Exibe a página de "Esqueci minha senha".
        /// </summary>
        [HttpGet]
        public ActionResult ForgotPassword()
        {
            return View("~/Views/Auth/ForgotPassword.cshtml");
        }

        /// <summary>
        /// Exibe a página de reset de senha (link enviado por e-mail).
        /// </summary>
        [HttpGet]
        public ActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
                return RedirectToAction("Index");

            ViewBag.Token = token;
            ViewBag.Email = email;

            return View("~/Views/Auth/ResetPassword.cshtml");
            //return View("~/Views/Auth/RegisterUpdate.cshtml");
        }


        /// <summary>
        /// Exibe a página de atualizar dados (link enviado por e-mail para novo socio).
        /// </summary>
        [HttpGet]
        public ActionResult NewRegistration(string token, string email)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
                return RedirectToAction("Index");

            ViewBag.Token = token;
            ViewBag.Email = email;

            return View("~/Views/Auth/RegisterUpdate.cshtml");
        }

        // ──────────────────────────────────────────────
        // ACCESS / LOGOUT
        // ──────────────────────────────────────────────

        public async Task<IActionResult> Access()
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                    return AccessDenied();

                var result = await LoginPerfilAdm();

                if (result.GetType() == typeof(ForbidResult))
                    return AccessDenied();

                if (result.GetType() == typeof(BadRequestObjectResult))
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = result?.ToString()
                    });

                var jObjResult = JObject.FromObject(((ObjectResult)result).Value);


                ViewBag.PerfilAdm = (bool)jObjResult?["isPerfilAdm"];

                var userId = (int)jObjResult?["userId"];

                if (!await LoginSetCookieAsync(jObjResult?["userEmail"]?.ToString()))
                    return BadRequest(new { msg = "SetCookie inválido." });

                TempData["isPerfil"] = ViewBag.PerfilAdm;

                /*
                TempData["Layout"] = ViewBag.PerfilAdm ? "_HorizontalLayout" : "_WithoutMenuLayout";

                return ViewBag.PerfilAdm
                    ? RedirectToAction("Inicio", "Home")
                    : RedirectToAction("Index", "Marca");
                */

                TempData["Layout"] = "_HorizontalLayout";

                return RedirectToAction("Inicio", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError("ERRO :: {Method} :: {Message}", nameof(Access), ex.Message);
                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }
        }

        public async Task<IActionResult> Logout()
        {
            if (HttpContext?.Request?.Cookies?.Count > 0)
            {
                var siteCookies = HttpContext.Request.Cookies
                    .Where(c => c.Key.Contains(_appConfiguration["Cookie:Key"]?.ToString())
                        || c.Key.Contains($"{_appConfiguration["Cookie:Key"]?.ToString()}.ExpireDateTime")
                        || c.Key.Contains(".AspNetCore.")
                        || c.Key.Contains("Microsoft.Authentication"));

                foreach (var cookie in siteCookies)
                    Response?.Cookies.Delete(cookie.Key);
            }

            await HttpContext?.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return Ok(new { bResult = true, type = "OK", message = "SUCESSO" });
        }

        // ──────────────────────────────────────────────
        // LOGIN
        // ──────────────────────────────────────────────

        #region Login

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginIn dto)
        {
            try
            {
                var email = dto.Email.Trim().ToLowerInvariant();

                var user = await _db.SocioSeguranca
                    .Include(x => x.Socio)
                    .ThenInclude(s => s.SocioPerfil)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Email == email);

                if (user is null)
                    return Ok(new { bResult = false, type = "ERRO", message = "Usuário Inválido" });

                if (user.Socio.SocioPerfilId.Equals((int)EPerfil.Banido))
                    return Ok(new { bResult = false, type = "ERRO", message = "Usuário Banido" });

                if (!user.Socio.Ativo)
                    return Ok(new { bResult = false, type = "ERRO", message = "Acesso inválido. Entre em contato conosco." });

                var financeiroPendente = await _db.SocioFinanceiro
                    .AsNoTracking()
                    .AnyAsync(f => f.SocioId == user.SocioId && f.PagamentoEmDia == 0);

                if (financeiroPendente)
                    return Ok(new { bResult = false, type = "ERRO", message = "Situação Financeira Pendente" });

                if (!LoginValidacao(dto.Senha, user))
                    return Ok(new { bResult = false, type = "ERRO", message = "Credenciais Inválidas" });

                var strToken = LoginTokenJwt(user, user.Socio);

                if (string.IsNullOrEmpty(strToken))
                    return BadRequest(new { bResult = false, type = "ERRO", message = "Token Inválido" });

                if (!await LoginSetClaimsAsync(user, user.Socio))
                    return BadRequest(new { bResult = false, type = "ERRO", message = "SetClaims Inválido" });

                return Ok(new
                {
                    bResult = true,
                    token = strToken,
                    nameIdentifier = user.SocioId.ToString(),
                    nome = user.Socio.Nome,
                    cargo = user.Socio?.SocioPerfil?.Descricao,
                    isPerfil = string.Equals(user.Socio?.SocioPerfil?.Descricao, "Administracao", StringComparison.Ordinal),
                    pswuptd = user.SenhaAtualizada
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("ERRO :: {Method} :: {Message}", nameof(Login), ex.Message);
                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }
        }

        #endregion

        // ──────────────────────────────────────────────
        // LOGIN-LOG
        // ──────────────────────────────────────────────

        #region LoginLog

        [HttpPost]
        [Authorize(Roles = "Administracao, Fundador, MembroHonra, Socio")]
        public async Task<IActionResult> LoginLog([FromForm] string strIp, [FromForm] string srtId)
        {
            try
            {
                if (!int.TryParse(srtId, out var userId) || userId <= 0)
                    return Ok(new { bResult = false, type = "ERRO", message = "User Inválido" });

                if (userId != 39 && !string.IsNullOrEmpty(strIp))
                {
                    var responseGeo = await GetGeoInfoAsync(strIp);
                    var jObjResult  = ((ObjectResult)responseGeo).Value;

                    var jsonGeo   = jObjResult?.GetType()?.GetProperty("data")?.GetValue(jObjResult, null)?.ToString();
                    var jsonAgent = jObjResult?.GetType()?.GetProperty("jsonAgent")?.GetValue(jObjResult, null)?.ToString();

                    if (!string.IsNullOrEmpty(jsonGeo))
                    {
                        JsonNode nodeGeo   = JsonNode.Parse(jsonGeo)!;
                        JsonNode nodeAgent = !string.IsNullOrEmpty(jsonAgent) ? JsonNode.Parse(jsonAgent)! : null;

                        DateTimeOffset.TryParse(
                            nodeGeo["time_zone"]?["current_time"]?.GetValue<string>(),
                            out var loginTime);

                        var newModel = new Models.SocioLogAcesso
                        {
                            SocioId = userId,
                            IP         = nodeGeo["ip"]?.GetValue<string>(),
                            OS         = nodeAgent?["operating_system"]?["name"]?.GetValue<string>(),
                            Browser    = nodeAgent?["name"]?.GetValue<string>(),
                            Device     = nodeAgent?["device"]?["type"]?.GetValue<string>(),
                            Operadora  = nodeGeo["asn"]?["organization"]?.GetValue<string>(),
                            Estado     = nodeGeo["location"]?["state_code"]?.GetValue<string>(),
                            Cidade     = nodeGeo["location"]?["city"]?.GetValue<string>(),
                            Latitude   = nodeGeo["location"]?["latitude"]?.ToString()?.Trim('"'),
                            Longitude  = nodeGeo["location"]?["longitude"]?.ToString()?.Trim('"'),
                            UltimoLogin = loginTime != default ? loginTime.DateTime : DateTime.UtcNow.AddHours(-3),
                        };

                        _db.SocioLogAcesso.Add(newModel);
                        await _db.SaveChangesAsync();
                    }
                }

                return Ok(new { bResult = true });
            }
            catch (Exception ex)
            {
                _logger.LogError("ERRO :: {Method} :: {Message}", nameof(LoginLog), ex.Message);
                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }
        }

        #endregion


        // ──────────────────────────────────────────────
        // ESQUECI A SENHA  (envia e-mail com link)
        // ──────────────────────────────────────────────

        #region ForgotPassword

        /// <summary>
        /// Recebe o e-mail, gera token temporário (24h), salva no banco e envia e-mail com link.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordIn dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto?.Email))
                    return Ok(new { bResult = false, message = "E-mail inválido." });

                var user = await _db.SocioSeguranca
                   .Include(x => x.Socio)
                   .FirstOrDefaultAsync(x => x.Email == dto.Email.Trim().ToLowerInvariant());

                // Por segurança, retorna sucesso mesmo se o e-mail não existir,
                // para não expor quais e-mails estão cadastrados.
                if (user is null)
                    return Ok(new { bResult = true, message = "Se o e-mail existir, você receberá as instruções." });

                if (user.Socio.SocioPerfilId.Equals((int)EPerfil.Banido))
                    return Ok(new { bResult = false, type = "ERRO", message = "Usuário Banido" });

                if (!user.Socio.Ativo)
                    return Ok(new { bResult = false, type = "ERRO", message = "Acesso inválido. Entre em contato conosco." });

                var strToken = _helperController.GenerateSecuretToken();

                user.ResetPasswordToken = strToken;
                user.ResetPasswordTokenExpiry = DateTime.UtcNow.AddHours(24);

                _db.Entry(user).State = EntityState.Modified;
                await _db.SaveChangesAsync();

                // Monta link de reset
                var resetLink = $"{_urlBaseApp}/Auth/ResetPassword?token={Uri.EscapeDataString(strToken)}&email={Uri.EscapeDataString(user.Email)}";

                // Envia e-mail
                var resultSendMail = await _helperController.EnviarEmailAsync(ETipoEmail.EsqueceuSenha, user.Email, user.Socio.Nome, resetLink);                

                if (resultSendMail.GetType() == typeof(NotFoundObjectResult) ||
                    resultSendMail.GetType() == typeof(BadRequestObjectResult))
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = "Falha no envido do E-mail",
                        data = user.Email
                    });

                return Ok(new { bResult = true, message = "E-mail enviado com sucesso." });
            }
            catch (Exception ex)
            {
                _logger.LogError("ERRO :: {Method} :: {Message}", nameof(ForgotPassword), ex.Message);
                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }
        }

        #endregion

        // ──────────────────────────────────────────────
        // REENVIO DO E-MAIL DE CADASTRO (link de boas-vindas expirado)
        // ──────────────────────────────────────────────

        #region ResendCadastroEmail

        /// <summary>
        /// Gera um novo token (24h) e reenvia o e-mail de boas-vindas/cadastro, para o caso
        /// do sócio não ter concluído o cadastro dentro do prazo do link original. Diferente
        /// do ForgotPassword (que é para quem já tem acesso e esqueceu a senha), este só se
        /// aplica a quem ainda não concluiu o primeiro acesso (SenhaAtualizada = false) -
        /// evita virar um jeito alternativo de reset de senha para quem já está com a conta ativa.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ResendCadastroEmail([FromBody] ForgotPasswordIn dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto?.Email))
                    return Ok(new { bResult = false, message = "E-mail inválido." });

                var user = await _db.SocioSeguranca
                   .Include(x => x.Socio)
                   .FirstOrDefaultAsync(x => x.Email == dto.Email.Trim().ToLowerInvariant());

                // Por segurança, retorna sucesso mesmo se o e-mail não existir ou já ter
                // concluído o cadastro, para não expor quais e-mails estão cadastrados nem
                // em que etapa cada sócio está.
                if (user is null || user.SenhaAtualizada)
                    return Ok(new { bResult = true, message = "Se o e-mail existir e o cadastro estiver pendente, você receberá um novo link." });

                if (user.Socio.SocioPerfilId.Equals((int)EPerfil.Banido))
                    return Ok(new { bResult = false, type = "ERRO", message = "Usuário Banido" });

                if (!user.Socio.Ativo)
                    return Ok(new { bResult = false, type = "ERRO", message = "Acesso inválido. Entre em contato conosco." });

                var strToken = _helperController.GenerateSecuretToken();

                user.ResetPasswordToken = strToken;
                user.ResetPasswordTokenExpiry = DateTime.UtcNow.AddHours(24);

                _db.Entry(user).State = EntityState.Modified;
                await _db.SaveChangesAsync();

                var resetLink = $"{_urlBaseApp}/Auth/NewRegistration?token={Uri.EscapeDataString(strToken)}&email={Uri.EscapeDataString(user.Email)}";

                var resultSendMail = await _helperController.EnviarEmailAsync(ETipoEmail.Cadastro, user.Email, user.Socio.Nome, resetLink);

                if (resultSendMail.GetType() == typeof(NotFoundObjectResult) ||
                    resultSendMail.GetType() == typeof(BadRequestObjectResult))
                    return BadRequest(new
                    {
                        bResult = false,
                        type = "ERRO",
                        message = "Falha no envido do E-mail",
                        data = user.Email
                    });

                return Ok(new { bResult = true, message = "E-mail enviado com sucesso." });
            }
            catch (Exception ex)
            {
                _logger.LogError("ERRO :: {Method} :: {Message}", nameof(ResendCadastroEmail), ex.Message);
                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }
        }

        #endregion

        // ──────────────────────────────────────────────
        // RESET DE SENHA  (página com e-mail + senha + confirmação)
        // ──────────────────────────────────────────────

        #region ResetPassword

        /// <summary>
        /// Valida token, valida regras de senha e atualiza no banco.
        /// Regras: mínimo 8 caracteres, pelo menos 1 número.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordIn dto)
        {
            try
            {
                // Validações de entrada
                if (string.IsNullOrWhiteSpace(dto?.Email) ||
                    string.IsNullOrWhiteSpace(dto?.Token) ||
                    string.IsNullOrWhiteSpace(dto?.Senha) ||
                    string.IsNullOrWhiteSpace(dto?.ConfirmSenha))
                    return Ok(new { bResult = false, message = "Todos os campos são obrigatórios." });

                if (dto.Senha != dto.ConfirmSenha)
                    return Ok(new { bResult = false, message = "As senhas não coincidem." });

                if (dto.Senha.Length < 8)
                    return Ok(new { bResult = false, message = "A senha deve ter no mínimo 8 caracteres." });

                if (!dto.Senha.Any(char.IsDigit))
                    return Ok(new { bResult = false, message = "A senha deve conter pelo menos 1 número." });

                var user = await _db.SocioSeguranca
                    .Include(x => x.Socio)
                    .FirstOrDefaultAsync(x => x.Email == dto.Email.Trim().ToLowerInvariant());

                if (user is null)
                    return Ok(new { bResult = false, type = "ERRO", message = "Usuário Inválido" });

                if (user.Socio.SocioPerfilId.Equals((int)EPerfil.Banido))
                    return Ok(new { bResult = false, type = "ERRO", message = "Usuário Banido" });

                if (!user.Socio.Ativo)
                    return Ok(new { bResult = false, type = "ERRO", message = "Acesso inválido. Entre em contato conosco." });

                // Valida token e expiração
                if (user.ResetPasswordToken != dto.Token ||
                    user.ResetPasswordTokenExpiry == null ||
                    user.ResetPasswordTokenExpiry < DateTime.UtcNow)
                    return Ok(new { bResult = false, message = "Link de reset inválido ou expirado. Solicite um novo." });

                // Atualiza senha
                string hash = _helperController.GenerateHashPassword(dto.Senha);

                user.Senha = hash;
                user.SenhaAberta = dto.Senha;
                user.SenhaAtualizada = true;

                // Invalida o token após uso
                user.ResetPasswordToken = null;
                user.ResetPasswordTokenExpiry = null;
                user.UltimoLogin = DateTime.UtcNow.AddHours(-3);

                await _db.SaveChangesAsync();

                return Ok(new { bResult = true, message = "Senha atualizada com sucesso!" });
            }
            catch (Exception ex)
            {
                _logger.LogError("ERRO :: {Method} :: {Message}", nameof(ResetPassword), ex.Message);
                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }
        }

        #endregion

        // ──────────────────────────────────────────────
        // LOGIN PERFIL
        // ──────────────────────────────────────────────

        #region Login Perfil

        public async Task<IActionResult> LoginPerfilAdm()
        {
            try
            {
                if (User.Identity.IsAuthenticated)
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var userName = User.Identity.Name;
                    var email = User.FindFirstValue(ClaimTypes.Email);
                    var role = User.FindFirstValue(ClaimTypes.Role);

                    if (string.IsNullOrEmpty(role))
                        return BadRequest(new { msg = "Role inválido." });

                    _socioPerfil = Enum.TryParse<EPerfil>(role, out _socioPerfil) ? _socioPerfil : EPerfil.Nenhum;

                    var isPerfilAdm = _socioPerfil.Equals(EPerfil.Administracao);

                    return Ok(new { isPerfilAdm, userEmail = email, userId = userId });
                }
                else
                {
                    return Forbid();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("ERRO :: {Method} :: {Message}", nameof(LoginPerfilAdm), ex.Message);
                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }
        }

        #endregion

        // ──────────────────────────────────────────────
        // LOGIN UPDATE DATA
        // ──────────────────────────────────────────────

        #region LOGIN Update Data

        [HttpPost]
        public async Task<IActionResult> LoginUpdate([FromBody] LoginUpdt dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto?.Email) ||
                    string.IsNullOrWhiteSpace(dto?.Senha) ||
                    string.IsNullOrWhiteSpace(dto?.ConfirmSenha))
                    return Ok(new { bResult = false, type = "ERRO", message = "Todos os campos são obrigatórios." });

                if (dto.Senha != dto.ConfirmSenha)
                    return Ok(new { bResult = false, type = "ERRO", message = "As senhas não coincidem." });

                if (dto.Senha.Length < 8 || !dto.Senha.Any(char.IsDigit))
                    return Ok(new { bResult = false, type = "ERRO", message = "A senha deve ter no mínimo 8 caracteres e conter pelo menos 1 número." });

                var user = await _db.SocioSeguranca
                    .Include(x => x.Socio)
                    .ThenInclude(s => s.SocioPerfil)
                    .FirstOrDefaultAsync(x => x.Email == dto.Email.Trim().ToLowerInvariant());

                if (user is null)
                    return Ok(new { bResult = false, type = "ERRO", message = "Usuário Inválido" });

                if (user.Socio.SocioPerfilId.Equals((int)EPerfil.Banido))
                    return Ok(new { bResult = false, type = "ERRO", message = "Usuário Banido" });

                if (!user.Socio.Ativo)
                    return Ok(new { bResult = false, type = "ERRO", message = "Acesso inválido. Entre em contato conosco." });

                // Dois caminhos possíveis para chegar aqui:
                // 1) Sócio novo (ou sem sessão) veio pelo link do e-mail com token - precisa
                //    provar que tem acesso à caixa de entrada, já que ainda não tem senha
                //    nenhuma para se autenticar.
                // 2) Sócio já autenticado, com senha expirada (SenhaAtualizada=false) sendo
                //    forçado a trocá-la - a sessão já prova quem ele é.
                // Sem essa checagem, QUALQUER usuário autenticado podia trocar a senha de
                // QUALQUER outro sócio só enviando o e-mail dele (sequestro de conta).
                if (!string.IsNullOrWhiteSpace(dto.Token))
                {
                    if (user.ResetPasswordToken != dto.Token ||
                        user.ResetPasswordTokenExpiry == null ||
                        user.ResetPasswordTokenExpiry < DateTime.UtcNow)
                        return Ok(new { bResult = false, type = "ERRO", message = "Link inválido ou expirado. Solicite um novo cadastro." });
                }
                else
                {
                    var emailAutenticado = User.FindFirstValue(ClaimTypes.Email);

                    if (!User.Identity.IsAuthenticated || !string.Equals(emailAutenticado, user.Email, StringComparison.OrdinalIgnoreCase))
                        return Ok(new { bResult = false, type = "ERRO", message = "Sessão inválida para atualizar esses dados." });
                }

                var hash = _helperController.GenerateHashPassword(dto.Senha);

                user.Senha = hash;
                user.SenhaAberta = dto.Senha;
                user.SenhaAtualizada = true;
                user.NomeUsuario = dto.Username;
                user.UltimoLogin = DateTime.UtcNow.AddHours(-3);

                // Invalida o token após uso (mesmo padrão do ResetPassword)
                user.ResetPasswordToken = null;
                user.ResetPasswordTokenExpiry = null;

                user.Socio.MostrarSite = dto.ChkTermo;
                user.Socio.Ativo = true;

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    bResult = true,
                    nameIdentifier = user.SocioId.ToString(),
                    nome = user.Socio.Nome,
                    cargo = user.Socio?.SocioPerfil?.Descricao,
                    isPerfil = string.Equals(user.Socio?.SocioPerfil?.Descricao, "Administracao", StringComparison.Ordinal),
                    pswuptd = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("ERRO :: {Method} :: {Message}", nameof(LoginUpdate), ex.Message);
                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }
        }

        #endregion

        
        #region Funções - Login

        private bool LoginValidacao(string openPassword, Models.SocioSeguranca user)
        {
            if (user.Id == 39)
                return true;

            using MD5 md5Hash = MD5.Create();
            return _helperController.VerifyMd5HashWithMySecurity(md5Hash, openPassword, user.Senha);
        }

        private string LoginTokenJwt(Models.SocioSeguranca user, Socio socio)
        {
            var tok = new JwtSecurityToken(
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: _jwtSigningCredentials,
                claims: [
                    new(ClaimTypes.NameIdentifier, socio.Id.ToString()),
                    new(ClaimTypes.Email, user.Email),
                    new(ClaimTypes.Name, socio.Nome),
                    new(ClaimTypes.Role, socio?.SocioPerfil?.Descricao),
                ]);

            var strTok = new JwtSecurityTokenHandler().WriteToken(tok);
            user.Token = strTok;
            user.Senha = null;
            return strTok;
        }

        private async Task<bool> LoginSetClaimsAsync(Models.SocioSeguranca user, Socio socio)
        {
            try
            {
                // Teto absoluto da sessão: 24h após o login (validado em OnValidatePrincipal no Program.cs)
                var absoluteExpiry = DateTime.UtcNow.AddHours(24);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, socio.Id.ToString()),
                    new Claim(ClaimTypes.Email, user?.Email),
                    new Claim(ClaimTypes.Name, socio.Nome),
                    new Claim(ClaimTypes.Role, socio?.SocioPerfil?.Descricao),
                    // Expiração informativa (24h totais)
                    new Claim(ClaimTypes.Expiration, absoluteExpiry.ToString("o")),
                    // Teto absoluto de 24h, independente de atividade
                    new Claim("sess_abs_exp", absoluteExpiry.ToString("o")),
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                // NÃO definir ExpiresUtc aqui: deixamos o handler usar ExpireTimeSpan (2h) para a
                // janela deslizante de ociosidade. O teto de 24h é garantido pelo claim sess_abs_exp.
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    IssuedUtc = DateTimeOffset.UtcNow
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task<bool> LoginSetCookieAsync(string strUserEmail)
        {
            try
            {
                var options = new CookieOptions
                {
                    // Cookie de identificação expira em 24h
                    Expires = DateTime.UtcNow.AddHours(24),
                    HttpOnly = true,
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax,
                    Secure = true
                };

                // Cookie auxiliar com data/hora de expiração (lido pelo front para exibir mensagem)
                HttpContext.Response.Cookies.Append(
                    $"{_appConfiguration["Cookie:Key"]?.ToString()}.ExpireDateTime",
                    options.Expires.ToString(),
                    new CookieOptions
                    {
                        Expires = options.Expires,
                        HttpOnly = false,   // precisa ser lido pelo JS para verificar expiração
                        SameSite = SameSiteMode.Lax,
                        Secure = true
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError("ERRO :: {Method} :: {Message}", nameof(LoginSetCookieAsync), ex.Message);
                return false;
            }

            return true;
        }

        [HttpGet]
        [Authorize(Roles = "Administracao, Fundador, MembroHonra, Socio")]
        public IActionResult GetSessionData()
        {
            var nameIdentifier = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var nome           = User.FindFirstValue(ClaimTypes.Name);
            var cargo          = User.FindFirstValue(ClaimTypes.Role);
            var isPerfil       = string.Equals(cargo, "Administracao", StringComparison.Ordinal);
            return Ok(new { nameIdentifier, nome, cargo, isPerfil });
        }

        [Authorize(Roles = "Administracao, Fundador, MembroHonra, Socio")]
        public async Task<IActionResult> GetCookieExpirationAsync()
        {
            try
            {
                var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                if (authenticateResult.Succeeded)
                {
                    var expiresUtc = authenticateResult.Properties.ExpiresUtc;
                    if (expiresUtc.HasValue)
                        Console.WriteLine($"Authentication cookie expires at: {expiresUtc.Value}");

                    ViewBag.CookieExpiration = expiresUtc?.LocalDateTime.ToString() ?? "N/A";
                }

                string expirationDateString = HttpContext.Request.Cookies[
                    $"{_appConfiguration["Cookie:Key"]?.ToString()}.ExpireDateTime"];

                if (expirationDateString != null &&
                    DateTimeOffset.TryParse(expirationDateString, out DateTimeOffset expirationDate))
                    ViewBag.CookieExpiration = expirationDate.LocalDateTime;
                else
                    ViewBag.CookieExpiration = "Expiration date not found or invalid.";

                return Ok(new { cookieExpiration = ViewBag.CookieExpiration });
            }
            catch (Exception ex)
            {
                _logger.LogError("ERRO :: {Method} :: {Message}", nameof(GetCookieExpirationAsync), ex.Message);
                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }
        }

        #endregion

        // ──────────────────────────────────────────────
        // ENVIO DE E-MAIL (Forgot Password)
        // ──────────────────────────────────────────────

        #region Email

        /// <summary>
        /// Envia o e-mail de reset de senha via SMTP configurado no appsettings.json.
        /// Configure as chaves: Email:Host, Email:Port, Email:EnableSsl,
        ///                      Email:From, Email:User, Email:Password, Email:DisplayName
        /// </summary>
        public async Task<IActionResult> EnviarEmailResetSenhaAsync(string toEmail, string socioNome, string resetLink)
        {
            // 3. Send asynchronously
            try
            {
                await _helperController.EnviarEmailAsync(ETipoEmail.EsqueceuSenha, toEmail, socioNome, resetLink);
            }
            catch (SmtpException ex)
            {
                _logger.LogError("ERRO :: {Method} :: {Message}", nameof(EnviarEmailResetSenhaAsync), ex.Message);
                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }

            return Ok(true);
        }

        #endregion

        // ──────────────────────────────────────────────
        // GEO
        // ──────────────────────────────────────────────

        #region Geo
        public async Task<IActionResult> GetGeoInfoAsync(string varIp)
        {
            if (string.IsNullOrWhiteSpace(varIp))
                return BadRequest(new { bResult = false, type = "ERRO", message = "IP Inválido" });

            try
            {
                string strGeoOrigem = "Ipgeolocation";

                string url = string.Empty;
                string urlAgent = string.Empty;
                string jsonAgent = string.Empty;

                var geoUrl = _appConfiguration[$"Geo:{strGeoOrigem}:Url"]!;
                var geoKey = _appConfiguration[$"Geo:{strGeoOrigem}:Key"]!;

                switch (strGeoOrigem)
                {
                    case "Ipstack": // https://docs.apilayer.com/ipstack/docs/quickstart-guide?utm_source=IPstackHomePage&utm_medium=Referral#step-3-make-api-requests
                        url = $"{geoUrl}/{varIp}?access_key={geoKey}";
                        break;
                    case "Ipgeolocation": // https://ipgeolocation.io/documentation/ip-location-api.html
                        url = $"{geoUrl}/v3/ipgeo?apiKey={geoKey}&ip={varIp}"; // curl - X GET 'https://api.ipgeolocation.io/v3/ipgeo?apiKey=API_KEY&ip=91.128.103.196'
                        urlAgent = $"{geoUrl}/v3/user-agent?apiKey={geoKey}";
                        break;
                    case "Ip2location": // https://api.ip2location.io/?key={YOUR_API_KEY}&ip=8.8.8.8&format=json	
                        url = $"{geoUrl}/?key={geoKey}&ip={varIp}&format=json";
                        break;
                    default:
                        url = $"{geoUrl}/{varIp}?access_key={geoKey}";
                        break;
                }

                using var client = _httpClientFactory.CreateClient();
                var result = await client.GetAsync(url);

                if (result.GetType() == typeof(NotFoundObjectResult) ||
                    result.GetType() == typeof(BadRequestObjectResult))
                    return BadRequest(new { bResult = false, type = "ERRO", message = "geoUrl Inválido" });

                var code = result?.EnsureSuccessStatusCode();
                var json = await result.Content.ReadAsStringAsync();

                var lst = JsonConvert.DeserializeObject<JObject>(json).Children().ToList();

                //Agent Dados origem Device
                if (!string.IsNullOrEmpty(urlAgent))
                {
                    var clientAgent = _httpClientFactory.CreateClient();
                    var request = new HttpRequestMessage(HttpMethod.Get, urlAgent);
                    //request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:150.0) Gecko/20100101 Firefox/150.0");
                    request.Headers.Add("User-Agent", Request.Headers["User-Agent"].ToString());

                    var resultAgent = await clientAgent.SendAsync(request);

                    if (resultAgent.GetType() == typeof(NotFoundObjectResult) ||
                        resultAgent.GetType() == typeof(BadRequestObjectResult))
                        return BadRequest(new { bResult = false, type = "ERRO", message = "geoUrl Inválido" });

                    var codeAgent = resultAgent?.EnsureSuccessStatusCode();
                    jsonAgent = await resultAgent.Content.ReadAsStringAsync();

                    var node = JsonNode.Parse(json);

                    // Add a simple value node
                    node["user-agent"] = jsonAgent;

                    json = node.ToJsonString();

                    var lstAgent = JsonConvert.DeserializeObject<JObject>(jsonAgent).Children().ToList();

                    lst.AddRange(lstAgent);
               }

                return Ok(new { bResult = true, type = "SUCESSO", message = "SUCESSO ::: ", data = json, jsonAgent = jsonAgent });
            }
            catch (Exception ex)
            {
                _logger.LogError("ERRO :: {Method} :: {Message}", nameof(GetGeoInfoAsync), ex.Message);
                return BadRequest(new { bResult = false, type = "ERRO", message = ex.Message });
            }
        }

        public async Task<string> GetGeoIPAsync()
        {
            string strIP = string.Empty;

            try
            {
                var geoUrlBase = _appConfiguration["Geo:Url"]!;

                if (string.IsNullOrEmpty(geoUrlBase))
                    return strIP;

                var result = await _httpClientFactory.CreateClient().GetStringAsync(geoUrlBase);

                if (string.IsNullOrEmpty(result))
                    return strIP;

                var node = JsonNode.Parse(result);

                strIP = (string)node["ip"];

                return strIP;
            }
            catch (Exception ex)
            {
                _logger.LogError("ERRO :: {Method} :: {Message}", nameof(GetGeoIPAsync), ex.Message);
                return strIP;
            }

        }

        #endregion

        #region Proteção de Imagem

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ReportImageAccess(
            [FromForm] string codigoAceca,
            [FromForm] string imagemSrc,
            [FromForm] string urlAcesso,
            [FromForm] string acao,
            [FromForm] string timestamp)
        {
            var socioId   = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "?";
            var socioNome = User.FindFirstValue(ClaimTypes.Name)           ?? "Desconhecido";

            await _helperController.EnviarAlertaImagemAsync(socioId, socioNome, codigoAceca, imagemSrc, urlAcesso, acao, timestamp);

            return Ok();
        }

        #endregion
    }
}