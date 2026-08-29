/**
 * Cadastro -> Aprovação
 *
 * Fila de aprovação de marcas_cadastro. Administracao vê e decide o status de todas as
 * submissões; qualquer outro usuário vê só as que ele mesmo enviou (o servidor já filtra
 * isso em FiltrarDadosAprovacao - aqui só ligamos/desligamos os botões de ação).
 */

'use strict';

//#region Declare

let var_Controller = '/Cadastro',
    var_ControllerCmb = '/HelperExtensions';

let strUrlImgInexistente = "https://www.aceca.com.br/assets/img/img_inexistente.jpg";

// EStatusCadastro (Helper/HelperExtensionsController.cs) - mantém os mesmos valores.
const E_STATUS_PENDENTE = 1,
    E_STATUS_APROVADO = 2,
    E_STATUS_NEGADO = 3;

const mapStatusBadge = {
    [E_STATUS_PENDENTE]: { texto: 'Pendente', classe: 'bg-label-warning' },
    [E_STATUS_APROVADO]: { texto: 'Aprovado', classe: 'bg-label-success' },
    [E_STATUS_NEGADO]: { texto: 'Negado', classe: 'bg-label-danger' },
};

let combosCarregados = {};

//#endregion

//#region CARREGAMENTO INICIAL

document.addEventListener('DOMContentLoaded', function () {
    fn_PopLoadCombos();
    fn_FiltrosChange();
    fn_MontarGrid();

    $('.btn-filter-clear').on('click', function () {
        $('#cmb_MarcaFase').prop('selectedIndex', 0).trigger('change.select2');
        $('#cmb_StatusCadastro').prop('selectedIndex', 0).trigger('change.select2');
        $('#cmb_MarcaSubTipo').prop('selectedIndex', 0).trigger('change.select2');
        $('.datatables-basic').DataTable().ajax.reload();
    });
});

//#endregion

//#region COMBOS

function fn_LoadCombo(id, url) {
    if (combosCarregados[id]) return;

    const $cmb = $(id);

    combosCarregados[id] = true;

    $.ajax({
        url: url,
        type: 'GET',
        cache: true,
        success: function (data) {
            let options = '<option value="-1">-- Selecionar --</option><option value="0">Todas</option>';

            (data || []).forEach(item => {
                options += `<option value="${item.value}">${item.text}</option>`;
            });

            $cmb.html(options);
            $cmb.trigger('change.select2');
        },
        error: function () {
            combosCarregados[id] = false;
        }
    });
}

function fn_PopLoadCombos() {
    fn_LoadCombo('#cmb_MarcaFase', `${var_ControllerCmb}/AsyncCmb_MarcaFase`);
    fn_LoadCombo('#cmb_StatusCadastro', `${var_ControllerCmb}/AsyncCmb_StatusCadastro`);
    fn_LoadCombo('#cmb_MarcaSubTipo', `${var_ControllerCmb}/AsyncCmb_MarcaSubTipo`);
}

function fn_FiltrosChange() {
    $('#cmb_MarcaFase, #cmb_StatusCadastro, #cmb_MarcaSubTipo').on('change', function () {
        $('.datatables-basic').DataTable().ajax.reload();
    });
}

//#endregion

//#region GRID

function fn_MontarGrid() {
    $.busyLoadFull("show");

    $('.datatables-basic').DataTable({
        processing: true,
        serverSide: true,
        autoWidth: false,
        order: [],
        language: { url: '/vendor/libs/datatables-bs5/i18n/pt-BR.json' },

        ajax: {
            url: `${var_Controller}/FiltrarDadosAprovacao`,
            type: 'POST',
            contentType: 'application/json',
            data: function (d) {
                return JSON.stringify({
                    draw: d.draw,
                    start: d.start,
                    length: d.length,
                    search: d.search,
                    order: d.order,
                    filtros: {
                        marcaFaseId: parseInt($('#cmb_MarcaFase').val()) || 0,
                        statusCadastro: parseInt($('#cmb_StatusCadastro').val()) || 0,
                        marcaSubTipoId: parseInt($('#cmb_MarcaSubTipo').val()) || 0,
                    }
                });
            },
            dataSrc: function (json) {
                $.busyLoadFull("hide");
                return json.data;
            }
        },

        columns: [
            { data: null, defaultContent: '', className: 'control', orderable: false, responsivePriority: 1 },
            { data: 'CriadoPorNome', className: 'text-center', responsivePriority: 10001 },
            { data: 'NomeAcervo', className: 'text-center', responsivePriority: 10002 },
            { data: 'NomeFase', className: 'text-center', responsivePriority: 10003 },
            {
                data: 'ImgPrincipalFull', className: 'text-center', orderable: false, responsivePriority: 10004,
                render: function (data, type) {
                    if (type !== 'display') return data || '';
                    return `<img alt="Imagem" onerror="this.onerror=null;this.src='${strUrlImgInexistente}';" src="${data}" style="width:48px;height:48px;object-fit:cover;border-radius:4px;">`;
                }
            },
            {
                data: 'ImgDetalheFull', className: 'text-center', orderable: false, responsivePriority: 10005,
                render: function (data, type) {
                    if (type !== 'display') return data || '';
                    return `<img alt="Detalhe" onerror="this.onerror=null;this.src='${strUrlImgInexistente}';" src="${data}" style="width:48px;height:48px;object-fit:cover;border-radius:4px;">`;
                }
            },
            { data: 'CodigoAceca', className: 'text-center', responsivePriority: 2, orderable: true },
            { data: 'NomeMarca', className: 'text-start', responsivePriority: 3 },
            { data: 'Descricao', className: 'text-start', responsivePriority: 10006 },
            {
                data: 'Tipo', className: 'text-center', responsivePriority: 10007,
                render: function (data, type, full) {
                    if (type !== 'display') return data || '';
                    return [data, full.SubTipo].filter(Boolean).join(' / ');
                }
            },
            {
                data: 'StatusCadastro', className: 'text-center', responsivePriority: 4,
                render: function (data, type) {
                    if (type !== 'display') return data || '';
                    const info = mapStatusBadge[data] || { texto: '-', classe: 'bg-label-secondary' };
                    return `<span class="badge ${info.classe}">${info.texto}</span>`;
                }
            },
            {
                data: 'Observacao', className: 'text-start', responsivePriority: 10008,
                render: function (data, type, full) {
                    if (type !== 'display') return data || '';
                    if (full.StatusCadastro !== E_STATUS_NEGADO || !data) return '';
                    return `<span title="${data}">${data}</span>`;
                }
            },
            {
                data: 'Id', className: 'text-center', orderable: false, searchable: false, responsivePriority: 5,
                render: function (data, type, full) {
                    if (type !== 'display') return '';

                    if (!isAdministracao) return '';

                    const statusAtual = full.StatusCadastro;

                    let btns = '<div class="d-flex gap-2 justify-content-center flex-wrap">';

                    if (statusAtual !== E_STATUS_APROVADO) {
                        btns += `<button type="button" class="btn btn-sm btn-label-success btn-aprovar" data-id="${data}"><i class="ri-check-line"></i> Aprovar</button>`;
                    }
                    if (statusAtual !== E_STATUS_NEGADO) {
                        btns += `<button type="button" class="btn btn-sm btn-label-danger btn-negar" data-id="${data}"><i class="ri-close-line"></i> Negar</button>`;
                    }
                    if (statusAtual !== E_STATUS_PENDENTE) {
                        btns += `<button type="button" class="btn btn-sm btn-label-secondary btn-pendente" data-id="${data}"><i class="ri-time-line"></i> Voltar p/ Pendente</button>`;
                    }

                    btns += '</div>';

                    return btns;
                }
            },
        ],
    });
}

//#endregion

//#region AÇÕES

$(document).on('click', '.btn-aprovar', function () {
    const id = $(this).data('id');

    Swal.fire({
        title: 'Aprovar cadastro?',
        icon: 'question',
        html: 'O item vai ser publicado no Acervo.',
        showCancelButton: true,
        confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Aprovar`,
        cancelButtonText: 'Cancelar',
        customClass: {
            confirmButton: 'btn btn-success waves-effect waves-light me-3',
            cancelButton: 'btn btn-label-secondary waves-effect'
        },
        buttonsStyling: false
    }).then((result) => {
        if (result.isConfirmed) fn_SetStatus(id, E_STATUS_APROVADO, null);
    });
});

$(document).on('click', '.btn-pendente', function () {
    const id = $(this).data('id');
    fn_SetStatus(id, E_STATUS_PENDENTE, null);
});

$(document).on('click', '.btn-negar', function () {
    const id = $(this).data('id');

    Swal.fire({
        title: 'Negar cadastro',
        icon: 'warning',
        input: 'textarea',
        inputPlaceholder: 'Motivo da recusa (obrigatório)...',
        showCancelButton: true,
        confirmButtonText: `<i class="ri-close-line"></i>&nbsp;Negar`,
        cancelButtonText: 'Cancelar',
        customClass: {
            confirmButton: 'btn btn-danger waves-effect waves-light me-3',
            cancelButton: 'btn btn-label-secondary waves-effect'
        },
        buttonsStyling: false,
        preConfirm: (observacao) => {
            if (!observacao || !observacao.trim()) {
                Swal.showValidationMessage('Preencha o motivo da recusa');
                return false;
            }
            return observacao.trim();
        }
    }).then((result) => {
        if (result.isConfirmed) fn_SetStatus(id, E_STATUS_NEGADO, result.value);
    });
});

function fn_SetStatus(id, status, observacao) {
    $.busyLoadFull("show");

    $.ajax({
        url: `${var_Controller}/SetStatus`,
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({ id: id, status: status, observacao: observacao }),
        success: function (response) {
            $.busyLoadFull("hide");

            if (!response?.bResult) {
                Swal.fire({
                    title: 'OPS!!',
                    icon: 'error',
                    html: response?.message || 'Não foi possível atualizar o status.',
                    confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                    customClass: { confirmButton: 'btn btn-label-danger waves-effect' }
                });
                return;
            }

            $('.datatables-basic').DataTable().ajax.reload(null, false);
        },
        error: function (xhr) {
            $.busyLoadFull("hide");

            Swal.fire({
                title: 'OPS!!',
                icon: 'error',
                html: xhr?.responseJSON?.message || 'Não foi possível atualizar o status.',
                confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                customClass: { confirmButton: 'btn btn-label-danger waves-effect' }
            });
        }
    });
}

//#endregion
