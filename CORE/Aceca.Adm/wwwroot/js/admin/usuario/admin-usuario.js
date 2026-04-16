/**
 * App -> Usuarios
 */

'use strict';

//#region Declare

let var_Nome = 'Gest&atilde;o & Seguran&ccedil;a -> Usu&aacute;rios',
    var_Controller = '/Usuario',
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

        varCol_Exportar = [2, 3, 4, 5, 6, 7],
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
                    //console.log("data fn :: ", result)
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
                    data: 'socio.nome',
                    targets: 2,
                },
                // COLUNA - Login
                {
                    data: 'nomeUsuario',
                    targets: 3,
                    //className: "text-center",
                },
                // COLUNA - Email
                {
                    data: 'email',
                    targets: 4,
                    //className: "text-center",
                },
                // COLUNA - Tipo Usuario
                {
                    data: 'socio.socioPerfilId',
                    targets: 5,
                    className: "text-center",
                    render: function (data, type, full) {
                        let id = full.id;

                        if (id != 0 && data !== undefined && data !== null) {

                            let statusClass,
                                statusLayout,
                                statusDescricao = full.socio.socioPerfil.descricao,
                                propostaStatusId = data;

                            switch (propostaStatusId) {
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
                // COLUNA - Ultimo Login
                {
                    data: 'ultimoLogin',
                    targets: 6,
                    className: "text-center",
                    render: function (data, type, full) {
                        let id = full.id;

                        if (id != 0 && data !== undefined && data !== null) {

                            var rowvalueallday = full["tm_mm_mitglieder.Geburtsdatum"];
                            //console.log("fn_GridListFilterDataProposta rowvalueallday ::: ", rowvalueallday);
                            if (rowvalueallday == '0000-00-00') {
                                var gdat = '1900-01-01';
                                return gdat;
                            } else {
                                let dataFormat = moment.utc(data.toLocaleString()).format("DD/MM/YYYY");

                                return (dataFormat);
                            }
                        } else {
                            return '';//'Data Indispon&iacute;vel';
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
                    visible: false,
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
                /*
                {
                    text: '<i class="ri-add-line"></i> <span class="d-none d-sm-inline-block">Adicionar Novo</span>',
                    className: 'btnAddNew create-new btn btn-primary waves-effect waves-light',
                    action: function (e, dt, node, config) {
                        //console.log("BTN NEW ::: ", dt);
                        fn_Pop(null, 'Create');
                    }
                }
                */
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

function fn_LoadCmb_SocioPerfil() {
    //console.log("fn_LoadCmb_SocioPerfil ::: ");

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

//#region POP

function fn_Pop(obj, action) {
    console.log("fn_Pop varItems_Row !", obj);
    //console.log("fn_Pop action !", action);

    const popAddNewItem = document.querySelector('#pop-add-new-item');

    popAddNewItemEl = new bootstrap.Offcanvas(popAddNewItem);

    // Pop ID
    (popAddNewItem.querySelector('#hdId').value = (obj === null ? 0 : obj.Id)),
        (popAddNewItem.querySelector('#hdSocioId').value = (obj === null ? 0 : obj.socioId)),

        // Pop Dados
        (popAddNewItem.querySelector('.dt-line-01').value = (obj === null ? '' : obj.socio.nome)),
        (popAddNewItem.querySelector('.dt-line-02').value = (obj === null ? '' : obj.nomeUsuario)),
        (popAddNewItem.querySelector('.dt-line-03').value = (obj === null ? '' : obj.email)),
        (popAddNewItem.querySelector('.dt-line-04').value = (obj === null ? '-- Selecionar --' : obj.socio.socioPerfilId));
        (popAddNewItem.querySelector('.dt-line-05').checked = (obj === null ? false : obj.ativo));


    // Pop Action
    (popAddNewItem.querySelector('.offcanvas-title').textContent = (action === 'Edit') ? 'Alterar Registro' : 'Novo Registro');
    (popAddNewItem.querySelector('.data-submit').textContent = (action === 'Edit') ? 'Alterar' : 'Adicionar');

    if (obj !== null) {

        $("#cmb_SocioPerfil").val(obj.socio.socioPerfilId).change();

        //console.log("fn_Pop ex val ::: ", $("#cmb_SocioPerfil").val());
    }

    // Open Pop
    popAddNewItemEl.show();
}

function fn_PopGetObj() {

    const objFormData = {
        Id: $('#hdId').val(),
        SocioId: $('#hdSocioId').val(),
        Email: $('.form-add-new-item .dt-line-03').val(),
        Nome: $('.form-add-new-item .dt-line-01').val(),
        NomeUsuario: $('.form-add-new-item .dt-line-02').val(),        
        SocioPerfilId: $('#cmb_SocioPerfil').val(),
        Ativo: $('.form-add-new-item .dt-line-05').is(':checked'),
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

    var varItems_Id = varItems_Row.id;

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
    console.log("EDIT CLICK ::: ", varItems_Row);
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