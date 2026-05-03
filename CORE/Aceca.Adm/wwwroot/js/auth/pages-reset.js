/**
 * pages-reset.js  –  ACECA | Redefinir senha
 *
 * Regras de senha:
 *  – Mínimo 8 caracteres
 *  – Pelo menos 1 número
 *  – Nova senha e confirmação devem ser iguais
 */

'use strict';

const VAR_CONTROLLER_RESET = '/Auth';
let resetFormValid;

//#region CARREGAMENTO INICIAL

document.addEventListener('DOMContentLoaded', function () {
    (function () {
        console.log(`AUTH Reset - Todos os recursos terminaram o carregamento!`);

        // Pré-preenche o campo de e-mail com o valor vindo da URL (injetado pelo Razor)
        const ctx = window._resetCtx ?? {};
        const emailInput = document.getElementById('rEmail');
        if (emailInput && ctx.email) {
            emailInput.value = decodeURIComponent(ctx.email);
        }

        const form = document.querySelector('#frmReset');
        if (!form) return;

        // ── Toggle mostrar/ocultar senha
        /*
        document.querySelectorAll('.eye-btn').forEach(btn => {
            btn.addEventListener('click', function () {
                const targetId = this.dataset.target;
                const input    = document.getElementById(targetId);
                const icon = this.querySelector('i');

                console.log(`AUTH Reset - targetId ::: `, targetId);
                //console.log(`AUTH Reset - input ::: `, input);
                //console.log(`AUTH Reset - icon ::: `, icon);

                if (!input) return;

                console.log(`AUTH Reset - input ::: `, input);
                console.log(`AUTH Reset - type ::: `, input.type);

                console.log(`AUTH Reset - icon ::: `, icon?.classList);

                if (input.type === 'password') {
                    input.type = 'text';
                    icon?.classList.replace('ri-eye-off-line', 'ri-eye-line');
                } else {
                    input.type = 'password';
                    icon?.classList.replace('ri-eye-line', 'ri-eye-off-line');
                }
            });
        });
        */

        // ── Indicador de força da senha
        document.getElementById('rSenha')?.addEventListener('input', function () {
            fn_PwdStrength(this.value);
        });

        // ── Validação do formulário
        resetFormValid = FormValidation.formValidation(form, {
            fields: {
                rEmail: {
                    validators: {
                        notEmpty    : { message: 'Digite seu e-mail' },
                        emailAddress: { message: 'Insira um endereço de e-mail válido' }
                    }
                },
                rSenha: {
                    validators: {
                        notEmpty    : { message: 'Digite a nova senha' },
                        stringLength: { min: 8, message: 'A senha deve ter no mínimo 8 caracteres' },
                        callback    : {
                            message : 'A senha deve conter pelo menos 1 número',
                            callback: function (input) {
                                return /\d/.test(input.value);
                            }
                        }
                    }
                },
                rConfirmSenha: {
                    validators: {
                        notEmpty : { message: 'Confirme a nova senha' },
                        identical: {
                            compare : function () {
                                return document.getElementById('rSenha')?.value;
                            },
                            message: 'As senhas não coincidem'
                        }
                    }
                }
            },
            plugins: {
                trigger   : new FormValidation.plugins.Trigger(),
                bootstrap5: new FormValidation.plugins.Bootstrap5({
                    rowSelector   : '.fg',
                    eleInvalidClass: '',
                    eleValidClass  : ''
                }),
                autoFocus : new FormValidation.plugins.AutoFocus()
            }
        });

        // Revalida confirmação quando a senha muda
        document.getElementById('rSenha')?.addEventListener('input', function () {
            resetFormValid?.revalidateField('rConfirmSenha');
        });

        const btn = document.getElementById('btnReset');
        btn?.addEventListener('click', function (e) {
            e.preventDefault();
            resetFormValid?.validate().then(status => {
                if (status === 'Valid') fn_ResetSubmit();
            });
        });

        form.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') { e.preventDefault(); btn?.click(); }
        });

    })();
});

//#endregion


// ──────────────────────────────────────────────
// SUBMIT RESET
// ──────────────────────────────────────────────

async function fn_ResetSubmit() {
    const ctx   = window._resetCtx ?? {};
    const email = document.getElementById('rEmail')?.value.trim().toLowerCase();
    const senha = document.getElementById('rSenha')?.value;
    const confirmSenha = document.getElementById('rConfirmSenha')?.value;
    const token = decodeURIComponent(ctx.token ?? '');

    const btn = document.getElementById('btnReset');
    const err = document.getElementById('resetErr');

    err.style.display = 'none';
    fn_BtnLoadingReset(btn, true);

    try {
        const response = await fetch(`${VAR_CONTROLLER_RESET}/ResetPassword`, {
            method : 'POST',
            headers: { 'Content-Type': 'application/json' },
            body   : JSON.stringify({ email, token, senha, confirmSenha }),
        });

        const data = await response.json();
        fn_BtnLoadingReset(btn, false);

        if (response.ok && data.bResult) {
            Swal.fire({
                icon : 'success',
                title: 'Senha atualizada!',
                html : 'Sua senha foi redefinida com sucesso.<br><br>Você será redirecionado para o login.',
                focusConfirm: true,
                confirmButtonText: '<i class="ri-check-double-line"></i>&nbsp;Ok!',
                customClass: { confirmButton: 'btn btn-label-success waves-effect' }
            }).then(() => {
                window.location.href = '/Auth/Index';
            });
        } else {
            const msg = data?.message ?? 'Não foi possível redefinir a senha.';
            err.innerHTML    = `❌ ${msg}`;
            err.style.display = 'block';

            Swal.fire({
                title: 'Ops!!',
                icon : 'error',
                html : `<b>${msg}</b>`,
                focusConfirm: false,
                confirmButtonText: '<i class="ri-check-double-line"></i>&nbsp;Ok!',
                customClass: { confirmButton: 'btn btn-label-danger waves-effect' }
            });
        }
    } catch (ex) {
        fn_BtnLoadingReset(btn, false);
        console.error('fn_ResetSubmit ex:', ex);
        Swal.fire({
            title: 'Ops!!',
            icon : 'error',
            html : '<b>Não foi possível redefinir a senha.</b>',
            focusConfirm: false,
            confirmButtonText: '<i class="ri-check-double-line"></i>&nbsp;Ok!',
            customClass: { confirmButton: 'btn btn-label-danger waves-effect' }
        });
    }
}

// ──────────────────────────────────────────────
// INDICADOR DE FORÇA DA SENHA
// ──────────────────────────────────────────────

function fn_PwdStrength(val) {
    const wrap  = document.getElementById('pwdStrengthWrap');
    const fill  = document.getElementById('pwdStrengthFill');
    const label = document.getElementById('pwdStrengthLabel');
    if (!wrap || !fill || !label) return;

    if (!val) { wrap.style.display = 'none'; return; }
    wrap.style.display = 'block';

    let score = 0;
    if (val.length >= 8)  score++;
    if (/[A-Z]/.test(val)) score++;
    if (/\d/.test(val))    score++;
    if (/[^A-Za-z0-9]/.test(val)) score++;

    const levels = [
        { pct: '25%',  color: '#ff4c51', text: 'Muito fraca'  },
        { pct: '50%',  color: '#ff9f43', text: 'Fraca'        },
        { pct: '75%',  color: '#00cfe8', text: 'Boa'          },
        { pct: '100%', color: '#28c76f', text: 'Forte'        },
    ];

    const lv = levels[score - 1] ?? levels[0];
    fill.style.width      = lv.pct;
    fill.style.background = lv.color;
    label.textContent     = lv.text;
    label.style.color     = lv.color;
}

// ──────────────────────────────────────────────
// HELPERS – UI
// ──────────────────────────────────────────────

function fn_BtnLoadingReset(btn, loading) {
    if (!btn) return;
    const textEl    = btn.querySelector('.btn-text');
    const spinnerEl = btn.querySelector('.btn-spinner');
    btn.disabled = loading;
    if (textEl)    textEl.style.display    = loading ? 'none'         : '';
    if (spinnerEl) spinnerEl.style.display = loading ? 'inline-block' : 'none';
}
