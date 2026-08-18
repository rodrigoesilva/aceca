using Aceca.Adm.Data;
using Aceca.Adm.Models;
using Microsoft.AspNetCore.Http;
using MySqlConnector;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;

namespace Aceca.Adm.Services
{
    /// <summary>
    /// Registro central de erros da aplicação: grava em log_erros (tabela) e, para os casos
    /// de exceção de verdade (não para BadRequest de validação de formulário), avisa o time
    /// de TI por e-mail — mesmo endereço já usado para os alertas de acesso indevido a imagem
    /// (ti@aceca.com.br, ver HelperExtensionsController.EnviarAlertaImagemAsync).
    /// </summary>
    public class ErrorLogService
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<ErrorLogService> _logger;

        private const string EmailMonitoramento = "ti@aceca.com.br";

        public ErrorLogService(AppDbContext db, IConfiguration config, ILogger<ErrorLogService> logger)
        {
            _db = db;
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// Exceção de verdade — catch local (ex.: SocioFinanceiroCheckService, rotina de
        /// índices no Program.cs) ou exceção não tratada capturada pelo middleware global.
        /// Grava em log_erros e envia e-mail de alerta.
        /// </summary>
        public Task RegistrarExcecaoAsync(HttpContext? httpContext, Exception ex) =>
            RegistrarAsync("Exception", httpContext, ex, ex.Message, enviarEmail: true);

        /// <summary>
        /// BadRequest devolvido por uma action (ver Filters/BadRequestLogFilter) — fica só
        /// no log para auditoria; não envia e-mail, pois a maioria é validação de formulário
        /// (senha incorreta, campo obrigatório etc.), não um erro de verdade.
        /// </summary>
        public Task RegistrarBadRequestAsync(HttpContext httpContext, object? valorRetornado) =>
            RegistrarAsync("BadRequest", httpContext, null, DescreverValor(valorRetornado), enviarEmail: false);

        private async Task RegistrarAsync(
            string tipo, HttpContext? httpContext, Exception? exception, string mensagemOriginal, bool enviarEmail)
        {
            // Nunca deixa uma falha aqui (DB fora do ar, SMTP indisponível etc.) derrubar o
            // fluxo real da aplicação — o log de erro é best-effort por definição.
            try
            {
                var url = DescreverUrl(httpContext);
                var usuario = DescreverUsuario(httpContext);
                var humanizada = Humanizar(tipo, exception);

                var log = new LogErro
                {
                    Tipo = tipo,
                    Url = url,
                    MetodoHttp = httpContext?.Request?.Method,
                    Usuario = usuario,
                    MensagemHumanizada = humanizada,
                    MensagemOriginal = mensagemOriginal,
                    StackTrace = exception?.StackTrace,
                    DataCriacao = DateTime.Now
                };

                _db.LogErro.Add(log);
                await _db.SaveChangesAsync();

                if (enviarEmail)
                {
                    var enviado = await EnviarEmailAlertaAsync(url, usuario, humanizada, mensagemOriginal);
                    if (enviado)
                    {
                        log.EmailEnviado = true;
                        await _db.SaveChangesAsync();
                    }
                }
            }
            catch (Exception exLog)
            {
                _logger.LogError(exLog,
                    "ERRO :: {Service} :: falha ao registrar/notificar erro original ({MensagemOriginal})",
                    nameof(ErrorLogService), mensagemOriginal);
            }
        }

        private static string DescreverUrl(HttpContext? httpContext)
        {
            if (httpContext?.Request is null)
                return "N/D (fora do ciclo de requisição — ex.: inicialização da aplicação)";

            var request = httpContext.Request;
            return $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";
        }

        private static string DescreverUsuario(HttpContext? httpContext)
        {
            var user = httpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return "Visitante não autenticado";

            var nome = user.FindFirstValue(ClaimTypes.Name) ?? "?";
            var email = user.FindFirstValue(ClaimTypes.Email) ?? "sem e-mail";
            var socioId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "?";

            return $"{nome} ({email}) — SocioId {socioId}";
        }

        private static string DescreverValor(object? valor)
        {
            if (valor is null)
                return "BadRequest sem detalhes";

            var propriedade = valor.GetType().GetProperty("message") ?? valor.GetType().GetProperty("msg");
            var mensagem = propriedade?.GetValue(valor)?.ToString();

            return !string.IsNullOrWhiteSpace(mensagem) ? mensagem : valor.ToString() ?? "BadRequest sem detalhes";
        }

        /// <summary>
        /// Tradução simples do tipo de exceção para uma frase que qualquer pessoa (não só
        /// dev) entende — a mensagem técnica original continua disponível ao lado, no e-mail
        /// e na tabela.
        /// </summary>
        private static string Humanizar(string tipo, Exception? ex)
        {
            if (tipo == "BadRequest" || ex is null)
                return "A aplicação recusou uma requisição por dados inválidos ou incompletos.";

            return ex switch
            {
                MySqlException => "Ocorreu um erro ao acessar o banco de dados.",
                SmtpException => "Falha ao enviar um e-mail pelo sistema.",
                TimeoutException => "Uma operação demorou demais e foi cancelada (timeout).",
                UnauthorizedAccessException => "Houve uma tentativa de acesso não autorizado.",
                NullReferenceException or ArgumentNullException =>
                    "A aplicação tentou usar uma informação que não estava disponível.",
                IOException => "Falha ao ler ou gravar um arquivo.",
                _ => "Ocorreu um erro inesperado na aplicação."
            };
        }

        private async Task<bool> EnviarEmailAlertaAsync(string url, string usuario, string humanizada, string mensagemOriginal)
        {
            var smtpHost = _config["Email:Host"] ?? "smtp.hostinger.com";
            var smtpPort = int.Parse(_config["Email:Port"] ?? "587");
            var smtpSsl = bool.Parse(_config["Email:EnableSsl"] ?? "true");
            var smtpFrom = _config["Email:From"] ?? "noreply@aceca.com.br";
            var smtpUser = _config["Email:User"] ?? smtpFrom;
            var smtpPassword = _config["Email:Password"] ?? "";
            var displayName = _config["Email:DisplayName"] ?? "ACECA - Monitoramento";

            var agoraBrasil = DateTime.UtcNow.AddHours(-3).ToString("dd/MM/yyyy HH:mm:ss") + " (Brasília)";

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
                    <h2 style=""color:#cc0000;margin-top:24px;"">🛑 Erro na Aplicação ACECA</h2>
                    <table style=""width:100%;border-collapse:collapse;margin-top:16px;"">
                      <tr style=""background:#f9f0ff;"">
                        <td style=""padding:10px 14px;font-weight:bold;width:32%;border:1px solid #e0d0f0;"">Quando</td>
                        <td style=""padding:10px 14px;border:1px solid #e0d0f0;"">{agoraBrasil}</td>
                      </tr>
                      <tr>
                        <td style=""padding:10px 14px;font-weight:bold;border:1px solid #e0d0f0;"">URL</td>
                        <td style=""padding:10px 14px;border:1px solid #e0d0f0;word-break:break-all;"">{WebUtility.HtmlEncode(url)}</td>
                      </tr>
                      <tr style=""background:#f9f0ff;"">
                        <td style=""padding:10px 14px;font-weight:bold;border:1px solid #e0d0f0;"">Usuário logado</td>
                        <td style=""padding:10px 14px;border:1px solid #e0d0f0;"">{WebUtility.HtmlEncode(usuario)}</td>
                      </tr>
                      <tr>
                        <td style=""padding:10px 14px;font-weight:bold;border:1px solid #e0d0f0;"">Erro</td>
                        <td style=""padding:10px 14px;border:1px solid #e0d0f0;"">{WebUtility.HtmlEncode(humanizada)}</td>
                      </tr>
                      <tr style=""background:#f9f0ff;"">
                        <td style=""padding:10px 14px;font-weight:bold;border:1px solid #e0d0f0;"">Mensagem original</td>
                        <td style=""padding:10px 14px;border:1px solid #e0d0f0;word-break:break-all;
                                    font-family:monospace;font-size:12px;"">{WebUtility.HtmlEncode(mensagemOriginal)}</td>
                      </tr>
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
                From = new MailAddress(smtpFrom, displayName),
                Subject = "🛑 Erro na aplicação ACECA",
                IsBodyHtml = true,
                Body = body
            };
            mailMessage.To.Add(EmailMonitoramento);

            using var smtp = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPassword),
                EnableSsl = smtpSsl
            };

            try
            {
                await smtp.SendMailAsync(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERRO :: {Service} :: falha ao enviar e-mail de alerta para {Email}",
                    nameof(ErrorLogService), EmailMonitoramento);
                return false;
            }
        }
    }
}
