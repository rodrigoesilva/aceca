/**
 * Admin -> Socio -> Endereco
 */

'use strict';

//#region Declare

let var_Nome = 'S&oacute;cio -> Endere&ccedil;o',
    var_Controller = '/SocioEndereco',
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

        // Form validation
        const formAddNewItem = document.getElementById('form-pop-add-new-item');

        formValid = fn_PopValidator(formAddNewItem);

        // Carrega Dados Grid
        fn_GridList(formValid);

        fn_LoadCmb_Socio();

        $('#cmb_Socio').on('change', function () {
            $('#hdSocioEnderecoId').val($(this).val());
        });
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
            autoWidth: false,
            scrollX: false,

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

                beforeSend: function () {
                    $.busyLoadFull("show");
                },
                complete: function () {
                    $.busyLoadFull("hide");
                },

                dataSrc: function (result) {
                    //console.log("data fn :: ", result)
                    return result.data;
                }
            },
            columnDefs: [
                // COLUNA - Responsive
                {
                    data: 'Id',
                    targets: 0,
                    className: 'control',
                    width: '1%',
                    searchable: false,
                    orderable: false,
                    responsivePriority: 1,
                    render: function (data, type, full, meta) {
                        return '';
                    }
                },
                // COLUNA - ID checkbox
                {
                    data: 'Id',
                    targets: 1,
                    // "never" (não "none"!) exclui essa coluna (só usada pra seleção em
                    // massa) do modal de detalhes no mobile - visible:false sozinho não
                    // basta, e "none" só afeta a visibilidade normal da coluna
                    // (col.includeIn), não o modal (_detailsObj só verifica col.never,
                    // setado apenas pela palavra literal "never" na className).
                    className: 'never',
                    visible: false,
                    checkboxes: true,
                    render: function () {
                        return '<input type="checkbox" class="dt-checkboxes form-check-input">';
                    },
                    checkboxes: {
                        selectAllRender: '<input type="checkbox" class="form-check-input">'
                    }
                },
                // COLUNA - Nome (2ª a aparecer no mobile) - span trava a largura
                // renderizada independente do tamanho real do nome (mesma técnica usada
                // em Código ACECA/Nome, pra não deixar o conteúdo real inflar a coluna e
                // sufocar o orçamento de largura calculado pelo Responsive no mobile).
                {
                    data: 'NomeSocio',
                    targets: 2,
                    width: '175px',
                    responsivePriority: 2,
                    render: function (data, type) {
                        if (type !== 'display') return data;
                        return `<span style="display:inline-block;max-width:175px;word-break:break-word;">${data ?? ''}</span>`;
                    }
                },
                // COLUNA - Endereco (render.text() escapa HTML - evita XSS armazenado
                // em campo de texto livre preenchido pelo admin no cadastro)
                {
                    data: 'Endereco',
                    targets: 3,
                    responsivePriority: 10004,
                    render: $.fn.dataTable.render.text(),
                },
                // COLUNA - numero
                {
                    data: 'Numero',
                    targets: 4,
                    className: "text-center",
                    responsivePriority: 10005,
                },
                // COLUNA - complemento
                {
                    data: 'Complemento',
                    targets: 5,
                    responsivePriority: 10006,
                    render: $.fn.dataTable.render.text(),
                },
                // COLUNA - bairro
                {
                    data: 'Bairro',
                    targets: 6,
                    responsivePriority: 10007,
                    render: $.fn.dataTable.render.text(),
                },
                // COLUNA - cidade
                {
                    data: 'Cidade',
                    targets: 7,
                    responsivePriority: 10008,
                    render: $.fn.dataTable.render.text(),
                },
                // COLUNA - estado
                {
                    data: 'Estado',
                    targets: 8,
                    className: "text-center",
                    responsivePriority: 10009,
                },
                // COLUNA - cep
                {
                    data: 'Cep',
                    targets: 9,
                    className: "text-center",
                    responsivePriority: 10010,
                },
                // COLUNA - Status (3ª a aparecer no mobile)
                {
                    targets: -2,
                    data: 'SocioAtivo',
                    className: "text-center",
                    responsivePriority: 3,
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
                    data: 'Id',
                    targets: -1,
                    className: "text-center",
                    orderable: false,
                    searchable: false,
                    responsivePriority: 10011,
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
                                '<a href="javascript:fnItem_Delete(' + itemObjJson + ');" class="btn btn-sm btn-icon btn-text-secondary waves-effect rounded-pill text-body"><i class="ri-delete-bin-line ri-22px"></i></a>' +
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
                            var titulo = data.nome || data.Nome || data.descricao || data.Descricao ||
                                data.NomeUsuario || data.nomeUsuario || data.socioNome || data.titulo || '';
                            return titulo ? ('Detalhes de ' + titulo) : 'Detalhes';
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

// fnhelper_MaskCEP é comum (helper-ui-common.js) - aqui chamada sem callback, só aplica a máscara
// (sem autocompletar endereço via ViaCEP).

//#region COMBO

function fn_LoadCmb_Socio() {
    if ($('#cmb_Socio option').length <= 1) {
        $.ajax({
            crossDomain: true,
            url: `${var_ControllerCmb}/AsyncCmb_Socio`,
            type: 'GET',
            success: function (data) {
                $.each(data, function (id, result) {
                    $("#cmb_Socio").append($("<option></option>").val(result.value).html(result.text));
                });
            },
            error: function (xhr, textStatus, errorThrown) {
                fnhelper_AlertErro(xhr, textStatus);
            },
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
        (popAddNewItem.querySelector('#hdSocioEnderecoId').value = (obj === null ? 0 : obj.SocioId)),

        // Pop Dados
        (popAddNewItem.querySelector('.dt-line-01').value = (obj === null ? '' : obj.NomeSocio)),
        (popAddNewItem.querySelector('.dt-line-02').value = (obj === null ? '' : obj.Cep)),
        (popAddNewItem.querySelector('.dt-line-03').value = (obj === null ? '' : obj.Endereco)),
        (popAddNewItem.querySelector('.dt-line-04').value = (obj === null ? '' : obj.Numero)),
        (popAddNewItem.querySelector('.dt-line-05').value = (obj === null ? '' : obj.Complemento)),
        (popAddNewItem.querySelector('.dt-line-06').value = (obj === null ? '' : obj.Bairro)),
        (popAddNewItem.querySelector('.dt-line-08').value = (obj === null ? '' : obj.Cidade)),

    // Criar: exige escolher o sócio via combo. Editar: sócio já definido, só mostra o nome.
    (obj === null) ? $('.div_cmb_Socio').show() : $('.div_cmb_Socio').hide();
    (obj === null) ? $('.div_txt_Socio').hide() : $('.div_txt_Socio').show();
    $('#cmb_Socio').val('-1').trigger('change.select2');

    // Pop Action
    (popAddNewItem.querySelector('.offcanvas-title').textContent = (action === 'Edit') ? 'Alterar Registro' : 'Novo Registro');
    (popAddNewItem.querySelector('.data-submit').textContent = (action === 'Edit') ? 'Alterar' : 'Adicionar');

    if (obj !== null) {

        (!obj.Estado) ? $("#cmb_SocioEstado").val('').trigger('change') : $("#cmb_SocioEstado").val(obj.Estado).trigger('change');

        //console.log("fn_Pop ex val ::: ", $("#cmb_SocioEstado").val());
    } else {
        $("#cmb_SocioEstado").val('').trigger('change');
    }

    // Open Pop
    popAddNewItemEl.show();
}

function fn_PopGetObj() {

    const objFormData = {
        Id: $('#hdId').val(),
        SocioId: $('#hdSocioEnderecoId').val(),
        Nome: $('.form-add-new-item .dt-line-01').val(),
        CEP: $('.form-add-new-item .dt-line-02').val(),
        Endereco: $('.form-add-new-item .dt-line-03').val(),
        Numero: $('.form-add-new-item .dt-line-04').val(),
        Complemento: $('.form-add-new-item .dt-line-05').val(),
        Bairro: $('.form-add-new-item .dt-line-06').val(),
        Estado: $('#cmb_SocioEstado').val(),
        Cidade: $('.form-add-new-item .dt-line-08').val(),
       // Ativo: $('.form-add-new-item .dt-line-09').is(':checked')
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
    // Delegado pro helper comum (helper-ui-common.js) - antes cada tela tinha sua
    // propria copia praticamente identica desta funcao (~25 arquivos); agora um
    // ajuste em fnhelper_ItemDelete vale pra todas de uma vez.
    fnhelper_ItemDelete(varItems_Row, var_Controller);
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
                url: varAjax_UrlController,
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
                url: varAjax_UrlController,
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
// fnhelper_AlertErro é comum (helper-ui-common.js).
//#endregion
