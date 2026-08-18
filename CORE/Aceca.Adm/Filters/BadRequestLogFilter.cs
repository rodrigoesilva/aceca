using Aceca.Adm.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Aceca.Adm.Filters
{
    /// <summary>
    /// Filtro global (registrado em Program.cs via options.Filters) que grava toda resposta
    /// BadRequest em log_erros, para auditoria — sem enviar e-mail, já que a maioria dos
    /// BadRequest do sistema é validação de formulário (senha incorreta, campo obrigatório
    /// etc.), não erro de verdade. Exceções de verdade (catch local ou não tratada) usam
    /// ErrorLogService.RegistrarExcecaoAsync diretamente e essas sim geram e-mail.
    /// </summary>
    public class BadRequestLogFilter : IAsyncResultFilter
    {
        private readonly ErrorLogService _errorLog;

        public BadRequestLogFilter(ErrorLogService errorLog)
        {
            _errorLog = errorLog;
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            object? valor = context.Result switch
            {
                BadRequestObjectResult bad => bad.Value,
                BadRequestResult => null,
                _ => null
            };

            var eBadRequest = context.Result is BadRequestObjectResult or BadRequestResult;

            if (eBadRequest)
                await _errorLog.RegistrarBadRequestAsync(context.HttpContext, valor);

            await next();
        }
    }
}
