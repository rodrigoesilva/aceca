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

        //fn_Auth();

        fn_Limpar();

        // Carrega Dados Grid
        //fn_GridListAll();

        // Filtros
        fn_LoadFiltros();
        fn_ChangeFiltros();

        $('.btn-filter').on('click', function () {
            fn_Filtrar();
        });

        $('.btn-filter-clear').on('click', function () {
            fn_Limpar();
        });

        // Update the clock immediately on load, and then every second
        fn_UpdateClock();
        setInterval(fn_UpdateClock, 1000); // Updates every 1000 milliseconds

        //console.log("DOMContentLoaded var_Filtrado ::: ", var_Filtrado);
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
   
    console.log("fn_Filtrar objFiltro : ", objFiltro);

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
    $('#txt_IncluidoPor').val('');
    $('#txt_CodigoAceca').val('');
    $('#txt_NomeMarca').val('');
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
                fn_ModalConfirmarFiltros();
            } else {
                fn_Filtrar();
            }
        }
    });

    $('#cmb_MarcaTipo').on('change', function () {

        let idMarcaTipo = $(this).find('option:selected').val();

        console.log("cmb_MarcaTipo change idMarcaTipo ::: ", idMarcaTipo);
        console.log("cmb_MarcaTipo change var_Filtrado ::: ", var_Filtrado);

        if (idMarcaTipo <= 0) {
            //fn_Limpar();
        } else {
            if (!var_Filtrado) {
                fn_ModalConfirmarFiltros();
            } else {
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
            }
        }
    });

    $('#cmb_MarcaSubTipo').on('change', function () {

        let idMarcaSubTipo = $(this).find('option:selected').val();

        console.log("cmb_MarcaSubTipo change idMarcaSubTipo ::: ", idMarcaSubTipo);
        console.log("cmb_MarcaSubTipo change var_Filtrado ::: ", var_Filtrado);

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

        let colDesc = varTbl_Data.settings()[0].aoColumns[8];

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

    $('#cmb_MarcaTipo').on('change', function () {
        let idMarcaTipo = $(this).find('option:selected').val();

        //console.log("cmb_MarcaTipo change  idMarcaTipo ::: ", idMarcaTipo);

        //Limpar Combo cinema
        document.querySelectorAll('#cmb_Cinema option').forEach(option => option.remove());

        $("#cmb_Cinema").append($("<option></option>").val(0).html("-- Selecionar --"));

        if ($(this).length <= 1 && idMarcaTipo > 0) {
            fn_LoadCmb_MarcaSubTipo(idMarcaTipo);
        }
    });

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

                    $("#cmb_MarcaFase").append($("<option></option>").val(0).html("Todas"));

                    $.each(data, function (id, result) {
                        //console.log("fn_LoadCmb_MarcaFase  result id ::: ", id);
                        //console.log("fn_LoadCmb_MarcaFase  result ::: ", result);
                        $("#cmb_MarcaFase").append($("<option></option>").val(result.value).html(result.text));
                    });
                },
                error: function (xhr, textStatus, errorThrown) {
                    fn_ModalErro(xhr, textStatus, errorThrown);
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
                error: function (xhr, textStatus, errorThrown) {
                    fn_ModalErro(xhr, textStatus, errorThrown);
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
                error: function (xhr, textStatus, errorThrown) {
                    fn_ModalErro(xhr, textStatus, errorThrown);
                },
            }
        );
    }
}
function fn_LoadCmb_MarcaSubTipo(idMarcaTipo) {

    //console.log("fn_LoadCmb_MarcaSubTipo  idMarcaTipo ::: ", idMarcaTipo);

    if ($('#cmb_MarcaSubTipo').length <= 1) {

        let urlLoad = idMarcaTipo > 0 ? `${var_ControllerCmb}/AsyncCmb_MarcaSubTipoByTipo` : `${var_ControllerCmb}/AsyncCmb_MarcaSubTipo`;

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
//#endregion

//#region GRID

function fn_GridListFilter(lstData) {

    console.log("fn_GridListFilter lstData ::: ", lstData);

    var varLang_UrlTranslate = 'https://cdn.datatables.net/plug-ins/1.12.1/i18n/pt-BR.json',

        varCol_Exportar = [2, 3, 4, 5, 6, 7, 8, 9],
        varCol_Ordenacao = [2, 'asc'], //set any columns order asc/desc

        varItems_QtdPorPage = 25,
        varItems_DivPage = [5, 10, 25, 50, 75, 100],
        varItems_Row = null,
        varItems_Id = 0;

    if (varTbl_Obj.length) {

        //console.log("fn_GridListFilter lstData ::: ", lstData);

        varTbl_Data = $('.datatables-basic').DataTable({
            //"processing": true,
            //"deferRender": true,
            data: lstData,

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
                // COLUNA - codigoAceca
                {
                    targets: 3,
                    data: 'codigoAceca',
                    className: "text-center"
                },
                // COLUNA - imagem
                {
                    targets: 4,
                    data: 'imagem',
                    className: "text-center",
                    render: function (data, type, row, meta) {
                        return `<img name="myImg" class="td-img cmyImg" alt="${row.codigoAceca}" src="${data}">`;
                    }
                },
                // COLUNA - imagemDetalhe
                {
                    targets: 5,
                    data: 'imagemDetalhe',
                    className: "text-center", 
                    render: function (data, type, row, meta) {
                        return `<img name="myImg" class="td-img cmyImg" alt="${row.codigoAceca}" src="${data}">`;
                    }
                },
                // COLUNA - nomeMarca
                {
                    targets: 6,
                    data: 'nomeMarca',
                    className: "text-center"
                },
                // COLUNA - fabricaNome
                {
                    targets: 7,
                    data: 'fabricaNome',
                    className: "text-start",
                },
                // COLUNA - descricao
                {
                    targets: 8,
                    data: 'descricao',
                    className: "text-start",
                    searchable: false,
                },
                // COLUNA - incluidoPor
                {
                    targets: 9,
                    data: 'incluidoPor',
                    className: "text-center",
                    render: function (data, type, full, meta) {
                        let id = full.id;

                        //data = full[9];

                        if (data !== undefined && data !== null) {
                            if (id !== 0 && type === 'display') {

                                let bQuebraLinha = data.includes("/");
                                //console.log("incluidoPor bQuebraLinha :::: ",bQuebraLinha );                               

                                let ulIn = `<ul class=" m-0 avatar-group d-flex align-items-center" style="list-style: none;">`;
                                let htmlContent = '';

                                for (let i = 0; i < data.split("/").length; i++) {
                                    htmlContent += 
                                        `<li class="avatar avatar-lg pull-up" data-bs-toggle="tooltip" data-bs-placement="top" data-popup="tooltip-custom" data-incluido="${data.split("/")[i]}" title="${data.split("/")[i]}">
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
                    visible: false,
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
                    className: 'btn btn-primary dropdown-toggle me-4 waves-effect waves-light',
                    text: '<i class="ri-download-line ri-16px me-2"></i><span class="d-none d-sm-inline-block">Exportar </span>',
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

        //console.log("fn_GridComplete var_Filtrado ::: ", var_Filtrado);

        if (countRows <= 0) {
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
        }
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

//#region AUTH GUARD
function fn_Auth() {
    console.log("fn_Auth ::: ");
    /*
    const sess = sessionStorage.getItem('aceca_sessao');

    if (!sess) {
       // window.location.href = 'login.html'; return;
    }

    const u = JSON.parse(sess);

    document.getElementById('tbNome').textContent = u.nome || 'Usuário';
    document.getElementById('tbCargo').textContent = u.cargo || '—';
    document.getElementById('tbAvatar').textContent = (u.nome || 'U')[0].toUpperCase();

    //console.log("AUTH GUARD - sess :::", sess);
    */
};

function fn_Logout() {
    console.log("fn_Logout ::: ");
    sessionStorage.removeItem('aceca_sessao');
    //window.location.href = 'login.html';
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
        console.log("fnZoom myModal::: ", img);

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