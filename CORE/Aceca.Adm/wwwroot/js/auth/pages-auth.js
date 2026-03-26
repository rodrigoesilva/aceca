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
      //console.log(`LIST ${var_Controller} - Todos os recursos terminaram o carregamento!`);

      fn_Limpar();

      // Form validation
      const formAuthentication = document.querySelector('#frmLogin');

      fn_FormValidator(formAuthentication);

      submitButton.addEventListener('click', function (e) {
          // Prevent default button action
          e.preventDefault();

          // Validate form before submit
          if (formValid) {
              formValid.validate().then(function (status) {
                  //console.log('validated!');

                  if (status == 'Valid') {
                      fn_Auth(e);                      
                  }
              });
          }
      });

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

function fn_FormValidator(formAuthentication) {
    //console.log("fn_FormValidator :::", formAuthentication);

    // Form validation for Add new record
    if (formAuthentication) {

        //console.log("formAuthentication :::", formAuthentication);

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

                //defaultSubmit: new FormValidation.plugins.DefaultSubmit(),
                //submitButton: new FormValidation.plugins.SubmitButton(),
                //submitButton: new FormValidation.plugins.SubmitButton(),
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

async function fn_Auth(e) {
    //console.log("fazerLogin - e :::", e);

    // Show loading indication
    submitButton.setAttribute('data-kt-indicator', 'on');

    // Disable button to avoid multiple click
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

                setTimeout(function () {

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

                        sessionStorage.setItem('aceca_sessao', JSON.stringify(user));
                        window.location.href = '/Auth/Access';
                    });
                }, 500);

                // Enable button
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

                //console.log("fazerLogin - err :::", err);

                fn_Limpar();
            }
        }
            
    }
    catch (ex){

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

/*
function fn() {
    const rmCheck = document.getElementById("rememberMe"),
        emailInput = document.getElementById("email");

    if (localStorage.checkbox && localStorage.checkbox !== "") {
        rmCheck.setAttribute("checked", "checked");
        emailInput.value = localStorage.username;
    } else {
        rmCheck.removeAttribute("checked");
        emailInput.value = "";
    }
}
function lsLembrarMe() {
    if (rmCheck.checked && emailInput.value !== "") {
        localStorage.username = emailInput.value;
        localStorage.checkbox = rmCheck.value;
    } else {
        localStorage.username = "";
        localStorage.checkbox = "";
    }
}

*/