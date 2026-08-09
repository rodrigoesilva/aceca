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
