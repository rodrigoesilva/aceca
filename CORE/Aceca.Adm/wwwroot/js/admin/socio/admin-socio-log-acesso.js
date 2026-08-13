/**
 * App -> Seguranca -> Usuarios
 */

'use strict';

//#region Declare

let var_Nome = 'Gest&atilde;o & Seguran&ccedil;a -> S&oacute;cios Log de Acesso',
    var_Controller = '/SocioLogAcesso',
    var_ControllerCmb = '/HelperExtensions',

    varTbl_Obj = $('.datatables-basic'),
    varTbl_Data,

    formValid, popAddNewItemEl;

var msg = 'O preenchimento &eacute; obrigat&oacute;rio';

//#endregion

//#region CARREGAMENTO INICIAL

document.addEventListener('DOMContentLoaded', function () {
    (function () {
        console.log(`LIST ${var_Controller} - Todos os recursos terminaram o carregamento!`);

        // Carrega Dados Grid
        fn_GridList(formValid);
    })();
});

//#endregion

//#region GRID
function fn_GridList(formValid) {

    var varLang_UrlTranslate = '/vendor/libs/datatables-bs5/i18n/pt-BR.json',

        varAjax_UrlController = `${var_Controller}/FiltrarDados`,
        varAjax_TypeAction = 'POST',
        varAjax_TypeContent = 'application/json; charset=utf-8',

        varCol_Exportar = [2, 3, 4, 5, 6, 7, 8, 9, 10],
        varCol_Ordenacao = [[2, 'asc']],

        varItems_QtdPorPage = 50,
        varItems_DivPage = [5, 10, 25, 50, 75, 100],
        varItems_Row = null,
        varItems_Id = 0;

    // List Table
    // --------------------------------------------------------------------

    if (varTbl_Obj.length) {

        $.busyLoadFull("show");

        varTbl_Data = varTbl_Obj.DataTable({
            serverSide: true,
            paging: true,
            scrollCollapse: true,
            ordering: true,
            destroy: true,

            ajax: {
                url: varAjax_UrlController,
                type: varAjax_TypeAction,
                contentType: varAjax_TypeContent,

                data: function (d) {
                    return JSON.stringify({
                        draw: d.draw,
                        start: d.start,
                        length: d.length,

                        search: d.search, // 🔥 OBRIGATÓRIO para server-side

                        somenteAtivos: document.getElementById('chkFilterAtivo')?.checked === true
                    });
                },

                // server-side: cada página/ordenação/busca dispara uma nova requisição —
                // mostra o overlay durante a ida ao servidor, não só no carregamento inicial
                beforeSend: function () {
                    $.busyLoadFull("show");
                },
                complete: function () {
                    $.busyLoadFull("hide");
                },

                dataSrc: function (result) {
                    //console.log("fn_GridList :: ", result)
                    return result.data;
                }
            },
            columnDefs: [
                // COLUNA - Responsive
                {
                    data: 'Id',
                    targets: 0,
                    className: 'control',
                    visible: false,
                    responsivePriority: 1,
                    render: function (data, type, full, meta) {
                        return '';
                    }
                },
                // COLUNA - ID checkbox
                {
                    data: 'Id',
                    targets: 1,
                    visible: false,
                    checkboxes: true,
                    render: function () {
                        return '<input type="checkbox" class="dt-checkboxes form-check-input">';
                    },
                    checkboxes: {
                        selectAllRender: '<input type="checkbox" class="form-check-input">'
                    }
                },
                // COLUNA - Nome
                {
                    data: 'NomeSocio',
                    targets: 2,
                    responsivePriority: 2,
                },
                // COLUNA - Endereco socio
                {
                    data: 'EnderecoCidade',
                    targets: 3,
                    className: "text-center",
                    responsivePriority: 10004,
                    render: function (data, type, full) {
                        let id = full.Id;

                        if (id != 0 && data !== undefined && data !== null) {

                            let socioEndereco = `${full.EnderecoEstado}<br>${full.EnderecoCidade}`;

                            return socioEndereco;

                        } else {
                            return '';
                        }
                    }
                },
                // COLUNA - Origem Acesso
                {
                    data: 'OrigemCidade',
                    targets: 4,
                    className: "text-center",
                    responsivePriority: 10005,
                    render: function (data, type, full) {
                        let id = full.Id;

                        if (id != 0 && data !== undefined && data !== null) {

                            let origemAcesso = `${full.OrigemEstado}<br>${full.OrigemCidade}`;

                            return origemAcesso;

                        } else {
                            return '';
                        }
                    }
                },
                // COLUNA - Tipo Acesso
                {
                    data: 'Browser',
                    targets: 5,
                    className: "text-center",
                    responsivePriority: 10006,

                    render: function (data, type, full) {
                        let id = full.Id;

                        if (id != 0 && data !== undefined && data !== null) {

                            let socioTipoAcesso = `${full.Browser}<br>${full.Device}<br>${full.Os}`;

                            return socioTipoAcesso;

                        } else {
                            return '';
                        }
                    }
                },
                // COLUNA - IP
                {
                    data: 'Ip',
                    targets: 6,
                    className: "text-center",
                    responsivePriority: 10007,
                },
                // COLUNA - Operadora
                {
                    data: 'Operadora',
                    targets: 7,
                    className: "text-center",
                    responsivePriority: 10008,
                },
                // COLUNA - Ultimo Login
                {
                    data: 'UltimoLogin',
                    targets: 8,
                    className: "text-center",
                    responsivePriority: 3,
                    render: function (data, type, full) {
                        let id = full.Id;

                        if (id != 0 && data !== undefined && data !== null) {
                            // UltimoLogin já é gravado em horário local (Brasil, UTC-3) pelo
                            // AuthController - tratar como UTC e subtrair 6h aqui deslocava
                            // o horário exibido incorretamente.
                            let dataFormat = data ? moment(data).format("DD/MM/YYYY[<br>]HH:mm:ss") : '-';

                            return (dataFormat);
                        } else {
                            return '';//'Data Indispon&iacute;vel';
                        }
                    }
                },
                // COLUNA - Status
                {
                    targets: -2,
                    data: 'SocioAtivo',
                    responsivePriority: 4,
                    render: function (data, type, full, meta) {

                        //console.log("Status data ::: ", data);
                        //console.log("Status type ::: ", type);
                        //console.log("Status full ::: ", full);

                        if (type === 'display') {

                            let statusClass = '';
                            let statusLayout = '';
                            let statusText = '';

                            statusClass = data ? 'bg-label-success' : 'bg-label-danger';

                            statusText = data ? 'Ativo' : 'Inativo';

                            statusLayout = '<span name="spStatus" data-status=' + data + ' class="badge rounded-pill ' + statusClass + '"> ' + statusText + '</span> ';

                            return statusLayout
                        }

                        return data;
                    }
                },
                // COLUNA - ACOES
                {
                    data: 'Id',
                    targets: -1,
                    className: "text-center",
                    orderable: false,
                    searchable: false,
                    render: function (data, type, full, meta) {

                        let btns = '';

                        //console.log("Acao data ::: ", data);
                        //console.log("Acao type ::: ", type);
                        //console.log("Acao full ::: ", full);
                        //console.log("Acao meta ::: ", meta);
                        if (type === 'display') {
                            let itemId = data;
                            let itemDados = full;
                            let itemObjJson = encodeURIComponent(JSON.stringify(full));

                            btns =
                                '<div class="d-inline-block text-nowrap">' +
                                '<a href="javascript:fn_Pop(' + itemObjJson + ',' + "'Edit'" + ');" class="btn btn-sm btn-icon btn-text-secondary waves-effect rounded-pill text-body me-1"><i class="ri-edit-box-line ri-22px"></i></a>' +
                                '</div>'
                        }

                        return (btns);
                    }
                },
            ],
            //order: varCol_Ordenacao,
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
                    className: 'dt-export-btn btn btn-label-primary dropdown-toggle me-4 waves-effect waves-light border-none',
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
            }
        });
    }
}

function fn_GridComplete(grid) {

    var thisApi = grid.api();

    var countRows = grid.api().rows().count();
    //console.log("countRows ::: ", countRows);

    $('.card-header').after('<hr class="my-0">');

    //Titulo Tabela
    $('div.head-label').html(`<h5 class="card-title mb-0">${var_Nome}</h5>`);

    if (countRows > 0) {
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

            });

        //Checkbox Filtar Ativos
        var elfilter = document.querySelector('.dataTables_filter'),
            divElement = document.createElement('div');

        divElement.setAttribute("id", "divFilter_ChkStatus");
        divElement.style.marginRight = "auto";
        divElement.style.marginTop = "0.75rem";

        divElement.innerHTML = `<div class="form-check form-switch mb-2">
          <input class="form-check-input" type="checkbox" id="chkFilterAtivo">
          <label class="form-check-label" for="chkFilterAtivo">Exibir Somente Ativos</label>
        </div>`;

        elfilter.insertAdjacentElement('beforebegin', divElement);

        //Verifca Selecao de ver Ativos
        fnhelper_CheckVerAtivos();

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

//#region FUNCOES

// fnhelper_CheckVerAtivos é comum (helper-ui-common.js).

//#endregion
