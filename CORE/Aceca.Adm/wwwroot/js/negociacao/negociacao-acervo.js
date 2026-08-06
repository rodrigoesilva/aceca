/**
 * Negociacao -> Acervo
 */

'use strict';

//#region Declare

let var_Nome = 'Negocia&ccedil;&atilde;o Acervo',
    var_Controller = '/Negociacao',
    var_ControllerCmb = '/HelperExtensions',

    varTbl_Obj = $('.datatables-basic'),
    varTbl_Data,
    objFiltro;

let var_Filtrado = false,
    var_ImgAlt = "ACECA",
    urlImgModal = "../img/logo/logo.png",
    urlImgModalIcon = "../img/logo/logo01.png",
    urlImgModaltext = "../img/logo/logo02.png";

var msg = 'O preenchimento &eacute; obrigat&oacute;rio';

let modalMarca = document.getElementById('ModalMarca'),
    objModalData;

let strUrlImgInexistente = "https://www.aceca.com.br/assets/img/img_inexistente.jpg";

let idMarcaFase, strMarcaAcervo;

let isPerfil = document.getElementById('hdIsPerfil').value;

//#endregion

//#region CARREGAMENTO INICIAL

document.addEventListener('DOMContentLoaded', function () {
    (function () {
        console.log(`LIST ${var_Controller} - Todos os recursos terminaram o carregamento!`);

        fn_Limpar();

        // Filtros
        fn_FiltrosHide();
        fn_PopLoadCombos();
        fn_FiltrosChange();

        $('.btn-filter-clear').on('click', function () {
            fn_Limpar();
        });

        //click salvar modal
        $("#btn-submit-modal").click(function (e) {
            //console.log(`btn-formadd-submit`);
            var action = document.querySelector('.data-submit').textContent;
            //console.log("action ::: ", action);

            if (action === 'Alterar') {
                var objFormData = fn_ModalGetObj();
                console.log("objFormData ::: ", objFormData);

                //const formData = new FormData(document.forms['form-modal-full-edit']);
                //console.log("fn_ModalSalvar formData ::: ", formData);

                fnItem_Edit(objFormData)
            } else {
                fnItem_Add(varTbl_Obj)
            }
        });

        // Carrega Dados Combos Modal
        //fn_PopLoadCombos();

        fn_Zoom();

        fn_BindColecaoIncluirActions();

        document.addEventListener('keydown', (event) => {
            if (event.key === 'Escape' || event.key === 'Enter') {
                //console.log("Esc ::: ");
                fn_ZoomImgClose();
            }
        });

    })();
});

//#endregion

//#region Botoes

function fn_Filtrar() {
    // Btn Filtro
    //console.log("fn_Filtrar ::: ");
    //console.log("fn_Filtrar var_Filtrado ::: ", var_Filtrado);

    objFiltro = {
        param_MarcaAcervoId: $('#hdMarcaAcervoId').val(),
        param_MarcaFaseId: $('#cmb_MarcaFase').find('option:selected').val(),
        param_MarcaFabricaId: $('#cmb_MarcaFabrica').find('option:selected').val(),
        param_MarcaFabricaNome: $('#cmb_MarcaFabrica').find('option:selected').text(),
        param_MarcaTipoId: $('#cmb_MarcaTipo').find('option:selected').val(),
        param_MarcaSubTipoId: $('#cmb_MarcaSubTipo').find('option:selected').val(),
        param_IncluidoPor: $('#txt_IncluidoPor').val(),
        param_CodigoAceca: $('#txt_CodigoAceca').val(),
        param_NomeMarca: $('#txt_NomeMarca').val(),
        param_PesquisarSemVariante: $('#chk_PesquisarSemVariante')[0].checked,
        param_PesquisarDescricao: $('#chk_PesquisarDescricao')[0].checked,
    };

    //console.log("fn_Filtrar objFiltro : ", objFiltro);

    /*
   console.log("fn_Filtrar param_MarcaFaseId ::: ", objFiltro.param_MarcaFaseId);
   console.log("fn_Filtrar param_MarcaFabricaId ::: ", objFiltro.param_MarcaFabricaId);
   console.log("fn_Filtrar param_MarcaTipoId ::: ", objFiltro.param_MarcaTipoId);
   console.log("fn_Filtrar param_MarcaSubTipoId ::: ", objFiltro.param_MarcaSubTipoId);
   console.log("fn_Filtrar param_IncluidoPor ::: ", objFiltro.param_IncluidoPor.length);
   console.log("fn_Filtrar param_CodigoAceca ::: ", objFiltro.param_CodigoAceca.length);
   console.log("fn_Filtrar param_NomeMarca ::: ", objFiltro.param_NomeMarca.length);
   */

    if (objFiltro.param_MarcaAcervoId < 0
        && objFiltro.param_MarcaFaseId < 0
        && objFiltro.param_MarcaFabricaId <= 0
        && objFiltro.param_MarcaTipoId <= 0
        && objFiltro.param_MarcaSubTipoId <= 0
    ) {

        //console.log("fn_Filtrar objFiltro NULO ::: ", objFiltro);

        Swal.fire({
            title: 'Dados Inv&aacute;lidos!!',
            icon: 'error',
            html: `<b>Os filtros n&atilde;o foram informados corretamente!!!</b>`,
            focusConfirm: false,
            confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
            customClass: {
                confirmButton: 'btn btn-label-danger waves-effect'
            }
        }).then((result) => {
            fn_Limpar();
        });
    } else {
        if (objFiltro.param_MarcaFaseId < 0) {
            Swal.fire({
                title: "OPS !!!",
                html: `Nenhuma opção de fase foi informada!`,
                imageUrl: `${urlImgModal}`,
                imageWidth: 300,
                imageAlt: `${var_ImgAlt}`,
            })
        } else {
            //console.log("fn_Filtrar objFiltro NULO ::: ", objFiltro);

            if (objFiltro.param_MarcaFaseId == 0) {
                swalWithBootstrapButtons.fire({
                    title: "Tem certeza?",
                    html: `Essa opção aumentará o tempo  <br><br> de carregamento dos dados!`,
                    imageUrl: `${urlImgModal}`,
                    imageWidth: 300,
                    imageAlt: `${var_ImgAlt}`,
                    showCancelButton: true,
                    confirmButtonText: "Sim, vou aguardar!",
                    cancelButtonText: "Não, vou escolher uma fase!",
                }).then((result) => {
                    if (result.isConfirmed) {
                        swalWithBootstrapButtons.fire({
                            title: "Carregando!",
                            text: "Aguarde o carregamento das informações.",
                            icon: "success",
                            confirmButtonText: "Ok, vamos aguardar!",
                            cancelButtonText: "Não, vou escolher uma fase!",
                        }).then((result) => {

                            //console.log("fn_Filtrar cmb_MarcaTipo length ::: ", $('#cmb_MarcaTipo option').length);
                            if ($('#cmb_MarcaTipo option').length <= 1) {
                                fn_LoadCmb_MarcaTipo();
                            }

                            fn_FiltrarDados(objFiltro);
                        });
                    }
                    else if (result.dismiss === Swal.DismissReason.cancel) {
                        swalWithBootstrapButtons.fire({
                            title: "Cancelado!",
                            text: "Realize a escolha de uma fase.",
                            icon: "info",
                        }).then((result) => {
                            $('#cmb_MarcaFase').prop('selectedIndex', 0).change();
                        });
                    }
                });
            } else {
                fn_FiltrarDados(objFiltro);
            }
        }
    }
}

function fn_Limpar() {
    //console.log("fn_Limpar ::: ");

    $.busyLoadFull("show");

    $('#cmb_MarcaFase').prop('selectedIndex', 0).change();
    //$('#cmb_MarcaFabrica').prop('selectedIndex', 0).change();
    $('#cmb_MarcaTipo').prop('selectedIndex', 0).change();
    $('#cmb_MarcaSubTipo').prop('selectedIndex', 0).change();
    $('#chk_PesquisarSemVariante')[0].checked = false;
    $('#chk_PesquisarDescricao')[0].checked = false;

    $(".card-datatable").hide();
    $('.datatables-basic').DataTable().clear().draw();

    var_Filtrado = false;

    $.busyLoadFull("hide");
}

//#endregion

//#region Filtros

function fn_FiltrosHide() {
    $('.div_MarcaTipo').attr('style', 'display: none !important');
    $('.div_MarcaSubTipo').attr('style', 'display: none !important');
    $('.div_PesquisarDescricao').attr('style', 'display: none !important');
    $('.div_PesquisarSemVariante').attr('style', 'display: none !important');
    $('.div_Botoes').attr('style', 'display: none !important');
}

function fn_FiltrosShow() {
    $('.div_MarcaTipo').attr('style', 'display: block !important');
    //$('.div_MarcaSubTipo').attr('style', 'display: block !important');
    $('.div_PesquisarDescricao').attr('style', 'display: block !important');
    $('.div_PesquisarSemVariante').attr('style', 'display: block !important');
    $('.div_Botoes').attr('style', 'display: block !important');
}

function fn_FiltrosChange() {

    $('#cmb_MarcaFase').on('change', function () {

        idMarcaFase = $(this).find('option:selected').val();

        //console.log("cmb_MarcaFase change idMarcaFase ::: ", idMarcaFase);
        //console.log("cmb_MarcaFase change var_Filtrado ::: ", var_Filtrado);

        if (idMarcaFase === undefined || idMarcaFase < 0) {
            fn_FiltrosHide();
        } else {
            fn_LoadCmb_MarcaTipo();
            //console.log("cmb_MarcaFase change ::: ");

            $('#chk_PesquisarDescricao')[0].checked = false;

            // Clear the search and redraw the table
            var table = $('.datatables-basic').DataTable();
            table.search('').draw();

            fn_Filtrar();
        }
    });

    $('#cmb_MarcaTipo').on('change', function () {

        let idMarcaTipo = $(this).find('option:selected').val();

        //console.log("cmb_MarcaTipo change idMarcaTipo ::: ", idMarcaTipo);
        //console.log("cmb_MarcaTipo change idMarcaFase ::: ", idMarcaFase);
        //console.log("cmb_MarcaTipo change var_Filtrado ::: ", var_Filtrado);

        if (idMarcaFase === undefined || idMarcaFase < 0) {
            fn_ModalSelecionarFase();
        } else {

            if (idMarcaTipo <= 0) {
                $('.div_MarcaSubTipo').attr('style', 'display: none !important');
                fn_Filtrar();
            } else {
                $('.div_MarcaSubTipo').attr('style', 'display: block !important');
                fn_Filtrar();
            }
        }
    });

    $('#cmb_MarcaSubTipo').on('change', function () {

        let idMarcaSubTipo = $(this).find('option:selected').val();

        //console.log("cmb_MarcaSubTipo change idMarcaSubTipo ::: ", idMarcaSubTipo);
        //console.log("cmb_MarcaSubTipo change var_Filtrado ::: ", var_Filtrado);
        if (idMarcaFase === undefined || idMarcaFase < 0) {
            fn_ModalSelecionarFase();
        } else {
            if (idMarcaSubTipo <= 0) {
                fn_Filtrar();
            } else {
                fn_Filtrar();
            }
        }
    });

    $('#chk_PesquisarSemVariante').change(function () {

        // 1. Get the checked status (boolean: true if checked, false otherwise)
        const isChecked = $(this).is(':checked');
        const checkboxValue = $(this).val();

        //console.log("chk_PesquisarSemVariante change ::: ", isChecked);

        fn_Filtrar();
    });

    $('#chk_PesquisarDescricao').change(function () {

        // 1. Get the checked status (boolean: true if checked, false otherwise)
        const isChecked = $(this).is(':checked');
        const checkboxValue = $(this).val();

        //console.log("chk_PesquisarDescricao change ::: ", isChecked);

        let colDesc = varTbl_Data.settings()[0].aoColumns[5]; //Descricao

        colDesc.bSearchable = isChecked;

        varTbl_Data.rows().invalidate().draw();

        $("input[type='search']").trigger("search");
    });
}

function fn_FiltrosLoad() {
    //console.log("fnItemLoadFiltros  ::: ");

    $.busyLoadFull("show");

    $(".card-datatable").hide();

    fn_LoadCmb_MarcaFase();

    fn_LoadCmb_MarcaFabrica();

    fn_LoadCmb_MarcaTipo();

    fn_LoadCmb_MarcaSubTipo(0);

    $.busyLoadFull("hide");
}

//#endregion

//#region GRID

function fn_FiltrarDados() {
    //console.log("bfn_FiltrarDados ::: ");
    var varAjax_UrlController = `${var_Controller}/ListGrid_PorAcervo`,
        varAjax_TypeAction = 'POST',
        varAjax_TypeData = 'JSON',
        varAjax_TypeContent = 'application/json; charset=utf-8';

    var varLang_UrlTranslate = 'https://cdn.datatables.net/plug-ins/1.12.1/i18n/pt-BR.json',

        varCol_Exportar = [1, 2, 5, 6, 7, 8, 9],
        varCol_Ordenacao = [2, 'asc'], //set any columns order asc/desc (NomeMarca)

        varItems_QtdPorPage = 10,
        varItems_DivPage = [5, 10, 25, 50, 75, 100],
        varItems_Row = null,
        varItems_Id = 0;

    $.busyLoadFull("show");

    $('.datatables-basic').DataTable().clear().destroy();

    varTbl_Data = $('.datatables-basic').DataTable({
        processing: true,
        serverSide: true,

        autoWidth: false,
        scrollX: false,

        order: [],

        ajax: {
            url: varAjax_UrlController,
            type: varAjax_TypeAction,
            contentType: varAjax_TypeContent,

            data: function (d) {
                //console.log("param d:: ", d)
                return JSON.stringify({
                    draw: d.draw,
                    start: d.start,
                    length: d.length,

                    search: d.search, // 🔥 OBRIGATÓRIO para server-side

                    order: d.order, // 🔥 OBRIGATÓRIO para ordenação server-side (clique no header)

                    filtros: {
                        marcaAcervoId: parseInt($('#hdMarcaAcervoId').val()) || 0,
                        marcaFaseId: parseInt($('#cmb_MarcaFase').val()) || 0,
                        marcaTipoId: parseInt($('#cmb_MarcaTipo').val()) || 0,
                        marcaSubTipoId: parseInt($('#cmb_MarcaSubTipo').val()) || 0,
                        pesquisarSemVariante: $('#chk_PesquisarSemVariante')[0].checked,
                        pesquisarDescricao: $('#chk_PesquisarDescricao')[0].checked,
                    }
                });
            },

            dataSrc: function (json) {
                //console.log("fn_FiltrarDados json:: ", json);
                return json.data;
            }
        },

        columns: [
            // COLUNA - control (sempre visível — prioridade máxima)
            { data: null, defaultContent: '', className: 'control', orderable: false, width: '30px', responsivePriority: 1 },
            // COLUNA - codigoAceca (2ª a aparecer no mobile)
            {
                data: 'CodigoAceca', className: 'text-center text-nowrap', width: '90px', responsivePriority: 2, orderable: true,
                render: function (data, type, full) {
                    if (!data || full.Id === 0 || type !== 'display') return '';

                    const codigoAceca = data.split('/').join("<br><br>");

                    return codigoAceca;
                }
            },
            // COLUNA - nomeMarca (3ª a aparecer no mobile)
            { data: 'NomeMarca', className: 'text-center', width: '120px' , responsivePriority: 3 },
            // COLUNA - imagem (some primeiro no mobile)
            {
                data: 'ImgPrincipalFull', className: 'text-center', responsivePriority: 10004, orderable: false,
                render: function (data, type, row) {
                    return `<img name="myImg" loading="lazy" class="td-img cmyImg" alt="${row?.CodigoAceca}" src="${data}">`;
                }
            },
            // COLUNA - imagemDetalhe
            {
                data: 'ImgDetalheFull', className: 'text-center', responsivePriority: 10005, orderable: false,
                render: function (data, type, row) {
                    return `<img name="myImg" loading="lazy" class="td-img cmyImg" alt="Detalhe :: ${row?.CodigoAceca}" src="${data}">`;
                }
            },
            // COLUNA - descricao
            { data: 'Descricao', className: 'text-start', responsivePriority: 10006 },
            // COLUNA - fabricaNome
            {
                data: 'NomeFabrica', className: 'text-center', responsivePriority: 10007,
                
                render: function (data, type, full) {
                    data = (data === '' || data === null || data === undefined) ? full.TxtFabrica : data;

                    let nomeFabrica = (data === '' || data === null || data === undefined) ? '' : data?.trim()?.split(/\s+/).join("<br>");
                    //console.log("nomeFabrica ::: ", nomeFabrica);
                    return nomeFabrica;
                }
            },
            // COLUNA - subTipo
            { data: 'SubTipo', className: 'text-center', responsivePriority: 10008 },
            // COLUNA - finalidade
            { data: 'NomeFinalidade', className: 'text-center', responsivePriority: 10009 },
            // COLUNA - nomeFase
            {
                data: 'NomeFase', className: 'text-center text-nowrap', responsivePriority: 10010,
                render: function (data, type, full) {
                    if (!data || full.Id === 0 || type !== 'display') return '';

                    const nomeFase = data?.trim()?.split(/\s+/).join("<br>");

                    return nomeFase;
                }
            },
            // COLUNA - incluidoPor (avatar)
            {
                data: 'IncluidoPor', visible: false, className: 'text-center', responsivePriority: 10010, orderable: false,
                render: function (data, type, full) {
                    if (!data || full.Id === 0 || type !== 'display') return '';
                    var ul = `<ul class="m-0 avatar-group d-flex align-items-center justify-content-center" style="list-style:none;">`;
                    var items = data.split('/').map(function (nome, i) {
                        let pathAvatar = nome == "Aceca" ? `../img/avatars/socio/imgAvatarAceca` : `../img/avatars/${i}`;
                        return `<li class="avatar avatar-lg pull-up" data-bs-toggle="tooltip" data-bs-placement="top"
                            title="${nome}" style="z-index:1;">
                            <img src="${pathAvatar}.png" alt="Avatar" class="rounded-circle">
                        </li>`;
                    }).join('');
                    return ul + items + '</ul>';
                }
            },
            // COLUNA - incluidoPor hidden (filtro)
            { targets: -2, data: 'IncluidoPor', visible: false, responsivePriority: 99 },
            // COLUNA - Ações (sempre visível junto com control)
            {
                //visible: false,
                data: 'Id', targets: -1, searchable: false, orderable: false, responsivePriority: 4,

                render: function (data, type, full, meta) {
                    //console.log("Acao full ::: ", full);
                    let itemId = data;
                    let itemDados = full;
                    let itemObjJson = encodeURIComponent(JSON.stringify(full));

                    let idColecaoStatus = $('#cmb_ColecaoStatus').find('option:selected').val();

                    //console.log("Ações idColecaoStatus ::: ", idColecaoStatus);

                    var btn = '<div class="d-flex align-items-center">';

                    if (full?.possui) {
                        btn += `<a href="javascript:fn_Pop(${itemObjJson});" class="btn btn-sm btn-icon btn-text-secondary rounded-pill waves-effect" data-bs-toggle="tooltip" title="Editar Observação"><i class="ri-edit-box-line ri-22px"></i></a>
                            <a href="javascript:fnItem_Colecao(${itemObjJson},'ColecaoDelete');" class="btn btn-sm btn-icon btn-text-danger rounded-pill waves-effect delete-record" data-bs-toggle="tooltip" title="Remover da Coleção"><i class="ri-delete-bin-7-line ri-22px"></i></a>
                            <a href="javascript:fnItem_Colecao(${itemObjJson},'ColecaoNegociar');" class="btn btn-sm btn-icon btn-text-${(full?.disponivel_negocio ? 'success' : 'secondary')} rounded-pill waves-effect" data-bs-toggle="tooltip" title="Para Negociação"><i class="ri-shopping-cart-2-line ri-22px"></i></a>`
                    };

                    if (idColecaoStatus < 3 && !full?.possui) {
                        // Mesma trava do "Incluir na Coleção": não troca o ícone (ri-eye-line),
                        // só marca como text-success quando o interesse já estiver registrado,
                        // e bloqueia o clique/duplo submit nesse caso.
                        btn += full?.interesse
                            ? `<a href="javascript:void(0);" class="btn btn-sm btn-icon btn-text-success rounded-pill waves-effect colecao-interesse-ja-incluida" data-bs-toggle="tooltip" title="Já marcado como Tenho Interesse"><i class="ri-eye-line ri-22px"></i></a>`
                            : `<a href="javascript:void(0);" class="btn btn-sm btn-icon btn-text-secondary rounded-pill waves-effect btn-colecao-interesse" data-obj="${itemObjJson}" data-bs-toggle="tooltip" title="Tenho Interesse"><i class="ri-eye-line ri-22px"></i></a>`
                    }

                    if (idColecaoStatus < 3 && full?.interesse) {
                        // Mesma trava do "Incluir na Coleção" da listagem do Acervo: ícone vira
                        // ri-archive-2-fill text-success quando já incluído, e bloqueia o clique.
                        btn += full?.possui
                            ? `<a href="javascript:void(0);" class="btn btn-sm btn-icon btn-text-success rounded-pill waves-effect colecao-ja-incluida" data-bs-toggle="tooltip" title="Incluído na Coleção"><i class="ri-archive-2-fill ri-22px"></i></a>`
                            : `<a href="javascript:void(0);" class="btn btn-sm btn-icon btn-text-secondary rounded-pill waves-effect btn-colecao-incluir" data-obj="${itemObjJson}" data-bs-toggle="tooltip" title="Incluir na Coleção"><i class="ri-mail-check-line ri-22px"></i></a>`
                    }
                    //'<a href="javascript:fnItem_Colecao(${itemObjJson},${(idColecaoStatus < 0 ? 'ColecaoInteresse' : 'ColecaoIncluir')});" class="btn btn-sm btn-icon btn-text-${(full?.interesse ? 'success' : 'secondary')} rounded-pill waves-effect" data-bs-toggle="tooltip" title="${(idColecaoStatus < 0 ? 'Tenho Interesse' : 'Incluir na Coleção')}"><i class="${(idColecaoStatus < 0 ? 'ri - eye - line' : 'ri - mail - check - line')} ri-22px"></i></a>' +

                    btn += '</div>'

                    //console.log("Ações btn ::: ", btn);

                    return btn;
                }
            }
        ],

        order: varCol_Ordenacao, // garante base na coluna NomeMarca
        autoWidth: false,
        dom: '<"card-header flex-column flex-md-row"<"head-label text-center"><"dt-action-buttons text-end pt-3 pt-md-0"B>><"row"<"col-sm-12 col-md-6"l><"col-sm-12 col-md-6 d-flex justify-content-center justify-content-md-end"f>>t<"row"<"col-sm-12 col-md-6"i><"col-sm-12 col-md-6"p>>',

        language: {
            url: varLang_UrlTranslate,
            paginate: {
                next: '<i class="ri-arrow-right-s-line"></i>',
                previous: '<i class="ri-arrow-left-s-line"></i>'
            }
        },
        buttons: [
            {
                extend: 'collection',
                className: 'btnExport export-data btn btn-label-primary dropdown-toggle me-4 waves-effect waves-light border-none',
                text: '<i class="ri-external-link-line me-sm-1"></i> <span class="d-none d-sm-inline-block">Exportar</span>',
                buttons: [
                    // BOTAO CABECALHO - EXPORTAR - IMPRIMIR
                    {
                        extend: 'print',
                        text: '<i class="ri-printer-line me-1" ></i>Imprimir',
                        className: 'dropdown-item',
                        exportOptions: {
                            columns: varCol_Exportar,
                        }
                    },
                    // BOTAO CABECALHO - EXPORTAR - CSV
                    {
                        extend: 'csv',
                        text: '<i class="ri-file-text-line me-1" ></i>Csv',
                        className: 'dropdown-item',
                        exportOptions: {
                            columns: varCol_Exportar,
                        }
                    },
                    // BOTAO CABECALHO - EXPORTAR - EXCEL
                    {
                        // extend: 'excel',
                        extend: 'excelHtml5',
                        text: '<i class="ri-file-excel-line me-1"></i>Excel',
                        className: 'dropdown-item',
                        exportOptions: {
                            columns: varCol_Exportar,
                        }
                    },
                    // BOTAO CABECALHO - EXPORTAR - PDF
                    {
                        //extend: 'pdf',
                        extend: "pdfHtml5",
                        text: '<i class="ri-file-pdf-line me-1"></i>PDF',
                        className: 'dropdown-item',
                        orientation: 'landscape',
                        exportOptions: {
                            columns: varCol_Exportar,
                        },
                    },
                    // BOTAO CABECALHO - EXPORTAR - COPIAR
                    {
                        //extend: 'copy',
                        extend: 'copyHtml5',
                        text: '<i class="ri-file-copy-line me-1" ></i>Copiar',
                        className: 'dropdown-item',
                        exportOptions: {
                            columns: varCol_Exportar
                        }
                    }
                ]
            },
           
            {
                text: '<i class="ri-add-line"></i> <span class="d-none d-sm-inline-block">Adicionar Novo</span>',
                className: 'btnAddNew create-new btn btn-primary waves-effect waves-light',
                action: function (e, dt, node, config) {
                    //console.log("BTN NEW ::: ", dt);
                    window.location.href = '/Marca/Cadastro';
                }
            }
            /* */
        ],
        responsive: {
            details: {
                type: 'column',
                target: 0,
                //target: 'tr',
                renderer: function (api, rowIdx, columns) {
                    var row = api.row(rowIdx).data();

                    // ✅ Função de zoom — injeta modal no body na primeira vez
                    if (!document.getElementById('imgZoomModal')) {
                        $('body').append(`<div id="imgZoomModal" style="
                                                    display:none;position:fixed;inset:0;z-index:99999;
                                                    background:rgba(0,0,0,0.85);
                                                    align-items:center;justify-content:center;cursor:pointer;"
                                                    onclick="document.getElementById('imgZoomModal').style.display='none';">
                                                    <div style="position:relative;max-width:92vw;max-height:92vh;">
                                                        <img id="imgZoomTarget" src="" style="
                                                            max-width:92vw;max-height:92vh;
                                                            object-fit:contain;border-radius:8px;
                                                            box-shadow:0 8px 40px rgba(0,0,0,0.6);">
                                                        <button onclick="document.getElementById('imgZoomModal').style.display='none';" style="
                                                            position:absolute;top:-14px;right:-14px;
                                                            width:30px;height:30px;border-radius:50%;
                                                            background:#fff;border:none;cursor:pointer;
                                                            font-size:16px;line-height:1;color:#333;
                                                            display:flex;align-items:center;justify-content:center;
                                                            box-shadow:0 2px 8px rgba(0,0,0,0.3);">&#x2715;</button>
                                                    </div>
                                                </div>
                                            `);

                        // Fecha com ESC
                        $(document).on('keydown.imgZoom', function (e) {
                            if (e.key === 'Escape') {
                                $('#imgZoomModal').css('display', 'none');
                            }
                        });
                    }

                    // ✅ Função global de abertura do zoom
                    window.fn_ZoomImg = function (src) {
                        $('#imgZoomTarget').attr('src', src);
                        $('#imgZoomModal').css('display', 'flex');
                    };

                    // ✅ Imagens clicáveis com cursor pointer e chamada ao zoom
                    var imgPrincipal = row.ImgPrincipalFull
                        ? `<img name="myImg" class="td-img cmyImg" alt="${row.CodigoAceca}" src="${row.ImgPrincipalFull}"
                                    onclick="fn_ZoomImg('${row.ImgPrincipalFull}')" style="width:64px;height:64px;object-fit:cover;border-radius:8px; 
                                    border:0.5px solid #ddd;cursor:pointer;transition:opacity .2s;"
                                    onmouseover="this.style.opacity='.75'" onmouseout="this.style.opacity='1'">`
                        : `<div style="width:64px;height:64px;background:#f4f4f4;border-radius:8px; 
                                    display:flex;align-items:center;justify-content:center;
                                    font-size:11px;color:#aaa;">sem img</div>`;

                    var imgDetalhe = row.ImgDetalheFull
                        ? `<img name="myImg" class="td-img cmyImg" alt="${row.CodigoAceca}" src="${row.ImgDetalheFull}"
                                    onclick="fn_ZoomImg('${row.ImgDetalheFull}')" style="width:64px;height:64px;object-fit:cover;border-radius:8px;
                                    border:0.5px solid #ddd;cursor:pointer;transition:opacity .2s;"
                                    onmouseover="this.style.opacity='.75'" onmouseout="this.style.opacity='1'">`
                        : `<div style="width:64px;height:64px;background:#f4f4f4;border-radius:8px;
                                    display:flex;align-items:center;justify-content:center;
                                    font-size:11px;color:#aaa;">sem detalhe</div>`;

                    // Fábrica
                    var fabrica = (row.TxtFabrica === "" || row.TxtFabrica === null)
                        ? row.NomeFabrica
                        : row.TxtFabrica;

                    // incluidoPor como TEXTO PURO no card mobile
                    var incluidoPorTexto = '';

                    if (row.IncluidoPor) {
                        // Substitui "/" por separador legível
                        incluidoPorTexto = row.IncluidoPor.split('/').join(', ');
                    }

                    // Botão editar
                    var itemObjJson = encodeURIComponent(JSON.stringify(row));
                    var btnEditar = `<a href="javascript:fn_Modal(${itemObjJson},'Edit');"
                                            class="btn btn-sm btn-icon btn-text-secondary waves-effect rounded-pill text-body">
                                            <i class="ri-edit-box-line ri-22px"></i>
                                        </a>`;

                    var card = `<div style="background:#fff;border:0.5px solid #e0e0e0;border-radius:12px;padding:1rem;margin:6px 0;">

                            <!-- ✅ MUDANÇA 1 — Primeira linha: SOMENTE as duas imagens, centralizadas -->
                            <div style="display:flex;justify-content:center;gap:100px;padding-bottom:12px;border-bottom:0.5px solid #eee;margin-bottom:12px;">
                                <div>
                                    <div style="font-size:13px;color:#555;text-transform:uppercase;letter-spacing:.4px;font-weight:400;">Imagem</div>
                                    <div style="font-size:11px;color:#aaa;text-transform:uppercase;letter-spacing:.4px;font-weight:400;">${row.CodigoAceca || ''}</div>
                                    <div>${imgPrincipal || ''}</div>
                                </div>
                                <div>
                                    <div style="font-size:13px;color:#555;text-transform:uppercase;letter-spacing:.4px;font-weight:200;">Detalhe</div>
                                    <div style="font-size:11px;color:#aaa;text-transform:uppercase;letter-spacing:.4px;font-weight:400;">${row.CodigoAceca || ''}</div>
                                    <div>${imgDetalhe || ''}</div>
                                </div>
                            </div>

                <!-- Grid de campos -->
                <!-- ✅label em negrito (font-weight:600), valor em normal (font-weight:400) -->
                <div style="display:grid;grid-template-columns:1fr 1fr;gap:8px 12px;">
                    <div>
                        <div style="font-size:13px;color:#555;text-transform:uppercase;letter-spacing:.4px;font-weight:400;">Fase</div>
                        <div style="font-size:11px;color:#aaa;font-weight:400;">${row.NomeFase || ''}</div>
                    </div>
                    <div>
                        <div style="font-size:11px;color:#555;text-transform:uppercase;letter-spacing:.4px;font-weight:400;">Código ACECA</div>
                        <div style="font-size:13px;color:#aaa;font-weight:400;">${row.CodigoAceca || ''}</div>
                    </div>
                    <div>
                        <div style="font-size:11px;color:#555;text-transform:uppercase;letter-spacing:.4px;font-weight:400;">Finalidade</div>
                        <div style="font-size:13px;color:#aaa;font-weight:400;">${row.NomeFinalidade || ''}</div>
                    </div>                                            
                    <div>
                        <div style="font-size:11px;color:#555;text-transform:uppercase;letter-spacing:.4px;font-weight:400;">SubTipo</div>
                        <div style="font-size:13px;color:#aaa;font-weight:400;">${row.SubTipo || ''}</div>
                    </div>
                    <!--
                    <div>
                        <div style="font-size:11px;color:#555;text-transform:uppercase;letter-spacing:.4px;font-weight:400;">Marca</div>
                        <div style="font-size:13px;color:#aaa;font-weight:400;">${row.NomeMarca || ''}</div>
                    </div>
                    -->
                    <div>
                        <div style="font-size:11px;color:#555;text-transform:uppercase;letter-spacing:.4px;font-weight:400;">Fábrica</div>
                        <div style="font-size:13px;color:#aaa;font-weight:400;">${fabrica || ''}</div>
                    </div>
                    <div>
                        <div style="font-size:11px;color:#555;text-transform:uppercase;letter-spacing:.4px;font-weight:400;">Incluído por</div>
                        <div style="font-size:13px;color:#aaa;font-weight:400;">${incluidoPorTexto || ''}</div>
                    </div>
                </div>

                <!-- Footer: incluidoPor em texto + botão editar -->
                <div style="display:flex;align-items:center;justify-content:space-between;margin-top:10px;padding-top:10px;border-top:0.5px solid #eee;">
                    <div>
                        <div style="font-size:11px;color:#555;text-transform:uppercase;letter-spacing:.4px;margin-bottom:4px;">Marca</div>
                        <div style="font-size:13px;color:#aaa;">${row.NomeMarca}</div>
                    </div>
                </div>

                <!-- Descrição -->
                <div style="margin-top:10px;padding-top:10px;border-top:0.5px solid #eee;">
                    <div style="font-size:11px;color:#555;text-transform:uppercase;letter-spacing:.4px;font-weight:400;margin-bottom:4px;">Descrição</div>
                    <div style="font-size:12px;color:#aaa;line-height:1.5;font-weight:400;">${row.Descricao || ''}</div>
                </div>

                
            </div>`;

                    return $(card);
                }
            },
            breakpoints: [
                { name: 'desktop', width: Infinity },
                { name: 'tablet', width: 1024 },
                { name: 'mobile', width: 768 }  // era 480 — aumentar dá mais espaço
            ]
        },

        drawCallback: function () {
            fn_Zoom();
            fn_LazyLoad();
        },
        initComplete: function (settings, json) {
            //console.log("settings ::: ", settings);
            //console.log("json ::: ", json);

            fn_GridComplete(this);
        }
    });
}

function fn_GridComplete(grid) {

    // var_Filtrado = true;

    var thisApi = grid.api();

    var countRows = grid.api().rows().count();
    //console.log("countRows ::: ", countRows);

    $('.card-header').after('<hr class="my-0">');

    //Titulo Tabela
    let var_MarcaAcervo = document.getElementById('hdMarcaAcervoNome').value;
    $('div.head-label').html(`<h5 class="card-title mb-0">${var_Nome} - ${var_MarcaAcervo}</h5>`);

    $(".card-datatable").show();

    if (countRows > 0) {

        fn_FiltrosShow();

        isPerfil = document.getElementById('hdIsPerfil').value;

        var columnNames = thisApi.columns().header().toArray().map(header => $(header).text());
        //console.log("fn_GridComplete - columnNames ::: ", columnNames);

        if (isPerfil === 'false') {
            console.log("fn_GridComplete - isPerfil ::: ", isPerfil);
            thisApi.column(12).visible(false); // coluna acoes

            // Botao criar
            $(".create-new").attr('style', 'display: none !important');
            $(".btnExport").attr('style', 'display: none !important');
            
        }

        //console.log("fn_GridComplete - idMarcaFase ::: ", idMarcaFase);
        if (idMarcaFase > 0) {
            thisApi.column(9).visible(false); // coluna fase
        }

        fn_Zoom();

        $.busyLoadFull("hide");

    } else {

        $.busyLoadFull("hide");

        Swal.fire({
            title: 'SEM DADOS!!',
            icon: 'info',
            html: `N&atilde;o h&aacute; dados para serem carregados, para o filtro selecionado!!`,
            focusConfirm: false,
            confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
            customClass: {
                confirmButton: 'btn btn-label-secondary waves-effect'
            },
        }).then((result) => {
            $('.card-header').after('<hr class="my-0">');

            //Titulo Tabela
            $('div.head-label').html(`<h5 class="card-title mb-0">${var_Nome}</h5>`);

            $(".card-datatable").show();
        });
    }
}

//#endregion

//#region IMAGENS

//#region ZOOM

function fn_InitZoom() {

    if (document.getElementById('imgZoomModal')) return;

    $('body').append(`
        <div id="imgZoomModal" style="
            display:none;position:fixed;inset:0;
            background:rgba(0,0,0,0.85);
            z-index:99999;
            align-items:center;
            justify-content:center;">
            <img id="imgZoomTarget" style="
                max-width:95vw;
                max-height:95vh;">
        </div>
    `);

    $('#imgZoomModal').click(function () {
        $(this).hide();
    });
}

function fn_ZoomImg(src) {
    $('#imgZoomTarget').attr('src', src);
    $('#imgZoomModal').css('display', 'flex');
}

function fn_ZoomImgClose() {
    const modal = document.getElementById('myModal');

    if (modal) {
        modal.style.display = 'none'; // Hides the div
    }
}

//#endregion

document.addEventListener('hidden.bs.modal', function (event) {
    if (document.activeElement) {
        document.activeElement.blur();
    }
});

function fn_Zoom() {
    //console.log("fn_Zoom ::: ");
    var modal = document.getElementById('myModal');

    var img = document.querySelectorAll(".cmyImg");
    var modalImg = document.getElementById("img01");
    var captionText = document.getElementById("caption");

    $(".cmyImg").click(function () {
        //console.log("cmyImg ::: ", modalImg);
        modal.style.display = "block";
        modalImg.src = this.src;
        modalImg.alt = this.alt;
        captionText.innerHTML = this.alt;
    });

    $("#myModal").click(function () {
        //console.log("myModal ::: ", img01);
        img01.className += " out";
        setTimeout(function () {
            modal.style.display = "none";
            img01.className = "modal-content";
        }, 400);

    });
}

function fn_LazyLoad() {
    const images = document.querySelectorAll('.lazy-img');

    const observer = new IntersectionObserver((entries, obs) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                const img = entry.target;
                img.src = img.dataset.src;
                img.classList.remove('lazy-img');
                obs.unobserve(img);
            }
        });
    });

    images.forEach(img => observer.observe(img));
}

function fn_PreviewImage(input) {
   // console.log("fn_PreviewImage input ::: ", input);
    if (input.files && input.files[0]) {
        const reader = new FileReader();
        reader.onload = function (e) {
            
            //console.log("fn_PreviewImage e ::: ", e);
            //console.log("fn_PreviewImage input id ::: ", input.id);
            if (input.id === 'txt_ImgPrincipal') {
                //document.getElementById('img_ImgPrincipal').src = e.target.result;
                document.getElementById('img_ImgPrincipal').src = e.target.result;
            } else {
                document.getElementById('img_ImgDetalhe').src = e.target.result;
            }
        };
        reader.readAsDataURL(input.files[0]); // Converts to Base64 string
    }
}

//#endregion

//#region COMBO

function fn_PopLoadCombos() {

    //console.log("fn_PopLoadCombos  ::: ");

    fn_LoadCmb_MarcaAcervo();
    fn_LoadCmb_MarcaFase();
    fn_LoadCmb_MarcaFinalidade();
    fn_LoadCmb_MarcaFabrica();
    fn_LoadCmb_MarcaDimensao();
    fn_LoadCmb_MarcaTipo();
    fn_LoadCmb_MarcaSubTipo(0);
    fn_LoadCmb_MarcaImpressora();
    fn_LoadCmb_MarcaQualidadeImagem();

    $('#cmb_MarcaTipo').on('change', function () {
        let idMarcaTipo = $(this).find('option:selected').val();

        //console.log("cmb_MarcaTipo change  idMarcaTipo ::: ", idMarcaTipo);

        //Limpar Combo cinema
        document.querySelectorAll('#cmb_MarcaSubTipo option').forEach(option => option.remove());

        $("#cmb_MarcaSubTipo").append($("<option></option>").val(0).html("-- Selecionar --"));

        if ($(this).length <= 1 && idMarcaTipo > 0) {
            fn_LoadCmb_MarcaSubTipo(idMarcaTipo);
        }
    });
}

let cmbMarcaFaseLoaded = false;

function fn_LoadCmb_MarcaAcervo() {
    // console.log("fn_LoadCmb_MarcaAcervo ::: ");

    if ($('#cmb_MarcaFase').length <= 1) {
        $.ajax(
            {
                crossDomain: true,
                url: `${var_ControllerCmb}/AsyncCmb_MarcaAcervo`,
                type: 'GET',
                success: function (data) {
                    //console.log("fn_LoadCmb_MarcaAcervo  data ::: ", data);

                    $.each(data, function (id, result) {
                        //console.log("fn_LoadCmb_MarcaAcervo  result id ::: ", id);
                        //console.log("fn_LoadCmb_MarcaAcervo  result ::: ", result);
                        $("#cmbPop_MarcaAcervo").append($("<option></option>").val(result.value).html(result.text));
                    });
                },
                error: function (xhr, textStatus, errorThrown) {
                    fn_ModalErro(xhr, textStatus, errorThrown);
                },
            }
        );
    }
}

function fn_LoadCmb_MarcaFase() {

    if (cmbMarcaFaseLoaded) return;

    cmbMarcaFaseLoaded = true;

    const $cmb = $('#cmb_MarcaFase');
    const $cmbPop = $('#cmbPop_MarcaFase');
    const idMarcaAcervo = parseInt($('#hdMarcaAcervoId').val()) || 0;

    // mantém o option original (-1)
    $cmb.prop('disabled', true);

    // filtra somente as fases que possuem itens cadastrados para o acervo selecionado
    let urlLoad = idMarcaAcervo > 0 ? `${var_ControllerCmb}/AsyncCmb_MarcaFaseByAcervo` : `${var_ControllerCmb}/AsyncCmb_MarcaFase`;

    $.ajax({
        url: urlLoad,
        type: 'GET',
        cache: true,
        data: {
            id: idMarcaAcervo,
        },
        success: function (data) {

            strMarcaAcervo = $('#hdMarcaAcervoNome').val();

            // 🔥 mantém o option fixo
            let options = '<option value="-1">-- Selecionar --</option>';

            // opcional: "Todas"
            options += '<option value="0">Todas</option>';

            data.forEach(item => {

                let strComboText = strMarcaAcervo !== "" ? `${strMarcaAcervo?.toUpperCase()} - ${item.text}` : item.text;

                //options += `<option value="${item.value}">${item.text}</option>`;

                options += `<option value="${item.value}">${strComboText}</option>`;
            });

            $cmb.html(options).prop('disabled', false);
            $cmbPop.html(options);
        },
        error: function (xhr, textStatus, errorThrown) {
            cmbMarcaFaseLoaded = false;
            fn_ModalErro(xhr, textStatus, errorThrown);
        }
    });
}

function fn_LoadCmb_MarcaFinalidade() {
    //console.log("fn_LoadCmb_MarcaFinalidade ::: ");

    if ($('#cmbPop_MarcaFinalidade').length <= 1) {

        $.ajax(
            {
                crossDomain: true,
                url: `${var_ControllerCmb}/AsyncCmb_MarcaFinalidade`,
                type: 'GET',
                success: function (data) {
                    //console.log("fn_LoadCmb_MarcaFinalidade  data ::: ", data);

                    $.each(data, function (id, result) {
                        //console.log("fn_LoadCmb_MarcaFinalidade  result id ::: ", id);
                        //console.log("fn_LoadCmb_MarcaFinalidade  result ::: ", result);
                        $("#cmbPop_MarcaFinalidade").append($("<option></option>").val(result.value).html(result.text));
                    });
                },
                error: function (xhr, textStatus, errorThrown) {
                    fn_ModalErro(xhr, textStatus, errorThrown);
                },
            }
        );
    }

    //console.log("fn_LoadCmb_CinemaProgramacao ::: ");
}

function fn_LoadCmb_MarcaFabrica() {
    //console.log("fn_LoadCmb_MarcaFabrica ::: ");

    if ($('#cmb_MarcaFabrica').length <= 1) {
        $.ajax(
            {
                crossDomain: true,
                url: `${var_ControllerCmb}/AsyncCmb_MarcaFabrica`,
                type: 'GET',
                success: function (data) {
                    //console.log("fn_LoadCmb_MarcaFabrica  data ::: ", data);

                    $.each(data, function (id, result) {
                        //console.log("fn_LoadCmb_MarcaFabrica  result id ::: ", id);
                        //console.log("fn_LoadCmb_MarcaFabrica  result ::: ", result);
                        $("#cmb_MarcaFabrica").append($("<option></option>").val(result.value).html(result.text));
                    });
                },
                error: function (xhr, textStatus, errorThrown) {
                    fn_ModalErro(xhr, textStatus, errorThrown);
                },
            }
        );
    }

    if ($('#cmbPop_MarcaFabrica').length <= 1) {
        $.ajax(
            {
                crossDomain: true,
                url: `${var_ControllerCmb}/AsyncCmb_MarcaFabrica`,
                type: 'GET',
                success: function (data) {
                    //console.log("fn_LoadCmb_MarcaFabrica  data ::: ", data);

                    $.each(data, function (id, result) {
                        //console.log("fn_LoadCmb_MarcaFabrica  result id ::: ", id);
                        //console.log("fn_LoadCmb_MarcaFabrica  result ::: ", result);
                        $("#cmbPop_MarcaFabrica").append($("<option></option>").val(result.value).html(result.text));
                    });
                },
                error: function (xhr, textStatus, errorThrown) {
                    fn_ModalErro(xhr, textStatus, errorThrown);
                },
            }
        );
    }
}

function fn_LoadCmb_MarcaDimensao() {
    //console.log("fn_LoadCmb_MarcaDimensao ::: ");

    if ($('#cmbPop_MarcaDimensao').length <= 1) {
        $.ajax(
            {
                crossDomain: true,
                url: `${var_ControllerCmb}/AsyncCmb_MarcaDimensao`,
                type: 'GET',
                success: function (data) {
                    //console.log("fn_LoadCmb_MarcaDimensao  data ::: ", data);

                    $.each(data, function (id, result) {
                        //console.log("fn_LoadCmb_MarcaDimensao  result id ::: ", id);
                        //console.log("fn_LoadCmb_MarcaDimensao  result ::: ", result);
                        $("#cmbPop_MarcaDimensao").append($("<option></option>").val(result.value).html(result.text));
                    });
                },
                error: function (xhr, textStatus, errorThrown) {
                    fn_ModalErro(xhr, textStatus, errorThrown);
                },
            }
        );
    }
}

function fn_LoadCmb_MarcaTipo() {
    //console.log("fn_LoadCmb_MarcaTipo ::: ");

    //console.log("cmb_MarcaTipo change idMarcaFase ::: ", idMarcaFase);

    let method = "AsyncCmb_MarcaTipo"; // (idMarcaFase <= 0 || idMarcaFase === undefined) ? "AsyncCmb_MarcaTipo" : "AsyncCmb_MarcaTipoByFase";
    //console.log("cmb_MarcaTipo change method ::: ", method);

    //console.log("cmb_MarcaTipo change length ::: ", $('#cmb_MarcaTipo option').length);

    //if (idMarcaFase !== undefined) {
    if ($('#cmb_MarcaTipo option').length <= 1) {
            $.ajax(
                {
                    crossDomain: true,
                    url: `${var_ControllerCmb}/${method}`,
                    type: 'GET',
                    data: {
                        id: idMarcaFase,
                    },
                    success: function (data) {
                        //console.log("fn_LoadCmb_MarcaTipo  data ::: ", data);

                        $.each(data, function (id, result) {
                            //console.log("fn_LoadCmb_MarcaTipo  result id ::: ", id);
                            //console.log("fn_LoadCmb_MarcaTipo  result ::: ", result);
                            $("#cmb_MarcaTipo").append($("<option></option>").val(result.value).html(result.text));
                        });
                    },
                    error: function (xhr, textStatus, errorThrown) {
                        fn_ModalErro(xhr, textStatus, errorThrown);
                    },
                }
            );
    }

    if ($('#cmbPop_MarcaTipo option').length <= 1) {
            $.ajax(
                {
                    crossDomain: true,
                    url: `${var_ControllerCmb}/AsyncCmb_MarcaTipo`,
                    type: 'GET',
                    success: function (data) {
                        //console.log("fn_LoadCmb_MarcaTipo  data ::: ", data);

                        $.each(data, function (id, result) {
                            //console.log("fn_LoadCmb_MarcaTipo  result id ::: ", id);
                            //console.log("fn_LoadCmb_MarcaTipo  result ::: ", result);
                            $("#cmbPop_MarcaTipo").append($("<option></option>").val(result.value).html(result.text));
                        });
                    },
                    error: function (xhr, textStatus, errorThrown) {
                        fn_ModalErro(xhr, textStatus, errorThrown);
                    },
                }
            );
            }
   // }
}

function fn_LoadCmb_MarcaSubTipo(idMarcaTipo) {

    //console.log("fn_LoadCmb_MarcaSubTipo  idMarcaTipo ::: ", idMarcaTipo);

    let urlLoad = idMarcaTipo > 0 ? `${var_ControllerCmb}/AsyncCmb_MarcaSubTipoByTipo` : `${var_ControllerCmb}/AsyncCmb_MarcaSubTipo`;

    if ($('#cmb_MarcaSubTipo').length <= 1) {

        $.ajax(
            {
                crossDomain: true,
                url: urlLoad,
                type: 'GET',
                data: {
                    id: idMarcaTipo,
                },
                success: function (data) {
                    //console.log("fn_LoadCmb_MarcaSubTipo  data ::: ", data);

                    $.each(data, function (id, result) {
                        //console.log("fn_LoadCmb_MarcaSubTipo  result id ::: ", id);
                        //console.log("fn_LoadCmb_MarcaSubTipo  result ::: ", result);
                        $("#cmb_MarcaSubTipo").append($("<option></option>").val(result.value).html(result.text));
                    });
                },
                error: function (xhr, textStatus, errorThrown) {
                    fn_ModalErro(xhr, textStatus, errorThrown);
                },
            }
        );
    }

    if ($('#cmbPop_MarcaSubTipo').length <= 1) {

        $.ajax(
            {
                crossDomain: true,
                url: urlLoad,
                type: 'GET',
                data: {
                    id: idMarcaTipo,
                },
                success: function (data) {
                    //console.log("fn_LoadCmb_MarcaSubTipo  data ::: ", data);

                    $.each(data, function (id, result) {
                        //console.log("fn_LoadCmb_MarcaSubTipo  result id ::: ", id);
                        //console.log("fn_LoadCmb_MarcaSubTipo  result ::: ", result);
                        $("#cmbPop_MarcaSubTipo").append($("<option></option>").val(result.value).html(result.text));
                    });
                },
                error: function (xhr, textStatus, errorThrown) {
                    fn_ModalErro(xhr, textStatus, errorThrown);
                },
            }
        );
    }
}

function fn_LoadCmb_MarcaImpressora() {
    //console.log("fn_LoadCmb_MarcaImpressora ::: ");

    if ($('#cmbPop_MarcaImpressora').length <= 1) {
        $.ajax(
            {
                crossDomain: true,
                url: `${var_ControllerCmb}/AsyncCmb_MarcaImpressora`,
                type: 'GET',
                success: function (data) {
                    //console.log("fn_LoadCmb_MarcaImpressora  data ::: ", data);

                    $.each(data, function (id, result) {
                        //console.log("fn_LoadCmb_MarcaImpressora  result id ::: ", id);
                        //console.log("fn_LoadCmb_MarcaImpressora  result ::: ", result);
                        $("#cmbPop_MarcaImpressora").append($("<option></option>").val(result.value).html(result.text));
                    });
                },
                error: function (xhr, textStatus, errorThrown) {
                    fn_ModalErro(xhr, textStatus, errorThrown);
                },
            }
        );
    }
}

function fn_LoadCmb_MarcaQualidadeImagem() {
    //console.log("fn_LoadCmb_MarcaQualidadeImagem ::: ");

    if ($('#cmbPop_MarcaQualidadeImagem').length <= 1) {
        $.ajax(
            {
                crossDomain: true,
                url: `${var_ControllerCmb}/AsyncCmb_MarcaQualidadeImagem`,
                type: 'GET',
                success: function (data) {
                    //console.log("fn_LoadCmb_MarcaQualidadeImagem  data ::: ", data);

                    $.each(data, function (id, result) {
                        //console.log("fn_LoadCmb_MarcaQualidadeImagem  result id ::: ", id);
                        //console.log("fn_LoadCmb_MarcaQualidadeImagem  result ::: ", result);
                        $("#cmbPop_MarcaQualidadeImagem").append($("<option></option>").val(result.value).html(result.text));
                    });
                },
                error: function (xhr, textStatus, errorThrown) {
                    fn_ModalErro(xhr, textStatus, errorThrown);
                },
            }
        );
    }
}

///

//#endregion

//#region MODAL

function fn_ModalConfirmarFiltros() {
    Swal.fire({
        title: 'Aten&ccedil;&atilde;o !!!',
        html: `Para confirmar a op&ccedil;&atilde;o, <br><br> clique no bot&atilde;o Pesquisar.<br><br> Caso prefira, utilize as op&ccedil;&otilde;es de filtros dispon&iacute;veis!`,
        imageUrl: `${urlImgModaltext}`,
        imageWidth: 400,
        imageAlt: `${var_ImgAlt}`,
        focusConfirm: false,
        confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
        customClass: {
            confirmButton: 'btn btn-primary waves-effect waves-light'
        },
    }).then((result) => {
        //console.log("cmb_MarcaFase change result ::: ", result);
    })
}

function fn_ModalSelecionarFase() {
    Swal.fire({
        title: 'Aten&ccedil;&atilde;o !!!',
        html: `Para utilizar essa op&ccedil;&atilde;o, <br><br> é necessário selecionar uma Fase!`,
        imageUrl: `${urlImgModaltext}`,
        imageWidth: 400,
        imageAlt: `${var_ImgAlt}`,
        focusConfirm: false,
        confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
        customClass: {
            confirmButton: 'btn btn-primary waves-effect waves-light'
        },
    }).then((result) => {

    })
}

function fn_ModalErro(xhr, textStatus, errorThrown) {
    const responseMessage = xhr.responseText;
    console.log("Server Response:", responseMessage);

    const objError = JSON.parse(xhr.responseText);
    //console.log("Server msg:", obj.message);

    console.log("XMLHttpRequest  :: ", xhr);
    console.log("textStatus  :: ", textStatus);
    console.log("errorThrown  :: ", errorThrown);
    console.log("result  :: Error while posting SendResult");

    $.busyLoadFull("hide");

    Swal.fire({
        title: 'OPS!!',
        icon: 'error',
        html: `<b> Erro ocorrido <br><br>${objError.message}</b>`,
        focusConfirm: false,
        confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
        customClass: {
            confirmButton: 'btn btn-label-danger waves-effect'
        }
    });
}

function fn_Modal(obj, action) {
    //console.log("fn_Modal obj ::: ", obj);
    //console.log("fn_Modal action::: ", action);

    const popAddNewItem = document.querySelector('#ModalMarca');

    // Pop ID
    (popAddNewItem.querySelector('#hdMarcaId').value = (obj === null ? 0 : obj.Id)),
        (popAddNewItem.querySelector('#hdMarcaAcervoId').value = (obj === null ? 0 : obj.IdMarcaAcervo)),
        (popAddNewItem.querySelector('#hdMarcaFaseId').value = (obj === null ? 0 : obj.IdMarcaFase)),
        (popAddNewItem.querySelector('#hdMarcaFinalidadeId').value = (obj === null ? 0 : obj.IdMarcaFinalidade)),
        (popAddNewItem.querySelector('#hdMarcaFabricaId').value = (obj === null ? 0 : obj.IdMarcaFabrica)),
        (popAddNewItem.querySelector('#hdMarcaDimensaoId').value = (obj === null ? 0 : obj.IdMarcaDimensao)),
        (popAddNewItem.querySelector('#hdMarcaTipoId').value = (obj === null ? 0 : obj.IdMarcaTipo)),
        (popAddNewItem.querySelector('#hdMarcaSubTipoId').value = (obj === null ? 0 : obj.IdMarcaSubTipo)),
        (popAddNewItem.querySelector('#hdMarcaImpressoraId').value = (obj === null ? 0 : obj.IdMarcaImpressora)),
        (popAddNewItem.querySelector('#hdMarcaQualidadeImagemId').value = (obj === null ? 0 : obj.IdMarcaQualidadeImagem)),

    // Pop Dados
        (popAddNewItem.querySelector('#cmbPop_MarcaAcervo').value = (obj === null ? '-1' : ((obj.IdMarcaAcervo === undefined || obj.IdMarcaAcervo === null || obj.IdMarcaAcervo <= 0) ? '-1' : obj.IdMarcaAcervo)));
     //   (popAddNewItem.querySelector('#cmbPop_MarcaFase').value = (obj === null ? '-1' : ((obj.IdMarcaFase === undefined || obj.IdMarcaFase === null || obj.IdMarcaFase <= 0) ? '-1' : obj.IdMarcaFase)));
    (popAddNewItem.querySelector('#txt_Codigo').value = (obj === null ? '' : obj.CodigoAceca));
    (popAddNewItem.querySelector('#txt_Nome').value = (obj === null ? '' : obj.NomeMarca));
    (popAddNewItem.querySelector('#txt_NomeFase').value = (obj === null ? '' : `${obj.NomeAcervo?.toUpperCase()} - ${obj.NomeFase?.toUpperCase()}`));
    (popAddNewItem.querySelector('#txt_IncluidoPor').value = (obj === null ? '' : obj.IncluidoPor)),
    (popAddNewItem.querySelector('#cmbPop_MarcaFinalidade').value = (obj === null ? '-1' : ((obj.IdMarcaFinalidade === undefined || obj.IdMarcaFinalidade === null || obj.IdMarcaFinalidade <= 0) ? '-1' : obj.IdMarcaFinalidade)));
    (popAddNewItem.querySelector('#cmbPop_MarcaFabrica').value = (obj === null ? '-1' : ((obj.IdMarcaFabrica === undefined || obj.IdMarcaFabrica === null || obj.IdMarcaFabrica <= 0) ? '-1' : obj.IdMarcaFabrica)));
    (popAddNewItem.querySelector('#cmbPop_MarcaDimensao').value = (obj === null ? '-1' : ((obj.IdMarcaDimensao === undefined || obj.IdMarcaDimensao === null || obj.IdMarcaDimensao <= 0) ? '-1' : obj.IdMarcaDimensao)));
    (popAddNewItem.querySelector('#cmbPop_MarcaTipo').value = (obj === null ? '-1' : ((obj.IdMarcaTipo === undefined || obj.IdMarcaTipo === null || obj.IdMarcaTipo <= 0) ? '-1' : obj.IdMarcaTipo)));
    (popAddNewItem.querySelector('#cmbPop_MarcaSubTipo').value = (obj === null ? '-1' : ((obj.IdMarcaSubTipo === undefined || obj.IdMarcaSubTipo === null || obj.IdMarcaSubTipo <= 0) ? '-1' : obj.IdMarcaSubTipo)));
    (popAddNewItem.querySelector('#cmbPop_MarcaImpressora').value = (obj === null ? '-1' : ((obj.IdMarcaImpressora === undefined || obj.IdMarcaImpressora === null || obj.IdMarcaImpressora <= 0) ? '-1' : obj.IdMarcaImpressora)));
    (popAddNewItem.querySelector('#cmbPop_MarcaQualidadeImagem').value = (obj === null ? '-1' : ((obj.IdQualidadeImagem === undefined || obj.IdQualidadeImagem === null || obj.IdQualidadeImagem <= 0) ? '-1' : obj.IdQualidadeImagem)));
    (popAddNewItem.querySelector('#txt_Descricao').value = (obj === null ? '' : obj.Descricao));

    //Pop Valores
    (obj.Valor !== null || obj.Valor1PI !== null || obj.Valor2PI !== null) ? $('.div_adicional').show() : $('.div_adicional').hide();
    (document.querySelector('#txt_Valor').value = (obj === null ? '' : obj.Valor));
    (document.querySelector('#txt_Valor1PI').value = (obj === null ? '' : obj.Valor1PI));
    (document.querySelector('#txt_Valor2PI').value = (obj === null ? '' : obj.Valor2PI));

    // Pop Action
    (popAddNewItem.querySelector('.address-title').textContent = (action === 'Edit') ? 'Alterar Registro' : 'Novo Registro');
    (popAddNewItem.querySelector('.data-submit').textContent = (action === 'Edit') ? 'Alterar' : 'Adicionar');

    // console.log("fn_Modal resultLoad::: ", resultLoad);
    $.busyLoadFull("hide");

    if (obj !== null) {

        //console.log("fn_Modal obj::: ", obj);
        $("#cmbPop_MarcaAcervo").val(((obj.IdMarcaAcervo === undefined || obj.IdMarcaAcervo === null || obj.IdMarcaAcervo <= 0) ? '-1' : obj.IdMarcaAcervo)).change();
        //$("#cmbPop_MarcaFase").val(((obj.IdMarcaFase === undefined || obj.IdMarcaFase === null || obj.IdMarcaFase <= 0) ? '-1' : obj.IdMarcaFase)).change();
        $("#cmbPop_MarcaFinalidade").val(((obj.IdMarcaFinalidade === undefined || obj.IdMarcaFinalidade === null || obj.IdMarcaFinalidade <= 0) ? '-1' : obj.IdMarcaFinalidade)).change();
        $("#cmbPop_MarcaFabrica").val(((obj.IdMarcaFabrica === undefined || obj.IdMarcaFabrica === null || obj.IdMarcaFabrica <= 0) ? '-1' : obj.IdMarcaFabrica)).change();
        $("#cmbPop_MarcaDimensao").val(((obj.IdMarcaDimensao === undefined || obj.IdMarcaDimensao === null || obj.IdMarcaDimensao <= 0) ? '-1' : obj.IdMarcaDimensao)).change();
        $("#cmbPop_MarcaTipo").val(((obj.IdMarcaTipo === undefined || obj.IdMarcaTipo === null || obj.IdMarcaTipo <= 0) ? '-1' : obj.IdMarcaTipo)).change();
        $("#cmbPop_MarcaSubTipo").val(((obj.IdMarcaSubTipo === undefined || obj.IdMarcaSubTipo === null || obj.IdMarcaSubTipo <= 0) ? '-1' : obj.IdMarcaSubTipo)).change();
        $("#cmbPop_MarcaImpressora").val(((obj.IdMarcaImpressora === undefined || obj.IdMarcaImpressora === null || obj.IdMarcaImpressora <= 0) ? '-1' : obj.IdMarcaImpressora)).change();
        $("#cmbPop_MarcaQualidadeImagem").val(((obj.IdQualidadeImagem === undefined || obj.IdQualidadeImagem === null || obj.IdQualidadeImagem <= 0) ? '-1' : obj.IdQualidadeImagem)).change();
        
        //Pop Arquivos
        //(document.querySelector('#txt_ImgPrincipal').value = '');
        //(document.querySelector('#txt_ImgDetalhe').value = '');

        (obj === null || obj?.imgPrincipal === null) ? (popAddNewItem.querySelector('#txt_ImgPrincipal').value = '') : fnItem_PopImgPrincipal(obj);
        (obj === null || obj?.imgDetalhe === null) ? (popAddNewItem.querySelector('#txt_ImgDetalhe').value = '') : fnItem_PopImgDetalhe(obj);
    }

    objModalData = obj;

    $('#hdMarcaId').val(obj.Id);

    //console.log("fn_Modal objModalData !", objModalData);

    $('#ModalMarca').modal('show');
}

function fnItem_PopImgPrincipal(obj) {
    //console.log("fnItem_PopImgPrincipal obj :::", obj);

    if (obj !== null) {

        const img = document.getElementById('img_ImgPrincipal');

        let imgName = obj?.ImgPrincipal,
            imgNameFul = obj?.ImgPrincipalFull;

        //console.log("fnItem_PopImgPrincipal imgName :::", imgName);
        //console.log("fnItem_PopImgPrincipal imgNameFul :::", imgNameFul);

        if (imgName === null || imgName === undefined) {
            
            img.src = strUrlImgInexistente;
            img.alt = "Imagem Inexistente";

        } else{
            let objFile = {},
                fileArq = imgName;

            //preview img            
            img.src = obj?.ImgPrincipalFull !== null ? obj?.ImgPrincipalFull : strUrlImgInexistente;
            img.alt = obj?.CodigoAceca !== null ? obj?.CodigoAceca : "Imagem Inexistente";

            //
            const fileInput = document.querySelector('#txt_ImgPrincipal');

            if (fileArq !== null && fileArq !== undefined) {
                objFile = {
                    NomeArquivo: fileArq.split('.')[0],
                    Extensao: fileArq.split('.').pop(),
                };
            }

            // Create a new File object
            const arqFile = new File(['ARQUIVO'], `${objFile.NomeArquivo}.${objFile.Extensao}`, {
                type: `application/${objFile.Extensao}`,
                //type: 'text/plain',
                lastModified: new Date(),
            });

            // Now let's create a DataTransfer to get a FileList
            const dataTransfer = new DataTransfer();
            dataTransfer.items.add(arqFile);
            fileInput.files = dataTransfer.files;

            // Help Safari out
            if (fileInput.webkitEntries.length) {
                fileInput.dataset.file = `${dataTransfer.files[0].name}`;
            }
        }
    }
}

function fnItem_PopImgDetalhe(obj) {
    //console.log("fnItem_PopImgDetalhe obj !", obj);

    if (obj !== null) {

        const img = document.getElementById('img_ImgDetalhe');

        let imgName = obj?.ImgDetalhe,
            imgNameFul = obj?.ImgDetalheFull;

        if (imgName === null || imgName === undefined) {

            img.src = strUrlImgInexistente;
            img.alt = "Imagem Inexistente";

        } else {
            let objFile = {},
                fileArq = imgName;

            //preview img            
            img.src = obj?.ImgDetalheFull !== null ? obj?.ImgDetalheFull : strUrlImgInexistente;
            img.alt = obj?.CodigoAceca !== null ? obj?.CodigoAceca : "Imagem Inexistente";

            //
            const fileInput = document.querySelector('#txt_ImgDetalhe');

            if (fileArq !== null && fileArq !== undefined) {
                objFile = {
                    NomeArquivo: fileArq.split('.')[0],
                    Extensao: fileArq.split('.').pop(),
                };
            }

            // Create a new File object
            const arqFile = new File(['ARQUIVO'], `${objFile.NomeArquivo}.${objFile.Extensao}`, {
                type: `application/${objFile.Extensao}`,
                //type: 'text/plain',
                lastModified: new Date(),
            });

            // Now let's create a DataTransfer to get a FileList
            const dataTransfer = new DataTransfer();
            dataTransfer.items.add(arqFile);
            fileInput.files = dataTransfer.files;

            // Help Safari out
            if (fileInput.webkitEntries.length) {
                fileInput.dataset.file = `${dataTransfer.files[0].name}`;
            }
            //}
        }
    }
}

function fn_ModalGetObj(data, action) {
    //console.log("fn_ModalGetObj data ::: ", data);
    //console.log("fn_ModalGetObj action ::: ", action);

    //console.log("fn_ModalGetObj objModalData ::: ", objModalData);

    var loadCmbs = fn_PopLoadCombos();
    //console.log("fn_ModalGetObj hdMarcaId ::: ", $('#hdMarcaId').val());
    //console.log("fn_ModalGetObj hdMarcaFaseId ::: ", $('#hdMarcaFaseId').val());

    const objFormData = {
        Id: $('#hdMarcaId').val(),
        IdMarcaAcervo: $('#hdMarcaAcervoId').val(),
        IdMarcaFase: $('#hdMarcaFaseId').val(),
        IdMarcaFinalidade: $('#hdMarcaFinalidadeId').val(),
        IdMarcaFabrica: $('#hdMarcaFabricaId').val(),
        IdMarcaDimensao: $('#hdMarcaDimensaoId').val(),
        IdMarcaTipo: $('#hdMarcaTipoId').val(),
        IdMarcaSubTipo: $('#hdMarcaSubTipoId').val(),
        IdMarcaImpressora: $('#hdMarcaImpressoraId').val(),
        IdMarcaQualidadeImagem: $('#hdMarcaQualidadeImagemId').val(),

        MarcaFaseId: $('#cmbPop_MarcaFase').val(),
        CodigoAceca: $('#txt_Codigo').val(),
        Nome: $('#txt_Nome').val(),
        IncluidoPor: $('#txt_IncluidoPor').val(),
        MarcaFinalidadeId: $('#cmbPop_MarcaFinalidade').val(),
        MarcaFabricaId: $('#cmbPop_MarcaFabrica').val(),
        MarcaDimensaoId: $('#cmbPop_MarcaDimensao').val(),
        MarcaTipoId: $('#cmbPop_MarcaTipo').val(),
        MarcaSubTipoId: $('#cmbPop_MarcaSubTipo').val(),
        MarcaImpressoraId: $('#cmbPop_MarcaImpressora').val(),
        MarcaQualidadeImagemId: $('#cmbPop_MarcaQualidadeImagem').val(),
        Descricao: $('#txt_Descricao').val(),

        Valor: $('#txt_Valor').val(),
        Valor1PI: $('#txt_Valor1PI').val(),
        Valor2PI: $('#txt_Valor2PI').val(),

        ImgPrincipal: $('#txt_ImgPrincipal').val(),
        ImgDetalhe: $('#txt_ImgDetalhe').val(),
    };

    console.log("fn_ModalGetObj !", objFormData);

    return objFormData;
}

function fnItem_Edit(varItems_Row) {
    //console.log("fnItem_Edit CLICK ::: ", varItems_Row);
    //var varPop_BtnAction = 'Edit';

    var varAjax_UrlController = `${var_Controller}/Edit`,
        varAjax_TypeAction = 'POST',
        varAjax_TypeData = 'JSON',
        varAjax_TypeContent = 'application/json; charset=utf-8';

    if (varItems_Row.Id === 0) {
        Swal.fire({
            title: 'OPS!!',
            icon: 'error',
            html: `Dados n&atilde;o identificados !!`,
            focusConfirm: false,
            confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
            customClass: {
                confirmButton: 'btn btn-label-danger waves-effect'
            },
        });
    } else {

        $.busyLoadFull("show");

        $.ajax(
            {
                url: varAjax_UrlController,
                type: varAjax_TypeAction,
                dataType: varAjax_TypeData,
                data: varItems_Row,
                // contentType: varAjax_TypeContent,
                success: function (result) {

                    //console.log("fnItem_Edit result  :: ", result);

                    if (result.bResult) {

                        $.busyLoadFull("hide");

                        var varTbl;

                        if ($.fn.dataTable.isDataTable('.datatables-basic')) {
                            //console.log("YES :: ");
                            varTbl = varTbl_Obj.DataTable();

                            $.busyLoadFull("hide");

                            Swal.fire({
                                title: 'Dados Salvos!',
                                icon: 'success',
                                text: 'Item alterado com sucesso.',
                                customClass: {
                                    confirmButton: 'btn btn-success waves-effect waves-light'
                                }
                            }).then((result) => {

                                //console.log("RESULTADO  :: ", result);
                                //varTbl.ajax.reload(lstData, false);

                                fn_FiltrarDados(objFiltro);

                                $('#ModalMarca').modal('hide');
                            });
                        } else {
                            console.log("NO :: ");
                        }
                    } else {
                        console.log("result  :: ", result);

                        $.busyLoadFull("hide");

                        Swal.fire({
                            title: 'OPS!!',
                            icon: 'error',
                            html: `<b> fn_Salvar - Erro ocorrido <br><br>` + result + `</b>`,
                            focusConfirm: false,
                            confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                            customClass: {
                                confirmButton: 'btn btn-label-danger waves-effect'
                            }
                        });

                        return false;
                    }
                },
                error: function (xhr, textStatus, errorThrown) {

                    fn_ModalErro(xhr, textStatus, errorThrown);

                    return false;
                },
            });
    }
}

//#endregion

//#region COLECAO

function fnItem_Colecao(obj, action, $btnEl) {

    let marcaId = obj?.Id;
    let actionId = -1;
    const socioId = document.getElementById('hdSocioLogadoId').value;

    //console.log("fnItem_Colecao obj ::: ", obj);
    //console.log("fnItem_Colecao action::: ", action);

    // Guarda contra clique em item que já está na coleção (proteção extra além do
    // bloqueio visual/estrutural do render - cobre estado desatualizado no DOM).
    if (action === 'ColecaoIncluir' && obj?.possui) {
        if ($btnEl && $btnEl.length) $btnEl.removeData('processing');

        Swal.fire({
            title: 'Item já incluído!',
            icon: 'info',
            html: `<b>Este item j&aacute; faz parte da sua Cole&ccedil;&atilde;o.</b>`,
            focusConfirm: false,
            confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
            customClass: {
                confirmButton: 'btn btn-label-info waves-effect'
            }
        });

        return;
    }

    // Mesma guarda para "Tenho Interesse".
    if (action === 'ColecaoInteresse' && obj?.interesse) {
        if ($btnEl && $btnEl.length) $btnEl.removeData('processing');

        Swal.fire({
            title: 'Item já marcado!',
            icon: 'info',
            html: `<b>Este item j&aacute; est&aacute; marcado como "Tenho Interesse" na sua Cole&ccedil;&atilde;o.</b>`,
            focusConfirm: false,
            confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
            customClass: {
                confirmButton: 'btn btn-label-info waves-effect'
            }
        });

        return;
    }

    if ((marcaId === undefined || marcaId === null || marcaId === '' || marcaId < 1)
        || (socioId === undefined || socioId === null || socioId === '' || socioId < 1)
    ) {
        if ($btnEl && $btnEl.length) $btnEl.removeData('processing');

        Swal.fire({
            title: 'Dados Inv&aacute;lidos!!',
            icon: 'error',
            html: `<b>Os dados n&atilde;o foram informados corretamente!!!</b>`,
            focusConfirm: false,
            confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
            customClass: {
                confirmButton: 'btn btn-label-danger waves-effect'
            }
        }).then((result) => {
            //fn_Limpar();
        });
    } else {
        switch (action) {
            case 'ColecaoDelete':
                actionId = 0;
                break;
            case 'ColecaoIncluir':
                actionId = 1;
                break;
            case 'ColecaoInteresse':
                actionId = 2;
                break;
            case 'ColecaoNegociar':
                actionId = 3;
                break;
            default:
                actionId = -1;
        }

        $.busyLoadFull("show");

        $.ajax({
            url: `/SocioColecao/ActionColecao`,
            type: 'POST',
            data: {
                itemColecaoId: 0,
                marcaId: marcaId,
                actionId: actionId,
                socioId: socioId,
                isPerfil: document.getElementById('hdIsPerfil').value
            },
            success: function (response) {

                console.log("Data received: ", response);

                $.busyLoadFull("hide");

                // Atualiza o botão imediatamente (não espera o fechamento do Swal nem o
                // reload da grid) para já travar o item contra novo clique/duplicidade.
                if (action === 'ColecaoIncluir' && $btnEl && $btnEl.length) {
                    fn_MarcarComoIncluidoNaColecao($btnEl);
                }

                if (action === 'ColecaoInteresse' && $btnEl && $btnEl.length) {
                    fn_MarcarComoInteresseNaColecao($btnEl);
                }

                Swal.fire({
                    title: 'Dados Salvos!',
                    icon: 'success',
                    text: 'Coleção atualizada com sucesso.',
                    customClass: {
                        confirmButton: 'btn btn-success waves-effect waves-light'
                    }
                }).then((resultSucesso) => {
                    //window.location.reload();
                    console.log("resultSucesso  :: ", resultSucesso);

                    // "table" (variável de topo removida) referenciava a instância criada
                    // antes de fn_FiltrarDados existir - ficava órfã após o grid real ser
                    // (re)criado com serverSide/ajax e derrubava o reload.
                    varTbl_Data.ajax.reload(null, false);
                });

                return true;
            },
            error: function (xhr, status, error) {
                console.error("Error: " + error);

                $.busyLoadFull("hide");

                if ($btnEl && $btnEl.length) $btnEl.removeData('processing');

                fn_ModalErro(xhr, status, error);
            }
        });

    }
}

// Troca o botão de "Incluir na Coleção" para o estado "já incluído", trocando o
// ícone para ri-archive-2-fill text-success e travando cliques futuros.
function fn_MarcarComoIncluidoNaColecao($btnEl) {
    $btnEl
        .removeClass('btn-colecao-incluir btn-text-secondary')
        .addClass('colecao-ja-incluida btn-text-success')
        .attr('href', 'javascript:void(0);')
        .removeAttr('data-obj')
        .removeData('processing')
        .attr('title', 'Incluído na Coleção')
        .find('i')
        .removeClass('ri-mail-check-line')
        .addClass('ri-archive-2-fill');
}

// Troca o botão de "Tenho Interesse" para o estado "já marcado" - o ícone
// (ri-eye-line) não muda, só ganha a classe btn-text-success.
function fn_MarcarComoInteresseNaColecao($btnEl) {
    $btnEl
        .removeClass('btn-colecao-interesse btn-text-secondary')
        .addClass('colecao-interesse-ja-incluida btn-text-success')
        .attr('href', 'javascript:void(0);')
        .removeAttr('data-obj')
        .removeData('processing')
        .attr('title', 'Já marcado como Tenho Interesse');
}

// Delegado (elementos são recriados a cada redraw do DataTable) - registrado uma
// única vez para os dois estados de "Incluir na Coleção" e "Tenho Interesse".
function fn_BindColecaoIncluirActions() {
    $(document).on('click', '.colecao-ja-incluida', function (e) {
        e.preventDefault();

        Swal.fire({
            title: 'Item já incluído!',
            icon: 'info',
            html: `<b>Este item j&aacute; faz parte da sua Cole&ccedil;&atilde;o.</b>`,
            focusConfirm: false,
            confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
            customClass: {
                confirmButton: 'btn btn-label-info waves-effect'
            }
        });
    });

    $(document).on('click', '.btn-colecao-incluir', function (e) {
        e.preventDefault();

        const $btn = $(this);

        // Trava contra duplo clique/duplo submit enquanto o AJAX ainda não respondeu.
        if ($btn.data('processing')) return;

        $btn.data('processing', true);

        let obj;

        try {
            obj = JSON.parse(decodeURIComponent($btn.attr('data-obj')));
        } catch (ex) {
            $btn.removeData('processing');
            return;
        }

        fnItem_Colecao(obj, 'ColecaoIncluir', $btn);
    });

    $(document).on('click', '.colecao-interesse-ja-incluida', function (e) {
        e.preventDefault();

        Swal.fire({
            title: 'Item já marcado!',
            icon: 'info',
            html: `<b>Este item j&aacute; est&aacute; marcado como "Tenho Interesse" na sua Cole&ccedil;&atilde;o.</b>`,
            focusConfirm: false,
            confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
            customClass: {
                confirmButton: 'btn btn-label-info waves-effect'
            }
        });
    });

    $(document).on('click', '.btn-colecao-interesse', function (e) {
        e.preventDefault();

        const $btn = $(this);

        // Trava contra duplo clique/duplo submit enquanto o AJAX ainda não respondeu.
        if ($btn.data('processing')) return;

        $btn.data('processing', true);

        let obj;

        try {
            obj = JSON.parse(decodeURIComponent($btn.attr('data-obj')));
        } catch (ex) {
            $btn.removeData('processing');
            return;
        }

        fnItem_Colecao(obj, 'ColecaoInteresse', $btn);
    });
}

//#endregion
