/**
 * Admin -> Marcas
 */

'use strict';

//#region Declare

let var_Nome = 'Marcas',
    var_Controller = '/Marca',
    var_ControllerCmb = '/HelperExtensions',

    varTbl_Obj = $('.datatables-basic'),
    varTbl_Data;

let var_Filtrado = false,
    var_ImgAlt = "ACECA",
    urlImgModal = "../img/logo/logo.png",
    urlImgModalIcon = "../img/logo/logo01.png",
    urlImgModaltext = "../img/logo/logo02.png";

var msg = 'O preenchimento &eacute; obrigat&oacute;rio';

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


//#endregion

//#region CARREGAMENTO INICIAL

document.addEventListener('DOMContentLoaded', function () {
    (function () {
        console.log(`LIST ${var_Controller} - Todos os recursos terminaram o carregamento!`);

        fn_Limpar();

        // Filtros
        fn_PopLoadCombos();
        fn_ChangeFiltros();

        $('.btn-filter').on('click', function () {
            fn_Filtrar();
        });

        $('.btn-filter-clear').on('click', function () {
            fn_Limpar();
        });

        // Carrega Dados Combos Modal
        //fn_PopLoadCombos();

        fn_Zoom();

    })();
});

//#endregion

//#region Botoes

function fn_Filtrar() {
    // Btn Filtro
    //console.log("fn_Filtrar ::: ");
    //console.log("fn_Filtrar var_Filtrado ::: ", var_Filtrado);

    let objFiltro = {
        param_MarcaFaseId: $('#cmb_MarcaFase').find('option:selected').val(),
        param_MarcaFabricaId: $('#cmb_MarcaFabrica').find('option:selected').val(),
        param_MarcaFabricaNome: $('#cmb_MarcaFabrica').find('option:selected').text(),
        param_MarcaTipoId: $('#cmb_MarcaTipo').find('option:selected').val(),
        param_MarcaSubTipoId: $('#cmb_MarcaSubTipo').find('option:selected').val(),
        param_IncluidoPor: $('#txt_IncluidoPor').val(),
        param_CodigoAceca: $('#txt_CodigoAceca').val(),
        param_NomeMarca: $('#txt_NomeMarca').val(),
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

    if (objFiltro.param_MarcaFaseId < 0
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
                        if (var_Filtrado) {
                            Swal.fire({
                                title: "Carregando!",
                                text: "Aguarde o carregamento das informações.",
                                icon: "success"
                            }).then((result) => {
                                fn_FiltrarDados(objFiltro);
                            });
                        } else {
                            fn_ModalConfirmarFiltros();
                        }
                    } else if (
                        result.dismiss === Swal.DismissReason.cancel
                    ) {
                        $('#cmb_MarcaFase').prop('selectedIndex', 0).change();
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
    $('#cmb_MarcaFabrica').prop('selectedIndex', 0).change();
    $('#cmb_MarcaTipo').prop('selectedIndex', 0).change();
    $('#cmb_MarcaSubTipo').prop('selectedIndex', 0).change();
    $('#chk_PesquisarDescricao')[0].checked = false;

    $(".card-datatable").hide();
    $('.datatables-basic').DataTable().clear().draw();

    var_Filtrado = false;

    $.busyLoadFull("hide");
}

function fn_FiltrarDados(objFiltro) {
    //console.log("bfn_FiltrarDados ::: ", objFiltro);

    $.busyLoadFull("show");

    varTbl_Obj.DataTable().clear().destroy();

    //console.log("fn_FiltrarDados objFiltro ::: ", objFiltro);

    var varAjax_UrlController = `${var_Controller}/FiltrarDados`,
        varAjax_TypeAction = 'POST',
        varAjax_TypeData = 'JSON',
        varAjax_TypeContent = 'application/json; charset=utf-8';

    $.ajax({
        url: varAjax_UrlController,
        type: varAjax_TypeAction,
        dataType: varAjax_TypeData,
        contentType: varAjax_TypeContent,
        data: JSON.stringify(objFiltro),
        success: function (result) {

            //console.log("fn_FiltrarDados result ::: ", result);

            if (result.bResult === false) {
                //console.log("busyLoadFull ::: hide");
                $.busyLoadFull("hide");

                Swal.fire({
                    title: 'OPS!!',
                    icon: 'error',
                    html: `<b> Erro ocorrido <br><br>${result.message}</b>`,
                    focusConfirm: false,
                    confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                    customClass: {
                        confirmButton: 'btn btn-label-danger waves-effect'
                    }
                });
            } else {

                var_Filtrado = true;

                ///console.log("busyLoadFull ::: hide");
                $.busyLoadFull("hide");

                let lstData = result.data;
                //console.log("jObj ::: ", jObj);

                fn_GridListFilter(lstData, objFiltro, "fn_FiltrarDados");
            }
        },
        error: function (xhr, textStatus, errorThrown) {

            console.log("XMLHttpRequest  :: ", xhr);
            console.log("textStatus  :: ", textStatus);
            console.log("errorThrown  :: ", errorThrown);
            console.log("result  :: Error while posting SendResult");

            fn_ModalErro(xhr, textStatus, errorThrown);
        },
    });
}

//#endregion

//#region Filtros

function fn_ChangeFiltros() {

    $('#cmb_MarcaFase').on('change', function () {

        let idMarcaFase = $(this).find('option:selected').val();

        //console.log("cmb_MarcaFase change idMarcaFase ::: ", idMarcaFase);
        //console.log("cmb_MarcaFase change var_Filtrado ::: ", var_Filtrado);

        if (idMarcaFase < 0) {
            //fn_Limpar();
        } else {
            if (!var_Filtrado) {
                if (idMarcaFase == 0) {

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
                            if (var_Filtrado) {
                                Swal.fire({
                                    title: "Carregando!",
                                    text: "Aguarde o carregamento das informações.",
                                    icon: "success"
                                }).then((result) => {
                                    fn_FiltrarDados(objFiltro);
                                });
                            } else {
                                fn_ModalConfirmarFiltros();
                            }
                        } else if (
                            result.dismiss === Swal.DismissReason.cancel
                        ) {
                            $('#cmb_MarcaFase').prop('selectedIndex', 0).change();
                        }
                    });
                } else {
                    fn_Filtrar();
                    //fn_ModalConfirmarFiltros();
                }
            } else {
                fn_Filtrar();
            }
        }
    });

    $('#cmb_MarcaFabrica').on('change', function () {

        let idMarcaFabrica = $(this).find('option:selected').val();

        //console.log("cmb_MarcaFabrica change idMarcaFabrica ::: ", idMarcaFabrica);
        //console.log("cmb_MarcaFabrica change var_Filtrado ::: ", var_Filtrado);

        if (idMarcaFabrica <= 0) {
            //fn_Limpar();
        } else {
            if (!var_Filtrado) {
                //fn_ModalConfirmarFiltros();
            } else {
                fn_Filtrar();
            }
        }
    });

    $('#cmb_MarcaTipo').on('change', function () {

        let idMarcaTipo = $(this).find('option:selected').val();

        //console.log("cmb_MarcaTipo change idMarcaTipo ::: ", idMarcaTipo);
        //console.log("cmb_MarcaTipo change var_Filtrado ::: ", var_Filtrado);

        if (idMarcaTipo <= 0) {
            //fn_Limpar();
        } else {
            if (!var_Filtrado) {
                fn_ModalConfirmarFiltros();
            } else {
                fn_Filtrar();
                /*
                Swal.fire({
                    title: 'Tipo Selecionado !!!',
                    html: `Para realizar o filtro por Tipo, <br><br> selecione o Sub-Tipo.<br><br> Caso prefira, utilize as op&ccedil;&otilde;es de filtros dispon&iacute;veis!`,
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
                */
            }
        }
    });

    $('#cmb_MarcaSubTipo').on('change', function () {

        let idMarcaSubTipo = $(this).find('option:selected').val();

        //console.log("cmb_MarcaSubTipo change idMarcaSubTipo ::: ", idMarcaSubTipo);
        //console.log("cmb_MarcaSubTipo change var_Filtrado ::: ", var_Filtrado);

        if (idMarcaSubTipo <= 0) {
            //fn_Limpar();
        } else {
            if (!var_Filtrado) {
                fn_ModalConfirmarFiltros();
            } else {
                fn_Filtrar();
            }
        }
    });

    $('#chk_PesquisarDescricao').change(function () {

        // 1. Get the checked status (boolean: true if checked, false otherwise)
        const isChecked = $(this).is(':checked');
        const checkboxValue = $(this).val();

        let colDesc = varTbl_Data.settings()[0].aoColumns[10];

        colDesc.bSearchable = isChecked;

        varTbl_Data.rows().invalidate().draw();

        $("input[type='search']").trigger("search");
    });
}

function fn_LoadFiltros() {
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

function fn_GridListFilter(lstData) {

    //console.log("fn_GridListFilter lstData ::: ", lstData);

    var varLang_UrlTranslate = 'https://cdn.datatables.net/plug-ins/1.12.1/i18n/pt-BR.json',

        varCol_Exportar = [2, 3, 4, 5, 6, 7, 8, 9],
        varCol_Ordenacao = [2, 'asc'], //set any columns order asc/desc

        varItems_QtdPorPage = 10,
        varItems_DivPage = [5, 10, 25, 50, 75, 100],
        varItems_Row = null,
        varItems_Id = 0;

    if (varTbl_Obj.length) {

        varTbl_Data = $('.datatables-basic').DataTable({
            //"processing": true,
            //"deferRender": true,
            data: lstData,
            ordering: true,
            destroy: true,

            columns: [
                // COLUNA - Responsive
                {
                    visible: false,
                    data: 'id',
                    className: 'control',
                    searchable: false,
                    orderable: false,
                    responsivePriority: 2,
                    targets: 0,
                    render: function (data, type, full, meta) {
                        return '';
                    }
                },
                // COLUNA - ID checkbox
                {
                    visible: false,
                    data: 'id',
                    targets: 1,
                    searchable: false,
                    orderable: false,
                    checkboxes: {
                        selectAllRender: '<input type="checkbox" class="form-check-input">'
                    },
                    render: function () {
                        return '<input type="checkbox" class="dt-checkboxes form-check-input" >';
                    },
                },
                // COLUNA - nomeFase
                {
                    targets: 2,
                    data: 'nomeFase',
                    className: "text-center"
                },
                // COLUNA - finalidade
                {
                    targets: 3,
                    data: 'nomeFinalidade',
                    className: "text-center",
                },                
                // COLUNA - codigoAceca
                {
                    targets: 4,
                    data: 'codigoAceca',
                    className: "text-center"
                },
                // COLUNA - imagem
                {
                    targets: 5,
                    data: 'imgPrincipalFull',
                    className: "text-center",
                    render: function (data, type, row, meta) {
                        return `<img name="myImg" class="td-img cmyImg" alt="${row.codigoAceca}" src="${data}">`;
                    }
                },
                // COLUNA - imagemDetalhe
                {
                    targets: 6,
                    data: 'imgDetalheFull',
                    className: "text-center",
                    render: function (data, type, row, meta) {
                        return `<img name="myImg" class="td-img cmyImg" alt="${row.codigoAceca}" src="${data}">`;
                    }
                },
                // COLUNA - nomeMarca
                {
                    targets: 7,
                    data: 'nomeMarca',
                    className: "text-center"
                },
                // COLUNA - fabricaNome
                {
                    targets: 8,
                    data: 'txtFabrica',
                    className: "text-center",
                    render: function (data, type, full, meta) {
                        return (data === "" || data === null) ? full.nomeFabrica : data;
                    }
                    
                },
                // COLUNA - subTipo
                {
                    targets: 9,
                    data: 'subTipo',
                    className: "text-center",
                },
               
                // COLUNA - descricao
                {
                    targets: 10,
                    data: 'descricao',
                    className: "text-start",
                    searchable: false,
                },
                // COLUNA - incluidoPor
                {
                    targets: 11,
                    data: 'incluidoPor',
                    className: "text-center",
                    render: function (data, type, full, meta) {
                        let id = full.id;

                        if (data !== undefined && data !== null) {
                            if (id !== 0 && type === 'display') {

                                let bQuebraLinha = data.includes("/");
                                //console.log("incluidoPor bQuebraLinha :::: ",bQuebraLinha );                               

                                let ulIn = `<ul class=" m-0 avatar-group d-flex align-items-center" style="list-style: none;">`;
                                let htmlContent = '';

                                for (let i = 0; i < data.split("/").length; i++) {
                                    htmlContent +=
                                        `<li class="avatar avatar-lg pull-up" data-bs-toggle="tooltip" data-bs-placement="top" data-popup="tooltip-custom" data-incluido="${data.split("/")[i]}" title="${data.split("/")[i]}" style="z-index: 1;">
                                        <img src="../img/avatars/${i}.png" alt="Avatar" class="rounded-circle">
                                    </li >`;
                                }

                                let dataImg = ulIn + htmlContent + "</ul>";

                                //console.log("incluidoPor for :::: ", dataImg); 

                                return dataImg; // data.replaceAll("/", "<br><br>");
                            } else {
                                return '';
                            }
                        } else {
                            return '';
                        }
                    },
                    filter: function (data, type) {
                        return data; // Use the text for filtering
                    }
                },
                // COLUNA - incluidoPor Hide
                {
                    targets: -2,
                    data: 'incluidoPor',
                    visible: false,
                },
                // COLUNA - Botoes Acoes
                {
                    //visible: false,
                    data: 'id',
                    targets: -1,
                    searchable: false,
                    orderable: false,
                    render: function (data, type, full, meta) {

                        let btns = '';

                        //console.log("Acao data ::: ", data);
                        //console.log("Acao lstData ::: ", lstData);
                        //console.log("Acao full ::: ", full);
                        //console.log("Acao meta ::: ", meta);
                        if (type === 'display') {
                            let itemId = data;
                            let itemObjJson = encodeURIComponent(JSON.stringify(full));

                            btns =
                                '<div class="d-inline-block text-nowrap">' +
                                '<a href="javascript:fn_Modal(' + itemObjJson + ',' + "'Edit'" + ');" class="btn btn-sm btn-icon btn-text-secondary waves-effect rounded-pill text-body me-1"><i class="ri-edit-box-line ri-22px"></i></a>' +
                                '</div>'
                        }

                        return (btns);
                    }
                },
            ],
            order: varCol_Ordenacao,
            dom: '<"card-header flex-column flex-md-row"<"head-label text-center"><"dt-action-buttons text-end pt-3 pt-md-0"B>><"row"<"col-sm-12 col-md-6"l><"col-sm-12 col-md-6 d-flex justify-content-center justify-content-md-end"f>>t<"row"<"col-sm-12 col-md-6"i><"col-sm-12 col-md-6"p>>',
            displayLength: varItems_QtdPorPage,
            lengthMenu: varItems_DivPage,
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
                    className: 'btn btn-label-primary dropdown-toggle me-4 waves-effect waves-light border-none',
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
            ],
            // For responsive popup
            responsive: {
                details: {
                    display: $.fn.dataTable.Responsive.display.modal({
                        header: function (row) {
                            var data = row.data();
                            return 'Details of ' + data['product_name'];
                        }
                    }),
                    type: 'column',
                    renderer: function (api, rowIdx, columns) {
                        var data = $.map(columns, function (col, i) {
                            return col.title !== '' // ? Do not show row in modal popup if title is blank (for check box)
                                ? '<tr data-dt-row="' +
                                col.rowIndex +
                                '" data-dt-column="' +
                                col.columnIndex +
                                '">' +
                                '<td>' +
                                col.title +
                                ':' +
                                '</td> ' +
                                '<td>' +
                                col.data +
                                '</td>' +
                                '</tr>'
                                : '';
                        }).join('');

                        return data ? $('<table class="table"/><tbody />').append(data) : false;
                    }
                }
            },
            initComplete: function () {
                //console.log("initComplete settings ::: ", settings);
                //console.log( "initComplete json ::: ", json);

                $.busyLoadFull("hide");

                fn_GridComplete(this);
            }
        });
    }
}

function fn_GridComplete(grid) {

    // var_Filtrado = true;

    var thisApi = grid.api();

    var countRows = grid.api().rows().count();
    //console.log("countRows ::: ", countRows);


    $('.card-header').after('<hr class="my-0">');

    //Titulo Tabela
    $('div.head-label').html(`<h5 class="card-title mb-0">${var_Nome}</h5>`);

    $(".card-datatable").show();

    if (countRows > 0) {
        $.busyLoadFull("hide");

        fn_Zoom();

        /*
        if (document.getElementById('hdIsPerfil').value.toLowerCase() === "false") {
            varTbl_Data.column(-1).visible(false);
            document.querySelector('.btnAddNew').style.setProperty('display', 'none', 'important');
        }
        */
    } else {
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

//#region ZOOM
function fn_Zoom() {

    var modal = document.getElementById('myModal');

    var img = document.querySelectorAll(".cmyImg");
    var modalImg = document.getElementById("img01");
    var captionText = document.getElementById("caption");

    $(".cmyImg").click(function () {
        modal.style.display = "block";
        modalImg.src = this.src;
        modalImg.alt = this.alt;
        captionText.innerHTML = this.alt;
    });

    $("#myModal").click(function () {
        img01.className += " out";
        setTimeout(function () {
            modal.style.display = "none";
            img01.className = "modal-content";
        }, 400);

    });
}

document.addEventListener('hidden.bs.modal', function (event) {
    if (document.activeElement) {
        document.activeElement.blur();
    }
});

//#endregion

//#region COMBO
function fn_PopLoadCombos() {

    //console.log("fn_PopLoadCombos  ::: ");

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

function fn_LoadCmb_MarcaFase() {
    // console.log("fn_LoadCmb_MarcaFase ::: ");

    if ($('#cmb_MarcaFase').length <= 1) {
        $.ajax(
            {
                crossDomain: true,
                url: `${var_ControllerCmb}/AsyncCmb_MarcaFase`,
                type: 'GET',
                success: function (data) {
                    //console.log("fn_LoadCmb_MarcaFase  data ::: ", data);

                    $("#cmb_MarcaFase").append($("<option></option>").val(0).html("Todas"));

                    $.each(data, function (id, result) {
                        //console.log("fn_LoadCmb_MarcaFase  result id ::: ", id);
                        //console.log("fn_LoadCmb_MarcaFase  result ::: ", result);
                        $("#cmb_MarcaFase").append($("<option></option>").val(result.value).html(result.text));
                        $("#cmbPop_MarcaFase").append($("<option></option>").val(result.value).html(result.text));
                    });
                },
                error: function (xhr, textStatus, errorThrown) {
                    fn_ModalErro(xhr, textStatus, errorThrown);
                },
            }
        );
    }
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

    if ($('#cmb_MarcaTipo').length <= 1) {
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
                        $("#cmb_MarcaTipo").append($("<option></option>").val(result.value).html(result.text));
                    });
                },
                error: function (xhr, textStatus, errorThrown) {
                    fn_ModalErro(xhr, textStatus, errorThrown);
                },
            }
        );
    }

    if ($('#cmbPop_MarcaTipo').length <= 1) {
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
    console.log("fn_Modal obj::: ", obj);
    //console.log("fn_Modal action::: ", action);

    //let resultLoad = fnItem_Edit_CarregarDados(obj, action)
    // console.log("fn_Modal resultLoad::: ", resultLoad);

    const popAddNewItem = document.querySelector('#modalAddNovaMarca');

    // Pop ID
    (popAddNewItem.querySelector('#hdId').value = (obj === null ? 0 : obj.id)),
        (popAddNewItem.querySelector('#hdMarcaFaseId').value = (obj === null ? 0 : obj.idMarcaFase)),
        (popAddNewItem.querySelector('#hdMarcaFinalidadeId').value = (obj === null ? 0 : obj.idMarcaFinalidade)),
        (popAddNewItem.querySelector('#hdMarcaFabricaId').value = (obj === null ? 0 : obj.idMarcaFabrica)),
        (popAddNewItem.querySelector('#hdMarcaDimensaoId').value = (obj === null ? 0 : obj.idMarcaDimensao)),
        (popAddNewItem.querySelector('#hdMarcaTipoId').value = (obj === null ? 0 : obj.idMarcaTipo)),
        (popAddNewItem.querySelector('#hdMarcaSubTipoId').value = (obj === null ? 0 : obj.idMarcaSubTipo)),
        (popAddNewItem.querySelector('#hdMarcaImpressoraId').value = (obj === null ? 0 : obj.idMarcaImpressora)),
        (popAddNewItem.querySelector('#hdMarcaQualidadeImagemId').value = (obj === null ? 0 : obj.idMarcaQualidadeImagem)),

        // Pop Dados
        (popAddNewItem.querySelector('#cmbPop_MarcaFase').value = (obj === null ? '-- Selecionar --' : obj.idMarcaFase));
    (popAddNewItem.querySelector('#txt_Codigo').value = (obj === null ? '-- Selecionar --' : obj.codigoAceca));
    (popAddNewItem.querySelector('#txt_Nome').checked = (obj === null ? false : obj.nomeMarca));
    (popAddNewItem.querySelector('#txt_IncluidoPor').value = (obj === null ? '' : obj.incluidoPor)),
        (popAddNewItem.querySelector('#cmbPop_MarcaFinalidade').value = (obj === null ? '-- Selecionar --' : obj.idMarcaFinalidade));
    (popAddNewItem.querySelector('#cmbPop_MarcaFabrica').value = (obj === null ? '-- Selecionar --' : obj.idMarcaFabrica));
    (popAddNewItem.querySelector('#cmbPop_MarcaDimensao').value = (obj === null ? '-- Selecionar --' : obj.idMarcaDimensao));
    (popAddNewItem.querySelector('#cmbPop_MarcaTipo').value = (obj === null ? '-- Selecionar --' : obj.idMarcaTipo));
    (popAddNewItem.querySelector('#cmbPop_MarcaSubTipo').value = (obj === null ? '-- Selecionar --' : obj.idMarcaSubTipo));
    (popAddNewItem.querySelector('#cmbPop_MarcaImpressora').value = (obj === null ? '-- Selecionar --' : obj.idMarcaImpressora));
    (popAddNewItem.querySelector('#cmbPop_MarcaQualidadeImagem').value = (obj === null ? '-- Selecionar --' : obj.idMarcaQualidadeImagem));
    (popAddNewItem.querySelector('#txt_Descricao').value = (obj === null ? '' : obj.descricao));

    (popAddNewItem.querySelector('#txt_Valor').value = (obj === null ? '' : obj.valor));
    (popAddNewItem.querySelector('#txt_Valor1PI').value = (obj === null ? '' : obj.valor1PI));
    (popAddNewItem.querySelector('#txt_Valor2PI').value = (obj === null ? '' : obj.valor2PI));

    //Pop Arquivos
    (obj === null || obj?.imgPrincipal === null) ? (popAddNewItem.querySelector('#txt_ImgPrincipal').value = '') : fnItem_PopImgPrincipal(obj);
    (obj === null || obj?.imgDetalhe === null) ? (popAddNewItem.querySelector('#txt_ImgDetalhe').value = '') : fnItem_PopImgDetalhe(obj);

    // Pop Action
    (popAddNewItem.querySelector('.address-title').textContent = (action === 'Edit') ? 'Alterar Registro' : 'Novo Registro');
    (popAddNewItem.querySelector('.data-submit').textContent = (action === 'Edit') ? 'Alterar' : 'Adicionar');

    // console.log("fn_Modal resultLoad::: ", resultLoad);
    $.busyLoadFull("hide");

    if (obj !== null) {

        $("#cmbPop_MarcaFase").val(obj.idMarcaFase).change();
        $("#cmbPop_MarcaFinalidade").val(obj.idMarcaFinalidade).change();
        $("#cmbPop_MarcaFabrica").val(obj.idMarcaFabrica).change();
        $("#cmbPop_MarcaDimensao").val(obj.idMarcaDimensao).change();
        $("#cmbPop_MarcaTipo").val(obj.idMarcaTipo).change();
        $("#cmbPop_MarcaSubTipo").val(obj.idMarcaSubTipo).change();
        $("#cmbPop_MarcaImpressora").val(obj.idMarcaImpressora).change();
        $("#cmbPop_MarcaQualidadeImagem").val(obj.idMarcaQualidadeImagem).change();

        (obj.valor !== null || obj.valor1PI !== null || obj.valor2PI !== null) ? $('.div_adicional').show() : $('.div_adicional').hide();
    }

    $('#modalAddNovaMarca').modal('show');
}

function fnItem_PopImgPrincipal(obj) {
    //console.log("fnItem_PopImgPrincipal obj !", obj);

    if (obj !== null) {
        let objFile = {},
            fileArq = obj?.imgPrincipal;

        const fileInput = document.querySelector('#txt_ImgPrincipal');

        if (fileArq !== undefined) {
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

function fnItem_PopImgDetalhe(obj) {
    //console.log("fnItem_PopImgDetalhe obj !", obj);

    if (obj !== null) {
        let objFile = {},
            fileArq = obj?.imgDetalhe;

        const fileInput = document.querySelector('#txt_ImgDetalhe');

        if (fileArq !== undefined) {
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

function fn_ModalSalvar(e) {
    const formData = new FormData(document.forms['form-modal-full-edit']);
    //console.log("fn_ModalSalvar formData ::: ", formData);
    //console.log("fn_ModalSalvar formEle GET ::: ", formData.get('cinema_Nome'));

    fn_ModalGetObj(formData, 'Save')
}

function fn_ModalGetObj(data, action) {
    console.log("fn_ModalGetObj data ::: ", data);
    //console.log("fn_ModalGetObj action ::: ", action);

    var loadCmbs = fn_PopLoadCombos();

    const objFormData = {
        Id: $('#hdId').val(),
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

function fnItem_Edit_CarregarDados(obj, action) {
    console.log("fnItem_Edit_CarregarDados obj ::: ", obj);
    //var varPop_BtnAction = 'Edit';

    //fn_Pop(obj, varPop_BtnAction);

    var varAjax_UrlController = `${var_Controller}/GetFullById`,
        varAjax_TypeAction = 'POST',
        varAjax_TypeData = 'JSON',
        varAjax_TypeContent = 'application/json; charset=utf-8';

    if (obj === undefined || obj === null || obj.id === 0) {
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
                data: {
                    id: obj.id
                },
                success: function (result) {
                    console.log("result  :: ", result);

                    if (result.bResult) {

                        fn_Modal(result.data, action);

                    } else {
                        //console.log("result  :: ", result);
                        $.busyLoadFull("hide");

                        Swal.fire({
                            title: 'OPS!!',
                            icon: 'error',
                            html: `<b> Erro ocorrido <br><br>` + result + `</b>`,
                            focusConfirm: false,
                            confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                            customClass: {
                                confirmButton: 'btn btn-label-danger waves-effect'
                            }
                        });
                    }
                },
                error: function (xhr, textStatus, errorThrown) {
                    fn_ModalErro(xhr, textStatus, errorThrown);
                },
            });
    }
}

//#endregion
