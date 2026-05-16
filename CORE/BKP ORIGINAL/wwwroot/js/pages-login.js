/**
 * page-login
 */

'use strict';

//#region Declare

let var_Nome = 'Auth',
    var_Controller = '/Auth',
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

        fn_Limpar();
    })();
});

//#endregion

function fn_Limpar() {
    document.getElementById('lEmail').value = '';
    document.getElementById('lSenha').value = '';
}

function fn_Viewpass() {
  const i = document.getElementById('lSenha');
  i.type = i.type === 'password' ? 'text' : 'password';
}

async function fn_Auth(e) {
    //console.log("fazerLogin - e :::", e);

  e.preventDefault();
  const email = document.getElementById('lEmail').value.trim().toLowerCase();
  const senha = document.getElementById('lSenha').value;
  const btn   = document.getElementById('btnEntrar');
  const err   = document.getElementById('loginErr');
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

        if (response.ok)
            user = await response.json();
  }
  catch {
  }

  if (!user) {
      Swal.fire({
          title: 'Dados Inv&aacute;lidos!!',
          icon: 'error',
          html: `<bN&atilde;o foi possível realizar o acesso!!!</b>`,
          focusConfirm: false,
          confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
          customClass: {
              confirmButton: 'btn btn-label-danger waves-effect'
          }
      }).then((result) => {
          fn_Limpar();
      });
  }

  btn.disabled = false; btn.textContent = 'Entrar';

  if (user) {
      sessionStorage.setItem('aceca_sessao', JSON.stringify(user));
      window.location.href = '/Auth/Access';
  } else {
        err.textContent = '❌ E-mail ou senha incorretos. Verifique suas credenciais.';
        err.style.display = 'block';

      console.log("fazerLogin - err :::", err);

      fn_Limpar();
  }
}

