'use strict';

document.addEventListener('DOMContentLoaded', function () {

    const frm = document.getElementById('frmVerificarCodigo');
    if (!frm) return;

    const err = document.getElementById('verifyErr');
    const ok = document.getElementById('verifyOk');
    const btn = document.getElementById('btnConfirmarCodigo');
    const btnReenviar = document.getElementById('btnReenviarCodigo');
    const inputEmail = document.getElementById('vEmail');

    if (window.__cadastroTesteEmail && !inputEmail.value)
        inputEmail.value = window.__cadastroTesteEmail;

    function setLoading(isLoading) {
        btn.disabled = isLoading;
        btn.querySelector('.btn-text').style.display = isLoading ? 'none' : 'inline';
        btn.querySelector('.btn-spinner').style.display = isLoading ? 'inline-flex' : 'none';
    }

    frm.addEventListener('submit', async function (e) {
        e.preventDefault();
        err.style.display = 'none';
        ok.style.display = 'none';
        setLoading(true);

        try {
            const response = await fetch('/Auth/VerificarCodigoCadastroTeste', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    email: inputEmail.value.trim(),
                    codigo: document.getElementById('vCodigo').value.trim(),
                }),
            });

            const result = await response.json();

            if (response.ok && result.bResult) {
                ok.textContent = '✅ E-mail confirmado! Entrando…';
                ok.style.display = 'block';
                window.location.href = result.redirectUrl || '/';
                return;
            }

            err.textContent = `❌ ${result?.message ?? 'Código inválido ou expirado.'}`;
            err.style.display = 'block';
            setLoading(false);
        } catch (ex) {
            console.error('VerificarCodigoCadastroTeste:', ex);
            err.textContent = '❌ Não foi possível verificar o código. Tente novamente.';
            err.style.display = 'block';
            setLoading(false);
        }
    });

    btnReenviar?.addEventListener('click', async function () {
        err.style.display = 'none';
        ok.style.display = 'none';

        try {
            const response = await fetch('/Auth/ReenviarCadastroTeste', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email: inputEmail.value.trim() }),
            });

            const result = await response.json();
            ok.textContent = `✅ ${result?.message ?? 'Se o cadastro existir, um novo e-mail foi enviado.'}`;
            ok.style.display = 'block';
        } catch (ex) {
            console.error('ReenviarCadastroTeste:', ex);
        }
    });
});
