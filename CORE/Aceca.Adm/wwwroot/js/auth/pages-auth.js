/**
 *  Pages Authentication
 */

'use strict';

//#region Declare

// cache busting
const VERSION = "1.0.2";
//console.log("Auth JS version:", VERSION);

let var_Nome = 'Auth',
    var_Controller = '/Auth';

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
    const err   = document.getElementById('loginErr');
    const email = document.getElementById('lEmail').value.trim().toLowerCase();
    const senha = document.getElementById('lSenha').value;

    fn_BtnLoading(loginSubmitButton);
    err.style.display = 'none';

    try {
        const response = await fetch(`${var_Controller}/Login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, senha }),
        });

        const user = await response.json();

        if (response.ok && user.bResult) {

            // Geo: fire-and-forget — não bloqueia o redirecionamento
            fn_LoginAuthGeo(user.nameIdentifier);

            fn_LoginCkSet(_cka, user.nome?.split(' ')[0], 1440); // 24h (teto absoluto da sessão)
            sessionStorage.setItem('aceca_sessao', JSON.stringify(user));

            // Botão permanece desabilitado enquanto o Swal exibe (redirect iminente)
            if (user.pswuptd === false) {
                Swal.fire({
                    title: `Ol&aacute; ${user.nome?.split(' ')[0]}!`,
                    html: `Identificamos que a sua senha expirou!!<br><br>Fa&ccedil;a a atualiza&ccedil;&atilde;o para realizar seu acesso.`,
                    imageUrl: urlImgModaltext,
                    imageWidth: 400,
                    imageAlt: var_ImgAlt,
                    focusConfirm: false,
                    confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                    customClass: { confirmButton: 'btn btn-primary waves-effect waves-light' }
                }).then(() => { window.location.href = '/Auth/UpdatePass'; });
            } else {
                Swal.fire({
                    icon: 'success',
                    title: `Ol&aacute; ${user.nome?.split(' ')[0]}!`,
                    html: `Seja bem-vindo`,
                    focusConfirm: true,
                    confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                    customClass: { confirmButton: 'btn btn-label-success waves-effect' }
                }).then(() => { window.location.href = '/Auth/Access'; });
            }

        } else {
            const msg = user?.message ?? 'Não foi possível realizar o acesso.';

            err.textContent = `❌ ${msg}`; // textContent evita XSS com conteúdo do servidor
            err.style.display = 'block';

            fn_BtnReset(loginSubmitButton);
            document.getElementById('lSenha').value = ''; // limpa só a senha, mantém o e-mail

            Swal.fire({
                title: msg,
                icon: 'error',
                html: `<b>Verifique suas credenciais e tente novamente.</b>`,
                focusConfirm: true,
                confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                customClass: { confirmButton: 'btn btn-label-danger waves-effect' }
            });
        }

    } catch (ex) {
        console.error('fn_LoginAuth:', ex);
        fn_BtnReset(loginSubmitButton);
        Swal.fire({
            title: 'Ops!!',
            icon: 'error',
            html: `<b>N&atilde;o foi poss&iacute;vel realizar o acesso.<br>Tente novamente.</b>`,
            focusConfirm: true,
            confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
            customClass: { confirmButton: 'btn btn-label-danger waves-effect' }
        });
    }
}

// Exibe estado de carregamento no botão usando a estrutura HTML do spinner
function fn_BtnLoading(btn) {
    btn.disabled = true;
    const txtSpan  = btn.querySelector('.btn-text');
    const spinSpan = btn.querySelector('.btn-spinner');
    if (txtSpan)  txtSpan.style.display  = 'none';
    if (spinSpan) spinSpan.style.display = 'inline-flex';
}

// Restaura o botão ao estado normal
function fn_BtnReset(btn) {
    btn.disabled = false;
    const txtSpan  = btn.querySelector('.btn-text');
    const spinSpan = btn.querySelector('.btn-spinner');
    if (txtSpan)  txtSpan.style.display  = '';
    if (spinSpan) spinSpan.style.display = 'none';
}

function fn_LoginAuthIni() {

    //console.log(`fn_LoginAuthIni ::`);

    const userCk         = fn_LoginCkGet(_cka);
    const urlParams      = new URLSearchParams(window.location.search);
    const comingExpired  = urlParams.get('expired') === '1';

    if (userCk !== "") {

        if (fn_LoginSessionIsValid()) {
            // Sessão ainda válida: exibe boas-vindas e redireciona

            //console.log(`AUTH fn_LoginAuthIni - userCk :: `, userCk.split("|"));

            if (sessionStorage?.getItem("aceca_sessao") !== null) {
                _sessionDataAuth = JSON.parse(sessionStorage.getItem("aceca_sessao"));
                if (_sessionDataAuth !== null) {
                    fn_LoginAuthGeo(_sessionDataAuth?.nameIdentifier);
                }
            }

            // Usa a foto cadastrada em imgAvatar no Swal de boas-vindas, se houver -
            // sem cadastro, mantém a imagem padrão (logo) já usada antes.
            $.ajax({
                url: '/Auth/GetAvatarInfo',
                type: 'GET',
                success: function (response) {
                    const temAvatar = !!(response?.bResult && response.data?.imgAvatar);

                    // Cache-busting: sem isso, o browser podia manter em cache uma versão
                    // antiga da imagem (mesmo nome de arquivo, imgAvatar{id}.png) e o círculo
                    // no Swal não atualizava depois de uma troca de foto recente.
                    const urlImagemBoasVindas = temAvatar
                        ? `${fnhelper_UrlAvatar(response.data.id, response.data.imgAvatar)}?t=${Date.now()}`
                        : urlImgModaltext;

                    fn_SwalBemVindoNovamente(userCk, urlImagemBoasVindas, temAvatar);
                },
                error: function () {
                    fn_SwalBemVindoNovamente(userCk, urlImgModaltext, false);
                }
            });

        } else {
            // Cookie presente mas timestamps indicam expiração (browser fechado + reaberto)
            fn_LoginLimpar();
            fn_LoginShowExpiredAlert();
        }

    } else if (comingExpired) {
        // Redirecionado da página SessionExpired
        fn_LoginLimpar();
        fn_LoginShowExpiredAlert();

    } else {
        // Sem sessão anterior: exibe formulário normalmente
        fn_LoginLimpar();
        fn_LoginSetupForm();
    }
}

function fn_SwalBemVindoNovamente(userCk, urlImagem, isAvatar) {
    // Foto do sócio (isAvatar) entra num círculo, igual ao .lc-circle da tela de login
    // (porém maior) - montado via html direto (em vez de imageUrl/imageClass do Swal2,
    // que não estava aplicando a moldura) pra ter controle total do marcador.
    // A logo padrão (fallback) mantém o tamanho/formato antigo, sem círculo.
    const conteudoImagem = isAvatar
        ? `<div class="swal-avatar-circle mx-auto mb-4"><img src="${urlImagem}" alt="${var_ImgAlt}" /></div>`
        : `<img src="${urlImagem}" alt="${var_ImgAlt}" style="width:400px;max-width:100%;" class="mb-4" />`;

    Swal.fire({
        title: `Ol&aacute; ${userCk.split("|")[0].trim()}!`,
        html: `${conteudoImagem}<div>Seja bem-vindo novamente</div>`,
        focusConfirm: false,
        confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
        customClass: {
            confirmButton: 'btn btn-primary waves-effect waves-light'
        },
    }).then(() => {
        window.location.href = '/Auth/Access';
    });
}

// Retorna true se a sessão local ainda está dentro dos limites de 2h ocioso / 24h absoluto
function fn_LoginSessionIsValid() {
    try {
        const IDLE_MS    = 2 * 60 * 60 * 1000; // 2h
        const lastAct    = parseInt(localStorage.getItem('aceca_last_activity'), 10) || 0;
        const absExp     = parseInt(localStorage.getItem('aceca_abs_exp'), 10) || 0;
        const now        = Date.now();

        if (lastAct && (now - lastAct) >= IDLE_MS) return false;
        if (absExp  && now >= absExp)               return false;

        return true;
    } catch (e) {
        return true; // se não conseguir verificar, assume válido e deixa o servidor decidir
    }
}

// Exibe Swal de sessão expirada e depois inicializa o formulário de login
function fn_LoginShowExpiredAlert() {
    fn_LoginSetupForm();

    Swal.fire({
        icon: 'warning',
        title: 'Sess&atilde;o expirada',
        html: 'Sua sess&atilde;o expirou.<br>Fa&ccedil;a o login novamente para continuar.',
        focusConfirm: true,
        confirmButtonText: `<i class="ri-lock-password-line"></i>&nbsp;Fazer login`,
        customClass: {
            confirmButton: 'btn btn-label-warning waves-effect'
        }
    });
}

// Inicializa validação e evento de submit do formulário de login
function fn_LoginSetupForm() {
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

    d.setTime(d.getTime() + (exmins * 60 * 1000)); // exmins em minutos → ms
    let expires = `expires=${d.toUTCString()}`;
    let ckFull = `${cname}= ${hash};${expires};path=/`;

    document.cookie = ckFull;
}

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