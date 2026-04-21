/**
 *  Pages Authentication Update
 */

'use strict';

//#region Declare

let var_Nome = 'Auth',
    var_Controller = '/Auth',
    var_ControllerCmb = '/HelperExtensions',

    varTbl_Obj = $('.datatables-basic'),
    varTbl_Data;

const _cka = "aceca_cookie";

//
const regUpdtSubmitButton = document.querySelector('.btn-register-update');
let regUpdtFormValid;

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
        console.log(`AUTH UPTD - Todos os recursos terminaram o carregamento!`);

        fn_RegUpdtAuthIni();

        $('#userNameCPF').mask('000.000.000-00', { reverse: true });
  })();
});

//#endregion

//#region Update Register

async function fn_RegUpdtAuth() {

    regUpdtSubmitButton.disabled = true;

    const userNameCPF = document.getElementById('userNameCPF').value;
    const email = document.getElementById('email').value.trim().toLowerCase();
    const senha = document.getElementById('password').value;
    const confirmSenha = document.getElementById('confirmPassword').value;
    const chkTermo = document.getElementById('terms-conditions').checked; //$('#chk_PesquisarDescricao')[0].checked,
    const btn = document.getElementById('btnRegisterUpdate');

    btn.disabled = true;
    btn.textContent = 'Atualizando…';

    let user = null;

    try {
        if (!chkTermo) {
            Swal.fire({
                title: 'Termos Inv&aacute;lidos!!',
                icon: 'error',
                html: `<b>É necessário aceitar os Termos de Privacidade!!!</b>`,
                focusConfirm: false,
                confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                customClass: {
                    confirmButton: 'btn btn-label-danger waves-effect'
                }
            }).then((result) => {
                return false;
            });
        } else {

            const response = await fetch(`${var_Controller}/LoginUpdate`, {
                method: 'POST',
                headers: {
                    "Content-Type": 'application/json',
                },
                body: JSON.stringify({ userNameCPF, email, senha, confirmSenha, chkTermo }),
            });

            if (response.ok) {

                let user = await response.json();

                if (user.bResult) {

                    btn.disabled = false;
                    btn.textContent = 'Entrar';

                    fn_RegUpdtCkSet(_cka, user?.nome?.split(" ")[0], 60);
                    sessionStorage.setItem('aceca_sessao', JSON.stringify(user));

                    regUpdtSubmitButton.disabled = false;

                    if (user?.pswuptd === false) {
                        Swal.fire({
                            title: `Ol&aacute; ${user?.nome?.split(" ")[0]}!`,
                            html: `Identificamos que a sua senha expirou!! <br><br> Fa&ccedil;a a atualiza&ccedil;&atilde;o para realizar seu acesso.`,
                            imageUrl: `${urlImgModaltext}`,
                            imageWidth: 400,
                            imageAlt: `${var_ImgAlt}`,
                            focusConfirm: false,
                            confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                            customClass: {
                                confirmButton: 'btn btn-primary waves-effect waves-light'
                            }
                        }).then((result) => {
                            window.location.href = '/Auth/UpdatePass';
                        });
                    } else {
                        Swal.fire({
                            icon: 'success',
                            title: `Ol&aacute; ${user?.nome?.split(" ")[0]}!`,
                            html: `Seus dados foram atualizados<br><br> Realize seu login !!`,
                            focusConfirm: true,
                            confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                            customClass: {
                                confirmButton: 'btn btn-label-success waves-effect'
                            }
                        }).then((result) => {
                            window.location.href = 'http://aceca.tryasp.net/';
                        });
                    }

                } else {

                    Swal.fire({
                        title: 'Dados Inv&aacute;lidos!!',
                        icon: 'error',
                        html: `<b>Verifique suas credenciais!!!</b>`,
                        focusConfirm: false,
                        confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                        customClass: {
                            confirmButton: 'btn btn-label-danger waves-effect'
                        }
                    }).then((result) => {
                        fn_RegUpdtLimpar();
                    });
                }
            } else {

                btn.disabled = false;
                btn.textContent = 'Atualizar Dados';

                let err = await response.json();
                console.log(`response err ::  ${err}`);

                const errObject = JSON.parse(err);

                console.log(`response errObject ::  ${errObject}`);

                Swal.fire({
                    title: 'Ops!!',
                    icon: 'error',
                    html: `<b>N&atilde;o foi possível realizar o acesso!!!</b>`,
                    focusConfirm: false,
                    confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                    customClass: {
                        confirmButton: 'btn btn-label-danger waves-effect'
                    }
                }).then((result) => {
                    fn_RegUpdtLimpar();
                });
            }
        }
    }
    catch (ex) {

        btn.disabled = false;
        btn.textContent = 'Entrar';

        console.log(`response ex ::  ${ex}`);

        Swal.fire({
            title: 'Ops!!',
            icon: 'error',
            html: `<b>N&atilde;o foi possível realizar o acesso!!!</b>`,
            focusConfirm: false,
            confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
            customClass: {
                confirmButton: 'btn btn-label-danger waves-effect'
            }
        }).then((result) => {
            fn_RegUpdtLimpar();
        });
    }

}

function fn_RegUpdtAuthIni() {

    const regUpdtFormAuthentication = document.querySelector('#frmRegisterUpdate');

    // Revalidate the confirmation password when changing the password
    regUpdtFormAuthentication.querySelector('[name="password"]').addEventListener('input', function () {
        regUpdtFormValid.revalidateField('confirmPassword');
    });

    fn_RegUpdtFormValidator(regUpdtFormAuthentication);

    regUpdtSubmitButton.addEventListener('click', function (e) {
        //console.log(`regUpdtSubmitButton  ::  ${e}`);

        if (fn_RegUpdtFormValidation()) {
            e.preventDefault();

            if (regUpdtFormValid) {
                regUpdtFormValid.validate().then(function (status) {               
                    if (status == 'Valid') {
                        fn_RegUpdtAuth();
                    }
                });
            }
        }
    });
}

function fn_RegUpdtFormValidator(regUpdtFormAuthentication) {

    if (regUpdtFormAuthentication) {
        regUpdtFormValid = FormValidation.formValidation(regUpdtFormAuthentication, {
            fields: {
                username: {
                    validators: {
                        notEmpty: {
                            message: 'Digite seu CPF'
                        },
                    }
                },
                email: {
                    validators: {
                        notEmpty: {
                            message: 'Digite seu e-mail'
                        },
                        emailAddress: {
                            message: 'Insira um endereço de e-mail válido'
                        }
                    }
                },
                

                password: {
                    validators: {
                        notEmpty: {
                            message: 'The password is required',
                        },
                    },
                },
                confirmPassword: {
                    validators: {
                        identical: {
                            compare: function () {
                                return form.querySelector('[name="password"]').value;
                            },
                            message: 'The password and its confirm are not the same',
                        },
                    },
                },

                terms: {
                    validators: {
                        notEmpty: {
                            message: 'Aceite os termos e condições'
                        }
                    }
                }
            },
            plugins: {
                trigger: new FormValidation.plugins.Trigger(),

                bootstrap5: new FormValidation.plugins.Bootstrap5({
                    rowSelector: '.input-group',
                    eleInvalidClass: '',
                    eleValidClass: ''
                }),

                autoFocus: new FormValidation.plugins.AutoFocus(),
            },
            init: instance => {
                instance.on('plugins.message.placed', function (e) {
                    if (e.element.parentElement.classList.contains('input-group')) {
                        e.element.parentElement.insertAdjacentElement('afterend', e.messageElement);
                    }
                });
            }
        });
    }

}

function fn_RegUpdtFormValidation() {

    const userNameCPF = document.getElementById('userNameCPF').value;
    const email = document.getElementById('email').value.trim().toLowerCase();
    const senha = document.getElementById('password').value;
    const confirmSenha = document.getElementById('confirmPassword').value;
    const chkTermo = document.getElementById('terms-conditions').checked; //$('#chk_PesquisarDescricao')[0].checked,
    const btn = document.getElementById('btnRegisterUpdate');

    // Regex Patterns
    const userPattern = /(^\d{3}\.\d{3}\.\d{3}\-\d{2}$)|(^\d{2}\.\d{3}\.\d{3}\/\d{4}\-\d{2}$)/;
    const emailPattern = /^[^ ]+@[^ ]+\.[a-z]{2,3}$/;
    const passPattern = /^(?=.*\d).{8,}$/;

    let valid = true;

    // userNameCPF
    if (!userPattern.test(userNameCPF)) {
        document.getElementById("regUpdtUserErr").style.display = "block";
        document.getElementById("regUpdtUserErr").textContent = "Insira um CPF válido";
        valid = false;
    } else {
        document.getElementById("regUpdtUserErr").style.display = "none";
    }
    // Email
    if (!emailPattern.test(email)) {
        document.getElementById("regUpdtEmailErr").style.display = "block";
        document.getElementById("regUpdtEmailErr").textContent = "Insira um endereço de e-mail válido";
        valid = false;
    } else {
        document.getElementById("regUpdtEmailErr").style.display = "none";
    }
    // Password
    if (!passPattern.test(senha)) {
        document.getElementById("regUpdtPassErr").style.display = "block";
        document.getElementById("regUpdtPassErr").textContent = "A senha deve ter pelo menos 8 caracteres e pelo menos 1 número";
        valid = false;
    } else {
        document.getElementById("regUpdtPassErr").style.display = "none";
    }
    // Confirm Password
    if (senha !== confirmSenha) {
        document.getElementById("regUpdtPassConfirmErr").style.display = "block";
        document.getElementById("regUpdtPassConfirmErr").textContent = "A senha e a confirmação não são iguais";
        valid = false;
    } else {
        document.getElementById("regUpdtPassConfirmErr").style.display = "none";
    }
    // Terms
    if (!chkTermo) {
        document.getElementById("regUpdtChkErr").style.display = "block";
        document.getElementById("regUpdtChkErr").textContent = "Aceite os termos e condições";
        valid = false;
    } else {
        document.getElementById("regUpdtChkErr").style.display = "none";
    }
    // Final check
    return valid;

}

function fn_RegUpdtLimpar() {
    document.getElementById('username').value = '';
    document.getElementById('email').value = '';
    document.getElementById('password').value = '';
    document.getElementById('confirmPassword').value = '';
    document.getElementById('terms-conditions').checked = false;

    document.cookie = `${_cka}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/;`;
    sessionStorage.removeItem('aceca_sessao');
}

function fn_RegUpdtCkSet(cname, cvalue, exmins) {
    const d = new Date();
    let hash = 0;
    let exdays = exmins * 24;

    for (const char of cvalue) {
        hash = (hash << 5) - hash + char.charCodeAt(0);
        hash |= 0; // Constrain to 32bit integer
    }

    hash = 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
        const r = Math.random() * 16 | 0,
            v = c === 'x' ? r : (r & 0x3 | 0x8);
        return `${cvalue}|${v.toString(16)}${hash}`;
    });

    d.setTime(d.getTime() + (exmins * 1000));
    let expires = `expires=${d.toUTCString()}`;
    let ckFull = `${cname}= ${hash};${expires};path=/`;

    document.cookie = ckFull;
}
//#endregion