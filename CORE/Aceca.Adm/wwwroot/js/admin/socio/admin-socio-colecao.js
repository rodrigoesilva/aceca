/**
 * Socio -> Colecao
 */

'use strict';

//#region Declare

let isPerfil = document.getElementById('hdIsPerfil').value;

let var_Nome = 'Cole&ccedil;&atilde;o',
    var_Controller = '/SocioColecao',
    var_ControllerCmb = '/HelperExtensions',

    varTbl_Obj = $('.datatables-basic'),
    varTbl_Data,
    objFiltro,

    formValid, popAddNewItemEl;

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

            var objFormData = fn_PopGetObj();
            //console.log("objFormData ::: ", objFormData);

            fnItem_Colecao(objFormData, 'ColecaoObs');

            //fnItem_Edit(objFormData,, 'ColecaoObs')
        });

        // Carrega Dados Combos Modal
        //fn_PopLoadCombos();

        fn_Zoom();

        document.addEventListener('keydown', (event) => {
            if (event.key === 'Escape' || event.key === 'Enter') {
                //console.log("Esc ::: ");
                fn_ZoomImgClose();
            }
        });

        // Carrega dados somente após sessão garantida (hdSocioLogadoId preenchido)
        fn_AuthSession(fn_FiltrarDados);
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
        param_ColecaoStatusId: $('#cmb_ColecaoStatus').find('option:selected').val(),
        param_MarcaFaseId: $('#cmb_MarcaFase').find('option:selected').val(),
        param_MarcaTipoId: $('#cmb_MarcaTipo').find('option:selected').val(),
        param_MarcaSubTipoId: $('#cmb_MarcaSubTipo').find('option:selected').val(),
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
        && objFiltro.param_ColecaoStatusId < 0
        && objFiltro.param_MarcaFaseId < 0
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
        fn_FiltrarDados();
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

    $('#cmb_ColecaoStatus').on('change', function () {

        let idColecaoStatus = $(this).find('option:selected').val();

        //console.log("cmb_ColecaoStatus change idColecaoStatus ::: ", idColecaoStatus);
        //console.log("cmb_ColecaoStatus change idMarcaFase ::: ", idMarcaFase);
        //console.log("cmb_ColecaoStatus change var_Filtrado ::: ", var_Filtrado);

        fn_Filtrar();
    });

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

//#endregion

//#region GRID

function fn_FiltrarDados() {
    //console.log("bfn_FiltrarDados ::: ");
    var varAjax_UrlController = `${var_Controller}/FiltrarDados`,
        varAjax_TypeAction = 'POST',
        varAjax_TypeData = 'JSON',
        varAjax_TypeContent = 'application/json; charset=utf-8';

    var varLang_UrlTranslate = '/vendor/libs/datatables-bs5/i18n/pt-BR.json',

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
                    },
                    filtrosColecao: {
                        socioId: parseInt($('#hdSocioLogadoId').val()) || 0,
                        colecaoStatus: parseInt($('#cmb_ColecaoStatus').val()) || 0,
                    }
                });
            },

            dataSrc: function (json) {
                console.log("fn_FiltrarDados dataSrc json:: ", json);
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

                    const codigoAceca = `${data.split('/').join("<br><br>")} / ${full.Id}`;

                    return codigoAceca;
                }
            },
            // COLUNA - nomeMarca (3ª a aparecer no mobile)
            { data: 'NomeMarca', className: 'text-center', width: '120px', responsivePriority: 3 },
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
                data: 'NomeFabrica', className: 'text-center text-nowrap', responsivePriority: 10007,
                render: function (data, type, full) {
                    data = (data === '' || data === null || data === undefined) ? full.TxtFabrica : data;

                    let nomeFabrica = (data === '' || data === null || data === undefined) ? '' : data?.trim()?.split(/\s+/).join("<br>");
                    //console.log("nomeFabrica ::: ", nomeFabrica);
                    return nomeFabrica;
                }
            },
            // COLUNA - subTipo
            { data: 'SubTipo', className: 'text-center', responsivePriority: 10008 },
            // COLUNA - nomeFase
            {
                data: 'NomeFase', className: 'text-center text-nowrap', responsivePriority: 10009,
                render: function (data, type, full) {
                    if (!data || full.Id === 0 || type !== 'display') return '';

                    const nomeFase = data?.trim()?.split(/\s+/).join("<br>");

                    return nomeFase;
                }
            },
            // COLUNA - observacao
            {
                data: 'observacao', className: 'text-start', responsivePriority: 4
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
                        btn+= `<a href="javascript:fn_Pop(${itemObjJson});" class="btn btn-sm btn-icon btn-text-secondary rounded-pill waves-effect" data-bs-toggle="tooltip" title="Editar Observação"><i class="ri-edit-box-line ri-22px"></i></a>
                            <a href="javascript:fnItem_Colecao(${itemObjJson},'ColecaoDelete');" class="btn btn-sm btn-icon btn-text-danger rounded-pill waves-effect delete-record" data-bs-toggle="tooltip" title="Remover da Coleção"><i class="ri-delete-bin-7-line ri-22px"></i></a>
                            <a href="javascript:fnItem_Colecao(${itemObjJson},'ColecaoNegociar');" class="btn btn-sm btn-icon btn-text-${(full?.disponivel_negocio ? 'success' : 'secondary')} rounded-pill waves-effect" data-bs-toggle="tooltip" title="Para Negociação"><i class="ri-shopping-cart-2-line ri-22px"></i></a>`
                    };

                    if (idColecaoStatus < 3 && !full?.possui) {
                        btn += `<a href="javascript: fnItem_Colecao(${itemObjJson},'ColecaoInteresse');" class="btn btn-sm btn-icon btn-text-${(full?.interesse ? 'success' : 'secondary')} rounded-pill waves-effect" data-bs-toggle="tooltip" title="Tenho Interesse"><i class="ri-eye-line ri-22px"></i></a>`
                    }

                    if (idColecaoStatus < 3 && full?.interesse) {
                        btn += `<a href="javascript: fnItem_Colecao(${itemObjJson},'ColecaoIncluir');" class="btn btn-sm btn-icon btn-text-${(!full?.interesse ? 'success' : 'secondary')} rounded-pill waves-effect" data-bs-toggle="tooltip" title="Incluir na Coleção"><i class="ri-mail-check-line ri-22px"></i></a>`
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
                className: 'dt-export-btn btnExport export-data btn btn-label-primary dropdown-toggle me-4 waves-effect waves-light border-none',
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
    let idColecaoStatus = $('#cmb_ColecaoStatus').find('option:selected').val();
    let strColecaoStatus = $('#cmb_ColecaoStatus').find('option:selected').text();

    $('div.head-label').html(`<h5 class="card-title mb-0">Listagem - ${idColecaoStatus < 0 ? var_Nome  : strColecaoStatus}</h5>`);

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
            thisApi.column(8).visible(false); // coluna fase
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

    fn_LoadCmb_ColecaoStatus();
    fn_LoadCmb_MarcaFase();
    fn_LoadCmb_MarcaTipo();
    fn_LoadCmb_MarcaSubTipo(0);

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

function fn_LoadCmb_ColecaoStatus() {
    // console.log("fn_LoadCmb_ColecaoStatus ::: ");

    if ($('#cmb_ColecaoStatus').length <= 1) {
        $.ajax(
            {
                crossDomain: true,
                url: `${var_ControllerCmb}/AsyncCmb_ColecaoStatus`,
                type: 'GET',
                success: function (data) {
                    //console.log("fn_LoadCmb_ColecaoStatus  data ::: ", data);

                    $.each(data, function (id, result) {
                        //console.log("fn_LoadCmb_ColecaoStatus  result id ::: ", id);
                        //console.log("fn_LoadCmb_ColecaoStatus  result ::: ", result);
                        $("#cmb_ColecaoStatus").append($("<option></option>").val(result.value).html(result.text));
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

//#endregion

//#region POP

function fn_Pop(obj) {
   console.log("fn_Pop varItems_Row !", obj);

    const popAddNewItem = document.querySelector('#pop-add-new-item');

    popAddNewItemEl = new bootstrap.Offcanvas(popAddNewItem);

    // Pop ID
    (popAddNewItem.querySelector('#hdId').value = (obj === null ? 0 : obj.Id)),
    (popAddNewItem.querySelector('#hdIdMarca').value = (obj === null ? 0 : obj.IdMarca)),

        // Pop Dados
        (popAddNewItem.querySelector('.dt-line-01').value = (obj === null ? '' : obj.observacao)),
        (popAddNewItem.querySelector('.dt-line-05').checked = (obj === null ? false : obj.disponivel_negocio));

    // Pop Action
    (popAddNewItem.querySelector('.offcanvas-title').textContent = 'Alterar Registro');
    (popAddNewItem.querySelector('.data-submit').textContent = 'Alterar');

    // Open Pop
    popAddNewItemEl.show();
}

function fn_PopGetObj() {

    const objFormData = {
        Id: $('#hdId').val(),
        IdMarca: $('#hdIdMarca').val(),
        Observacao: $('.form-add-new-item .dt-line-01').val(),
        Disponivel_negocio: $('.form-add-new-item .dt-line-05').is(':checked')
    };

    //console.log("fn_PopGetObj !", objFormData);

    return objFormData;
}

function fn_PopValidator(formAddNewItem) {
    var varformValid = FormValidation.formValidation(formAddNewItem, {
        fields: {
            pop_line_item_01: {
                validators: {
                    notEmpty: {
                        message: 'O preenchimento &eacute; obrigat&oacute;rio'
                    }
                }
            }
        },
        plugins: {
            trigger: new FormValidation.plugins.Trigger(),
            bootstrap5: new FormValidation.plugins.Bootstrap5({
                // Use this for enabling/changing valid/invalid class
                // eleInvalidClass: '',
                eleValidClass: '',
                rowSelector: '.col-sm-12'
            }),
            submitButton: new FormValidation.plugins.SubmitButton(),
            // defaultSubmit: new FormValidation.plugins.DefaultSubmit(),
            autoFocus: new FormValidation.plugins.AutoFocus()
        },
        init: instance => {
            instance.on('plugins.message.placed', function (e) {
                if (e.element.parentElement.classList.contains('input-group')) {
                    e.element.parentElement.insertAdjacentElement('afterend', e.messageElement);
                }
            });
        }
    });

    return varformValid;
}

//#endregion

//#region COLECAO

function fnItem_Colecao(obj, action) {
    console.log("fnItem_Colecao obj ::: ", obj);
    console.log("fnItem_Colecao action::: ", action);

    let itemColecaoId = obj?.Id;
    let itemColecaoObs = obj?.Observacao;
    let marcaId = obj?.IdMarca;    
    let actionId = -1;
    const socioId = document.getElementById('hdSocioLogadoId').value;

    //console.log("fnItem_Colecao socioId::: ", socioId);
    
    if ((itemColecaoId === undefined || itemColecaoId === null || itemColecaoId === '' || itemColecaoId < 1)
        || (marcaId === undefined || marcaId === null || marcaId === '' || marcaId < 1)
        || (socioId === undefined || socioId === null || socioId === '' || socioId < 1)
    ) {
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
            case 'ColecaoObs':
                actionId = 4;
                break;
            default:
                actionId = -1;
        }

        $.busyLoadFull("show");

        $.ajax({
            url: `/SocioColecao/ActionColecao`,
            type: 'POST',
            dataType: 'JSON',
            data: {
                itemColecaoId: itemColecaoId,
                marcaId: marcaId,
                actionId: actionId,
                socioId: socioId,
                isPerfil: document.getElementById('hdIsPerfil').value,
                itemColecaoObs: itemColecaoObs,
                disponivelNegocio: obj?.Disponivel_negocio === true
            },
            success: function (response) {
                
                console.log("Data received: ", response);

                $.busyLoadFull("hide");

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

                    let table = $('.datatables-basic').DataTable();
                    table.ajax.reload(null, false);

                    const popAddNewItem = document.querySelector('#pop-add-new-item');
                    popAddNewItemEl = new bootstrap.Offcanvas(popAddNewItem);
                    popAddNewItemEl.hide();
                    //$('.offcanvas').hide()
                });

                return true;
            },
            error: function (xhr, status, error) {
                console.error("Error: " + error);
            }
        });
    }
}

//#endregion
