/**
 * Account Settings - Segurança
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
        console.log(`AUTH USER SEGURANCA - Todos os recursos terminaram o carregamento!`);

        fn_WireTrocaSenha();
        fn_PopularGridAcessos();
    })();
});

//#endregion

//#region TROCA DE SENHA

// Mesmas 3 regras exibidas ao usuário em tempo real (fn_ValidarRequisitosSenha) e
// checadas antes de enviar o formulário - uma só fonte de verdade pros requisitos.
function fn_RequisitosSenha(senha) {
    return {
        minLength: senha.length >= 8,
        uppercase: /[A-Z]/.test(senha),
        especial: /[0-9\W]/.test(senha)
    };
}

function fn_ValidarRequisitosSenha(senha) {
    const requisitos = fn_RequisitosSenha(senha);

    fn_MarcarRequisito('#req_MinLength', requisitos.minLength);
    fn_MarcarRequisito('#req_Uppercase', requisitos.uppercase);
    fn_MarcarRequisito('#req_Especial', requisitos.especial);

    return requisitos;
}

function fn_MarcarRequisito(seletor, atendido) {
    const item = document.querySelector(seletor);

    if (!item) return;

    const icon = item.querySelector('i');

    item.classList.toggle('text-muted', !atendido);
    item.classList.toggle('text-success', atendido);

    icon.classList.toggle('ri-circle-line', !atendido);
    icon.classList.toggle('ri-checkbox-circle-fill', atendido);
}

function fn_WireTrocaSenha() {
    const form = document.querySelector('#formTrocaSenha');

    if (!form) return;

    form.addEventListener('submit', function (e) {
        e.preventDefault();

        const currentPassword = document.getElementById('currentPassword').value,
            newPassword = document.getElementById('newPassword').value,
            confirmPassword = document.getElementById('confirmPassword').value;

        if (!currentPassword || !newPassword || !confirmPassword) {
            fn_SwalErro(`${msg}: Senha Atual, Nova Senha e Confirmação`);
            return;
        }

        const requisitos = fn_RequisitosSenha(newPassword);

        if (!requisitos.minLength || !requisitos.uppercase || !requisitos.especial) {
            fn_SwalErro('A nova senha n&atilde;o atende aos requisitos listados abaixo do campo');
            return;
        }

        if (newPassword !== confirmPassword) {
            fn_SwalErro('A nova senha e a confirmação não coincidem');
            return;
        }

        $.busyLoadFull("show");

        $.ajax({
            url: `${var_Controller}/UpdatePassword`,
            type: 'POST',
            data: { currentPassword, newPassword, confirmPassword },
            success: function (response) {
                $.busyLoadFull("hide");

                if (!response?.bResult) {
                    fn_SwalErro(response?.message || 'Falha ao atualizar a senha');
                    return;
                }

                form.reset();

                Swal.fire({
                    title: 'Senha Atualizada!',
                    icon: 'success',
                    text: 'Sua senha foi alterada com sucesso.',
                    customClass: { confirmButton: 'btn btn-success waves-effect waves-light' }
                });
            },
            error: function (xhr, status, error) {
                $.busyLoadFull("hide");
                console.error("fn_WireTrocaSenha error: " + error);
            }
        });
    });
}

function fn_SwalErro(mensagem) {
    Swal.fire({
        title: 'Erro!!',
        icon: 'error',
        html: `<b>${mensagem}</b>`,
        confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
        customClass: { confirmButton: 'btn btn-label-danger waves-effect' }
    });
}

//#endregion

//#region ULTIMOS ACESSOS

// fnhelper_IconeAcesso, fnhelper_FormatarDataAcesso, fnhelper_TextoBrowserOs e fnhelper_CarregarUltimosAcessos
// (fetch) são comuns a esta grid e à timeline de Atividade em ProfileUser.cshtml -
// centralizados em helper-ui-common.js, carregado em toda página não-front.
function fn_PopularGridAcessos() {
    const tbody = document.querySelector('#tbl_UltimosAcessos tbody');

    if (!tbody) return;

    fnhelper_CarregarUltimosAcessos(function (acessos) {
        if (!acessos.length) {
            tbody.innerHTML = '<tr><td colspan="4" class="text-center text-muted">Nenhum acesso registrado</td></tr>';
            return;
        }

        tbody.innerHTML = acessos.map(function (acesso) {
            const { icone, cor } = fnhelper_IconeAcesso(acesso.os, acesso.device);
            const local = [acesso.cidade, acesso.estado].filter(Boolean).join(' - ') || '-';

            return `<tr>
                <td class="text-truncate text-heading"><i class="${icone} ri-20px ${cor} me-3"></i>${fnhelper_TextoBrowserOs(acesso.browser, acesso.os)}</td>
                <td class="text-truncate">${fnhelper_TextoDispositivo(acesso.device)}</td>
                <td class="text-truncate">${local}</td>
                <td class="text-truncate">${fnhelper_FormatarDataAcesso(acesso.ultimoLogin)}</td>
            </tr>`;
        }).join('');
    });
}

//#endregion
