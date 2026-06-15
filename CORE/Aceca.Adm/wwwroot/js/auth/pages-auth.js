/**
 *  Pages Authentication
 */

'use strict';

//#region Declare

// cache busting
const VERSION = "1.0.2";
//console.log("Auth JS version:", VERSION);

let var_Nome = 'Auth',
    var_Controller = '/Auth',
    var_ControllerCmb = '/HelperExtensions',

    varTbl_Obj = $('.datatables-basic'),
    varTbl_Data;

let _sessionDataAuth = null;
const _cka = "aceca_cookie";

//
const loginSubmitButton = document.querySelector('.btn-entrar');
let loginFormValid;

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

        fn_LoginAuthIni();
  })();
});

//#endregion

//#region Login

async function fn_LoginAuth() {
    //console.log(`fn_LoginAuth ::`);

    loginSubmitButton.setAttribute('data-kt-indicator', 'on');

    loginSubmitButton.disabled = true;

    const email = document.getElementById('lEmail').value.trim().toLowerCase();
    const senha = document.getElementById('lSenha').value;
    const btn = document.getElementById('btnEntrar');
    const err = document.getElementById('loginErr');

    err.style.display = 'none';
    btn.disabled = true;
    btn.textContent = 'Verificando…';

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
            
            let user = await response.json();

            if (user.bResult) {

                //console.log(`AUTH fn_LoginAuth - user :: `, user);

                btn.disabled = false;
                btn.textContent = 'Entrar';

                fn_LoginAuthGeo(user.nameIdentifier);

                fn_LoginCkSet(_cka, user?.nome?.split(" ")[0], 60);
                sessionStorage.setItem('aceca_sessao', JSON.stringify(user));

                loginSubmitButton.disabled = false;

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
                        html: `Seja bem-vindo`,
                        focusConfirm: true,
                        confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                        customClass: {
                            confirmButton: 'btn btn-label-success waves-effect'
                        }
                    }).then((result) => {
                        window.location.href = '/Auth/Access';
                    });
                }

            } else {
                let msgContato = `<b>Não é possivel realizar o acesso. <br><br>Entre em contato conosco !!!</b>`;

                //console.log(`AUTH fn_LoginAuth - user :: `, user);

                Swal.fire({
                    title: `${user?.message}`,
                    icon: 'error',
                    html: `${msgContato}`,
                    focusConfirm: false,
                    confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                    customClass: {
                        confirmButton: 'btn btn-label-danger waves-effect'
                    }
                }).then((resultFail) => {

                    btn.disabled = false;
                    btn.textContent = 'Entrar';

                    fn_LoginLimpar();
                });

                err.innerHTML = `❌ ${msgContato}`;
                err.style.display = 'block';

                btn.disabled = false;
                btn.textContent = 'Entrar';

                fn_LoginLimpar();
            }
        } else {

            btn.disabled = false;
            btn.textContent = 'Entrar';

            const errObject = await response.json();

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
            }).then((resultFail) => {
                fn_LoginLimpar();
            });
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
            fn_LoginLimpar();
        });
    }
}

function fn_LoginAuthIni() {

    //console.log(`fn_LoginAuthIni ::`);

    let userCk = fn_LoginCkGet(_cka);

    if (userCk != "") {

        //console.log(`AUTH fn_LoginAuthIni - userCk :: `, userCk.split("|"));

        if (sessionStorage?.getItem("aceca_sessao") !== null) {
            _sessionDataAuth = JSON.parse(sessionStorage.getItem("aceca_sessao"));

            if (_sessionDataAuth !== null) {
                fn_LoginAuthGeo(_sessionDataAuth?.nameIdentifier);
            }
        }


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
        fn_LoginLimpar();

        const loginFormAuthentication = document.querySelector('#frmLogin');

        fn_LoginFormValidator(loginFormAuthentication);

        loginSubmitButton.addEventListener('click', function (e) {

            e.preventDefault();

            if (loginFormValid) {
                loginFormValid.validate().then(function (status) {

                    if (status == 'Valid') {
                        fn_LoginAuth();
                    }
                });
            }
        });
    }
}

function fn_LoginLimpar() {
    document.getElementById('lEmail').value = '';
    document.getElementById('lSenha').value = '';

    document.cookie = `${_cka}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/;`;
    sessionStorage.removeItem('aceca_sessao');
}

function fn_LoginFormValidator(loginFormAuthentication) {

    if (loginFormAuthentication) {
        loginFormValid = FormValidation.formValidation(loginFormAuthentication, {
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

function fn_LoginCkGet(cname) {
    
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

function fn_LoginCkSet(cname, cvalue, exmins ) {
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

// ==========================
// LOGIN
// ==========================
async function handleLogin(event) {

    console.log(`handleLogin ::  ${event}`);

    event.preventDefault();

    const btn = document.getElementById("btn-login");
    btn.disabled = true;

    const email = document.getElementById("lEmail").value.trim();
    const password = document.getElementById("lSenha").value.trim();

    if (!email || !password) {
        Swal.fire("Erro", "Preencha todos os campos", "error");
        btn.disabled = false;
        return;
    }

    try {

        console.log(`var_Controller ::  ${var_Controller}`);
        console.log(`/Login ::  ${var_Controller}/Login`);

        const response = await fetch(`${var_Controller}/Login`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ email, password })
        });

        const data = await response.json();

        console.log(`data ::  ${data}`);

        if (data.ok) {

            // cache leve
            sessionStorage.setItem("user", email);

            if (document.getElementById("rememberMe").checked) {
                document.cookie = "user=" + email + "; max-age=3600; path=/";
            }

            Swal.fire({
                icon: "success",
                title: "Login realizado",
                timer: 1500,
                showConfirmButton: false
            });

            setTimeout(() => window.location.href = "/dashboard", 1500);

        } else {
            Swal.fire("Erro", data.message, "error");
        }

    } catch (err) {
        Swal.fire("Erro", "Falha na requisição", "error");
    }

    btn.disabled = false;
}

//#endregion

//#region ESQUECI SENHA

// ==========================
// ESQUECI SENHA
// ==========================
async function handleForgotPassword(e) {
    e.preventDefault();

    const { value: email } = await Swal.fire({
        title: "Recuperar senha",
        input: "email",
        inputLabel: "Digite seu email",
        inputPlaceholder: "email@exemplo.com",
        confirmButtonText: "Enviar",
        showCancelButton: true
    });

    if (!email) return;

    const res = await fetch(`${var_Controller}/ForgotPassword`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email })
    });

    const data = await res.json();

    if (data.ok) {
        Swal.fire("Sucesso", "Email de recuperação enviado", "success");
    } else {
        Swal.fire("Erro", data.message, "error");
    }
}

//#endregion

//#region GEO

// ==========================
// GEO
// ==========================
    async function fn_LoginAuthGeo(userId) {
        //console.log(`fn_LoginAuthGeo userId::: ${userId}`);

        try {

            let url = `https://api.ipify.org?format=json`;

            const response = await fetch(`${url}`);

            const data = await response.json();

            if (data?.ip !== '') {

                try {

                    let varIp = data.ip;
                    //console.log(`fn_LoginAuthGeo varIp ::: ${data.ip}`);

                    const response = await fetch(`${var_Controller}/LoginLog`, {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/x-www-form-urlencoded',
                        },
                        body: new URLSearchParams({
                            strIp: varIp,
                            srtId: userId
                        }),
                    });

                    if (!response.ok) {
                        throw new Error(`HTTP error! status: ${response.status}`);
                    }

                    const result = await response.json(); // Wait for data to parse
                    //console.log("Success:", result);
                    return result;
                } catch (error) {
                    console.error("Error:", error);
                }
            }

        } catch (error) {
            console.error('Error fetching IP address:', error);
        }
    }
//#endregion