/**
 * helper-ui-common.js
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

// Duas Swal.fire() concorrentes sem a anterior ter sido fechada (ex.: uma tela que já
// mostra um Swal de sucesso ao carregar e o usuário clica rápido numa ação que abre
// outro Swal por cima) deixam botões de diálogos diferentes empilhados na mesma tela -
// o efeito visual é "botões do Swal se encavalando/sobrepondo". Fecha qualquer Swal
// ainda aberto antes de abrir um novo, pra qualquer chamada em qualquer página (direto
// em Swal.fire ou via Swal.mixin(...).fire, usado em várias telas) - sem precisar achar
// e corrigir chamada por chamada.
const _swalFireOriginal = Swal.fire;
Swal.fire = function (...args) {
    if (Swal.isVisible()) {
        Swal.close();
    }
    return _swalFireOriginal.apply(this, args);
};

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
function fnhelper_UrlAvatar(id, imgAvatar) {
    return imgAvatar
        ? `${assetsPath}img/avatars/socio/imgAvatar${id}.png`
        : `${assetsPath}img/avatars/socio/imgAvatarAceca.png`;
}

// Mesmos ícones/cores do layout original do template de "Últimos Acessos" (um
// por tipo de dispositivo), escolhidos a partir do OS/Device reais gravados em
// socio_log_acesso.
function fnhelper_IconeAcesso(os, device) {
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
function fnhelper_FormatarDataAcesso(ultimoLogin) {
    if (!ultimoLogin) return '-';

    const dataMoment = moment(ultimoLogin).locale('pt-br');
    const mes = dataMoment.format('MMMM');
    const mesCapitalizado = mes.charAt(0).toUpperCase() + mes.slice(1);

    return `${dataMoment.format('D')} de ${mesCapitalizado} de ${dataMoment.format('YYYY')} às ${dataMoment.format('HH:mm')}`;
}

function fnhelper_TextoBrowserOs(browser, os) {
    return `${browser || 'Navegador'} no ${os || 'Dispositivo'}`;
}

function fnhelper_TextoDispositivo(device) {
    return device || 'Dispositivo Desconhecido';
}

// Últimos acessos (socio_log_acesso) do sócio autenticado - usado tanto na tela
// de Segurança (grid) quanto na tela Meu Perfil (timeline de Atividade).
function fnhelper_CarregarUltimosAcessos(callback) {
    $.ajax({
        url: '/Auth/GetUltimosAcessos',
        type: 'POST',
        success: function (response) {
            callback(response?.bResult ? (response.data || []) : []);
        },
        error: function (xhr, status, error) {
            console.error("fnhelper_CarregarUltimosAcessos error: " + error);
            callback([]);
        }
    });
}

//#endregion

//#region MASCARAS - antes duplicadas em admin-socio.js, admin-socio-contato.js,
// admin-socio-endereco.js, negociacao-socio.js e pages-auth-account-settings.js.

function fnhelper_MaskTelefone(input) {
    // Remove tudo que não for dígito
    let value = input.value.replace(/\D/g, '');

    // Limita a 11 dígitos (DDD + 9 dígitos)
    value = value.substring(0, 11);

    // Aplica a máscara: (00) 00000-0000 / (00) 0000-0000
    value = value.replace(/^(\d{2})(\d)/, '($1) $2');
    value = value.replace(/(\d)(\d{4})$/, '$1-$2');

    input.value = value;
}

function fnhelper_MaskDataAniversario(input) {
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
// callback (ex.: fnhelper_MaskCEP(this, fn_PreencherEndereco)); sem ele, só aplica a máscara
// (mesmo comportamento de telas como SocioEndereco.cshtml, que preenchem manualmente).
function fnhelper_MaskCEP(input, onEncontrado) {
    // Remove tudo que não for dígito
    let value = input.value.replace(/\D/g, '');

    // Limita a 8 dígitos
    value = value.substring(0, 8);

    // Aplica a máscara: 00000-000
    value = value.replace(/(\d{5})(\d)/, '$1-$2');

    input.value = value;

    if (onEncontrado && value.replace(/\D/g, '').length === 8) {
        fnhelper_BuscaEnderecoPorCep(value, onEncontrado);
    }
}

// onEncontrado recebe o objeto retornado pela ViaCEP ({logradouro, bairro, localidade,
// uf, ...}) já validado (nunca chamado se não encontrar) - cada tela decide em quais
// campos preencher esses dados.
function fnhelper_BuscaEnderecoPorCep(cep, onEncontrado) {
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

//#region GRID - antes duplicada em praticamente todo admin-*.js/negociacao-*.js
// (fnhelper_CheckVerAtivos em 25 arquivos - cópias idênticas). O handler de erro de
// $.ajax que também vivia aqui (fn_ModalErro) foi substituído por
// fnhelper_AlertErro, na região "Alertas (Swal) comuns" abaixo.

// Liga o checkbox "Exibir Somente Ativos" (#chkFilterAtivo, injetado no DOM pela
// própria grid ao carregar) ao redraw da DataTable - o filtro em si já é aplicado
// no servidor (ver ajax.data em cada fn_GridList), aqui só redesenha.
function fnhelper_CheckVerAtivos() {
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

//#region Alertas (Swal) comuns entre telas

// Resolve a melhor mensagem de erro disponivel a partir do que os callbacks de
// ajax/DataTables tem em maos: o jqXHR (contem responseJSON.message quando a Api/Web
// devolve BadRequest/Ok com { message }), o errorThrown do jQuery (string tipo "Bad
// Request"), um objeto de resultado de sucesso com bResult === false (tem .message),
// ou simplesmente uma string/mensagem estatica ja pronta.
function fnhelper_ResolverMensagemErro(erro, fallback) {
    if (erro === null || erro === undefined || erro === '') return fallback || 'Erro desconhecido';

    if (typeof erro === 'string') return erro;

    if (typeof erro === 'object') {
        const mensagem = erro.responseJSON?.message
            || erro.message
            || erro.msg
            || erro.responseText;

        if (mensagem) return mensagem;
    }

    return fallback || 'Erro desconhecido';
}

// Alerta padrao de erro (Swal). Aceita:
//   fnhelper_AlertErro('mensagem estatica')
//   fnhelper_AlertErro(result)                       // result.bResult === false, usa result.message
//   fnhelper_AlertErro(XMLHttpRequest, errorThrown)   // callback de erro do ajax/DataTables
function fnhelper_AlertErro(erro, fallback) {
    const mensagem = fnhelper_ResolverMensagemErro(erro, fallback);

    // Idempotente - a maioria dos callbacks ja chama isso antes, mas garante que o
    // overlay de loading nunca fique preso na tela atras do alerta de erro.
    if (window.jQuery && $.busyLoadFull) $.busyLoadFull("hide");

    Swal.fire({
        title: 'OPS!!',
        icon: 'error',
        html: `<b> Erro ocorrido <br><br>` + mensagem + `</b>`,
        focusConfirm: false,
        confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
        customClass: {
            confirmButton: 'btn btn-label-danger waves-effect'
        }
    });
}

//#endregion

//#region CRUD GENERICO (telas simples de "tabela de apoio" - ver AdmConfig)

// Fecha e reseta o offcanvas #pop-add-new-item padrão usado por essas telas.
function fnhelper_PopFechar() {
    const popAddNewItem = document.querySelector('#pop-add-new-item');

    if (popAddNewItem) bootstrap.Offcanvas.getOrCreateInstance(popAddNewItem).hide();
}

// Cada tela que usa esses helpers precisa definir fn_PopGetObj() (globalmente) com os
// campos do seu próprio formulário - só assim fnhelper_ItemAdd sabe o que enviar, já que
// é chamado sem receber os dados do formulário diretamente (ver app-config-adm-
// admconfig.js::formValid.on('core.form.valid', ...)).
function fnhelper_ItemAdd(varTblObj, controller) {
    const objFormData = fn_PopGetObj();

    $.busyLoadFull("show");

    $.ajax({
        url: `${controller}/Create`,
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(objFormData),
        success: function (response) {
            $.busyLoadFull("hide");

            if (!response?.bResult) {
                fnhelper_AlertErro(response);
                return;
            }

            fnhelper_PopFechar();

            Swal.fire({
                icon: 'success',
                title: 'Salvo!',
                html: 'Registro adicionado com sucesso.',
                confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                customClass: { confirmButton: 'btn btn-label-success waves-effect' }
            }).then(() => {
                (varTblObj?.DataTable ? varTblObj : $('.datatables-basic')).DataTable().ajax.reload(null, false);
            });
        },
        error: function (xhr, textStatus) {
            $.busyLoadFull("hide");

            fnhelper_AlertErro(xhr, textStatus);
        }
    });
}

function fnhelper_ItemEdit(objFormData, controller) {
    $.busyLoadFull("show");

    $.ajax({
        url: `${controller}/Edit`,
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(objFormData),
        success: function (response) {
            $.busyLoadFull("hide");

            if (!response?.bResult) {
                fnhelper_AlertErro(response);
                return;
            }

            fnhelper_PopFechar();

            Swal.fire({
                icon: 'success',
                title: 'Salvo!',
                html: 'Registro alterado com sucesso.',
                confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                customClass: { confirmButton: 'btn btn-label-success waves-effect' }
            }).then(() => {
                $('.datatables-basic').DataTable().ajax.reload(null, false);
            });
        },
        error: function (xhr, textStatus) {
            $.busyLoadFull("hide");

            fnhelper_AlertErro(xhr, textStatus);
        }
    });
}

// item precisa ter .id (o objeto completo da linha, como já é passado pelas telas que
// chamam isso a partir da coluna de Ações).
function fnhelper_ItemDelete(item, controller) {
    Swal.fire({
        title: 'Tem certeza?',
        icon: 'warning',
        html: 'Essa ação não poderá ser desfeita.',
        showCancelButton: true,
        reverseButtons: true,
        confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Sim, excluir!`,
        cancelButtonText: 'Cancelar',
        customClass: {
            confirmButton: 'btn btn-label-danger waves-effect',
            cancelButton: 'btn btn-label-secondary waves-effect me-3'
        },
        buttonsStyling: false
    }).then((result) => {
        if (!result.isConfirmed) return;

        $.busyLoadFull("show");

        $.ajax({
            url: `${controller}/Delete?id=${item.id}`,
            type: 'DELETE',
            success: function (response) {
                $.busyLoadFull("hide");

                if (!response?.bResult) {
                    fnhelper_AlertErro(response);
                    return;
                }

                Swal.fire({
                    icon: 'success',
                    title: 'Excluído!',
                    html: 'Registro excluído com sucesso.',
                    confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                    customClass: { confirmButton: 'btn btn-label-success waves-effect' }
                }).then(() => {
                    $('.datatables-basic').DataTable().ajax.reload(null, false);
                });
            },
            error: function (xhr, textStatus) {
                $.busyLoadFull("hide");

                fnhelper_AlertErro(xhr, textStatus);
            }
        });
    });
}

// Igual a fnhelper_CheckVerAtivos, mas recebendo o seletor da tabela em vez de assumir
// '.datatables-basic' fixo - fnhelper_CheckVerAtivos continua como está (usada em 25+
// arquivos), esta é só a variante parametrizada para quem precisar.
function fnhelper_ExibirSomenteAtivos(seletorTabela) {
    const chkVerAtivos = document.querySelector('#chkFilterAtivo');

    if (!chkVerAtivos) return;

    chkVerAtivos.addEventListener('change', function () {
        var table = $(seletorTabela).DataTable();

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

//#endregion