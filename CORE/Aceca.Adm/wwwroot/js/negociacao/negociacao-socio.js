/**
 * Negociacao -> Socio
 */

'use strict';

//#region Declare

let var_Nome = 'Negocia&ccedil;&atilde;o S&oacute;cio',
    var_Controller = '/Negociacao',
    var_ControllerCmb = '/HelperExtensions',

    varTbl_Obj = $('.datatables-basic'),
    varTbl_Data;

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
        console.log(`LIST ${var_Controller}- Todos os recursos terminaram o carregamento!`);

        fn_Masks();

        // Carrega Dados Grid
        fn_GridList();
    })();
});

//#endregion

//#region DATA PICKERS


$(function () {
    var bsDatepickerFormat = $('.dt-calendar');

    // Format
    if (bsDatepickerFormat.length) {
        bsDatepickerFormat.datepicker({
            autoclose: true,
            todayHighlight: true,
            format: 'dd/mm',
            language: 'pt-BR',
            orientation: isRtl ? 'auto right' : 'auto left'
        });
    }
});
//#endregion

//#region GRID

function fn_GridList() {

    var varLang_UrlTranslate = 'https://cdn.datatables.net/plug-ins/1.12.1/i18n/pt-BR.json',

        varAjax_UrlController = `${var_Controller}/ListGrid_PorSocio`,
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
                    //console.log("data fn :: ", result)
                    return result.data;
                }
            },
            columnDefs: [
                // COLUNA - Responsive
                {
                    data: 'socioId',
                    targets: 0,
                    className: 'control',
                    visible: false,
                    render: function (data, type, full, meta) {
                        return '';
                    }
                },
                // COLUNA - ID checkbox
                {
                    data: 'socioId',
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
                // COLUNA - Bandeira  / Nome                   
                {
                    targets: 2,
                    data: 'socioNome'
                },
                 // COLUNA - Contato
                {
                    data: 'socioTelefone',
                    targets: 3,
                    className: "text-center",
                    render: function (data, type, full) {
                        let id = full.id;

                        if (id != 0 && data !== undefined && data !== null) {

                            let flag_country = (full?.socioDDI === 54 ? 'ar' : 'br');

                            if (flag_country) {
                              var flag = `<i class ="fis fi fi-${flag_country} rounded-circle me-2 fs-4"></i>`;
                            } else {
                              // For Avatar badge
                               var flag= `<i class ="fis fi fi-xx rounded-circle me-2 fs-4"></i>`;
                            }

                            var flag_icon =
                                `<div class="d-flex justify-content-center align-items-center customer-country">
                                    <div>${flag}</div>
                                    <div>
                                        <span>(${full.socioDDD}) ${full.socioTelefone}</span>
                                    </div>
                                </div>`;

                            return flag_icon;  ``;
                        } else {
                            return '';//'Data Indispon&iacute;vel';
                        }
                    }
                },
                // COLUNA - Email
                {
                    data: 'socioEmail',
                    targets: 4,
                },
                // COLUNA - Items
                {
                    data: 'quantidadePossui',
                    className: "text-center",
                    targets: -2,
                },
                // COLUNA - Botoes Acoes
                {
                    data: 'socio.id',
                    targets: -1,
                    className: "text-center",
                    orderable: false,
                    searchable: false,
                    //visible: false,
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

                            btns = `<a href="javascript: fnItem_Negociacao(${itemObjJson},'NegociacaoSocio');" class="btn btn-sm btn-icon btn-text-secondary rounded-pill waves-effect" data-bs-toggle="tooltip" title="Ver Itens para Negociação"><i class="ri ri-eye-line ri-22px"></i></a>`
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

//#region FUNCOES MASCARA

function fn_Masks() {

    //mascar para telefone celular
    $('.phone-mask').mask('(00) 00000-0000');
}

function fn_MaskCEP(input) {
    // Remove tudo que não for dígito
    let value = input.value.replace(/\D/g, '');

    // Limita a 8 dígitos
    value = value.substring(0, 8);

    // Aplica a máscara: 00000-000
    value = value.replace(/(\d{5})(\d)/, '$1-$2');

    input.value = value;
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
    const idMarcaAcervo = $('#hdMarcaAcervoId').val();

    // mantém o option original (-1)
    $cmb.prop('disabled', true);

    $.ajax({
        url: `${var_ControllerCmb}/AsyncCmb_MarcaFase`,
        type: 'GET',
        cache: true,
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

function fnItem_Negociacao(obj, action) {
    console.log("fnItem_Negociacao obj ::: ", obj);
    console.log("fnItem_Negociacao action::: ", action);

    let socioNegociacaoId = obj?.socioId;
    let quantidadePossui = obj?.quantidadePossui;
    let actionId = -1;

    const socioLogadoId = document.getElementById('hdSocioLogadoId').value;

    console.log("fnItem_Negociacao socioNegociacaoId::: ", socioNegociacaoId);
    console.log("fnItem_Negociacao quantidadePossui::: ", quantidadePossui);
    console.log("fnItem_Negociacao socioLogadoId::: ", socioLogadoId);

    if ((socioNegociacaoId === undefined || socioNegociacaoId === null || socioNegociacaoId === '' || socioNegociacaoId < 1)
        || (quantidadePossui === undefined || quantidadePossui === null || quantidadePossui === '' || quantidadePossui < 1)
        || (socioLogadoId === undefined || socioLogadoId === null || socioLogadoId === '' || socioLogadoId < 1)
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
            case 'NegociacaoMeusNegocios':
                actionId = 0;
                break;
            case 'NegociacaoSocio':
                actionId = 1;
                break;
            case 'NegociacaoAcervo':
                actionId = 2;
                break;
            default:
                actionId = -1;
        }

        $.busyLoadFull("show");

        $.ajax({
            url: `/Negociacao/ActionNegociacao`,
            type: 'POST',
            dataType: 'JSON',
            data: {
                socioNegociacaoId: socioNegociacaoId,
                quantidadePossui: quantidadePossui,
                socioLogadoId: socioLogadoId,
                actionId: actionId,
                isPerfil: document.getElementById('hdIsPerfil').value
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
