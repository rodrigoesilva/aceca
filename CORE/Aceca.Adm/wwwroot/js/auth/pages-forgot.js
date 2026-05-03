/**
 * pages-forgot.js  –  ACECA | Esqueci minha senha
 */

'use strict';

const VAR_CONTROLLER_FORGOT = '/Auth';
let forgotFormValid;

//#region CARREGAMENTO INICIAL

document.addEventListener('DOMContentLoaded', function () {
    (function () {
        console.log(`AUTH forgot - Todos os recursos terminaram o carregamento!`);

        const form = document.querySelector('#frmForgot');
        if (!form) return;

        // Validação do formulário
        forgotFormValid = FormValidation.formValidation(form, {
            fields: {
                fEmail: {
                    validators: {
                        notEmpty: { message: 'Digite seu e-mail' },
                        emailAddress: { message: 'Insira um endereço de e-mail válido' }
                    }
                }
            },
            plugins: {
                trigger: new FormValidation.plugins.Trigger(),
                bootstrap5: new FormValidation.plugins.Bootstrap5({
                    rowSelector: '.fg',
                    eleInvalidClass: '',
                    eleValidClass: ''
                }),
                autoFocus: new FormValidation.plugins.AutoFocus()
            }
        });

        const btn = document.getElementById('btnForgot');
        btn?.addEventListener('click', function (e) {
            e.preventDefault();
            forgotFormValid?.validate().then(status => {
                if (status === 'Valid') fn_ForgotSubmit();
            });
        });

        form.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') { e.preventDefault(); btn?.click(); }
        });
    })();
});

//#endregion

async function fn_ForgotSubmit() {
    const email = document.getElementById('fEmail')?.value.trim().toLowerCase();
    const btn   = document.getElementById('btnForgot');
    const err   = document.getElementById('forgotErr');
    const ok    = document.getElementById('forgotOk');

    err.style.display = 'none';
    ok.style.display = 'none';

    fn_BtnLoadingForgot(btn, true);

    try {
        const response = await fetch(`${VAR_CONTROLLER_FORGOT}/ForgotPassword`, {
            method : 'POST',
            headers: { 'Content-Type': 'application/json' },
            body   : JSON.stringify({ email }),
        });

        const data = await response.json();

        console.log(`AUTH forgot - data :: `, data);

        fn_BtnLoadingForgot(btn, false);

        if (response.ok && data.bResult) {
            Swal.fire({
                icon : 'success',
                title: 'E-mail enviado!',
                html : `Verifique sua caixa de entrada.<br><br>
                        <small>Se o e-mail estiver cadastrado, você receberá as instruções.</small>`,
                focusConfirm: true,
                confirmButtonText: '<i class="ri-check-double-line"></i>&nbsp;Ok!',
                customClass: { confirmButton: 'btn btn-label-success waves-effect' }
            }).then(() => {
                window.location.href = '/Auth/Index';
            });
        } else {
            err.innerHTML    = `❌ ${data?.message ?? 'Não foi possível processar a solicitação.'}`;
            err.style.display = 'block';

            Swal.fire({
                title: 'Ops!!',
                icon : 'error',
                html : `<b>${data?.message ?? 'Não foi possível processar a solicitação.'}</b>`,
                focusConfirm: false,
                confirmButtonText: '<i class="ri-check-double-line"></i>&nbsp;Ok!',
                customClass: { confirmButton: 'btn btn-label-danger waves-effect' }
            });
        }
    } catch (ex) {
        fn_BtnLoadingForgot(btn, false);
        console.error('fn_ForgotSubmit ex:', ex);
        Swal.fire({
            title: 'Ops!!',
            icon : 'error',
            html : '<b>Não foi possível processar a solicitação.</b>',
            focusConfirm: false,
            confirmButtonText: '<i class="ri-check-double-line"></i>&nbsp;Ok!',
            customClass: { confirmButton: 'btn btn-label-danger waves-effect' }
        });
    }
}

function fn_BtnLoadingForgot(btn, loading) {
    if (!btn) return;

    console.log(`AUTH forgot - btn :: `, btn);

    const textEl    = btn.querySelector('.btn-text');
    const spinnerEl = btn.querySelector('.btn-spinner');
    btn.disabled = loading;

    console.log(`AUTH forgot - textEl :: `, textEl);
    console.log(`AUTH forgot - spinnerEl :: `, spinnerEl);

    if (textEl) {
        textEl.style.display = loading ? 'none' : '';
        btn.textContent = 'Enviando…';
    }

    if (spinnerEl) spinnerEl.style.display = loading ? 'inline-block' : 'none';
}
