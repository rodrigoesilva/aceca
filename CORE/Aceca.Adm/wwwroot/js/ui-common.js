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

//#region MASCARAS - antes duplicadas em admin-socio.js, admin-socio-contato.js,
// admin-socio-endereco.js, negociacao-socio.js e pages-auth-account-settings.js.

function fn_MaskTelefone(input) {
    // Remove tudo que não for dígito
    let value = input.value.replace(/\D/g, '');

    // Limita a 11 dígitos (DDD + 9 dígitos)
    value = value.substring(0, 11);

    // Aplica a máscara: (00) 00000-0000 / (00) 0000-0000
    value = value.replace(/^(\d{2})(\d)/, '($1) $2');
    value = value.replace(/(\d)(\d{4})$/, '$1-$2');

    input.value = value;
}

function fn_MaskDataAniversario(input) {
    // Remove tudo que não for dígito (funciona tanto ao digitar quanto ao colar
    // uma data completa, ex.: "23/02/1956", pois o evento "input" dispara nos dois casos)
    let value = input.value.replace(/\D/g, '');

    // Limita a 8 dígitos (DDMMYYYY)
    value = value.substring(0, 8);

    // Aplica a mascara DD/MM/YYYY
    if (value.length > 4) {
        value = value.replace(/(\d{2})(\d{2})(\d{1,4})/, '$1/$2/$3');
    } else if (value.length > 2) {
        value = value.replace(/(\d{2})(\d{1,2})/, '$1/$2');
    }

    input.value = value;
}

// onEncontrado é opcional - só telas que querem autocompletar o endereço passam esse
// callback (ex.: fn_MaskCEP(this, fn_PreencherEndereco)); sem ele, só aplica a máscara
// (mesmo comportamento de telas como SocioEndereco.cshtml, que preenchem manualmente).
function fn_MaskCEP(input, onEncontrado) {
    // Remove tudo que não for dígito
    let value = input.value.replace(/\D/g, '');

    // Limita a 8 dígitos
    value = value.substring(0, 8);

    // Aplica a máscara: 00000-000
    value = value.replace(/(\d{5})(\d)/, '$1-$2');

    input.value = value;

    if (onEncontrado && value.replace(/\D/g, '').length === 8) {
        fn_BuscaEnderecoPorCep(value, onEncontrado);
    }
}

// onEncontrado recebe o objeto retornado pela ViaCEP ({logradouro, bairro, localidade,
// uf, ...}) já validado (nunca chamado se não encontrar) - cada tela decide em quais
// campos preencher esses dados.
function fn_BuscaEnderecoPorCep(cep, onEncontrado) {
    const cepLimpo = cep.replace(/\D/g, '');

    // fetch() nativo (não $.ajax) de propósito: o $.ajaxSetup acima injeta o header
    // X-CSRF-TOKEN em toda chamada $.ajax - inclusive pra um domínio externo como a
    // ViaCEP, que não libera esse header customizado e forçava um preflight CORS que
    // sempre falhava.
    fetch(`https://viacep.com.br/ws/${cepLimpo}/json/`)
        .then(function (response) {
            if (!response.ok) throw new Error('Falha na consulta do CEP');

            return response.json();
        })
        .then(function (result) {
            if (!result || result.erro) {
                Swal.fire({
                    title: 'CEP n&atilde;o encontrado!!',
                    icon: 'warning',
                    html: `Preencha o endere&ccedil;o manualmente.`,
                    focusConfirm: false,
                    confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                    customClass: { confirmButton: 'btn btn-label-warning waves-effect' }
                });

                return;
            }

            onEncontrado(result);
        })
        .catch(function (erro) {
            console.log("Falha ao consultar CEP via ViaCEP:", erro);
        });
}

//#endregion

//#region GRID - antes duplicadas em praticamente todo admin-*.js/negociacao-*.js
// (fn_ModalErro em 32 arquivos, fn_CheckVerAtivos em 25 - cópias idênticas).

// Handler padrão de erro de $.ajax (error: fn_ModalErro) usado em quase toda tela
// admin/negociação. Sempre fecha o busyLoad e mostra a mensagem do servidor, se a
// resposta de erro vier em JSON - senão cai numa mensagem genérica.
function fn_ModalErro(xhr, textStatus, errorThrown) {
    console.log("Server Response:", xhr.responseText);
    console.log("XMLHttpRequest  :: ", xhr);
    console.log("textStatus  :: ", textStatus);
    console.log("errorThrown  :: ", errorThrown);
    console.log("result  :: Error while posting SendResult");

    // Sempre esconde o loading e exibe o Swal, mesmo que a resposta de erro
    // nao seja um JSON valido (ex.: pagina de erro HTML, timeout, requisicao
    // abortada) - sem isso o JSON.parse podia estourar exception e travar o
    // busyLoadFull aberto para sempre, sem nenhuma mensagem para o usuario.
    $.busyLoadFull("hide");

    let mensagemErro = 'Ocorreu um erro inesperado, tente novamente.';

    try {
        const objError = JSON.parse(xhr.responseText);

        if (objError?.message)
            mensagemErro = objError.message;
    } catch (e) {
        console.log("Falha ao interpretar resposta de erro do servidor:", e);
    }

    Swal.fire({
        title: 'OPS!!',
        icon: 'error',
        html: `<b> Erro ocorrido <br><br>${mensagemErro}</b>`,
        focusConfirm: false,
        confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
        customClass: {
            confirmButton: 'btn btn-label-danger waves-effect'
        }
    });
}

// Liga o checkbox "Exibir Somente Ativos" (#chkFilterAtivo, injetado no DOM pela
// própria grid ao carregar) ao redraw da DataTable - o filtro em si já é aplicado
// no servidor (ver ajax.data em cada fn_GridList), aqui só redesenha.
function fn_CheckVerAtivos() {
    const chkVerAtivos = document.querySelector('#chkFilterAtivo');

    if (chkVerAtivos) {
        chkVerAtivos.addEventListener('change', function () {
            var table = $('.datatables-basic').DataTable();

            if (this.checked) {
                Swal.fire({
                    title: 'INFO!!',
                    icon: 'info',
                    html: 'Essa op&ccedil;&atilde;o <br> exbir&aacute; somente os itens ativos !!',
                    focusConfirm: false,
                    confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                    customClass: {
                        confirmButton: 'btn btn-label-info waves-effect'
                    },
                }).then((result) => {
                    table.draw();
                });
            } else {
                table.draw();
            }
        });
    }
}

//#endregion
