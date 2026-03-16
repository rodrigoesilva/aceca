/**
 * Admin -> Socio
 */

'use strict';

//#region Declare

let var_Nome = 'S&oacute;cio',
    var_Controller = '/Socio',
    var_ControllerCmb = '/HelperExtensions',

    varTbl_Obj = $('.datatables-basic'),
    varTbl_Data,

    formValid, popAddNewItemEl;

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

        fn_LoadCmb_SocioPerfil();

        // Form validation
        const formAddNewItem = document.getElementById('form-pop-add-new-item');

        formValid = fn_PopValidator(formAddNewItem);

        // Carrega Dados Grid
        fn_GridList(formValid);
    })();
});

//#endregion

//#region GRID
function fn_GridList(formValid) {

    var varLang_UrlTranslate = 'https://cdn.datatables.net/plug-ins/1.12.1/i18n/pt-BR.json',

        varAjax_UrlController = `${var_Controller}/ListGrid`,
        varAjax_TypeAction = 'GET',

        varCol_Exportar = [2, 3, 4],
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
            //serverSide: true,
            paging: true,
            scrollCollapse: true,
            ordering: true,
            destroy: true,

            ajax: {
                crossDomain: true,
                url: varAjax_UrlController,
                type: varAjax_TypeAction,
                //dataSrc: ''
                dataSrc: function (result) {
                    console.log("data fn :: ", result)
                    return result.data;
                }
            },
            columnDefs: [
                // COLUNA - Responsive
                {
                    data: 'id',
                    targets: 0,
                    className: 'control',
                    visible: false,
                    render: function (data, type, full, meta) {
                        return '';
                    }
                },
                // COLUNA - ID checkbox
                {
                    data: 'id',
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
                    data: 'nome',
                    targets: 2,
                },
                // COLUNA - Tipo Socio
                {
                    data: 'socioPerfilId',
                    targets: 3,
                    className: "text-center",
                    render: function (data, type, full) {
                        let id = full.id;

                        if (id != 0 && data !== undefined && data !== null) {

                            let statusClass,
                                statusLayout,
                                statusDescricao = full.socioPerfil.descricao,
                                socioPerfilId = data;

                            switch (socioPerfilId) {
                                case 1: //'Nivel 1'
                                    statusClass = 'bg-label-warning';
                                    break;
                                case 2: //'Nivel 2'
                                    statusClass = 'bg-label-info';
                                    break;
                                case 3: //'Nivel 3'
                                    statusClass = 'bg-label-secondary';
                                    break;
                                case 4: //'Nivel 4'
                                    statusClass = 'bg-label-secondary';
                                    break;
                                case 5: //'Aprovada'
                                    statusClass = 'bg-label-success';
                                    break;
                                case 6: //'Cancelada'
                                    statusClass = 'bg-label-danger';
                                    break;
                            }

                            statusLayout = '<span class="badge rounded-pill ' + statusClass + '"> ' + statusDescricao + '</span> ';

                            //console.log("Status statusLayout ::: ", statusLayout);

                            return statusLayout;

                        } else {
                            return '';
                        }
                    }
                },
                // COLUNA - Status                    
                {
                    targets: -2,
                    data: 'ativo',
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
                // COLUNA - Botoes Acoes
                {
                    data: 'id',
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
                        fn_Pop(null, 'Create');
                    }
                }
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

    // VALIDA SUBMIT POP
    formValid.on('core.form.valid', function (e) {
        //console.log("e ::: ", e);

        var action = document.querySelector('.data-submit').textContent;
        //console.log("action ::: ", action);

        if (action === 'Alterar') {
            var objFormData = fn_PopGetObj();
            //console.log("objFormData ::: ", objFormData);

            fnItem_Edit(objFormData)
        } else {
            fnItem_Add(varTbl_Obj)
        }
        //fnItem_Add(abc);
    });
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
        fn_CheckVerAtivos();

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

function fn_CheckVerAtivos() {

    const chkVerAtivos = document.querySelector('#chkFilterAtivo');

    // //CHK VER ATIVOS
    if (chkVerAtivos) {
        chkVerAtivos.addEventListener('change', function () {
            var table = $('.datatables-basic').DataTable();

            if (this.checked) {
                //console.log("Checkbox is checked..");
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
                    //console.log("result :: ", result);

                    $.fn.dataTable.ext.search.push(
                        function (settings, data, dataIndex) {

                            var rowAtivo = $(table.row(dataIndex).node()).find('[name="spStatus"]').data("status") == true;

                            return rowAtivo
                        }
                    );

                    table.draw();
                });
            } else {
                //console.log("Checkbox is not checked..");
                $.fn.dataTable.ext.search.pop();
                table.draw();
            }
        });
    }
}

//#endregion

//#region POP

function fn_Pop(obj, action) {
    console.log("fn_Pop varItems_Row !", obj);
    //console.log("fn_Pop action !", action);

    const popAddNewItem = document.querySelector('#pop-add-new-item');

    popAddNewItemEl = new bootstrap.Offcanvas(popAddNewItem);

    // Pop ID
    (popAddNewItem.querySelector('#hdId').value = (obj === null ? 0 : obj.Id)),
        (popAddNewItem.querySelector('#hdSocioPerfilId').value = (obj === null ? 0 : obj.socioPerfilId)),

        // Pop Dados
        (popAddNewItem.querySelector('.dt-line-01').value = (obj === null ? '' : obj.nome)),
        (popAddNewItem.querySelector('.dt-line-04').value = (obj === null ? '-- Selecionar --' : obj.socioPerfilId));
        (popAddNewItem.querySelector('.dt-line-05').checked = (obj === null ? false : obj.ativo));


    // Pop Action
    (popAddNewItem.querySelector('.offcanvas-title').textContent = (action === 'Edit') ? 'Alterar Registro' : 'Novo Registro');
    (popAddNewItem.querySelector('.data-submit').textContent = (action === 'Edit') ? 'Alterar' : 'Adicionar');

    if (obj !== null) {

        $("#cmb_SocioPerfil").val(obj.socioPerfilId).change();

        console.log("fn_Pop ex val ::: ", $("#cmb_SocioPerfil").val());
    }

    // Open Pop
    popAddNewItemEl.show();
}

function fn_PopGetObj() {

    const objFormData = {
        Id: $('#hdId').val(),
        Nome: $('.form-add-new-item .dt-line-01').val(),
        SocioPerfilId: $('#cmb_SocioPerfil').val(),
        Ativo: $('.form-add-new-item .dt-line-05').is(':checked')
    };

    console.log("fn_PopGetObj !", objFormData);

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

function fnItem_Delete(varItems_Row) {

    //console.log("DELETE OBJ ::: ", varItems_Row);

    var varItems_Id = varItems_Row.Id;

    //console.log("DELETE ID ::: ", varItems_Id);

    var varAjax_UrlController = `${var_Controller}/Delete`, //'/TipoMidia/Delete',
        varAjax_TypeAction = 'DELETE',
        varAjax_TypeData = 'JSON';

    const swalWithBootstrapButtons = Swal.mixin({
        customClass: {
            confirmButton: "btn btn-label-success waves-effect",
            cancelButton: "btn btn-label-danger waves-effect"
        },
        buttonsStyling: false
    });

    swalWithBootstrapButtons.fire({
        title: "Tem certeza?",
        icon: "warning",
        html: `<b> Essa a&ccedil;&atilde;o ir&aacute; excluir esse item </b> <br><br> voc&ecirc; n&atilde;o poder&aacute; reverter isso!`,
        showCancelButton: true,
        confirmButtonText: `<i class="ri-chat-delete-line"></i> &nbsp; Sim, confirmar!`,
        cancelButtonText: `<i class="ri-check-double-line"></i> &nbsp; N&atilde;o, cancelar!`,
        reverseButtons: true
    }).then((result) => {
        if (result.isConfirmed) {

            $.busyLoadFull("show");

            $.ajax(
                {
                    type: varAjax_TypeAction,
                    //dataType: varAjax_TypeData,
                    url: varAjax_UrlController,
                    data: {id: varItems_Id},
                    success: function (result) {
                        //console.log("result  :: ", result);
                        //console.log("result bResult :: ", result.bResult);

                        var varTbl;

                        if ($.fn.dataTable.isDataTable('.datatables-basic')) {
                            //console.log("YES :: ");
                            varTbl = varTbl_Obj.DataTable();

                            $.busyLoadFull("hide");

                            Swal.fire({
                                title: 'Deletado!',
                                icon: 'success',
                                html: 'Item exclu&iacute;do com sucesso !!',
                                customClass: {
                                    confirmButton: 'btn btn-success waves-effect waves-light'
                                }
                            }).then((result) => {
                                varTbl.ajax.reload(null, false);
                            });
                        } else {
                            // console.log("NO :: ");
                            varTbl = $('#example').DataTable({
                                paging: false
                            });
                        }
                    },
                    error: function (XMLHttpRequest, textStatus, errorThrown) {
                        console.log("XMLHttpRequest  :: ", XMLHttpRequest);
                        console.log("textStatus  :: ", textStatus);
                        console.log("errorThrown  :: ", errorThrown);
                        console.log("result  :: Error while posting SendResult");

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

                        return false;
                    }
                });

        } else if (result.dismiss === Swal.DismissReason.cancel) {
            swalWithBootstrapButtons.fire({
                title: "Cancelado",
                icon: "info",
                html: "Nenhuma a&ccedil;&atilde;o foi realizada !!",
                focusConfirm: true,
                confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                customClass: {
                    confirmButton: 'btn btn-label-secondary waves-effect'
                }
            });
        }
    });
}

function fnItem_Edit(varItems_Row) {
    //console.log("EDIT CLICK ::: ", varItems_Row);
    //var varPop_BtnAction = 'Edit';

    //fn_Pop(varItems_Row, varPop_BtnAction);

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
                url: varAjax_UrlController, //"/TipoMidia/Create",  //
                type: varAjax_TypeAction,
                dataType: varAjax_TypeData,
                data: varItems_Row,
                // contentType: varAjax_TypeContent,
                success: function (result) {

                    //console.log("result  :: ", result);
                    //console.log("result bResult :: ", result.bResult);

                    if (result.bResult) {

                        $.busyLoadFull("hide");

                        var varTbl;

                        if ($.fn.dataTable.isDataTable('.datatables-basic')) {
                            //console.log("YES :: ");
                            varTbl = varTbl_Obj.DataTable();

                            // Hide offcanvas using javascript method
                            popAddNewItemEl.hide();

                            $.busyLoadFull("hide");

                            Swal.fire({
                                title: 'Dados Salvos!',
                                icon: 'success',
                                text: 'Item alterado com sucesso.',
                                customClass: {
                                    confirmButton: 'btn btn-success waves-effect waves-light'
                                }
                            }).then((result) => {
                                varTbl.ajax.reload(null, false);
                            });
                        } else {
                            // console.log("NO :: ");
                            varTbl = $('#example').DataTable({
                                paging: false
                            });
                        }
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
                error: function (XMLHttpRequest, textStatus, errorThrown) {
                    //console.log("XMLHttpRequest  :: ", XMLHttpRequest);
                    //console.log("textStatus  :: ", textStatus);
                    //console.log("errorThrown  :: ", errorThrown);
                    //console.log("result  :: Error while posting SendResult");

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

                    return false;
                }
            });
    }
}

function fnItem_Add(varTbl_Obj) {
    //console.log("ADD CLICK ::: ", varTbl_Obj.row);

    var varPop_BtnAction = 'Create';

    var varAjax_UrlController = `${var_Controller}/Create`,
        varAjax_TypeAction = 'POST',
        varAjax_TypeData = 'JSON',
        varAjax_TypeContent = 'application/json; charset=utf-8';

    const formData_newItem = fn_PopGetObj();

    if (formData_newItem != '') {

        $.busyLoadFull("show");

        $.ajax(
            {
                url: varAjax_UrlController, //"/TipoMidia/Create",  //
                type: varAjax_TypeAction,
                dataType: varAjax_TypeData,
                data: formData_newItem,
                // contentType: varAjax_TypeContent,
                success: function (result) {

                    //console.log("result  :: ", result);
                    //console.log("result bResult :: ", result.bResult);

                    if (result.bResult) {

                        $.busyLoadFull("hide");

                        var varTbl;

                        if ($.fn.dataTable.isDataTable('.datatables-basic')) {
                            //console.log("YES :: ");
                            varTbl = varTbl_Obj.DataTable();


                            // Hide offcanvas using javascript method
                            popAddNewItemEl.hide();

                            $.busyLoadFull("hide");


                            Swal.fire({
                                title: 'Dados Salvos!',
                                icon: 'success',
                                text: 'Item adicionado com sucesso.',
                                customClass: {
                                    confirmButton: 'btn btn-success waves-effect waves-light'
                                }
                            }).then((result) => {
                                varTbl.ajax.reload(null, false);
                            });
                        } else {
                            // console.log("NO :: ");
                            varTbl = $('#example').DataTable({
                                paging: false
                            });
                        }
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

                    return false;
                }
            });
    }
}

//#endregion


//#region MODAL

function fn_Modal(obj, action) {
    //console.log("fn_Modal obj::: ", obj);
    //console.log("fn_Modal action::: ", action);

    let resultLoad = fnItem_Edit_CarregarDados(obj, action)

    // console.log("fn_Modal resultLoad::: ", resultLoad);
}
function fn_ModalSalvar(e) {
    const formData = new FormData(document.forms['form-modal-full-edit']);
    //console.log("fn_ModalSalvar formData ::: ", formData);
    //console.log("fn_ModalSalvar formEle GET ::: ", formData.get('cinema_Nome'));

    fn_ModalGetObj(formData, 'Save')
}
async function fn_ModalGetObj(data, action) {
    console.log("fn_ModalGetObj data ::: ", data);
    console.log("fn_ModalGetObj action ::: ", action);

    $.busyLoadFull("hide");

    $('#modalAddNovaMarca').modal('show');

    /*
    //Carregamento de dados ao abrir
    if (action === 'Edit') {
        let socioId = $("#hdId").val(data.socio.id),
            socioAniversarioId = $("#hdSocioAniversarioId").val(data.socioAniversario.id),
            socioContatoId = $("#hdSocioContatoId").val(data.socioContato.id),
            socioEnderecoId = $("#hdSocioEnderecoId").val(data.socioEndereco.id),
            socioFinanceiroId = $("#hdSocioFinanceiroId").val(data.socioFinanceiro.id),
            socioPerfilId = $("#hdSocioPerfilId").val(data.socioPerfil.id);

        var loadCmbs = await fn_PopLoadCombos(action);
        console.log("fn_ModalGetObj loadCmb ::: ", loadCmbs);

        if (loadCmbs) {

            const modalFullEditItem = document.querySelector('#modal-full-edit-item');
            (modalFullEditItem.querySelector('.modal-title').textContent = (action === 'UPDATE') ? 'Alterar Registro' : 'Novo Registro');
            (modalFullEditItem.querySelector('.data-submit').textContent = (action === 'UPDATE') ? 'Alterar' : 'Adicionar');

            //#region ACCORDIONS

            //#region <!-- Cinema Info -->//Table: cinema

            $('#cinema_Nome').val(data.nome);
            $('#cinema_Exibidor').val(data.exibidorId);
            $('#cinema_Tipo').val(data.tipoCinemaId);
            $('#cinema_Classificacao').val(data.cinemaClassificacaoId);
            $('#cinema_Programacao').val(data.tipoCinemaProgramacaoId);
            $('#cinema_Salas').val(data.quantidadeSalas);
            $('#cinema_QtdMonitores').val(data.quantidadeMonitores);
            $('#cinema_IdSEcom').val(data.codSecom);
            $('#cinema_Obs').val(data.observacao);
            $('#cinema_Status').prop('checked', data.ativo);
            //PublicoAnual
            //NaoMostrarSite
            //Fechado

            //#endregion

            //#region <!-- Email Receber Material Info -->//Table: cinema_email_recebe_material -- 
            // id
            // cinemaId

            //$('#chkCinemaEmailReceberMaterial_SalvarDados').prop('checked', data.cinemaEmailRecebeMaterial.ativo);

            $('#cinemaEmailReceberMaterial_Email01').val(data.cinemaEmailRecebeMaterial.emailRecebeMaterial1);
            $('#cinemaEmailReceberMaterial_Email02').val(data.cinemaEmailRecebeMaterial.emailRecebeMaterial2);
            $('#cinemaEmailReceberMaterial_Email03').val(data.cinemaEmailRecebeMaterial.emailRecebeMaterial3);
            $('#cinemaEmailReceberMaterial_Email04').val(data.cinemaEmailRecebeMaterial.emailRecebeMaterial4);

            //#endregion

            //#region <!-- Email Programacao Info -->//Table: cinema_email_programacao -- 
            // id
            // cinemaId

            //$('#chkCinemaEmailProgramacao_SalvarDados').prop('checked', data.cinemaEmailProgramacao.ativo);

            $('#cinemaEmailProgramacao_Email01').val(data.cinemaEmailProgramacao.emailProg1);
            $('#cinemaEmailProgramacao_Email02').val(data.cinemaEmailProgramacao.emailProg2);
            $('#cinemaEmailProgramacao_Email03').val(data.cinemaEmailProgramacao.emailProg3);
            $('#cinemaEmailProgramacao_Email04').val(data.cinemaEmailProgramacao.emailProg4);
            $('#cinemaEmailProgramacao_Email05').val(data.cinemaEmailProgramacao.emailProg5);
            $('#cinemaEmailProgramacao_Email06').val(data.cinemaEmailProgramacao.emailProg6);
            $('#cinemaEmailProgramacao_Email07').val(data.cinemaEmailProgramacao.emailProg7);
            $('#cinemaEmailProgramacao_Email08').val(data.cinemaEmailProgramacao.emailProg8);
            $('#cinemaEmailProgramacao_Email09').val(data.cinemaEmailProgramacao.emailProg9);
            $('#cinemaEmailProgramacao_Email10').val(data.cinemaEmailProgramacao.emailProg10);

            //#endregion

            //#region <!-- Contato Info -->//Table: cinema_contato --  
            // id
            // cinemaId

            //$('#chkCinemaContato_SalvarDados').prop('checked', data.cinemaContato.ativo);

            $('#cinemaContato_Nome').val(data.cinemaContato.contato); // contato
            $('#cinemaContato_Tel01').val(data.cinemaContato.tel1);
            $('#cinemaContato_Email01').val(data.cinemaContato.email);
            $('#cinemaContato_Tel02').val(data.cinemaContato.tel2);
            $('#cinemaContato_Email02').val(data.cinemaContato.email);


            // ddd1
            // tel1
            // tipoTel1
            // compTel1
            // tel2
            // ddd2
            // tipoTel2
            // compTel2
            // email
            //#endregion

            //#region <!-- Ingresso.com Info -->//Table: cinema_ingresso
            // id
            // cinemaId

            //$('#chkCinemaIngresso_SalvarDados').prop('checked', data.cinemaIngresso.ativo);

            $('#cinemaIngresso_Nome').val(data.cinemaIngresso.ingressoComName);
            $('#cinemaIngresso_ID').val(data.cinemaIngresso.ingressoComId);
            $('#cinemaIngresso_CidadeID').val(data.cinemaIngresso.ingressoComCityId);
            $('#cinemaIngresso_UrlKey').val(data.cinemaIngresso.ingressoComUrlKey);

            //#endregion

            //#region <!-- Venda Bem Info -->//Table: cinema_vendabem -- 
            // id
            // cinemaId

            //$('#chkCinemaVendaBem_SalvarDados').prop('checked', data.cinemaVendaBem.ativo);

            $('#cinemaVendaBem_Nome').val(data.cinemaVendaBem.descricao);
            $('#cinemaVendaBem_Filial').val(data.cinemaVendaBem.filial);

            //#endregion

            //#region <!-- Velox Info -->//Table: cinema_velox --  
            // id
            // cinemaId

            //$('#chkCinemaVelox_SalvarDados').prop('checked', data.cinemaVelox.ativo);

            $('#cinemaVelox_Nome').val(data.cinemaVelox.descricao); // descricao
            $('#cinemaVelox_Praca').val(data.cinemaVelox.codpraca);

            //#endregion

            //#region <!-- Rentrak Info -->//Table: cinema_rentrak --  
            // id
            // cinemaId

            //$('#chkCinemaRentrak_SalvarDados').prop('checked', data.cinemaRentrak.ativo);

            $('#cinemaRentrak_Nome').val(data.cinemaRentrak.descricao);
            $('#cinemaRentrak_CodigoReferencia').val(data.cinemaRentrak.codigoReferencia);

            //#endregion

            //#region <!-- Endereco Info -->//Table: cinema_endereco -- 
            // id
            // cinemaId

            //$('#chkCinemaEndereco_SalvarDados').prop('checked', data.cinemaEndereco.ativo);

            $('#cinemaEndereco_Localizacao').val(data.cinemaEndereco.localizacao);
            $('#cinemaEndereco_Endereco').val(data.cinemaEndereco.endereco);
            $('#cinemaEndereco_Numero').val(data.cinemaEndereco.numero);
            $('#cinemaEndereco_Complemento').val(data.cinemaEndereco.complemento);
            $('#cinemaEndereco_Bairro').val(data.cinemaEndereco.bairro);
            $('#cinemaEndereco_Cidade').val(data.cinemaEndereco.cidade);
            $('#cinemaEndereco_UF').val(data.cinemaEndereco.estadoId);
            $('#cinemaEndereco_Regiao').val(data.cinemaEndereco.regiaoId);

            //#endregion

            //#region <!-- Classe Social Info -->//Table: cinema_classe_social --  
            // id
            // cinemaId

            //$('#chkCinemaClasseSocial_SalvarDados').prop('checked', true);

            //#endregion

            //#region <!-- Juridico Info -->//Table: cinema_juridico -  
            // id
            // cinemaId
            // razaoSocial
            // cnpj
            // ie
            // ccm

            //$('#chkCinemaJuridico_SalvarDados').prop('checked', data.cinemaJuridico.ativo);

            $('#cinemaJuridico_RazaoSocial').val(data.cinemaJuridico.razaoSocial);
            $('#cinemaJuridico_CNPJ').val(data.cinemaJuridico.cnpj);
            $('#cinemaJuridico_InscricaoEstadual').val(data.cinemaJuridico.ie);
            $('#cinemaJuridico_CCM').val(data.cinemaJuridico.ccm);

            //#endregion

            //#region <!-- Carimbo Info -->//Table: cinema_carimbo --  
            // id
            // cinemaId
            // carimboComprovanteExibicao
            // mostraCarimboComprovanteExibicao
            //#endregion

            //#endregion      

            $.busyLoadFull("hide");

            $('#modal-full-edit-item').modal('show');
        }
    } else {

        let socioId = $("#hdId").val(data.socio.id),
            socioAniversarioId = $("#hdSocioAniversarioId").val(data.socioAniversario.id),
            socioContatoId = $("#hdSocioContatoId").val(data.socioContato.id),
            socioEnderecoId = $("#hdSocioEnderecoId").val(data.socioEndereco.id),
            socioFinanceiroId = $("#hdSocioFinanceiroId").val(data.socioFinanceiro.id),
            socioPerfilId = $("#hdSocioPerfilId").val(data.socioPerfil.id);

        //#region ACCORDIONS

        //#region <!-- Cinema Info -->//Table: cinema

        const objFormData_CinemaInfo = {
            Id: data.get('hdCinemaId'),
            Nome: data.get('cinema_Nome'),
            ExibidorId: data.get('#cinema_Exibidor'),
            TipoCinemaId: data.get('cinema_Tipo'),
            CinemaClassificacaoId: data.get('cinema_Classificacao'),
            TipoCinemaProgramacaoId: data.get('cinema_Programacao'),
            QuantidadeSalas: data.get('cinema_Salas'),
            QuantidadeMonitores: data.get('cinema_QtdMonitores'),
            CodSecom: data.get('cinema_IdSEcom'),
            Observacao: data.get('cinema_Obs'),
            Ativo: data.get('cinema_Status') === 'on' ? true : false

            //PublicoAnual
            //NaoMostrarSite
            //Fechado
        };
        //console.log("fn_ModalSalvar objFormData_CinemaInfo ::: ", objFormData_CinemaInfo);

        //#endregion

        //#region <!-- Email Receber Material Info -->//Table: cinema_email_recebe_material -- 
        // id
        // cinemaId

        const objFormData_CinemaEmailReceberMaterial = {
            CinemaEmailReceberMaterial_SalvarDados: data.get('chkCinemaEmailReceberMaterial_SalvarDados') === 'on' ? true : false,

            CinemaId: cinemaId,
            EmailReceberMaterial_Email01: data.get('cinemaEmailReceberMaterial_Email01'),
            EmailReceberMaterial_Email02: data.get('cinemaEmailReceberMaterial_Email02'),
            EmailReceberMaterial_Email03: data.get('cinemaEmailReceberMaterial_Email03'),
            EmailReceberMaterial_Email04: data.get('cinemaEmailReceberMaterial_Email04'),
        }
        console.log("fn_ModalSalvar objFormData_CinemaEmailReceberMaterial ::: ", objFormData_CinemaEmailReceberMaterial);

        //#endregion

        //#region <!-- Email Programacao Info -->//Table: cinema_email_programacao -- 
        // id

        const objFormData_CinemaEmailProgramacao = {
            CinemaEmailProgramacao_SalvarDados: data.get('chkCinemaEmailProgramacao_SalvarDados') === 'on' ? true : false,

            CinemaId: cinemaId,
            EmailProgramacao_Email01: data.get('cinemaEmailProgramacao_Email01'),
            EmailProgramacao_Email02: data.get('cinemaEmailProgramacao_Email02'),
            EmailProgramacao_Email03: data.get('cinemaEmailProgramacao_Email03'),
            EmailProgramacao_Email04: data.get('cinemaEmailProgramacao_Email04'),
            EmailProgramacao_Email05: data.get('cinemaEmailProgramacao_Email05'),
            EmailProgramacao_Email06: data.get('cinemaEmailProgramacao_Email06'),
            EmailProgramacao_Email07: data.get('cinemaEmailProgramacao_Email07'),
            EmailProgramacao_Email08: data.get('cinemaEmailProgramacao_Email08'),
            EmailProgramacao_Email09: data.get('cinemaEmailProgramacao_Email09'),
            EmailProgramacao_Email10: data.get('cinemaEmailProgramacao_Email10'),
        }
        console.log("fn_ModalSalvar objFormData_CinemaEmailProgramacao ::: ", objFormData_CinemaEmailProgramacao);

        //#endregion

        //#region <!-- Contato Info -->//Table: cinema_contato --  
        // id
        // ddd1
        // tipoTel1
        // compTel1
        // tel2
        // ddd2
        // tipoTel2
        // compTel2

        const objFormData_CinemaContato = {
            CinemaContato_SalvarDados: data.get('chkCinemaContato_SalvarDados') === 'on' ? true : false,

            CinemaId: cinemaId,
            Nome: data.get('cinemaContato_Nome'),
            Tel01: data.get('cinemaContato_Tel01'),
            Email01: data.get('cinemaContato_Email01'),
            Tel02: data.get('cinemaContato_Tel02'),
            Email02: data.get('cinemaContato_Email02'),
        }
        console.log("fn_ModalSalvar objFormData_CinemaContato ::: ", objFormData_CinemaContato);
        //#endregion

        //#region <!-- Ingresso.com Info -->//Table: cinema_ingresso
        // id

        const objFormData_CinemaIngresso = {
            CinemaIngresso_SalvarDados: data.get('chkCinemaIngresso_SalvarDados') === 'on' ? true : false,

            CinemaId: cinemaId,
            Nome: data.get('cinemaIngresso_Nome'),
            ID: data.get('cinemaIngresso_ID'),
            CidadeID: data.get('cinemaIngresso_CidadeID'),
            UrlKey: data.get('cinemaIngresso_UrlKey'),
        }
        console.log("fn_ModalSalvar objFormData_CinemaIngresso ::: ", objFormData_CinemaIngresso);

        //#endregion

        //#region <!-- Venda Bem Info -->//Table: cinema_vendabem -- 
        // id
        const objFormData_CinemaVendaBem = {
            CinemaVendaBem_SalvarDados: data.get('chkCinemaVendaBem_SalvarDados') === 'on' ? true : false,

            CinemaId: cinemaId,
            Nome: data.get('cinemaVendaBem_Nome'),
            Filial: data.get('cinemaVendaBem_Filial'),
        }
        console.log("fn_ModalSalvar objFormData_CinemaVendaBem ::: ", objFormData_CinemaVendaBem);

        //#endregion

        //#region <!-- Velox Info -->//Table: cinema_velox --  
        // id
        const objFormData_CinemaVelox = {
            CinemaVendaBem_SalvarDados: data.get('chkCinemaVelox_SalvarDados') === 'on' ? true : false,

            CinemaId: cinemaId,
            Nome: data.get('cinemaVelox_Nome'),
            Praca: data.get('cinemaVelox_Praca'),
        }
        console.log("fn_ModalSalvar objFormData_CinemaVelox ::: ", objFormData_CinemaVelox);

        //#endregion

        //#region <!-- Rentrak Info -->//Table: cinema_rentrak --  
        // id
        const objFormData_CinemaRentrak = {
            CinemaVendaBem_SalvarDados: data.get('chkCinemaRentrak_SalvarDados') === 'on' ? true : false,

            CinemaId: cinemaId,
            Nome: data.get('cinemaRentrak_Nome'),
            CodigoReferencia: data.get('cinemaRentrak_CodigoReferencia'),
        }
        console.log("fn_ModalSalvar objFormData_CinemaRentrak ::: ", objFormData_CinemaRentrak);

        //#endregion

        //#region <!-- Endereco Info -->//Table: cinema_endereco -- 
        // id
        const objFormData_CinemaEndereco = {
            CinemaEmailProgramacao_SalvarDados: data.get('chkCinemaEndereco_SalvarDados') === 'on' ? true : false,

            CinemaId: cinemaId,
            Localizacao: data.get('cinemaEndereco_Localizacao'),
            Endereco: data.get('cinemaEndereco_Endereco'),
            Numero: data.get('cinemaEndereco_Numero'),
            Complemento: data.get('cinemaEndereco_Complemento'),
            Bairro: data.get('cinemaEndereco_Bairro'),
            Cidade: data.get('cinemaEndereco_Cidade'),
            UF: data.get('cinemaEndereco_UF'),
            Regiao: data.get('cinemaEndereco_Regiao'),
        }
        console.log("fn_ModalSalvar objFormData_CinemaEndereco ::: ", objFormData_CinemaEndereco);

        //#endregion

        //#region <!-- Classe Social Info -->//Table: cinema_classe_social --  
        // id
        // cinemaId

        //$('#chkCinemaClasseSocial_SalvarDados').prop('checked', true);


        //#endregion

        //#region <!-- Juridico Info -->//Table: cinema_juridico -  
        // id
        const objFormData_CinemaJuridico = {
            CinemaJuridico_SalvarDados: data.get('chkCinemaJuridico_SalvarDados') === 'on' ? true : false,

            CinemaId: cinemaId,
            RazaoSocial: data.get('cinemaJuridico_RazaoSocial'),
            CNPJ: data.get('cinemaJuridico_CNPJ'),
            InscricaoEstadual: data.get('cinemaJuridico_InscricaoEstadual'),
            CCM: data.get('cinemaJuridico_CCM'),
        }
        console.log("fn_ModalSalvar objFormData_CinemaJuridico ::: ", objFormData_CinemaJuridico);

        //#endregion

        //#region <!-- Carimbo Info -->//Table: cinema_carimbo --  
        // id
        // cinemaId
        // carimboComprovanteExibicao
        // mostraCarimboComprovanteExibicao
        //#endregion

        //#endregion

        $.busyLoadFull("hide");

        $('#modal-full-edit-item').modal('hide');
    }

    */
}
function fn_ModalGetInfoAccordion(cinemaId, dataInfo) {
    console.log("fn_ModalGetInfoAccordion cinemaId ::: ", cinemaId);
    console.log("fn_ModalGetInfoAccordion dataInfo ::: ", dataInfo);

    /* 
    if (cinemaId === undefined || cinemaId === null || cinemaId === 0) {
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
        
        let var_ControllerBusca= dataInfo.toLowerCase(),
            varAjax_UrlController = `${var_ControllerBusca}/GetFullById`,
            varAjax_TypeAction = 'POST',
            varAjax_TypeData = 'JSON',
            varAjax_TypeContent = 'application/json; charset=utf-8';

        console.log("fn_ModalGetInfoAccordion varAjax_UrlController ::: ", varAjax_UrlController);
        
        
           $.busyLoadFull("show");
   
           $.ajax(
               {
                   url: varAjax_UrlController,
                   type: varAjax_TypeAction,
                   dataType: varAjax_TypeData,
                   data: {
                       id: obj.Id
                   },
                   success: function (result) {
                       //console.log("result  :: ", result);
   
                       if (result.bResult) {
   
                           fn_ModalGetObj(result.data, action);
   
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
                   error: function (XMLHttpRequest, textStatus, errorThrown) {
                       //console.log("XMLHttpRequest  :: ", XMLHttpRequest);
                       //console.log("textStatus  :: ", textStatus);
                       //console.log("errorThrown  :: ", errorThrown);
                       //console.log("result  :: Error while posting SendResult");
   
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
   
                       return false;
                   }
               });
       
    }
    */
}
function fnItem_Edit_CarregarDados(obj, action) {
    console.log("EfnItem_Edit_CarregarDados obj ::: ", obj);
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

                        fn_ModalGetObj(result.data, action);

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

                    return false;
                }
            });
    }
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
//#endregion

//#region COMBO
function fn_LoadCmb_SocioPerfil() {
    console.log("fn_LoadCmb_SocioPerfil ::: ");

    if ($('#cmb_SocioPerfil').length <= 1) {
        $.ajax(
            {
                crossDomain: true,
                url: `${var_ControllerCmb}/AsyncCmb_SocioPerfil`,
                type: 'GET',
                success: function (data) {
                    //console.log("fn_LoadCmb_SocioPerfil  data ::: ", data);

                    $.each(data, function (id, result) {
                        //console.log("fn_LoadCmb_SocioPerfil  result id ::: ", id);
                        //console.log("fn_LoadCmb_SocioPerfil  result ::: ", result);
                        $("#cmb_SocioPerfil").append($("<option></option>").val(result.value).html(result.text));
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