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

//#endregion

//#region CARREGAMENTO INICIAL

document.addEventListener('DOMContentLoaded', function () {
    (function () {
        console.log(`LIST ${var_Controller}- Todos os recursos terminaram o carregamento!`);

        fn_Masks();

        // Form validation
        const formAddNewItem = document.getElementById('form-pop-add-new-item');

        formValid = fn_PopValidator(formAddNewItem);

        // Carrega Dados Grid
        fn_GridList(formValid);
    })();
});

//#endregion

//#region DATA PICKERS

//#endregion

//#region GRID

function fn_GridList(formValid) {

    var varLang_UrlTranslate = '/vendor/libs/datatables-bs5/i18n/pt-BR.json',

        varAjax_UrlController = `${var_Controller}/FiltrarDados`,
        varAjax_TypeAction = 'POST',
        varAjax_TypeContent = 'application/json; charset=utf-8',

        varCol_Exportar = [2, 3, 4, 5],
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
                // COLUNA - Nome
                {
                    data: 'NomeSocio',
                    targets: 2,
                },
                // COLUNA - Tipo Socio
                {
                    data: 'SocioPerfilId',
                    targets: 3,
                    className: "text-center",
                    render: function (data, type, full) {

                        let id = full.Id;

                        if (id != 0 && data !== undefined && data !== null) {

                            let statusClass,
                                statusLayout,
                                statusDescricao = full.SocioPerfilDescricao,
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
                // COLUNA - Bloqueio (tentativas de captura de tela - ver ReportImageAccess/Login)
                {
                    targets: 4,
                    data: 'QtdInfracoesPrint',
                    className: "text-center",
                    render: function (data, type, full) {
                        if (type !== 'display') return data || 0;

                        const qtd = data || 0;

                        if (full.Bloqueado) {
                            return '<span class="badge rounded-pill bg-label-danger" title="Bloqueado após ' + qtd + ' tentativa(s) de captura de tela - aguardando liberação da administração">Bloqueado</span>';
                        }

                        if (qtd > 0) {
                            return '<span class="badge rounded-pill bg-label-warning" title="' + qtd + ' tentativa(s) de captura de tela registrada(s)">' + qtd + '</span>';
                        }

                        return '<span class="text-muted">-</span>';
                    }
                },
                // COLUNA - Status
                {
                    targets: -2,
                    data: 'SocioAtivo',
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
                                '<a href="javascript:;" class="btn btn-sm btn-text-secondary rounded-pill btn-icon dropdown-toggle hide-arrow" data-bs-toggle="dropdown"><i class="ri-more-2-line ri-22px"></i></a>' +
                                '<ul class="dropdown-menu dropdown-menu-end m-0">' +
                                '<li><a href="javascript:fn_Pop(' + itemObjJson + ',' + "'Edit'" + ');" class="dropdown-item edit-record">Editar</a></li>' +
                                '<div class="dropdown-divider"></div>' +
                                '<li><a href="javascript:fnItem_Delete(' + itemObjJson + ');" class="dropdown-item text-danger delete-record">Excluir</a></li>' +
                                '</ul>' +
                                '</div>'

                            /*
                            btns =
                                '<div class="d-inline-block text-nowrap">' +
                                '<a href="javascript:fn_Pop(' + itemObjJson + ',' + "'Edit'" + ');" class="btn btn-sm btn-icon btn-text-secondary waves-effect rounded-pill text-body me-1"><i class="ri-edit-box-line ri-22px"></i></a>' +
                                '</div>'

                                */
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
    });
     /* */
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

//#region FUNCOES MASCARA

function fn_Masks() {

    //mascar para telefone celular
    $('.phone-mask').mask('(00) 00000-0000');
}

// fnhelper_MaskDataAniversario, fnhelper_MaskCEP e fnhelper_BuscaEnderecoPorCep são comuns (helper-ui-common.js).
// fnhelper_MaskCEP(this, fn_PreencherEnderecoPopup) é quem dispara o autocompletar abaixo.

// Callback de fnhelper_MaskCEP (via helper-ui-common.js/fnhelper_BuscaEnderecoPorCep) específico do popup
// de cadastro - cada tela decide em quais campos preencher o retorno da ViaCEP.
function fn_PreencherEnderecoPopup(result) {
    const popAddNewItem = document.querySelector('#pop-add-new-item');

    popAddNewItem.querySelector('.dt-line-05').value = result.logradouro || '';
    popAddNewItem.querySelector('.dt-line-08').value = result.bairro || '';
    popAddNewItem.querySelector('.dt-line-10').value = result.localidade || '';

    $("#cmb_SocioEstado").val(result.uf || '').trigger('change');

    // Foca no numero para o usuario continuar o preenchimento
    popAddNewItem.querySelector('.dt-line-06').focus();
}

//#endregion

//#region POP

function fn_FormataDataAniversario(dia, mes, ano) {

    if (!dia || !mes) return '';

    const pad2 = (n) => String(n).padStart(2, '0');

    return ano ? `${pad2(dia)}/${pad2(mes)}/${ano}` : `${pad2(dia)}/${pad2(mes)}`;
}

function fn_FormataTelefone(ddd, telefone) {
    if (!ddd && !telefone) return '';
    return `(${ddd ?? ''}) ${telefone ?? ''}`;
}

function fn_Pop(obj, action) {
    //console.log("fn_Pop varItems_Row !", obj);
    //console.log("fn_Pop action !", action);

    const popAddNewItem = document.querySelector('#pop-add-new-item');

    popAddNewItemEl = new bootstrap.Offcanvas(popAddNewItem);

        // Pop ID - "?? default" cobre tanto obj null (registro novo) quanto obj existente
        // mas com o sub-registro (contato/endereço/aniversário) ainda não criado - nesse
        // 2º caso, "obj === null ? default : obj.X" deixava passar null puro, e
        // input.value = null vira literalmente o texto "null" na tela (peculiaridade do
        // DOM), não vazio. Ver LEFT JOIN em SocioController.FiltrarDados/Edit.
        (popAddNewItem.querySelector('#hdId').value = (obj?.Id ?? 0)),
        (popAddNewItem.querySelector('#hdSocioContatoId').value = (obj?.SocioContatoId ?? 0)),
        (popAddNewItem.querySelector('#hdSocioEnderecoId').value = (obj?.SocioEnderecoId ?? 0)),
        (popAddNewItem.querySelector('#hdSocioAniversarioId').value = (obj?.SocioAniversarioId ?? 0)),
        (popAddNewItem.querySelector('#hdSocioPerfilId').value = (obj?.SocioPerfilId ?? 0)),
        // Pop Dados
        (popAddNewItem.querySelector('.dt-line-01').value = (obj?.NomeSocio ?? '')),
        (popAddNewItem.querySelector('.dt-line-02').value = (obj?.Email ?? '')),
        (popAddNewItem.querySelector('.dt-line-03').value = (obj === null ? '' : fn_FormataTelefone(obj.Ddd, obj.Telefone))),
        (popAddNewItem.querySelector('.dt-line-04').value = (obj?.Cep ?? '')),
        (popAddNewItem.querySelector('.dt-line-05').value = (obj?.Endereco ?? '')),
        (popAddNewItem.querySelector('.dt-line-06').value = (obj?.Numero ?? '')),
        (popAddNewItem.querySelector('.dt-line-07').value = (obj?.Complemento ?? '')),
        (popAddNewItem.querySelector('.dt-line-08').value = (obj?.Bairro ?? '')),
        (popAddNewItem.querySelector('.dt-line-09').value = (obj?.Estado ?? '')),
        (popAddNewItem.querySelector('.dt-line-10').value = (obj?.Cidade ?? '')),
        (popAddNewItem.querySelector('.dt-line-11').value = (obj === null ? '' : fn_FormataDataAniversario(obj.Dia, obj.Mes, obj.Ano))),
        (popAddNewItem.querySelector('.dt-line-12').checked = (obj === null ? true : obj.SocioAtivo));
        (popAddNewItem.querySelector('.dt-line-13').checked = (obj === null ? true : obj.MostrarSite));
        (popAddNewItem.querySelector('.dt-line-14').checked = (obj === null ? false : obj.Bloqueado));

    // Contador de tentativas de captura de tela - só contexto pra decidir se libera; o
    // campo em si (checkbox acima) é quem efetivamente desbloqueia.
    (function () {
        const qtd = obj?.QtdInfracoesPrint || 0;
        const badge = document.getElementById('lblQtdInfracoesPrint');
        if (qtd > 0) {
            badge.textContent = qtd;
            badge.title = `${qtd} tentativa(s) de captura de tela registrada(s)`;
            badge.style.display = '';
        } else {
            badge.textContent = '';
            badge.style.display = 'none';
        }
    })();

    // Pop Action
    (popAddNewItem.querySelector('.offcanvas-title').textContent = (action === 'Edit') ? 'Alterar Registro' : 'Novo Registro');
    (popAddNewItem.querySelector('.data-submit').textContent = (action === 'Edit') ? 'Alterar' : 'Adicionar');

    if (obj !== null) {

        $("#cmb_SocioEstado").val(obj.Estado).trigger('change');

        //console.log("fn_Pop ex val ::: ", $("#cmb_SocioEstado").val());
    } else {
        $("#cmb_SocioEstado").val('').trigger('change');
    }

    //console.log("fn_Pop popAddNewItem ::: ", popAddNewItem);

    // Open Pop
    popAddNewItemEl.show();
}

function fn_PopGetObj() {

    const objFormData = {
        Id: $('#hdId').val(),
        Nome: $('.form-add-new-item .dt-line-01').val(),
        Email: $('.form-add-new-item .dt-line-02').val(),
        Telefone: $('.form-add-new-item .dt-line-03').val(),
        CEP: $('.form-add-new-item .dt-line-04').val(),
        Endereco: $('.form-add-new-item .dt-line-05').val(),
        Numero: $('.form-add-new-item .dt-line-06').val(),
        Complemento: $('.form-add-new-item .dt-line-07').val(),
        Bairro: $('.form-add-new-item .dt-line-08').val(),
        Estado: $('#cmb_SocioEstado').val(),
        Cidade: $('.form-add-new-item .dt-line-10').val(),
        DataAniversario: $('.form-add-new-item .dt-line-11').val(),
        Ativo: $('.form-add-new-item .dt-line-12').is(':checked'),
        MostrarSite: $('.form-add-new-item .dt-line-13').is(':checked'),
        Bloqueado: $('.form-add-new-item .dt-line-14').is(':checked'),

        SocioEstadoId: $('#cmb_SocioEstado').val(),
        SocioContatoId: $('#hdSocioContatoId').val(),
        SocioEnderecoId: $('#hdSocioEnderecoId').val(),
        SocioAniversarioId: $('#hdSocioAniversarioId').val(),
        SocioPerfilId: $('#hdSocioPerfilId').val(),
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
                rowSelector: '.form-floating-outline'
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

    var varAjax_UrlController = `${var_Controller}/Delete/${varItems_Id}`, // rota exige o id no path (Delete(int id) no controller); antes o id só ia no body do DELETE, que o model binder padrão não lê pra tipo simples sem [FromBody] - sempre chegava 0 no servidor
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

                    success: function (result) {
                        //console.log("result  :: ", result);
                        //console.log("result bResult :: ", result.bResult);

                        var varTbl;

                        $.busyLoadFull("hide");

                        if ($.fn.dataTable.isDataTable('.datatables-basic')) {
                            //console.log("YES :: ");
                            varTbl = varTbl_Obj.DataTable();

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
                    error: function (xhr, textStatus, errorThrown) {
                        fnhelper_AlertErro(xhr, textStatus);

                        return false;
                    }
                });

        } else if (result.dismiss === Swal.DismissReason.cancel) {

            $.busyLoadFull("hide");

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
    //console.log("fnItem_Edit varItems_Row ::: ", varItems_Row);
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
                error: function (xhr, textStatus, errorThrown) {
                    fnhelper_AlertErro(xhr, textStatus);

                    return false;
                }
            });
    }
}

function fnItem_Add(varTbl_Obj) {
    //console.log("fnItem_Add varTbl_Obj ::: ", varTbl_Obj);

    var varPop_BtnAction = 'Create';

    var varAjax_UrlController = `${var_Controller}/Create`,
        varAjax_TypeAction = 'POST',
        varAjax_TypeData = 'JSON',
        varAjax_TypeContent = 'application/json; charset=utf-8';

    const formData_newItem = fn_PopGetObj();
    //console.log("fnItem_Add formData_newItem ::: ", formData_newItem);

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
                            html: `<b> Erro ocorrido :: <br><br>` + result.message + `</b>`,
                            focusConfirm: false,
                            confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                            customClass: {
                                confirmButton: 'btn btn-label-danger waves-effect'
                            }
                        });
                    }
                },
                error: function (xhr, textStatus, errorThrown) {
                    fnhelper_AlertErro(xhr, textStatus);

                    return false;
                }
            });
    }
}

//#endregion

//#region MODAL

// fnhelper_AlertErro é comum (helper-ui-common.js).

//#endregion
