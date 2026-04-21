/**
 * Admin -> Marcas
 */

'use strict';

//#region Declare

let var_Nome = 'Marcas',
    var_Controller = '/Marca',
    var_ControllerCmb = '/HelperExtensions';

let var_ImgAlt = "ACECA",
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

let objVariante;

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

        //// Combos
        fn_PopLoadCombos();
        fn_ChangeCombos();

        //// TEXT INPUTS
        $('#txt_Nome').on('change', function (e) {
            //console.log("tecla pressionada campo txt_Nome ::: ", e.key);

            let txtNome = $(this).val();

            //console.log("txtNome ::", txtNome);

            if (txtNome !== null && txtNome !== undefined && txtNome !== '') {

                $('.div_codigo').hide();
                
                $('.div_dados').hide();
                $('.div_imagem').hide();
                $('.div_botoes').hide();
                $('.div_variante').hide();
                $('.div_variante_codigo').hide();
                $('.div_original_variante').hide();

                $('#cmbPop_MarcaFase').prop('selectedIndex', 0).change();
                $('#cmbPop_MarcaVariante').prop('selectedIndex', 0).change();

                $('.div_fase').show();
                //$('.div_fase').show();
            }else {
                fn_Limpar();
            }
            /*
            if (e.key === 'Enter' || e.keyCode === 13) {
                console.log("tecla enter pressionada campo marca ::: ");
            }*/
        });

        $('#txt_CodigoVariante').on('change', function (e) {
            //console.log("tecla pressionada campo txt_CodigoVariante ::: ", e.key);

            let txtCodigoVariante = $(this).val();
            //console.log("txtCodigoVariante ::", txtCodigoVariante);

            if (txtCodigoVariante !== null && txtCodigoVariante !== undefined && txtCodigoVariante !== '') {
                fn_GetCodigoAceca();
            } else {
                fn_Limpar();
            }
            /*
            if (e.key === 'Enter' || e.keyCode === 13) {
                console.log("tecla enter pressionada campo marca ::: ");
            }*/
        });

        //// FORM
        $('.data-submit').on('click', function (e) {
            //console.log("submit click::")
            fn_ModalSalvar(e);
        });

    })();
});

//#endregion

//#region Botoes
function fn_Limpar() {
    //console.log("fn_Limpar ::: ");

    $.busyLoadFull("show");

    $('#txt_Nome').val('');
    //$('#cmbPop_MarcaVariante').prop('selectedIndex', 0).change();
    $('#cmbPop_MarcaFase').prop('selectedIndex', 0).change();
    $('#txt_Codigo').val('');
    $('#txt_CodigoVariante').val('');
    
    $('#cmbPop_MarcaFinalidade').prop('selectedIndex', 0).change();
    $('#cmbPop_MarcaFabrica').prop('selectedIndex', 0).change();
    $('#cmbPop_MarcaDimensao').prop('selectedIndex', 0).change();
    $('#cmbPop_MarcaTipo').prop('selectedIndex', 0).change();
    $('#cmbPop_MarcaSubTipo').prop('selectedIndex', 0).change();
    $('#cmbPop_MarcaImpressora').prop('selectedIndex', 0).change();
    $('#cmbPop_MarcaQualidadeImagem').prop('selectedIndex', 0).change();
    $('#txt_Descricao').val('');
    $('#txt_Valor').val('');
    $('#txt_Valor1PI').val('');
    $('#txt_Valor2PI').val('');

    $('#txt_ImgPrincipal').val('');
    $('#txt_ImgDetalhe').val('');  
    
    $('.div_variante').hide();
    $('.div_fase').hide();
    $('.div_variante_codigo').hide();
    $('.div_original_variante').hide();

    $('.div_codigo').hide();
    
    $('.div_dados').hide();
    $('.div_adicional').hide();
    $('.div_imagem').hide();
    $('.div_botoes').hide();    

    $.busyLoadFull("hide");
}

//#endregion

//#region MODAL

function fn_ModalOpcaoInvalida() {
    Swal.fire({
        title: 'Aten&ccedil;&atilde;o !!!',
        html: `Op&ccedil;&atilde;o, Inv&aacute;lida <br><br> Selecione uma das op&ccedil;&otilde;es de filtros dispon&iacute;veis!`,
        imageUrl: `${urlImgModaltext}`,
        imageWidth: 400,
        imageAlt: `${var_ImgAlt}`,
        focusConfirm: false,
        confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
        customClass: {
            confirmButton: 'btn btn-primary waves-effect waves-light'
        },
    }).then((result) => {
        //fn_Limpar();
        //console.log("fn_ModalOpcaoInvalida result ::: ", result);
    })
}

function fn_ModalErro(xhr, textStatus, errorThrown) {
    console.log("XMLHttpRequest  :: ", xhr);
    console.log("textStatus  :: ", textStatus);
    console.log("errorThrown  :: ", errorThrown);

    const responseMessage = xhr.responseText;
    console.log("Server Response:", responseMessage);

    const objError = JSON.parse(xhr.responseText);
    //console.log("Server msg:", obj.message);

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
            (popAddNewItem.querySelector('#cmbPop_MarcaFase').value = (obj === null ? '-1' : obj.idMarcaFase));
        (popAddNewItem.querySelector('#txt_Codigo').value = (obj === null ? '-1' : obj.codigoAceca));
    (popAddNewItem.querySelector('#txt_Nome').checked = (obj === null ? false : obj.nomeMarca));
        (popAddNewItem.querySelector('#txt_IncluidoPor').value = (obj === null ? '' : obj.incluidoPor)),
            (popAddNewItem.querySelector('#cmbPop_MarcaFinalidade').value = (obj === null ? '-1' : obj.idMarcaFinalidade));
    (popAddNewItem.querySelector('#cmbPop_MarcaFabrica').value = (obj === null ? '-1' : obj.idMarcaFabrica));
    (popAddNewItem.querySelector('#cmbPop_MarcaDimensao').value = (obj === null ? '-1' : obj.idMarcaDimensao));
    (popAddNewItem.querySelector('#cmbPop_MarcaTipo').value = (obj === null ? '-1' : obj.idMarcaTipo));
    (popAddNewItem.querySelector('#cmbPop_MarcaSubTipo').value = (obj === null ? '-1' : obj.idMarcaSubTipo));
    (popAddNewItem.querySelector('#cmbPop_MarcaImpressora').value = (obj === null ? '-1' : obj.idMarcaImpressora));
    (popAddNewItem.querySelector('#cmbPop_MarcaQualidadeImagem').value = (obj === null ? '-1' : obj.idMarcaQualidadeImagem));
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

        $("#cmbPop_MarcaFase").val(((obj.idMarcaFase === undefined || obj.idMarcaFase === null || obj.idMarcaFase <= 0) ? '-1' : obj.idMarcaFase)).change();
        $("#cmbPop_MarcaFinalidade").val(((obj.idMarcaFinalidade === undefined || obj.idMarcaFinalidade === null || obj.idMarcaFinalidade <= 0) ? '-1' : obj.idMarcaFinalidade)).change();
        $("#cmbPop_MarcaFabrica").val(((obj.idMarcaFabrica === undefined || obj.idMarcaFabrica === null || obj.idMarcaFabrica <= 0) ? '-1' : obj.idMarcaFabrica)).change();
        $("#cmbPop_MarcaDimensao").val(((obj.idMarcaDimensao === undefined || obj.idMarcaDimensao === null || obj.idMarcaDimensao <= 0) ? '-1' : obj.idMarcaDimensao)).change();
        $("#cmbPop_MarcaTipo").val(((obj.idMarcaTipo === undefined || obj.idMarcaTipo === null || obj.idMarcaTipo <= 0) ? '-1' : obj.idMarcaTipo)).change();
        $("#cmbPop_MarcaSubTipo").val(((obj.idMarcaSubTipo === undefined || obj.idMarcaSubTipo === null || obj.idMarcaSubTipo <= 0) ? '-1' : obj.idMarcaSubTipo)).change();
        $("#cmbPop_MarcaImpressora").val(((obj.idMarcaImpressora === undefined || obj.idMarcaImpressora === null || obj.idMarcaImpressora <= 0) ? '-1' : obj.idMarcaImpressora)).change();
        $("#cmbPop_MarcaQualidadeImagem").val(((obj.idQualidadeImagem === undefined || obj.idQualidadeImagem === null || obj.idQualidadeImagem <= 0) ? '-1' : obj.idQualidadeImagem)).change();

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

function fn_ModalGetObj() {
    //console.log("fn_ModalGetObj ::: ");

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
        CodigoVariante: $('#txt_CodigoVariante').val(),
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

        ImgPrincipal: $('#txt_ImgPrincipal').val().length > 0 ? $('#txt_ImgPrincipal').val() : null,
        ImgDetalhe: $('#txt_ImgDetalhe').val().length > 0 ? $('#txt_ImgDetalhe').val() : null,

        //FileImgPrincipal: fileImgPrincipal !== undefined ? objFileImgPrincipal : null,
       // FileImgDetalhe: fileImgDetalhe !== undefined ? objFileImgDetalhe : null,
    };

    //console.log("fn_ModalGetObj !", objFormData);

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

//#region COMBO
function fn_ChangeCombos() {    

    $('#cmbPop_MarcaFase').on('change', function () {

        let strTermoBusca = document.querySelector('#txt_Nome');
        let idMarcaFase = Number($(this).find('option:selected').val());

        //console.log("cmb_MarcaFase change idMarcaFase ::: ", idMarcaFase);
        //console.log("cmb_MarcaFase change strTermoBusca ::: ", strTermoBusca);

        if (idMarcaFase > 0) {

            switch (true) {
                case idMarcaFase === 14: // SA
                case (idMarcaFase >= 27 && idMarcaFase <= 29): //27-Palheiros , 28 Fumos, 29 Exportacao
                case (idMarcaFase >= 32 && idMarcaFase <= 34): //32-Cortadas, 33-Outros, 34-Quarentena
                case (idMarcaFase === 36): // Comemorativas
                case (idMarcaFase >= 39 && idMarcaFase <= 41): //39-Clandestinas, 40-Exterior, 41-M&C
                    fn_ShowCampos();
                    break;
                default:
                    fn_ChecaInicioNumero(strTermoBusca);
            }
        }
    });

    $('#cmbPop_MarcaVariante').on('change', function () {

        let idMarcaVariante = $(this).find('option:selected').val();

        //console.log("cmbPop_MarcaVariante change idMarcaVariante ::: ", idMarcaVariante);

        if (idMarcaVariante > 0) {

            $('.div_codigo').hide();
            $('.div_dados').hide();
            $('.div_imagem').hide();
            $('.div_botoes').hide();

            $('.div_variante_codigo').show();
        }
        else {
            if (idMarcaVariante >= 0) {
                $('.div_variante_codigo').hide();
                $('#txt_CodigoVariante').val('');

                fn_GetCodigoAceca()
            } else {
                $('.div_variante_codigo').hide();
                $('#txt_CodigoVariante').val('');

                //fn_ModalOpcaoInvalida();
            }
        }
    });

    $('#cmbPop_MarcaFabrica').on('change', function () {

        let idMarcaFabrica = $(this).find('option:selected').val();
        //console.log("cmb_MarcaFabrica change idMarcaFabrica ::: ", idMarcaFabrica);

        let bNovaVariante = $('#cmbPop_MarcaVariante').val() > 0 ? true : false;
        //console.log("cmb_MarcaTipo change bNovaVariante ::: ", bNovaVariante);

        if (idMarcaFabrica <= 0 && bNovaVariante === false) {
            fn_ModalOpcaoInvalida();
        }
    });

    $('#cmbPop_MarcaTipo').on('change', function () {
        let idMarcaTipo = $(this).find('option:selected').val();
        //console.log("cmb_MarcaTipo change idMarcaTipo ::: ", idMarcaTipo);

        let bNovaVariante = $('#cmbPop_MarcaVariante').val() > 0 ? true : false;
        //console.log("cmb_MarcaTipo change bNovaVariante ::: ", bNovaVariante);

        //Limpar Combo
        document.querySelectorAll('#cmbPop_MarcaSubTipo option').forEach(option => option.remove());

        $("#cmbPop_MarcaSubTipo").append($("<option></option>").val(0).html("-- Selecionar --"));

        if ($(this).length <= 1 && idMarcaTipo > 0) {
            fn_LoadCmb_MarcaSubTipo(idMarcaTipo);
        }

        if (idMarcaTipo <= 0 && bNovaVariante === false) {
            fn_ModalOpcaoInvalida();
        } else {
            //console.log("cmb_MarcaTipo change objVariante ::: ", objVariante);
            
            let marcaSubTipoId = ((objVariante.marcaSubTipoId === undefined || objVariante.marcaSubTipoId === null || objVariante.marcaSubTipoId <= 0) ? '-1' : objVariante.marcaSubTipoId);
            //console.log("cmb_MarcaTipo change marcaSubTipoId ::: ", marcaSubTipoId);
            //$("#cmbPop_MarcaSubTipo").val(Number(marcaSubTipoId)).change();
            /**/
        }
    });

   /*
    $('#cmbPop_MarcaSubTipo').on('change', function () {

        let idMarcaSubTipo = $(this).find('option:selected').val();
       console.log("cmb_MarcaSubTipo change idMarcaSubTipo ::: ", idMarcaSubTipo);

        let bNovaVariante = $('#cmbPop_MarcaVariante').val() > 0 ? true : false;
        //console.log("cmb_MarcaTipo change bNovaVariante ::: ", bNovaVariante);

        if (idMarcaSubTipo <= 0 && bNovaVariante === false) {
            //fn_ModalOpcaoInvalida();
        }
    });
     */
}

function fn_PopLoadCombos() {

    //console.log("fn_PopLoadCombos  ::: ");

    fn_LoadCmb_MarcaVariante();
    fn_LoadCmb_MarcaFase();
    fn_LoadCmb_MarcaFinalidade();
    fn_LoadCmb_MarcaFabrica();
    fn_LoadCmb_MarcaDimensao();
    fn_LoadCmb_MarcaTipo();
    fn_LoadCmb_MarcaSubTipo(0);
    fn_LoadCmb_MarcaImpressora();
    fn_LoadCmb_MarcaQualidadeImagem();
}

function fn_LoadCmb_MarcaVariante() {
    //console.log("fn_LoadCmb_MarcaVariante ::: ");

    if ($('#cmbPop_MarcaVariante').length <= 1) {
        $.ajax(
            {
                crossDomain: true,
                url: `${var_ControllerCmb}/AsyncCmb_Variante`,
                type: 'GET',
                success: function (data) {
                    //console.log("fn_LoadCmb_MarcaVariante  data ::: ", data);
                    $.each(data, function (id, result) {
                        //console.log("fn_LoadCmb_MarcaVariante  result id ::: ", id);
                        //console.log("fn_LoadCmb_MarcaVariante  result ::: ", result);
                        $("#cmbPop_MarcaVariante").append($("<option></option>").val(result.value).html(result.text));
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
    //console.log("fn_LoadCmb_MarcaFase ::: ");

    if ($('#cmbPop_MarcaFase').length <= 1) {
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

//#region FUNCOES

function fn_ShowCampos() {
    if ($('#txt_Codigo').val() !== '') {

        $('#cmbPop_MarcaVariante').prop('selectedIndex', 0).change();


        $('#txt_Codigo').val('');
        $('#txt_CodigoVariante').val('');

        /*
        $('#cmbPop_MarcaFinalidade').prop('selectedIndex', 0).change();
        $('#cmbPop_MarcaFabrica').prop('selectedIndex', 0).change();
        $('#cmbPop_MarcaDimensao').prop('selectedIndex', 0).change();
        $('#cmbPop_MarcaTipo').prop('selectedIndex', 0).change();
        $('#cmbPop_MarcaSubTipo').prop('selectedIndex', 0).change();
        $('#cmbPop_MarcaImpressora').prop('selectedIndex', 0).change();
        $('#cmbPop_MarcaQualidadeImagem').prop('selectedIndex', 0).change();
        */
        $('#txt_Descricao').val('');
        $('#txt_Valor').val('');
        $('#txt_Valor1PI').val('');
        $('#txt_Valor2PI').val('');

        $('#txt_ImgPrincipal').val('');
        $('#txt_ImgDetalhe').val('');

        //$('.div_variante').hide();
        //$('.div_fase').hide();
        //$('.div_variante_codigo').hide();
        $('.div_codigo').hide();

        $('.div_dados').hide();
        $('.div_adicional').hide();
        $('.div_imagem').hide();
        $('.div_botoes').hide();

    } else {
        //
        $('.div_variante').show();
    }
}

function fn_ChecaInicioNumero(strTermoBusca) {
    //console.log("default strTermoBusca match ::: ", strTermoBusca.value.match(/^\d/));

    //verifica se iniciada com numero
    if (strTermoBusca.value.match(/^\d/) !== null) {
        // console.log("Fases que as marcas iniciam com Numeros");

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
            html: `<b> Essa fase n&atilde;o possui marcas iniciadas com letras. </b> <br><br> Deseja continuar continuar?`,
            showCancelButton: true,
            confirmButtonText: `<i class="ri-chat-delete-line"></i> &nbsp; Sim, confirmar!`,
            cancelButtonText: `<i class="ri-check-double-line"></i> &nbsp; N&atilde;o, cancelar!`,
            reverseButtons: true
        }).then((result) => {
            if (result.isConfirmed) {

                fn_ShowCampos();

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
                }).then((result) => {
                    $('#txt_Nome').val();

                    $(this).prop('selectedIndex', 0).change();
                });
            }
        });

    } else {
        fn_ShowCampos();
    }
}

function fn_GetCodigoAceca() {
    //console.log("fn_GetCodigoAceca idMarcaFase ::: ", idMarcaFase);

    let idMarcaFase = $('#cmbPop_MarcaFase').find('option:selected').val();

    let txtNome = $('#txt_Nome').val();
    let txtCodigoVariante = $('#txt_CodigoVariante').val();
    let bNovaVariante = $('#cmbPop_MarcaVariante').val() > 0 ? true : false;

    $.ajax(
        {
            crossDomain: true,
            url: `${var_Controller}/GetNovoCodigoAceca`,
            type: 'POST',
            data: {
                idFase: idMarcaFase,
                strTermoBusca: bNovaVariante ? txtCodigoVariante : txtNome,
                bvariante: bNovaVariante
            },
            success: function (result) {
                //console.log("fn_GetCodigoAceca  result ::: ", result);

                if (result.bResult) {

                    //console.log("fn_GetCodigoAceca bNovaVariante ::: ", bNovaVariante);

                    if (bNovaVariante) { 
                        objVariante = result.data;
                        fn_GetDataVariante(objVariante);
                    }

                    //
                    $('#txt_Codigo').css('color', '#8c57ff');
                    $('#txt_Codigo').css('background-color', '#ede4ff');
                    $('#txt_Codigo').css('text-align', 'center');
                    $('#txt_Codigo').css("font-weight", "500");
                    $('#txt_Codigo').css("font-size", "0.9375rem");
                    $('#txt_Codigo').val(result.dataNovoCodigo)

                    $('.div_codigo').show();
                    $('.div_dados').show();
                    $('.div_imagem').show();
                    $('.div_botoes').show();
                } else {
                    //console.log("fn_GetCodigoAceca  result ::: ", result);
                    Swal.fire({
                        title: 'OPS!!',
                        icon: 'error',
                        html: `C&oacute;digo de Variante Inexistente !!<br><br> ERRO::: <b>` + result.data + `</b>`,
                        focusConfirm: false,
                        confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                        customClass: {
                            confirmButton: 'btn btn-label-danger waves-effect'
                        }
                    }).then((resultFalha) => {
                        //console.log("resultFalha  :: ", resultFalha);
                        fn_Limpar()
                    });

                    return false;
                }
            },
            error: function (xhr, textStatus, errorThrown) {
                fn_ModalErro(xhr, textStatus, errorThrown);
            },
        }
    );
}

function fn_GetDataVariante(obj) {
    //console.log("fn_GetDataVariante obj::: ", obj);

    // Pop ID
    (document.querySelector('#hdId').value = (obj === null ? 0 : obj.id));
    (document.querySelector('#hdMarcaFaseId').value = (obj === null ? 0 : obj.marcaFaseId));
    (document.querySelector('#hdMarcaFinalidadeId').value = (obj === null ? 0 : obj.marcaFinalidadeId));
    (document.querySelector('#hdMarcaFabricaId').value = (obj === null ? 0 : obj.marcaFabricaId));
    (document.querySelector('#hdMarcaDimensaoId').value = (obj === null ? 0 : obj.marcaDimensaoId));
    (document.querySelector('#hdMarcaTipoId').value = (obj === null ? 0 : obj.marcaTipoId));
    (document.querySelector('#hdMarcaSubTipoId').value = (obj === null ? 0 : obj.marcaSubTipoId));
    (document.querySelector('#hdMarcaImpressoraId').value = (obj === null ? 0 : obj.marcaImpressoraId));
    (document.querySelector('#hdMarcaQualidadeImagemId').value = (obj === null ? 0 : obj.marcaQualidadeImagemId));

        // Pop Variante
        const divVariante = document.getElementById("div_NomePaiVariante");
        $('#div_NomePaiVariante').css('color', '#FFFFFF');
        $('#div_NomePaiVariante').css('background-color', '#47007b');
        $('#div_NomePaiVariante').css('text-align', 'center');
        $('#div_NomePaiVariante').css("font-weight", "500");
        $('#div_NomePaiVariante').css("font-size", "0.9375rem");
        divVariante.style.padding = "20px";
        (document.getElementById("div_NomePaiVariante").innerHTML = (obj === null ? '' : `${obj.nome}<br><br>${obj.descricao}`));
        $('.div_original_variante').show();

        // Pop Dados
        (document.querySelector('#txt_IncluidoPor').value = (obj === null ? '' : obj.incluidoPor)),

        $("#cmbPop_MarcaFinalidade").val(((obj.marcaFinalidadeId === undefined || obj.marcaFinalidadeId === null || obj.marcaFinalidadeId <= 0) ? '-1' : obj.marcaFinalidadeId)).change(); 
        $("#cmbPop_MarcaFabrica").val(((obj.marcaFabricaId === undefined || obj.marcaFabricaId === null || obj.marcaFabricaId <= 0) ? '-1' : obj.marcaFabricaId)).change();
        $("#cmbPop_MarcaDimensao").val(((obj.marcaDimensaoId === undefined || obj.marcaDimensaoId === null || obj.marcaDimensaoId <= 0) ? '-1' : obj.marcaDimensaoId)).change();
        $("#cmbPop_MarcaTipo").val(((obj.marcaSubTipo.marcaTipoId === undefined || obj.marcaSubTipo.marcaTipoId === null || obj.marcaSubTipo.marcaTipoId <= 0) ? '-1' : obj.marcaSubTipo.marcaTipoId)).change();
        //$("#cmbPop_MarcaSubTipo").val(((obj.marcaSubTipoId === undefined || obj.marcaSubTipoId === null || obj.marcaSubTipoId <= 0) ? '-1' : obj.marcaSubTipoId)).change();
        $("#cmbPop_MarcaImpressora").val(((obj.marcaImpressoraId === undefined || obj.marcaImpressoraId === null || obj.marcaImpressoraId <= 0) ? '-1' : obj.marcaImpressoraId)).change();
        $("#cmbPop_MarcaQualidadeImagem").val(((obj.marcaQualidadeImagemId === undefined || obj.marcaQualidadeImagemId === null || obj.marcaQualidadeImagemId <= 0) ? '-1' : obj.marcaQualidadeImagemId)).change();

        (document.querySelector('#txt_Descricao').value = (obj === null ? '' : obj.descricao));

        //Pop Valores
        (obj.valor !== null || obj.valor1PI !== null || obj.valor2PI !== null) ? $('.div_adicional').show() : $('.div_adicional').hide();
        (document.querySelector('#txt_Valor').value = (obj === null ? '' : obj.valor));
        (document.querySelector('#txt_Valor1PI').value = (obj === null ? '' : obj.valor1PI));
        (document.querySelector('#txt_Valor2PI').value = (obj === null ? '' : obj.valor2PI));

        //Pop Arquivos
        (document.querySelector('#txt_ImgPrincipal').value = '')
        (document.querySelector('#txt_ImgDetalhe').value = '')
        //(obj === null || obj?.imgPrincipal === null) ? (document.querySelector('#txt_ImgPrincipal').value = '') : fnItem_PopImgPrincipal(obj);
        //(obj === null || obj?.imgDetalhe === null) ? (document.querySelector('#txt_ImgDetalhe').value = '') : fnItem_PopImgDetalhe(obj);
}

//#endregion

//#region CRUD

function fn_ModalSalvar(e) {
    //console.log("fn_FomSendData form ::", form);

    e.preventDefault();

    let formPage = document.forms.item(0);

    if (formPage === null || formPage === undefined) {
        $.busyLoadFull("hide");

        Swal.fire({
            title: 'OPS',
            html: 'Falha no carregamento do formul&aacute;rio da P&aacute;gina',
            icon: 'error',
            customClass: {
                confirmButton: 'btn btn-label-danger waves-effect'
            }
        });
    } else {

        let formObjData = fn_ModalGetObj();
        //console.log("fn_FomSendData formObjData ::", formObjData);

        let objFileImgPrincipal = {},
            objFileImgDetalhe = {};

        let fileImgPrincipal = $('#txt_ImgPrincipal').prop("files")[0];
        let fileImgDetalhe = $('#txt_ImgDetalhe').prop("files")[0];
        //console.log("fn_ModalGetObj fileImgPrincipal ::", fileImgPrincipal);
        //console.log("fn_ModalGetObj fileImgDetalhe ::", fileImgDetalhe);

        if (fileImgPrincipal !== undefined) {
            objFileImgPrincipal = {
                lastModified: fileImgPrincipal.lastModified,
                lastModifiedDate: fileImgPrincipal.lastModifiedDate,
                name: fileImgPrincipal.name,
                size: fileImgPrincipal.size,
                type: fileImgPrincipal.type,
                webkitRelativePath: fileImgPrincipal.webkitRelativePath,
            };
        }

        if (fileImgDetalhe !== undefined) {
            objFileImgDetalhe = {
                lastModified: fileImgDetalhe.lastModified,
                lastModifiedDate: fileImgDetalhe.lastModifiedDate,
                name: fileImgDetalhe.name,
                size: fileImgDetalhe.size,
                type: fileImgDetalhe.type,
                webkitRelativePath: fileImgDetalhe.webkitRelativePath,
            };
        }

        //console.log("fn_ModalGetObj objFileImgPrincipal ::", objFileImgPrincipal);
        //console.log("fn_ModalGetObj objFileImgDetalhe ::", objFileImgDetalhe);

        const formData = new FormData(document.forms.item(0));

        //formData.append('lstFile', fileImgPrincipal);
        //formData.append('lstFile', fileImgDetalhe);

        formData.append('strObjModel', JSON.stringify(formObjData));
        formData.append('iFileImgPrincipal', fileImgPrincipal);
        formData.append('iFileImgDetalhe', fileImgDetalhe);

        $.ajax({
            url: `${var_Controller}/Create`,
            type: 'POST',
            data: formData,
            cache: false,
            contentType: false,
            processData: false,
            success: function (result) {
                //console.log("result  :: ", result);

                $.busyLoadFull("hide");

                if (result.bResult === true && result.type === "OK") {

                    $.busyLoadFull("hide");

                    Swal.fire({
                        title: 'Dados Salvos!',
                        icon: 'success',
                        text: 'Marca cadastrada com sucesso.',
                        customClass: {
                            confirmButton: 'btn btn-success waves-effect waves-light'
                        }
                    }).then((resultSucesso) => {
                        window.location.reload();
                        //console.log("resultSucesso  :: ", resultSucesso);
                    });

                    return true;

                } else {

                    Swal.fire({
                        title: 'OPS!!',
                        icon: 'error',
                        html: `Dados n&atilde;o podem ser Salvos !!<br><br> ERRO::: <b>` + result + `</b>`,
                        focusConfirm: false,
                        confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                        customClass: {
                            confirmButton: 'btn btn-label-danger waves-effect'
                        }
                    }).then((resultFalha) => {
                        console.log("resultFalha  :: ", resultFalha);
                    });

                    return false;
                }
            },
            error: function (xhr, textStatus, errorThrown) {
                fn_ModalErro(xhr, textStatus, errorThrown);
            },
        });
    }
}

//#endregion