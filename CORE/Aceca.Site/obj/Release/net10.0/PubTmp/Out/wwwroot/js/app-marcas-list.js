/**
 * app-marcas-list
 */

'use strict';

//#region Declare

let var_Nome = 'Marcas',
    var_Controller = '/Marcas',
    var_ControllerCmb = '/HelperExtensions',

    varTbl_Obj = $('.datatables-basic'),
    varTbl_Data;

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

//#region DeclareDatatable (jquery)
$(function () {

    // Variable declaration for table
    var productAdd = '/Ecommerce/ProductAdd',
        statusObj = {
            1: { title: 'Scheduled', class: 'bg-label-warning' },
            2: { title: 'Publish', class: 'bg-label-success' },
            3: { title: 'Inactive', class: 'bg-label-danger' }
        },
        categoryObj = {
            0: { title: 'Household' },
            1: { title: 'Office' },
            2: { title: 'Electronics' },
            3: { title: 'Shoes' },
            4: { title: 'Accessories' },
            5: { title: 'Game' }
        },
        stockObj = {
            0: { title: 'Out_of_Stock' },
            1: { title: 'In_Stock' }
        },
        stockFilterValObj = {
            0: { title: 'Out of Stock' },
            1: { title: 'In Stock' }
        };
});

//#endregion

//#region CARREGAMENTO INICIAL

document.addEventListener('DOMContentLoaded', function () {
    (function () {
        console.log(`LIST ${var_Controller}- Todos os recursos terminaram o carregamento!`);

        // Update the clock immediately on load, and then every second
        fn_UpdateClock();
        setInterval(fn_UpdateClock, 1000); // Updates every 1000 milliseconds

        fn_Auth();

        // Filtros
        fn_LoadFiltros();

        // Carrega Dados Grid
        fn_GridList();
    })();
});

//#endregion

//#region Filtros

function fn_LoadFiltros() {
    //console.log("fnItemLoadFiltros  ::: ");

    $.busyLoadFull("show");

    $(".card-datatable").hide();

    fn_LoadCmb_MarcaFase();
    fn_LoadCmb_MarcaFabrica();
    fn_LoadCmb_MarcaTipo();
    fn_LoadCmb_MarcaSubTipo();

    $.busyLoadFull("hide");
}
function fn_LoadCmb_MarcaFase() {

    if ($('#cmb_MarcaFase').length <= 1) {
        $.ajax(
            {
                crossDomain: true,
                url: `${var_ControllerCmb}/AsyncCmb_MarcaFase`,
                type: 'GET',
                success: function (data) {
                    //console.log("fn_LoadCmb_MarcaFase  data ::: ", data);

                    $.each(data, function (id, result) {
                        //console.log("fn_LoadCmb_MarcaFase  result id ::: ", id);
                        //console.log("fn_LoadCmb_MarcaFase  result ::: ", result);
                        $("#cmb_MarcaFase").append($("<option></option>").val(result.value).html(result.text));
                    });
                },
                error: function (XMLHttpRequest, textStatus, errorThrown) {
                    console.log("XMLHttpRequest  :: ", XMLHttpRequest);
                    console.log("textStatus  :: ", textStatus);
                    console.log("errorThrown  :: ", errorThrown);
                    console.log("result  :: Error while posting SendResult");

                    $.busyLoadFull("hide");

                    Swal.fire({
                        title: 'OPS!!',
                        icon: 'error',
                        html: `<b> Erro ocorrido <br><br>` + errorThrown.msg + `</b>`,
                        focusConfirm: false,
                        confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                        customClass: {
                            confirmButton: 'btn btn-label-danger waves-effect'
                        }
                    });
                },
            }
        );
    }
}
function fn_LoadCmb_MarcaFabrica() {

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
                error: function (XMLHttpRequest, textStatus, errorThrown) {
                    console.log("XMLHttpRequest  :: ", XMLHttpRequest);
                    console.log("textStatus  :: ", textStatus);
                    console.log("errorThrown  :: ", errorThrown);
                    console.log("result  :: Error while posting SendResult");

                    $.busyLoadFull("hide");

                    Swal.fire({
                        title: 'OPS!!',
                        icon: 'error',
                        html: `<b> Erro ocorrido <br><br>` + errorThrown.msg + `</b>`,
                        focusConfirm: false,
                        confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                        customClass: {
                            confirmButton: 'btn btn-label-danger waves-effect'
                        }
                    });
                },
            }
        );
    }
}
function fn_LoadCmb_MarcaTipo() {

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
                error: function (XMLHttpRequest, textStatus, errorThrown) {
                    console.log("XMLHttpRequest  :: ", XMLHttpRequest);
                    console.log("textStatus  :: ", textStatus);
                    console.log("errorThrown  :: ", errorThrown);
                    console.log("result  :: Error while posting SendResult");

                    $.busyLoadFull("hide");

                    Swal.fire({
                        title: 'OPS!!',
                        icon: 'error',
                        html: `<b> Erro ocorrido <br><br>` + errorThrown.msg + `</b>`,
                        focusConfirm: false,
                        confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                        customClass: {
                            confirmButton: 'btn btn-label-danger waves-effect'
                        }
                    });
                },
            }
        );
    }
}
function fn_LoadCmb_MarcaSubTipo() {

    if ($('#cmb_MarcaSubTipo').length <= 1) {
        $.ajax(
            {
                crossDomain: true,
                url: `${var_ControllerCmb}/AsyncCmb_MarcaSubTipo`,
                type: 'GET',
                success: function (data) {
                    //console.log("fn_LoadCmb_MarcaSubTipo  data ::: ", data);

                    $.each(data, function (id, result) {
                        //console.log("fn_LoadCmb_MarcaSubTipo  result id ::: ", id);
                        //console.log("fn_LoadCmb_MarcaSubTipo  result ::: ", result);
                        $("#cmb_MarcaSubTipo").append($("<option></option>").val(result.value).html(result.text));
                    });
                },
                error: function (XMLHttpRequest, textStatus, errorThrown) {
                    console.log("XMLHttpRequest  :: ", XMLHttpRequest);
                    console.log("textStatus  :: ", textStatus);
                    console.log("errorThrown  :: ", errorThrown);
                    console.log("result  :: Error while posting SendResult");

                    $.busyLoadFull("hide");

                    Swal.fire({
                        title: 'OPS!!',
                        icon: 'error',
                        html: `<b> Erro ocorrido <br><br>` + errorThrown.msg + `</b>`,
                        focusConfirm: false,
                        confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                        customClass: {
                            confirmButton: 'btn btn-label-danger waves-effect'
                        }
                    });
                },
            }
        );
    }
}

//#endregion

//#region GRID
function fn_GridList() {
    let varLang_UrlTranslate = 'https://cdn.datatables.net/plug-ins/1.12.1/i18n/pt-BR.json',

        varAjax_UrlController = `${var_Controller}/ListGrid`,
        varAjax_TypeAction = 'GET',

        varCol_Exportar = [2, 3, 4, 5, 6, 7, 8, 9,],
        varCol_Ordenacao = [2, ''], //set any columns order asc/desc[[2, 'asc']],

        varItems_QtdPorPage = 25,
        varItems_DivPage = [5, 10, 25, 50, 75, 100],
        varItems_Row = null,
        varItems_Id = 0;

    // List Table
    // --------------------------------------------------------------------

    // E-commerce Products datatable

    if (varTbl_Obj.length) {

        $.busyLoadFull("show");

        varTbl_Data = varTbl_Obj.DataTable({
            //serverSide: true,
            //paging: true,
            //scrollCollapse: true,
            //ordering: true,
            //destroy: true,

            ajax: {
                crossDomain: true,
                url: varAjax_UrlController,
                type: varAjax_TypeAction,
                dataSrc: function (result) {
                    console.log("result fn :: ", result)

                    if (result.bResult === false) {
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
                        if (result.bResult === true && result.type === "VAZIO") {

                            $.busyLoadFull("hide");

                            Swal.fire({
                                title: 'SEM DADOS!!',
                                icon: 'info',
                                html: `N&atilde;o h&aacute; dados da API INGRESSO para serem carregados, para o cinema selecionado!!`,
                                focusConfirm: false,
                                confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                                customClass: {
                                    confirmButton: 'btn btn-label-secondary waves-effect'
                                },
                            });

                        } else {

                            //console.log("data.data :: ", result.data)

                            return result.data;
                        }
                    }
                }
            },
            columns: [
                // columns according to JSON
                { data: 'id' },
                { data: 'id' },
                { data: 'nomeFase' },
                { data: 'codigoAceca' },
                { data: 'imagem' },
                { data: 'imagemDetalhe' },
                { data: 'nomeMarca' },
                { data: 'fabricaNome' },
                { data: 'descricao' },
                { data: 'incluidoPor' },
                { data: 'fabricaId' }
            ],
            columnDefs: [
                // For Responsive
                {
                    visible: false,
                    className: 'control',
                    searchable: false,
                    orderable: false,
                    responsivePriority: 2,
                    targets: 0,
                    render: function (data, type, full, meta) {
                        return '';
                    }
                },
                // For Checkboxes
                {
                    visible: false,
                    targets: 1,
                    orderable: false,
                    checkboxes: {
                        selectAllRender: '<input type="checkbox" class="form-check-input">'
                    },
                    render: function () {
                        return '<input type="checkbox" class="dt-checkboxes form-check-input" >';
                    },
                    searchable: false
                },
                // COLUNA - nomeFase
                {
                    targets: 2,
                    className: "text-center",
                    responsivePriority: 1,
                    searchable: false,
                    orderable: false,
                    render: function (data, type, full, meta) {
                        let id = full.id;

                        if (data !== undefined && data !== null) {
                            if (id !== 0 && type === 'display') {
                                return data;
                            } else {
                                return '';
                            }
                        } else {
                            return '';
                        }
                    }
                },
                // COLUNA - codigoAceca
                {
                    targets: 3,
                    className: "text-nowrap text-center",
                    render: function (data, type, full, meta) {
                        let id = full.id;

                        if (data !== undefined && data !== null) {
                            if (id !== 0 && type === 'display') {
                                return data;
                            } else {
                                return '';
                            }
                        } else {
                            return '';
                        }
                    }
                },
                // COLUNA - imagem
                {
                    targets: 4,
                    className: "text-center",
                    render: function (data, type, full, meta) {
                        let id = full.id;

                        if (data !== undefined && data !== null) {
                            if (id !== 0 && type === 'display') {

                                let varImagem = full.imagem,
                                    varCodigoAceca = full.codigoAceca;
                                return data;
                                //return `<img name="myImg" class="td-img cmyImg" src="${varImagem}" alt="${varCodigoAceca}">`;
                            } else {
                                return '<div class="td-img-placeholder">📷</div>';
                            }
                        } else {
                            return '<div class="td-img-placeholder">📷</div>';
                        }
                    }
                },
                // COLUNA - imagemDetalhe
                {
                    targets: 5,
                    className: "text-center",
                    render: function (data, type, full, meta) {
                        let id = full.id;

                        if (data !== undefined && data !== null) {
                            if (id !== 0 && type === 'display') {

                                let varImagemmDetalhe = full.imagemDetalhe,
                                    varCodigoAceca = full.codigoAceca;
                                return data;
                                //return `<img name="myImg" class="td-img cmyImg" src="${varImagemmDetalhe}" alt="${varCodigoAceca}">`;
                            } else {
                                return '<div class="td-img-placeholder">📷</div>';
                            }
                        } else {
                            return '<div class="td-img-placeholder">📷</div>';
                        }
                    }
                },
                // COLUNA - nomeMarca
                {
                    targets: 6,
                    className: "text-center",
                    render: function (data, type, full, meta) {
                        let id = full.id;

                        if (data !== undefined && data !== null) {
                            if (id !== 0 && type === 'display') {
                                return data;
                            } else {
                                return '';
                            }
                        } else {
                            return '';
                        }
                    }
                },
                // COLUNA - fabricaNome
                {
                    targets: 7,
                    className: "text-start",
                    ///sWidth: '25%'
                    render: function (data, type, full, meta) {
                        let id = full.id;

                        if (data !== undefined && data !== null) {
                            if (id !== 0 && type === 'display') {
                                return data;
                            } else {
                                return '';
                            }
                        } else {
                            return '';
                        }
                    }
                },
                // COLUNA - descricao
                {
                    targets: 8,
                    className: "text-start",
                    //sWidth: '25%'
                    render: function (data, type, full, meta) {
                        let id = full.id;

                        if (data !== undefined && data !== null) {
                            if (id !== 0 && type === 'display') {
                                return data;
                            } else {
                                return '';
                            }
                        } else {
                            return '';
                        }
                    }
                },
                // COLUNA - incluidoPor
                {
                    targets: 9,
                    className: "text-nowrap text-center",
                    //sWidth: '25%'
                    render: function (data, type, full, meta) {
                        let id = full.id;

                        if (data !== undefined && data !== null) {
                            if (id !== 0 && type === 'display') {

                                let bQuebraLinha = data.includes("/");
                                //console.log("incluidoPor bQuebraLinha :::: ",bQuebraLinha );

                                const firstSlashIndex = data.indexOf("/");
                                //console.log("incluidoPor :::: ", firstSlashIndex); // Output: 5

                                return data.replaceAll("/", "<br><br>");
                            } else {
                                return '';
                            }
                        } else {
                            return '';
                        }
                    }
                },
                // Actions
                {
                    visible: false,
                    targets: -1,
                    title: 'Actions',
                    searchable: false,
                    orderable: false,
                    render: function (data, type, full, meta) {
                        let id = full.id;
                        let btns = '';

                        //console.log("Acao data ::: ", data);
                        //console.log("Acao lstData ::: ", lstData);
                        //console.log("Acao full ::: ", full);
                        //console.log("Acao meta ::: ", meta);
                        if (type === 'display') {
                            let itemId = data;
                            let itemObjJson = encodeURIComponent(JSON.stringify(full));

                            btns =
                                '<div class="d-inline-block" data-pi="' + full[0] + '">' +
                                '<a href="javascript:;" class="btn btn-sm btn-text-secondary rounded-pill btn-icon dropdown-toggle hide-arrow" data-bs-toggle="dropdown"><i class="ri-more-2-line ri-22px"></i></a>' +
                                '<ul class="dropdown-menu dropdown-menu-end m-0">' +
                                '<li><a href="javascript:fnItem_Pop(' + itemObjJson + ',' + "'Edit'" + ');" class="dropdown-item edit-record" data-pi="' + full[0] + '">Editar</a></li>' +
                                '<div class="dropdown-divider"></div>' +
                                '<li><a href="javascript:fnItem_PopInfoPi(' + itemObjJson + ',' + "'View'" + ');" class="dropdown-item edit-record">Visualizar PI</a></li>' +
                                '</ul>' +
                                '</div>'
                        }

                        return (btns);
                    }
                }
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
            ],
            responsive: {
                details: {
                    display: $.fn.dataTable.Responsive.display.modal({
                        header: function (row) {
                            var data = row.data();
                            return 'Detalhes de ' + data['full_name'];
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
            error: function (obj, textstatus) {
                $.busyLoadFull("hide");
                Swal.fire({
                    title: 'OPS!!',
                    icon: 'error',
                    html: `<b>` + obj.msg + `</b>`,
                    focusConfirm: false,
                    confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                    customClass: {
                        confirmButton: 'btn btn-label-danger waves-effect'
                    }
                });
                //alert(obj.msg);
            },
            initComplete: function (settings, json) {
                $.busyLoadFull("hide");

                fn_GridComplete(this);

                //fn_initComplete(this);
            }
        });
        $('.dataTables_length').addClass('my-0');
        $('.dt-action-buttons').addClass('pt-0');
        $('.dt-buttons').addClass('d-flex flex-wrap');
    }
}

function fn_GridComplete(grid) {

    var thisApi = grid.api();

    var countRows = grid.api().rows().count();
    //console.log("countRows ::: ", countRows);

    if (countRows > 0) {

        fn_Zoom();

        $.busyLoadFull("hide");

        Swal.fire({
            icon: 'success',
            title: 'Carregado!',
            html: `Dados carregados com sucesso.`,
            focusConfirm: true,
            confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
            customClass: {
                confirmButton: 'btn btn-label-success waves-effect'
            }
        }).then((result) => {
            $('.card-header').after('<hr class="my-0">');

            //Titulo Filtros
            $('div.head-label').html(`<h5 class="card-title mb-0">${var_Nome}</h5>`);

            $(".card-datatable").show();
        });

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

            //Titulo Filtros
            //$('div.head-label').html(`<h5 class="card-title mb-0">${var_Nome}</h5>`);

            $(".card-datatable").show();
        });
    }
}

function fn_initComplete(grid) {
    // Adding status filter once table initialized
    grid.api()
        .columns(-2)
        .every(function () {
            var column = this;
            var select = $(
                '<select id="ProductStatus" class="form-select text-capitalize"><option value="">Select Status</option></select>'
            )
                .appendTo('.product_status')
                .on('change', function () {
                    var val = $.fn.dataTable.util.escapeRegex($(this).val());
                    column.search(val ? '^' + val + '$' : '', true, false).draw();
                });

            column
                .data()
                .unique()
                .sort()
                .each(function (d, j) {
                    select.append('<option value="' + statusObj[d].title + '">' + statusObj[d].title + '</option>');
                });
        });
    // Adding category filter once table initialized
    grid.api()
        .columns(3)
        .every(function () {
            var column = this;
            var select = $(
                '<select id="ProductCategory" class="form-select text-capitalize"><option value="">Category</option></select>'
            )
                .appendTo('.product_category')
                .on('change', function () {
                    var val = $.fn.dataTable.util.escapeRegex($(this).val());
                    column.search(val ? '^' + val + '$' : '', true, false).draw();
                });

            column
                .data()
                .unique()
                .sort()
                .each(function (d, j) {
                    select.append('<option value="' + categoryObj[d].title + '">' + categoryObj[d].title + '</option>');
                });
        });
    // Adding stock filter once table initialized
    grid.api()
        .columns(4)
        .every(function () {
            var column = this;
            var select = $(
                '<select id="ProductStock" class="form-select text-capitalize"><option value=""> Stock </option></select>'
            )
                .appendTo('.product_stock')
                .on('change', function () {
                    var val = $.fn.dataTable.util.escapeRegex($(this).val());
                    column.search(val ? '^' + val + '$' : '', true, false).draw();
                });

            column
                .data()
                .unique()
                .sort()
                .each(function (d, j) {
                    select.append('<option value="' + stockObj[d].title + '">' + stockFilterValObj[d].title + '</option>');
                });
        });
}

//#endregion

//#region AUTH GUARD
function fn_Auth() {
    const sess = sessionStorage.getItem('aceca_sessao');

    if (!sess) {
        window.location.href = 'login.html'; return;
    }

    const u = JSON.parse(sess);

    document.getElementById('tbNome').textContent = u.nome || 'Usuário';
    document.getElementById('tbCargo').textContent = u.cargo || '—';
    document.getElementById('tbAvatar').textContent = (u.nome || 'U')[0].toUpperCase();

    //console.log("AUTH GUARD - sess :::", sess);
};

function fn_Logout() {
    sessionStorage.removeItem('aceca_sessao');
    window.location.href = 'login.html';
}

//#endregion

//#region ZOOM
function fn_Zoom() {
    var modal = document.getElementById('myModal');
    //console.log("fnZoom modal ::: ", modal);

    // Get the image and insert it inside the modal - use its "alt" text as a caption
    var img = document.querySelectorAll(".cmyImg"); //document.getElementById('myImg');
    var modalImg = document.getElementById("img01");
    var captionText = document.getElementById("caption");

    //console.log("fnZoom img ::: ", img);
    //console.log("fnZoom modalImg ::: ", modalImg);
    //console.log("fnZoom captionText ::: ", captionText);

    $(".cmyImg").click(function () {
        //console.log("fnZoom cmyImg ::: ", $(this));
        //console.log("fnZoom cmyImg  modal ::: ", modal);
        //console.log("fnZoom cmyImg  modalImg ::: ", modalImg);

        modal.style.display = "block";
        modalImg.src = this.src;
        modalImg.alt = this.alt;
        captionText.innerHTML = this.alt;
    });

    // When the user clicks on <span> (x), close the modal
    $("#myModal").click(function () {
        //console.log("fnZoom myModal::: ", img);

        img01.className += " out";
        setTimeout(function () {
            modal.style.display = "none";
            img01.className = "modal-content";
        }, 400);

    });
}

document.addEventListener('hidden.bs.modal', function (event) {
    console.log("addEventListener hidden::: ", event);
    console.log("addEventListener activeElement::: ", document.activeElement);
    if (document.activeElement) {
        document.activeElement.blur();
    }
});

//#endregion

//#region CLOCK DATE
function fn_UpdateClock() {
    const now = new Date();
    // Format the date and time for display
    const timeString = now.toLocaleTimeString();
    const dateString = now.toLocaleDateString(undefined, { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' });

    document.getElementById('date-time').textContent = `${dateString} - ${timeString}`;
}

//#endregion