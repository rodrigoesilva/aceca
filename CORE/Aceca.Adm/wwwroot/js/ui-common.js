/**
 * ui-common.js
 *
 * Inicializações repetidas em quase toda página administrativa/negociação:
 * SweetAlert2 com estilo padrão de botões, cores de tema (dark/light) e o
 * setup do busy-load, além do datepicker padrão (.dt-calendar).
 *
 * Antes duplicado no topo de cada admin-*.js / negociacao-*.js / pages-*.js.
 * Agora carregado uma única vez pelo layout comum (_CommonMasterLayout),
 * depois de main.js (que define isDarkStyle/config/isRtl) e antes de
 * qualquer script de página (PageScripts) — por isso os scripts de página
 * podem continuar usando swalWithBootstrapButtons/borderColor/bodyBg/
 * headingColor livremente, sem declará-los de novo.
 */

'use strict';

// CSRF: anexa o token antifalsificação (renderizado uma vez por página em
// _CommonMasterLayout.cshtml) em toda chamada $.ajax, via header - as chamadas
// deste projeto enviam JSON no body, então o token não viaja como campo de
// formulário e precisa ir pelo header configurado em Program.cs (AddAntiforgery).
(function () {
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');

    if (tokenInput) {
        $.ajaxSetup({
            headers: { 'X-CSRF-TOKEN': tokenInput.value }
        });
    }
})();

const swalWithBootstrapButtons = Swal.mixin({
    customClass: {
        confirmButton: "btn btn-label-secondary waves-effect",
        cancelButton: "btn btn-label-primary waves-effect"
    },
    buttonsStyling: false
});

let borderColor, bodyBg, headingColor;

if (isDarkStyle) {
    borderColor = config.colors_dark.borderColor;
    bodyBg = config.colors_dark.bodyBg;
    headingColor = config.colors_dark.headingColor;
} else {
    borderColor = config.colors.borderColor;
    bodyBg = config.colors.bodyBg;
    headingColor = config.colors.headingColor;
};

$.busyLoadSetup({
    animation: "slide",
    background: "rgba(71,0,123, 0.86)"
});

$(function () {
    var bsDatepickerFormat = $('.dt-calendar');

    // Format - cada campo pode sobrescrever via atributo data-date-format
    // (ex.: data-date-format="dd/mm/yyyy" para exibir/aceitar o ano). Sem isso
    // o "format: 'dd/mm'" abaixo era aplicado a TODO campo .dt-calendar do site,
    // sem chance de um campo específico usar um formato com ano.
    bsDatepickerFormat.each(function () {
        var elFormat = $(this).data('dateFormat') || 'dd/mm';

        $(this).datepicker({
            autoclose: true,
            todayHighlight: true,
            format: elFormat,
            language: 'pt-BR',
            orientation: isRtl ? 'auto right' : 'auto left'
        });
    });
});

//#region MEU PERFIL - comum a ProfileUser (timeline de atividade) e
// AccountSettingsSecurity (grid de últimos acessos) - antes duplicado nos dois
// arquivos de página.

// Foto cadastrada em imgAvatar (coluna socio.imgAvatar) fica em
// img/avatars/socio/imgAvatar{id}.png; sem cadastro, usa o avatar padrão da ACECA.
function fn_UrlAvatar(id, imgAvatar) {
    return imgAvatar
        ? `${assetsPath}img/avatars/socio/imgAvatar${id}.png`
        : `${assetsPath}img/avatars/socio/imgAvatarAceca.png`;
}

// Mesmos ícones/cores do layout original do template de "Últimos Acessos" (um
// por tipo de dispositivo), escolhidos a partir do OS/Device reais gravados em
// socio_log_acesso.
function fn_IconeAcesso(os, device) {
    const strOs = (os || '').toLowerCase();
    const strDevice = (device || '').toLowerCase();

    if (strOs.includes('android'))
        return { icone: 'ri-android-line', cor: 'text-success' };

    if (strOs.includes('mac'))
        return { icone: 'ri-mac-line', cor: 'text-info' };

    if (strOs.includes('ios'))
        return { icone: 'ri-smartphone-line', cor: 'text-info' };

    if (strDevice.includes('mobile') || strDevice.includes('phone'))
        return { icone: 'ri-smartphone-line', cor: 'text-danger' };

    if (strOs.includes('windows'))
        return { icone: 'ri-macbook-line', cor: 'text-warning' };

    return { icone: 'ri-computer-line', cor: 'text-secondary' };
}

// "11 de Agosto de 2026 às 14:18" - moment/locale pt-br só tem o nome do mês em
// minúsculo, então capitaliza a primeira letra à parte. .locale() aqui só afeta
// esta instância do moment, sem mudar o locale padrão usado no site.
function fn_FormatarDataAcesso(ultimoLogin) {
    if (!ultimoLogin) return '-';

    const dataMoment = moment(ultimoLogin).locale('pt-br');
    const mes = dataMoment.format('MMMM');
    const mesCapitalizado = mes.charAt(0).toUpperCase() + mes.slice(1);

    return `${dataMoment.format('D')} de ${mesCapitalizado} de ${dataMoment.format('YYYY')} às ${dataMoment.format('HH:mm')}`;
}

function fn_TextoBrowserOs(browser, os) {
    return `${browser || 'Navegador'} no ${os || 'Dispositivo'}`;
}

function fn_TextoDispositivo(device) {
    return device || 'Dispositivo Desconhecido';
}

// Últimos acessos (socio_log_acesso) do sócio autenticado - usado tanto na tela
// de Segurança (grid) quanto na tela Meu Perfil (timeline de Atividade).
function fn_CarregarUltimosAcessos(callback) {
    $.ajax({
        url: '/Auth/GetUltimosAcessos',
        type: 'POST',
        success: function (response) {
            callback(response?.bResult ? (response.data || []) : []);
        },
        error: function (xhr, status, error) {
            console.error("fn_CarregarUltimosAcessos error: " + error);
            callback([]);
        }
    });
}

//#endregion
