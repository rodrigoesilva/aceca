using Aceca.Adm.Data;
using Aceca.Adm.Helper;
using Microsoft.EntityFrameworkCore;

namespace Aceca.Adm.Services
{
    /// <summary>
    /// Automação que roda dentro do próprio processo da aplicação, diariamente à 00:01
    /// (horário de Brasília, UTC-3), e verifica a situação financeira dos sócios
    /// (tabela socio_financeiro):
    ///
    ///  - TipoPagamentoId 2 (Anual)     -> vencimento = DataUltimoPagamento + 1 ano
    ///  - TipoPagamentoId 3 (Semestral) -> vencimento = DataUltimoPagamento + 6 meses
    ///  - TipoPagamentoId 4 (Mensal)    -> vencimento = DataUltimoPagamento + 30 dias
    ///
    /// Se vencimento + 7 dias de tolerância já passou, PagamentoEmDia é zerado (false).
    ///
    /// É enviado um e-mail de aviso em exatamente dois momentos por ciclo de cobrança:
    /// 7 dias antes do vencimento e 2 dias antes do vencimento — nunca mais que isso.
    /// O controle de "já enviado" fica em DataAvisoVencimento7Dias/DataAvisoVencimento2Dias:
    /// guardam a data de vencimento à qual o aviso corresponde, então uma renovação
    /// (DataUltimoPagamento mudou -> vencimento recalculado é outro) libera novo aviso
    /// automaticamente, sem precisar zerar nada manualmente.
    ///
    /// Todas as comparações de data usam o horário de Brasília (UTC-3), pois é assim que
    /// as datas de pagamento são lançadas pelos administradores (mesma convenção já usada
    /// em outros pontos do sistema, ex.: UltimoLogin = DateTime.UtcNow.AddHours(-3)).
    /// </summary>
    public class SocioFinanceiroCheckService : BackgroundService
    {
        private static readonly TimeSpan _offsetBrasil = TimeSpan.FromHours(-3);
        private static readonly TimeSpan _horarioExecucao = new(0, 1, 0); // 00:01

        private readonly ILogger<SocioFinanceiroCheckService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public SocioFinanceiroCheckService(
            ILogger<SocioFinanceiroCheckService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await GarantirColunasControleAsync(stoppingToken);

            // Cobre o caso de o processo ter ficado fora do ar durante a última 00:01
            // (ex.: reinício de App Pool). As flags por data de vencimento tornam isso
            // seguro para rodar de novo sem duplicar avisos já enviados.
            await ExecutarVerificacaoFinanceiraAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                var atraso = TempoAteProximaExecucao();

                try
                {
                    await Task.Delay(atraso, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                await ExecutarVerificacaoFinanceiraAsync(stoppingToken);
            }
        }

        private static DateTime AgoraBrasil() => DateTime.UtcNow.Add(_offsetBrasil);

        private static TimeSpan TempoAteProximaExecucao()
        {
            var agoraBrasil = AgoraBrasil();
            var proximaBrasil = agoraBrasil.Date.Add(_horarioExecucao);

            if (agoraBrasil >= proximaBrasil)
                proximaBrasil = proximaBrasil.AddDays(1);

            // Converte o horário-alvo (em "hora de Brasília") de volta para o instante UTC real
            var proximaUtc = proximaBrasil.Subtract(_offsetBrasil);

            return proximaUtc - DateTime.UtcNow;
        }

        private async Task GarantirColunasControleAsync(CancellationToken ct)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // "ADD COLUMN IF NOT EXISTS" só é aceito a partir do MySQL 8.0.29 e o
                // servidor em uso rejeita com erro de sintaxe — checa via INFORMATION_SCHEMA
                // antes do ALTER (DbSchemaHelper) para continuar idempotente em qualquer versão.
                if (!await DbSchemaHelper.ColunaExisteAsync(db.Database, "socio_financeiro", "data_aviso_vencimento_7dias"))
                    await db.Database.ExecuteSqlRawAsync(
                        "ALTER TABLE socio_financeiro ADD COLUMN data_aviso_vencimento_7dias DATETIME NULL", ct);

                if (!await DbSchemaHelper.ColunaExisteAsync(db.Database, "socio_financeiro", "data_aviso_vencimento_2dias"))
                    await db.Database.ExecuteSqlRawAsync(
                        "ALTER TABLE socio_financeiro ADD COLUMN data_aviso_vencimento_2dias DATETIME NULL", ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERRO :: {Service} :: {Method}", nameof(SocioFinanceiroCheckService), nameof(GarantirColunasControleAsync));

                using var scope = _scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<ErrorLogService>().RegistrarExcecaoAsync(null, ex);
            }
        }

        private async Task ExecutarVerificacaoFinanceiraAsync(CancellationToken ct)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var helper = scope.ServiceProvider.GetRequiredService<HelperExtensionsController>();

                var agoraBrasil = AgoraBrasil();

                var registros = await db.SocioFinanceiro
                    .Include(f => f.Socio)
                    .Where(f => f.DataUltimoPagamento != null
                             && (f.TipoPagamentoId == 2 || f.TipoPagamentoId == 3 || f.TipoPagamentoId == 4))
                    .ToListAsync(ct);

                var aviso7Enviados = 0;
                var aviso2Enviados = 0;
                var pagamentosVencidos = 0;

                foreach (var registro in registros)
                {
                    var dataUltimoPagamento = registro.DataUltimoPagamento!.Value;

                    DateTime? dataVencimento = registro.TipoPagamentoId switch
                    {
                        2 => dataUltimoPagamento.AddYears(1),
                        3 => dataUltimoPagamento.AddMonths(6),
                        4 => dataUltimoPagamento.AddDays(30),
                        _ => null
                    };

                    if (dataVencimento is null)
                        continue;

                    var prazoComTolerancia = dataVencimento.Value.AddDays(7);
                    var dataAviso7 = dataVencimento.Value.AddDays(-7);
                    var dataAviso2 = dataVencimento.Value.AddDays(-2);

                    // Vencimento + 7 dias de tolerância já passou -> marca como não em dia
                    if (agoraBrasil >= prazoComTolerancia && registro.PagamentoEmDia != 0)
                    {
                        registro.PagamentoEmDia = 0;
                        pagamentosVencidos++;
                    }

                    // Aviso de 7 dias: dispara uma única vez por ciclo (por data de vencimento)
                    if (registro.SocioId.HasValue
                        && agoraBrasil >= dataAviso7 && agoraBrasil < dataVencimento.Value
                        && registro.DataAvisoVencimento7Dias?.Date != dataAviso7.Date)
                    {
                        var enviado = await EnviarAvisoVencimentoAsync(db, helper, registro, ct);
                        if (enviado)
                        {
                            registro.DataAvisoVencimento7Dias = dataAviso7;
                            aviso7Enviados++;
                        }
                    }

                    // Aviso de 2 dias: dispara uma única vez por ciclo (por data de vencimento)
                    if (registro.SocioId.HasValue
                        && agoraBrasil >= dataAviso2 && agoraBrasil < dataVencimento.Value
                        && registro.DataAvisoVencimento2Dias?.Date != dataAviso2.Date)
                    {
                        var enviado = await EnviarAvisoVencimentoAsync(db, helper, registro, ct);
                        if (enviado)
                        {
                            registro.DataAvisoVencimento2Dias = dataAviso2;
                            aviso2Enviados++;
                        }
                    }
                }

                await db.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "SocioFinanceiroCheckService :: verificação concluída ({AgoraBrasil:yyyy-MM-dd HH:mm} horário Brasília) - {Total} registros, {Aviso7} avisos de 7 dias, {Aviso2} avisos de 2 dias, {Vencidos} marcados como não em dia",
                    agoraBrasil, registros.Count, aviso7Enviados, aviso2Enviados, pagamentosVencidos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERRO :: {Service} :: {Method}", nameof(SocioFinanceiroCheckService), nameof(ExecutarVerificacaoFinanceiraAsync));

                using var scope = _scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<ErrorLogService>().RegistrarExcecaoAsync(null, ex);
            }
        }

        private async Task<bool> EnviarAvisoVencimentoAsync(
            AppDbContext db, HelperExtensionsController helper, Models.SocioFinanceiro registro, CancellationToken ct)
        {
            var contato = await db.SocioContato
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.SocioId == registro.SocioId && !string.IsNullOrWhiteSpace(c.Email), ct);

            if (contato?.Email is null)
            {
                _logger.LogWarning(
                    "SocioFinanceiroCheckService :: Sócio {SocioId} sem e-mail em socio_contato — aviso de vencimento não enviado",
                    registro.SocioId);
                return false;
            }

            var nomeSocio = registro.Socio?.Nome ?? "Sócio ACECA";

            var resultado = await helper.EnviarEmailAsync(
                HelperExtensionsController.ETipoEmail.FinanceiroPendente, contato.Email, nomeSocio, string.Empty);

            if (resultado is Microsoft.AspNetCore.Mvc.BadRequestObjectResult)
            {
                _logger.LogError(
                    "SocioFinanceiroCheckService :: Falha ao enviar aviso de vencimento para SocioId {SocioId} ({Email})",
                    registro.SocioId, contato.Email);
                return false;
            }

            return true;
        }
    }
}
