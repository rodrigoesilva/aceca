/**
 * Account Settings - Financeiro
 */

'use strict';

//#region Declare

let var_Nome = 'Auth',
    var_Controller = '/Auth';
var msg = 'O preenchimento &eacute; obrigat&oacute;rio';

//#endregion

//#region CARREGAMENTO INICIAL

document.addEventListener('DOMContentLoaded', function () {
    (function () {
        console.log(`AUTH USER FINANCEIRO - Todos os recursos terminaram o carregamento!`);

        fn_CarregarInfoFinanceira();
        fn_WireCancelSubscription();
        fn_WireFormaPagamento();
    })();
});

//#endregion

//#region FORMA DE PAGAMENTO (Cartão x Pix)

// Alterna qual bloco aparece em div_formapagamento (Meus Cartões x Pix) conforme o
// radio "collapsible-payment" selecionado (collapsible-payment-cc / -cash).
function fn_WireFormaPagamento() {
    const radios = document.querySelectorAll('input[name="collapsible-payment"]');

    if (!radios.length) return;

    radios.forEach(function (radio) {
        radio.addEventListener('change', fn_AtualizarFormaPagamento);
    });

    fn_AtualizarFormaPagamento();
}

function fn_AtualizarFormaPagamento() {
    const ehCartao = document.getElementById('collapsible-payment-cc')?.checked ?? true;

    document.getElementById('div_meuscartoes')?.classList.toggle('d-none', !ehCartao);
    document.getElementById('div_pix')?.classList.toggle('d-none', ehCartao);
}

//#endregion

//#region INFO FINANCEIRA

function fn_CarregarInfoFinanceira() {
    $.ajax({
        url: `${var_Controller}/GetInfoFinanceira`,
        type: 'GET',
        success: function (response) {
            if (!response?.bResult || !response?.data) return;

            const f = response.data;
            const emDia = f.pagamentoEmDia !== 0;

            $('#fin_TipoPagamento').text(f.tipoPagamento || 'Plano não definido');
            $('#fin_AtivoDesde').text('Ativo desde ' + fn_FormatarDataFinanceiro(f.dataUltimoPagamento));
            $('#fin_UltimoPagamentoTexto').text('Último pagamento em ' + fn_FormatarDataFinanceiro(f.dataUltimoPagamento));

            $('#fin_StatusBadge')
                .text(emDia ? 'Em Dia' : 'Pendente')
                .removeClass('bg-label-secondary bg-label-success bg-label-danger')
                .addClass(emDia ? 'bg-label-success' : 'bg-label-danger');

            const ciclo = fn_CicloPagamento(f.tipoPagamentoId, f.dataUltimoPagamento);

            // TipoPagamento sem ciclo de renovação (ex.: Fundador/Isento) - sem data de
            // vencimento pra calcular, esconde o alerta e a barra de dias.
            if (!ciclo) {
                $('#fin_Cadencia').text('Sem renovação automática');
                $('#fin_Vencimento').text('Sem vencimento definido');
                $('#fin_AlertWrapper').addClass('d-none');
                $('#fin_StatisticsWrapper').addClass('d-none');
                return;
            }

            $('#fin_AlertWrapper, #fin_StatisticsWrapper').removeClass('d-none');

            $('#fin_Cadencia').text(`Renovação ${ciclo.cadenciaTexto}`);
            $('#fin_Vencimento').text('Vencimento em ' + fn_FormatarDataFinanceiro(ciclo.dataVencimento));

            const diasDecorridos = Math.max(0, Math.min(ciclo.totalDias, ciclo.diasDecorridos));
            const percentual = Math.round((diasDecorridos / ciclo.totalDias) * 100);

            $('#fin_DiasLabel').text(`${diasDecorridos} de ${ciclo.totalDias} dias`);

            $('#fin_ProgressBar')
                .css('width', percentual + '%')
                .attr('aria-valuenow', percentual)
                .text(percentual + '%')
                .removeClass('bg-primary bg-warning bg-danger');

            $('#fin_DiasRestantesTexto').text(
                ciclo.diasRestantes >= 0
                    ? `Faltam ${ciclo.diasRestantes} dia(s) para o vencimento`
                    : `Vencido há ${Math.abs(ciclo.diasRestantes)} dia(s)`
            );

            // Mesma janela de 7 dias usada pelos avisos automáticos por e-mail
            // (SocioFinanceiroCheckService).
            const precisaAlerta = !emDia || (ciclo.diasRestantes >= 0 && ciclo.diasRestantes <= 7);

            let corEstado, icone, titulo, texto;

            if (!emDia) {
                corEstado = 'danger';
                icone = 'ri-close-circle-line';
                titulo = 'Sua assinatura venceu!';
                texto = 'Faça a sua renovação para continuar em dia com a associação.';
            } else if (precisaAlerta) {
                corEstado = 'warning';
                icone = 'ri-alert-line';
                titulo = 'Precisamos da sua atenção!';
                texto = `Seu pagamento vence em ${ciclo.diasRestantes} dia(s).`;
            } else {
                corEstado = 'primary';
                icone = 'ri-checkbox-circle-line';
                titulo = 'Você está com tudo em dia!';
                texto = 'Não há nenhuma pendência no momento.';
            }

            $('#fin_ProgressBar').addClass('bg-' + corEstado);

            $('#fin_AlertWrapper')
                .removeClass('alert-primary alert-warning alert-danger')
                .addClass('alert-' + corEstado);

            $('#fin_AlertWrapper .alert-icon i')
                .removeClass('ri-close-circle-line ri-alert-line ri-checkbox-circle-line')
                .addClass(icone);

            $('#fin_AlertTitulo').text(titulo);
            $('#fin_AlertTexto').text(texto);
        },
        error: function (xhr, status, error) {
            console.error("fn_CarregarInfoFinanceira error: " + error);
        }
    });
}

// TipoPagamentoId 2/3/4 = Anual/Semestral/Mensal - mesma regra de
// Services/SocioFinanceiroCheckService.cs (vencimento = último pagamento + 1 ano/6
// meses/30 dias). Outros valores (ex.: Fundador/Isento) não têm ciclo de renovação.
function fn_CicloPagamento(tipoPagamentoId, dataUltimoPagamentoStr) {
    if (!dataUltimoPagamentoStr) return null;

    const dataUltimoPagamento = new Date(dataUltimoPagamentoStr);
    const dataVencimento = new Date(dataUltimoPagamento);
    let cadenciaTexto;

    switch (tipoPagamentoId) {
        case 2:
            dataVencimento.setFullYear(dataVencimento.getFullYear() + 1);
            cadenciaTexto = 'anual';
            break;
        case 3:
            dataVencimento.setMonth(dataVencimento.getMonth() + 6);
            cadenciaTexto = 'semestral';
            break;
        case 4:
            dataVencimento.setDate(dataVencimento.getDate() + 30);
            cadenciaTexto = 'mensal';
            break;
        default:
            return null;
    }

    const umDiaMs = 24 * 60 * 60 * 1000;
    const hoje = new Date();

    return {
        dataVencimento: dataVencimento,
        cadenciaTexto: cadenciaTexto,
        totalDias: Math.round((dataVencimento - dataUltimoPagamento) / umDiaMs),
        diasDecorridos: Math.round((hoje - dataUltimoPagamento) / umDiaMs),
        diasRestantes: Math.round((dataVencimento - hoje) / umDiaMs)
    };
}

function fn_FormatarDataFinanceiro(data) {
    if (!data) return '-';

    return moment(data).locale('pt-br').format('DD/MM/YYYY');
}

//#endregion

//#region CANCELAMENTO (fluxo de exemplo do template - ver depois)

function fn_WireCancelSubscription() {
    const cancelSubscription = document.querySelector('.cancel-subscription');

    if (!cancelSubscription) return;

    cancelSubscription.onclick = function () {
        Swal.fire({
            text: 'Tem certeza que deseja cancelar?',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Sim',
            customClass: {
                confirmButton: 'btn btn-primary me-2 waves-effect waves-light',
                cancelButton: 'btn btn-outline-secondary waves-effect'
            },
            buttonsStyling: false
        });
    };
}

//#endregion
