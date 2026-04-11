/**
 *  Pages Authentication
 */

'use strict';

//#region Declare

let var_Nome = 'Auth',
    var_Controller = '/Auth',
    var_ControllerCmb = '/HelperExtensions',

    varTbl_Obj = $('.datatables-basic'),
    varTbl_Data;

let formValid;

const _cka = "aceca_cookie";
const submitButton = document.querySelector('.btn-entrar');

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
        console.log(`AUTH - Todos os recursos terminaram o carregamento!`);

        fn_AuthIni();
  })();
});

//#endregion

function fn_Limpar() {
    document.getElementById('lEmail').value = '';
    document.getElementById('lSenha').value = '';

    document.cookie = `${_cka}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/;`;
    sessionStorage.removeItem('aceca_sessao');
}

function fn_Viewpass() {
    const i = document.getElementById('lSenha');
    i.type = i.type === 'password' ? 'text' : 'password';
}

function fn_FormValidator(formAuthentication) {

    if (formAuthentication) {
        formValid = FormValidation.formValidation(formAuthentication, {
            fields: {
                lEmail: {
                    validators: {
                        notEmpty: {
                            message: 'Digite seu e-mail'
                        },
                        emailAddress: {
                            message: 'Insira um endereço de e-mail válido'
                        }
                    }
                },
                lSenha: {
                    validators: {
                        notEmpty: {
                            message: 'Digite sua senha'
                        },
                        /*
                        stringLength: {
                          min: 6,
                          message: 'Password must be more than 6 characters'
                        }*/
                    }
                },
            },
            plugins: {
                trigger: new FormValidation.plugins.Trigger(),

                bootstrap5: new FormValidation.plugins.Bootstrap5({
                    rowSelector: '.fg',
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

function fn_AuthIni() {

    let userCk = fn_CkGet(_cka);

    if (userCk != "") {

        Swal.fire({
            title: `Ol&aacute; ${userCk.split("|")[0]}!`,
            html: `Seja bem-vindo novamente`,
            imageUrl: `${urlImgModaltext}`,
            imageWidth: 400,
            imageAlt: `${var_ImgAlt}`,
            focusConfirm: false,
            confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
            customClass: {
                confirmButton: 'btn btn-primary waves-effect waves-light'
            },
        }).then((result) => {
            window.location.href = '/Auth/Access';
        })

    } else {
        fn_Limpar();

        const formAuthentication = document.querySelector('#frmLogin');

        fn_FormValidator(formAuthentication);

        submitButton.addEventListener('click', function (e) {

            e.preventDefault();

            if (formValid) {
                formValid.validate().then(function (status) {

                    if (status == 'Valid') {
                        fn_Auth();
                    }
                });
            }
        });
    }
}

async function fn_Auth() {
    
    submitButton.setAttribute('data-kt-indicator', 'on');

    submitButton.disabled = true;

    const email = document.getElementById('lEmail').value.trim().toLowerCase();
    const senha = document.getElementById('lSenha').value;
    const btn = document.getElementById('btnEntrar');
    const err = document.getElementById('loginErr');

    err.style.display = 'none';
    btn.disabled = true; btn.textContent = 'Verificando…';

    let user = null;

    try {

        const response = await fetch(`${var_Controller}/Login`, {
            method: 'POST',
            headers: {
                "Content-Type": 'application/json',
            },
            body: JSON.stringify({ email, senha }),
        });

        if (response.ok) {
            user = await response.json();

            btn.disabled = false;
            btn.textContent = 'Entrar';

            if (user) {

                Swal.fire({
                    icon: 'success',
                    title: `Ol&aacute; ${user?.nome?.split(" ")[0]}!`,
                    html: `Seja bem-vindo`,
                    focusConfirm: true,
                    confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                    customClass: {
                        confirmButton: 'btn btn-label-success waves-effect'
                    }
                }).then((result) => {
                    fn_CkSet(_cka, user?.nome?.split(" ")[0], 60);
                    sessionStorage.setItem('aceca_sessao', JSON.stringify(user));
                    window.location.href = '/Auth/Access';
                });

                submitButton.disabled = false;

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
                    fn_Limpar();
                });

                err.textContent = '❌ E-mail ou senha incorretos. Verifique suas credenciais.';
                err.style.display = 'block';

                fn_Limpar();
            }
        } else {

            btn.disabled = false;
            btn.textContent = 'Entrar';

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
                fn_Limpar();
            });
        } 
    }
    catch (ex){

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
            fn_Limpar();
        });
    }
}

function fn_CkGet(cname) {
    
    let name = cname + "=";
    let decodedCookie = decodeURIComponent(document.cookie);
    let ca = decodedCookie.split(';');
    for (let i = 0; i < ca.length; i++) {
        let c = ca[i];
        while (c.charAt(0) == ' ') {
            c = c.substring(1);
        }
        if (c.indexOf(name) == 0) {
            return c.substring(name.length, c.length);
        }
    }
    return "";
}

function fn_CkSet(cname, cvalue, exmins ) {
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
        return `${cvalue}|${v.toString(16) }${hash}`;
    });

    d.setTime(d.getTime() + (exmins * 1000));
    let expires = `expires=${d.toUTCString()}`;
    let ckFull = `${cname}= ${hash};${expires};path=/`;

    document.cookie = ckFull;
}
