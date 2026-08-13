/**
 * Account Settings - Meus Dados
 */

'use strict';

//#region Declare

let var_Nome = 'Auth',
    var_Controller = '/Auth';
var msg = 'O preenchimento &eacute; obrigat&oacute;rio';

//#endregion

//#region CARREGAMENTO INICIAL

document.addEventListener('DOMContentLoaded', function () {
    (function () {
        console.log(`AUTH USER ACCOUNT - Todos os recursos terminaram o carregamento!`);

        fn_AuthUserAccount();
    })();
});

//#endregion

function fn_AuthUserAccount() {

    fn_CarregarMeusDados();
    fn_WireUploadAvatar();
    fn_WireSalvar();

    // Desativar conta continua com o fluxo de exemplo do template - ver depois.
    const deactivateAcc = document.querySelector('#formAccountDeactivation'),
        deactivateButton = deactivateAcc?.querySelector('.deactivate-account'),
        accountActivation = document.querySelector('#accountActivation');

    if (accountActivation && deactivateButton) {
        accountActivation.addEventListener('change', function () {
            deactivateButton.disabled = !accountActivation.checked;
        });
    }
}

//#region MEUS DADOS

function fn_CarregarMeusDados() {
    $.busyLoadFull("show");

    $.ajax({
        url: `${var_Controller}/GetFullById`,
        type: 'POST',
        success: function (response) {
            $.busyLoadFull("hide");

            if (!response?.bResult || !response?.data) return;

            const d = response.data;

            document.getElementById('nome').value = d.nome || '';
            document.getElementById('usuario').value = d.usuario || '';
            document.getElementById('email').value = d.email || '';
            document.getElementById('aniversario').value = fn_FormatarAniversarioInput(d.aniversarioDia, d.aniversarioMes, d.aniversarioAno);
            document.getElementById('telefone').value = fn_FormatarTelefoneInput(d.contatoDDD, d.contatoTelefone);
            document.getElementById('cep').value = d.cep || '';
            document.getElementById('endereco').value = d.endereco || '';
            document.getElementById('numero').value = d.numero || '';
            document.getElementById('complemento').value = d.complemento || '';
            document.getElementById('bairro').value = d.bairro || '';
            document.getElementById('cidade').value = d.cidade || '';
            $('#estado').val(d.estado || '').trigger('change');

            document.getElementById('uploadedAvatar').src = fnhelper_UrlAvatar(d.id, d.imgAvatar);
        },
        error: function (xhr, status, error) {
            $.busyLoadFull("hide");
            console.error("fn_CarregarMeusDados error: " + error);
        }
    });
}

function fn_FormatarAniversarioInput(dia, mes, ano) {
    if (!dia || !mes) return '';

    let strData = String(dia).padStart(2, '0') + '/' + String(mes).padStart(2, '0');

    return ano ? strData + '/' + ano : strData;
}

function fn_FormatarTelefoneInput(ddd, telefone) {
    if (!ddd || !telefone) return '';

    let strTelefone = String(telefone);

    return `(${ddd}) ${strTelefone.slice(0, -4)}-${strTelefone.slice(-4)}`;
}

// fnhelper_UrlAvatar é comum (helper-ui-common.js) - usada aqui e em pages-auth-user.js.

function fn_WireSalvar() {
    const form = document.querySelector('#formAccountSettings');

    if (!form) return;

    form.addEventListener('submit', function (e) {
        e.preventDefault();

        const nome = document.getElementById('nome').value.trim();

        if (!nome) {
            Swal.fire({
                title: 'Dados Inválidos!!',
                icon: 'error',
                html: `<b>${msg}: Nome</b>`,
                confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                customClass: { confirmButton: 'btn btn-label-danger waves-effect' }
            });

            return;
        }

        const telefoneDigitos = document.getElementById('telefone').value.replace(/\D/g, '');

        $.busyLoadFull("show");

        $.ajax({
            url: `${var_Controller}/UpdateProfile`,
            type: 'POST',
            data: {
                nome: nome,
                usuario: document.getElementById('usuario').value.trim(),
                telefoneDDD: telefoneDigitos ? telefoneDigitos.substring(0, 2) : null,
                telefoneNumero: telefoneDigitos ? telefoneDigitos.substring(2) : null,
                email: document.getElementById('email').value.trim(),
                aniversario: document.getElementById('aniversario').value.trim(),
                cep: document.getElementById('cep').value.trim(),
                endereco: document.getElementById('endereco').value.trim(),
                numero: document.getElementById('numero').value.trim(),
                complemento: document.getElementById('complemento').value.trim(),
                bairro: document.getElementById('bairro').value.trim(),
                cidade: document.getElementById('cidade').value.trim(),
                estado: $('#estado').val() || '',
            },
            success: function (response) {
                $.busyLoadFull("hide");

                if (!response?.bResult) {
                    Swal.fire({
                        title: 'Erro!!',
                        icon: 'error',
                        html: `<b>${response?.message || 'Falha ao salvar os dados'}</b>`,
                        confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                        customClass: { confirmButton: 'btn btn-label-danger waves-effect' }
                    });

                    return;
                }

                Swal.fire({
                    title: 'Dados Salvos!',
                    icon: 'success',
                    text: 'Suas informações foram atualizadas com sucesso.',
                    customClass: { confirmButton: 'btn btn-success waves-effect waves-light' }
                }).then(function () {
                    window.location.href = '/Home/Inicio';
                });
            },
            error: function (xhr, status, error) {
                $.busyLoadFull("hide");
                console.error("fn_WireSalvar error: " + error);
            }
        });
    });
}

//#endregion

//#region CEP

// fnhelper_MaskCEP e fnhelper_BuscaEnderecoPorCep são comuns (helper-ui-common.js). Callback específico
// desta tela - decide em quais campos preencher o retorno da ViaCEP.
function fn_PreencherEnderecoMeusDados(result) {
    document.getElementById('endereco').value = result.logradouro || '';
    document.getElementById('bairro').value = result.bairro || '';
    document.getElementById('cidade').value = result.localidade || '';
    $('#estado').val(result.uf || '').trigger('change');

    document.getElementById('numero').focus();
}

//#endregion

// fnhelper_MaskTelefone e fnhelper_MaskDataAniversario são comuns (helper-ui-common.js).

//#region AVATAR

function fn_WireUploadAvatar() {
    const accountUserImage = document.getElementById('uploadedAvatar');
    const fileInput = document.querySelector('.account-file-input');
    const resetFileInput = document.querySelector('.account-image-reset');

    if (!accountUserImage || !fileInput) return;

    const imagemOriginal = accountUserImage.src;

    fileInput.onchange = () => {
        if (!fileInput.files[0]) return;

        // Preview imediato, antes mesmo do upload terminar
        accountUserImage.src = window.URL.createObjectURL(fileInput.files[0]);

        let formData = new FormData();
        formData.append('arquivo', fileInput.files[0]);

        $.busyLoadFull("show");

        $.ajax({
            url: `${var_Controller}/UploadAvatar`,
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function (response) {
                $.busyLoadFull("hide");

                if (!response?.bResult) {
                    Swal.fire({
                        title: 'Erro!!',
                        icon: 'error',
                        html: `<b>${response?.message || 'Falha ao enviar a foto'}</b>`,
                        confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                        customClass: { confirmButton: 'btn btn-label-danger waves-effect' }
                    });

                    accountUserImage.src = imagemOriginal;

                    return;
                }

                // cache-busting: o nome do arquivo não muda entre uploads (imgAvatar{id}.png)
                accountUserImage.src = `${assetsPath}img/avatars/socio/${response.data.imgAvatar}?t=${Date.now()}`;
            },
            error: function (xhr, status, error) {
                $.busyLoadFull("hide");
                console.error("fn_WireUploadAvatar error: " + error);

                accountUserImage.src = imagemOriginal;
            }
        });
    };

    if (resetFileInput) {
        resetFileInput.onclick = () => {
            fileInput.value = '';
            accountUserImage.src = imagemOriginal;
        };
    }
}

//#endregion
