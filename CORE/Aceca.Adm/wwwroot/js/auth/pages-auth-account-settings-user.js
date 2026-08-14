/**
 * profile - user (jquery)
 */
'use strict';

//#region Declare

let var_Nome = 'Auth',
    var_Controller = '/Auth',
    varItems_QtdPorPage = 10,
    varItems_DivPage = [5, 10, 25, 50, 75, 100];
var msg = 'O preenchimento &eacute; obrigat&oacute;rio';

//#endregion

//#region CARREGAMENTO INICIAL

document.addEventListener('DOMContentLoaded', function () {
    (function () {
        console.log(`AUTH USER - Todos os recursos terminaram o carregamento!`);

        fn_AuthUserIni();
    })();
});

//#endregion

function fn_AuthUserIni() {

    const socioId = document.getElementById('hdSocioLogadoId').value;
    // console.log("socioId ::: ", socioId);

    // Div "Sobre" (dados fixos do sócio) - div_atividade continua com os dados de
    // exemplo do template por enquanto.
    fn_CarregarSobre();

  // Variable declaration for table
    var dt_project_table = $('.datatable-project');

    if (dt_project_table.length) {
        fn_GridColecao(dt_project_table)
    }
};

function fn_GridColecao(dt_project_table) {
    // Grid "Coleção" (div_grid_colecao) - fases/tipos com mais quantidade na coleção do
    // sócio autenticado (quantidade retornada/limitada pelo Take() do backend em
    // AuthController.GetTopFasesColecao - varItems_QtdPorPage aqui só controla quantas
    // dessas linhas são exibidas de uma vez, então mantenha >= ao Take() do backend).
    // Cada quantidade (Possui/Favorito) é exibida como percentual relativo ao total geral
    // da coleção do próprio sócio, via barra de progresso.
    // --------------------------------------------------------------------
   
        var dt_project = dt_project_table.DataTable({
            ajax: {
                url: '/Auth/GetTopFasesColecao',
                type: 'POST',
                dataSrc: function (json) {
                    return json?.data || [];
                }
            },
            columns: [
                { data: null },
                { data: 'nomeFase' },
                { data: 'tipo' },
                { data: 'percentPossui' },
                { data: 'percentFavorito' }
            ],
            columnDefs: [
                {
                    // For Responsive
                    className: 'control',
                    searchable: false,
                    orderable: false,
                    responsivePriority: 2,
                    targets: 0,
                    render: function (data, type, full, meta) {
                        return '';
                    }
                },
                {
                    // Nome da fase
                    targets: 1,
                    responsivePriority: 1,
                    render: function (data, type, full, meta) {
                        return '<span class="text-truncate fw-medium text-heading">' + (full.nomeFase || '-') + '</span>';
                    }
                },
                {
                    // Tipo (maço, box, ...)
                    targets: 2,
                    render: function (data, type, full, meta) {
                        return '<span class="text-heading">' + (full.tipo || '-') + '</span>';
                    }
                },
                {
                    // Coleção (% Possui em relação ao total da coleção do sócio)
                    targets: 3,
                    render: function (data, type, full, meta) {
                        return fn_RenderProgressoColecao(full.percentPossui, full.qtdPossui);
                    }
                },
                {
                    // Favoritos (% Favorito em relação ao total de favoritos do sócio)
                    targets: 4,
                    render: function (data, type, full, meta) {
                        return fn_RenderProgressoColecao(full.percentFavorito, full.qtdFavorito);
                    }
                }
            ],
            order: [[3, 'desc']],
            ordering: false,
            dom: 't',
            displayLength: varItems_QtdPorPage,
            lengthMenu: varItems_DivPage,
            info: false,
            searching: false,
            language: {
                emptyTable: 'Nenhum item na coleção'
            }
        });
    

}

function fn_RenderProgressoColecao(percent, qtd) {
  var $percent = percent || 0;

  return (
    '<div class="d-flex align-items-center" title="' + (qtd || 0) + ' item(ns)">' +
    '<div class="progress rounded-pill w-px-75" style="height: 8px;">' +
    '<div class="progress-bar" role="progressbar" style="width:' +
    $percent +
    '%;" aria-valuenow="' +
    $percent +
    '" aria-valuemin="0" aria-valuemax="100"></div>' +
    '</div>' +
    '<div class="text-heading ms-2">' +
    $percent +
    '%</div>' +
    '</div>'
  );
}

//#region SOBRE

function fn_CarregarSobre() {
    $.ajax({
        url: '/Auth/GetFullById',
        type: 'POST',
        success: function (response) {
            if (!response?.bResult || !response?.data) return;

            const d = response.data;

            // Header (div_header)
            $('#sp_HeaderNome').text(d.nome || '-');
            $('#sp_HeaderCidade').text(d.cidade || '-');
            $('#sp_HeaderSocioDesde').text(fn_FormatarSocioDesde(d.dataCriacao));
            $('#img_HeaderAvatar').attr('src', fnhelper_UrlAvatar(d.id, d.imgAvatar));

            // Sobre / Contatos / Correspondência (div_sobre)
            $('#sp_Nome').text(d.nome || '-');
            $('#sp_Usuario').text(d.usuario || '-');
            $('#sp_Perfil').text(d.perfil || '-');
            $('#sp_Aniversario').text(fn_FormatarAniversario(d.aniversarioDia, d.aniversarioMes, d.aniversarioAno));
            $('#sp_Telefone').text(fn_FormatarTelefone(d.contatoDDI, d.contatoDDD, d.contatoTelefone));
            $('#sp_Email').text(d.email || '-');
            $('#sp_Endereco').text(fn_FormatarEndereco(d));

            // Atividade (div_atividade) - mesmos dados de socio_log_acesso da grid de
            // Últimos Acessos em AccountSettingsSecurity.cshtml
            fn_PopularTimelineAtividade(d);
        },
        error: function (xhr, status, error) {
            console.error("fn_CarregarSobre error: " + error);
        }
    });
}

function fn_FormatarAniversario(dia, mes, ano) {
    if (!dia || !mes) return '-';

    let strData = String(dia).padStart(2, '0') + '/' + String(mes).padStart(2, '0');

    return ano ? strData + '/' + ano : strData;
}

function fn_FormatarTelefone(ddi, ddd, telefone) {
    if (!ddd || !telefone) return '-';

    let strTelefone = String(telefone);

    return `+(${ddi || 55} ${ddd}) ${strTelefone.slice(0, -4)}-${strTelefone.slice(-4)}`;
}

const MESES_PT = ['Janeiro', 'Fevereiro', 'Março', 'Abril', 'Maio', 'Junho', 'Julho', 'Agosto', 'Setembro', 'Outubro', 'Novembro', 'Dezembro'];

function fn_FormatarSocioDesde(dataCriacao) {
    if (!dataCriacao) return '-';

    let data = new Date(dataCriacao);

    if (isNaN(data.getTime())) return '-';

    return `Sócio desde ${MESES_PT[data.getMonth()]} de ${data.getFullYear()}`;
}

// fnhelper_UrlAvatar é comum (helper-ui-common.js) - usada aqui e em pages-auth-account-settings.js.

function fn_FormatarEndereco(d) {
    if (!d.endereco) return '-';

    let strEndereco = d.endereco + (d.numero ? ', ' + d.numero : '');

    if (d.complemento) strEndereco += ' - ' + d.complemento;
    if (d.cidade) strEndereco += ' - ' + d.cidade;
    if (d.estado) strEndereco += ' - ' + d.estado;
    if (d.cep) strEndereco += ' - ' + d.cep;

    return strEndereco;
}

//#endregion

//#region ATIVIDADE

// Timeline de div_atividade: mesmos 3 slots já existentes no layout, preenchidos com
// os últimos acessos reais (socio_log_acesso) - fnhelper_CarregarUltimosAcessos/fnhelper_IconeAcesso/
// fnhelper_FormatarDataAcesso/fnhelper_TextoBrowserOs vêm de helper-ui-common.js (comuns com a grid de
// Últimos Acessos em AccountSettingsSecurity.cshtml). O bloco de avatar é o mesmo sócio
// autenticado nos 3 itens - troca a foto/nome fixos do template pelos dados reais, com
// fallback pro avatar padrão da ACECA quando não há imgAvatar cadastrado.
function fn_PopularTimelineAtividade(dadosPerfil) {
    const urlAvatar = fnhelper_UrlAvatar(dadosPerfil.id, dadosPerfil.imgAvatar);

    $('.tl-avatar-img').attr('src', urlAvatar);
    $('.tl-avatar-nome').text(dadosPerfil.nome || '-');
    $('.tl-avatar-perfil').text(dadosPerfil.perfil || '-');

    fnhelper_CarregarUltimosAcessos(function (acessos) {
        for (let i = 1; i <= 3; i++) {
            const acesso = acessos[i - 1];

            if (!acesso) {
                $(`#tl_Titulo_${i}`).text('Nenhum acesso registrado');
                $(`#tl_Data_${i}`).text('-');
                $(`#tl_Local_${i}`).text('-');
                continue;
            }

            const local = [acesso.cidade, acesso.estado].filter(Boolean).join(' - ') || '-';

            $(`#tl_Titulo_${i}`).text(fnhelper_TextoBrowserOs(acesso.browser, acesso.os));
            $(`#tl_Data_${i}`).text(fnhelper_FormatarDataAcesso(acesso.ultimoLogin));
            $(`#tl_Local_${i}`).text(`Acesso via ${fnhelper_TextoDispositivo(acesso.device)} - ${local}`);
        }
    });
}

//#endregion
