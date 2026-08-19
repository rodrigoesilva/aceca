/**
 * App Configuracao -> Geral
 */

'use strict';

//#region Declare

let var_Nome = 'Configurações Gerais',
    var_Controller = '/AdmConfig',

    varTbl_Obj = $('.datatables-basic'),
    varTbl_Data,

    formValid, popAddNewItemEl;

//#endregion

//#region CARREGAMENTO INICIAL

document.addEventListener('DOMContentLoaded', function () {
    (function () {
        //console.log("LIST - Todos os recursos terminaram o carregamento!");

        // Form validation
        const formAddNewItem = document.getElementById('form-pop-add-new-item');

        formValid = fn_PopValidator(formAddNewItem);

        // Carrega Dados Grid
        fn_GridList();        
    })();
});

//#endregion

//#region GRID
function fn_GridList() {

    var varLang_UrlTranslate = '/vendor/libs/datatables-bs5/i18n/pt-BR.json',

        varAjax_UrlController = `${var_Controller}/ListGrid`,
        varAjax_TypeAction = 'GET',

        varCol_Exportar = [3, 4, 5, 6],
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
            autoWidth: false,
            scrollX: false,

            ajax: {
                crossDomain: true,
                url: varAjax_UrlController,
                type: varAjax_TypeAction,
                dataSrc: function (result) {
                    //console.log("data fn :: ", result)
                    return result.data;
                }
            },
            columns: [
                // Colunas do JSON
                { data: 'id' },
                { data: 'id' },
                { data: 'id' },
                { data: 'parametro' },
                { data: 'descricao' },
                { data: 'valor' },
                { data: 'ativo' },

            ],
            columnDefs: [
                // COLUNA - Responsive
                {
                    targets: 0,
                    className: 'control',
                    width: '1%',
                    searchable: false,
                    orderable: false,
                    render: function (data, type, full, meta) {
                        return '';
                    }
                },
                // COLUNA - ID checkbox
                {
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
                // COLUNA - Botoes Acoes
                {
                    targets: 2,
                    /*title: 'Ações',*/
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
                                '<div class="d-inline-block">' +
                                '<a href="javascript:;" class="btn btn-sm btn-text-secondary rounded-pill btn-icon dropdown-toggle hide-arrow" data-bs-toggle="dropdown" data-bs-popper-config="{&quot;strategy&quot;:&quot;fixed&quot;}"><i class="ri-more-2-line ri-22px"></i></a>' +
                                '<ul class="dropdown-menu dropdown-menu-end m-0">' +
                                '<li><a href="javascript:fn_Pop(' + itemObjJson + ',' +  "'Edit'" + ');" class="dropdown-item edit-record">Editar</a></li>' +
                                '<div class="dropdown-divider"></div>' +
                                '<li><a href="javascript:fnhelper_ItemDelete(' + itemObjJson + ',' + "'" + var_Controller + "'" + ');" class="dropdown-item text-danger delete-record">Excluir</a></li>' +
                                '</ul>' +
                                '</div>'
                        }

                        return (btns);
                    }
                },
                // COLUNA - Parametro
                {
                    targets: 3,
                    //className: "text-center",
                },
                // COLUNA - Descricao
                {
                    targets: 4,
                    //className: "text-center",
                },
                // COLUNA - Valor
                {
                    targets: 5,
                    //className: "text-center",
                },
                // COLUNA - Status
                {
                    targets: 6,
                    //orderable: true,
                    //searchable: false,
                    //visible: false
                    render: function (data, type, full, meta) {

                        //console.log("Status data ::: ", data);
                        //console.log("Status type ::: ", type);
                        //console.log("Status full ::: ", full);

                        if (type === 'display') {

                            let statusClass = '';
                            let statusLayout = '';
                            let statusText = '';

                            statusClass = full.ativo ? 'bg-label-success' : 'bg-label-danger';

                            statusText = full.ativo ? 'Ativo' : 'Inativo';

                            statusLayout = '<span name="spStatus" data-status=' + full.ativo + ' class="badge rounded-pill ' + statusClass + '"> ' + statusText + '</span> ';

                            return statusLayout
                        }

                        return data;
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
                fnhelper_AlertErro(obj);
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
            
            fnhelper_ItemEdit(objFormData, var_Controller)
        } else {
            fnhelper_ItemAdd(varTbl_Obj, var_Controller)
        }        
        //fnhelper_ItemAdd(abc, var_Controller);
    });
}
function fn_GridComplete(grid) {
    const countRows = grid.api().rows().count();

    const swalConfig = countRows > 0
        ? {
            icon: 'success',
            title: 'Carregado!',
            html: 'Dados carregados com sucesso.',
            focusConfirm: true,
            confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
            customClass: { confirmButton: 'btn btn-label-success waves-effect' }
        }
        : {
            title: 'SEM DADOS!!',
            icon: 'info',
            html: 'Não há dados para serem carregados, para o filtro selecionado!!',
            focusConfirm: false,
            confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
            customClass: { confirmButton: 'btn btn-label-secondary waves-effect' }
        };

    Swal.fire(swalConfig).then(() => {
        $('.card-header').after('<hr class="my-0">');
        $('div.head-label').html(`<h5 class="card-title mb-0">${var_Nome}</h5>`);
        $(".card-datatable").show();
        $($.fn.dataTable.tables(true)).DataTable().columns.adjust().draw(false);
    });

    fnhelper_ExibirSomenteAtivos('.datatables-basic');
}

//#endregion

//#region FUNCOES

function fn_Pop(obj, action) {
    //console.log("fn_Pop varItems_Row !", obj);
    //console.log("fn_Pop action !", action);

    const popAddNewItem = document.querySelector('#pop-add-new-item');

    // Comment editor
    const commentEditor = document.querySelector('.comment-editor');

    if (commentEditor) {
        new Quill(commentEditor, {
            modules: {
                toolbar: '.comment-toolbar'
            },
            placeholder: 'Descricao...',
            theme: 'snow'
        });
    }

    popAddNewItemEl = new bootstrap.Offcanvas(popAddNewItem);

    // Pop ID
        (popAddNewItem.querySelector('#hdId').value = (obj === null ? 0 : obj.id)),

    // Pop Dados
        (popAddNewItem.querySelector('.dt-line-01').value = (obj === null ? '' : obj.parametro)),
        (popAddNewItem.querySelector('.dt-line-02').value = (obj === null ? '' : obj.descricao)),
        (popAddNewItem.querySelector('.dt-line-03').value = (obj === null ? '' : obj.valor)),
        (popAddNewItem.querySelector('.dt-line-04').checked = (obj === null ? false : obj.ativo));

    // O Parametro e a chave do registro: trava a edicao apos criado para evitar quebrar
    // quem ja depende desse nome (GetByParametroAsync), mas mantem editavel na criacao.
    (popAddNewItem.querySelector('.dt-line-01').disabled = (action === 'Edit'));

    // Pop Action
    (popAddNewItem.querySelector('.offcanvas-title').textContent = (action === 'Edit') ? 'Alterar Registro' : 'Novo Registro');
    (popAddNewItem.querySelector('.data-submit').textContent = (action === 'Edit') ? 'Alterar' : 'Adicionar');

    // Open Pop
        popAddNewItemEl.show();
}
function fn_PopGetObj() {
    //console.log("fn_PopGetObj !",);

    const objFormData = {
        Id: $('#hdId').val(),
        Parametro: $('.form-add-new-item .dt-line-01').val(),
        Descricao: $('.form-add-new-item .dt-line-02').val(),
        Valor: $('.form-add-new-item .dt-line-03').val(),
        Ativo: $('.form-add-new-item .dt-line-04').is(':checked')
    };

    return objFormData;
}
function fn_PopValidator(formAddNewItem) {
    var varformValid = FormValidation.formValidation(formAddNewItem, {
        fields: {
            pop_line_item_01: {
                validators: {
                    notEmpty: {
                        message: 'O preenchimento é obrigatório'
                    }
                }
            },
            pop_line_item_02: {
                validators: {
                    notEmpty: {
                        message: 'O preenchimento é obrigatório'
                    }
                }
            },
            pop_line_item_03: {
                validators: {
                    notEmpty: {
                        message: 'O preenchimento é obrigatório'
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