/**
 * Proposta -> Editar
 **/

//#region Declare

let varSessionDataSite = 1;
const _ck = "aceca_cookie";

//#endregion

//#region CARREGAMENTO INICIAL

document.addEventListener('DOMContentLoaded', function () {
    (function () {
        console.log(`SITE - Todos os recursos terminaram o carregamento!`);

        /*
        const successCallback = (position) => {
            console.log("Latitude: ", position.coords.latitude);
            console.log("Longitude: ", position.coords.longitude);
        };

        const errorCallback = (error) => {
            console.error("Error Code: " + error.code + " - " + error.message);
        };

        // Optional configuration
        const options = {
            enableHighAccuracy: true, // Use GPS if available for better precision
            timeout: 5000,            // Wait up to 5 seconds for a response
            maximumAge: 0             // Do not use a cached position
        };

        navigator.geolocation.getCurrentPosition(successCallback, errorCallback, options);
        */

        // Update the clock immediately on load, and then every second
        var pgLogin = document.querySelector(".pg-login");

        if (pgLogin === null) {
            fn_AuthSession();
            setInterval(fn_CheckSession, 1000);
            fn_ImageProtect();
        }

        $('.btn-logout').on('click', function () {
            //console.log("cclick logout ::: ");
            fn_AuthOut();
        });

        $('.btn-voltar-home').on('click', function () {
            window.location.href = 'https://www.aceca.com.br/';
        });

    })();
});

//#endregion

//#region CLOCK DATE
function fn_UpdateClock() {
    console.log(`::`);
    const timeString = new Date().toLocaleTimeString();
    const dateString = new Date().toLocaleDateString(undefined, { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' });

    if(document.getElementById('date-time') !== null)
        document.getElementById('date-time').textContent = `${dateString} - ${timeString}`;

    //AUTH
    typeof Storage !== "undefined" ? fn_AuthSession() : window.location.href = 'https://www.aceca.com.br/';
}

//#endregion

//#region AUTH

function fn_AuthOut() {
    //console.log(`fn_AuthOut ::`);
    try {
        $.ajax({
            url: '/Auth/Logout',
            type: 'GET',
            success: function (result) {
                // 1. Limpa estado do cliente (sem redirect aqui)
                document.cookie = `${_ck}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/;`;
                sessionStorage.removeItem('aceca_sessao');
                try {
                    localStorage.removeItem('aceca_last_activity');
                    localStorage.removeItem('aceca_abs_exp');
                } catch (e) { /* ignore */ }

                // 2. Exibe Swal e só então redireciona
                Swal.fire({
                    icon: 'success',
                    title: `At&eacute; mais ${varSessionDataSite?.nome?.split(" ")[0] ?? ''}!`,
                    html: `Nos vemos em breve`,
                    focusConfirm: true,
                    confirmButtonText: `<i class="ri-check-double-line"></i>&nbsp;Ok!`,
                    customClass: {
                        confirmButton: 'btn btn-label-success waves-effect'
                    }
                }).then(() => {
                    window.location.href = '/Auth/Index';
                });
            },
            error: function (XMLHttpRequest, textStatus, errorThrown) {
                console.log(`response XMLHttpRequest :: ${XMLHttpRequest}`);
                return false;
            }
        });
    }
    catch (ex) {
        console.log(`response ex :: ${ex}`);
    }
}

let _sessionInvalidating = false;

function fn_CheckSession() {
    if (_sessionInvalidating) return;
    // Aguarda a primeira confirmação de sessão (varSessionDataSite sai de 1 após fn_SetSessionData)
    if (!varSessionDataSite || varSessionDataSite === 1) return;

    var raw = sessionStorage.getItem("aceca_sessao");
    if (!raw) { fn_SessionInvalid(); return; }

    try {
        var d = JSON.parse(raw);
        if (!d || !d.nameIdentifier) fn_SessionInvalid();
    } catch (e) {
        fn_SessionInvalid();
    }
}

function fn_SessionInvalid() {
    if (_sessionInvalidating) return;
    _sessionInvalidating = true;
    document.cookie = `${_ck}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/;`;
    sessionStorage.removeItem('aceca_sessao');
    try {
        localStorage.removeItem('aceca_last_activity');
        localStorage.removeItem('aceca_abs_exp');
    } catch (e) { /* ignore */ }
    window.location.href = '/Auth/AccessDenied';
}

function fn_AuthSession(callback) {
    if (sessionStorage?.getItem("aceca_sessao") !== null) {
        var sessionData = JSON.parse(sessionStorage.getItem("aceca_sessao"));
        if (sessionData !== null) {
            fn_SetSessionData(sessionData);
            if (callback) callback();
        } else {
            fn_RestoreSession(callback);
        }
    } else {
        fn_RestoreSession(callback);
    }
}
function fn_RestoreSession(callback) {
    $.ajax({
        url: '/Auth/GetSessionData',
        type: 'GET',
        success: function (data) {
            sessionStorage.setItem('aceca_sessao', JSON.stringify(data));
            fn_SetSessionData(data);
            if (callback) callback();
        },
        error: function () {
            fn_CleanUser();
        }
    });
}
function fn_SetSessionData(data) {
    varSessionDataSite = data;
    document.getElementById('hdSocioLogadoId').value = `${data?.nameIdentifier}`;
    document.getElementById('hdIsPerfil').value = `${data?.isPerfil}`;
    if (document.getElementById('tbAvatar')) document.getElementById('tbAvatar').textContent = `${data?.avatar}`;
    document.getElementById('tbNome').textContent = `${data?.nome}`;
    document.getElementById('tbCargo').textContent = `${data?.cargo}`;
}
function fn_CleanUser() {
    document.cookie = `${_ck}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/;`;
    sessionStorage.removeItem('aceca_sessao');
    try {
        localStorage.removeItem('aceca_last_activity');
        localStorage.removeItem('aceca_abs_exp');
    } catch (e) { /* ignore */ }
    window.location.href = '/Auth/Index';
}
function fn_CkRemove(_ck) {
    document.cookie = `${_ck}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/;`;
}
//#endregion

//#region PROTEÇÃO DE IMAGEM

function fn_ImageProtect() {

    // ── helpers ──────────────────────────────────────────────────────────────

    function _imgMeta(img) {
        return {
            codigoAceca: img?.getAttribute('data-codigo') || img?.getAttribute('data-id') || '',
            src:         img?.src || img?.getAttribute('data-src') || ''
        };
    }

    function _swalAviso() {
        Swal.fire({
            icon: 'warning',
            title: 'Ação não permitida',
            html: 'As imagens do acervo ACECA são protegidas por direitos autorais.<br><b>Cópia, download e captura de tela são proibidos.</b>',
            confirmButtonText: '<i class="ri-check-line"></i>&nbsp;Entendi',
            customClass: { confirmButton: 'btn btn-label-warning waves-effect' }
        });
    }

    function _reportar(img, acao) {
        var meta = _imgMeta(img);
        var ts   = new Date().toISOString();
        var fd   = new FormData();
        fd.append('codigoAceca', meta.codigoAceca);
        fd.append('imagemSrc',   meta.src);
        fd.append('acao',        acao);
        fd.append('timestamp',   ts);
        // silencioso — sem feedback visual
        fetch('/Auth/ReportImageAccess', { method: 'POST', body: fd }).catch(() => {});
    }

    // Cria canvas watermark e insere como overlay sobre a imagem alvo.
    // Retorna a função de remoção para limpeza posterior.
    function _watermark(img) {
        var rect  = img.getBoundingClientRect();
        var w     = rect.width  || img.naturalWidth  || 300;
        var h     = rect.height || img.naturalHeight || 300;

        var canvas = document.createElement('canvas');
        canvas.width  = w;
        canvas.height = h;
        canvas.style.cssText = [
            'position:fixed',
            `left:${rect.left + window.scrollX}px`,
            `top:${rect.top  + window.scrollY}px`,
            `width:${w}px`,
            `height:${h}px`,
            'z-index:2147483647',
            'pointer-events:none'
        ].join(';');

        var ctx = canvas.getContext('2d');
        ctx.fillStyle = 'rgba(255,255,255,0.55)';
        ctx.fillRect(0, 0, w, h);

        ctx.save();
        ctx.translate(w / 2, h / 2);
        ctx.rotate(-Math.PI / 4);
        ctx.font = `bold ${Math.max(14, Math.min(w / 12, 28))}px Arial`;
        ctx.fillStyle = 'rgba(180,0,0,0.72)';
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';
        var lines = ['ACECA', 'CÓPIA NÃO AUTORIZADA'];
        var lh = Math.max(18, Math.min(w / 10, 34));
        lines.forEach(function (line, i) {
            ctx.fillText(line, 0, (i - (lines.length - 1) / 2) * lh);
        });
        ctx.restore();

        document.body.appendChild(canvas);
        return function () { canvas.remove(); };
    }

    // ── variáveis de controle ─────────────────────────────────────────────────

    var _removeWatermark = null;
    var _lastImg         = null;   // imagem mais recente alvo de zoom

    // ── rastrear qual .cmyImg foi clicada por último ──────────────────────────

    $(document).on('click', '.cmyImg', function () {
        _lastImg = this;
    });

    // ── contextmenu (botão direito) ───────────────────────────────────────────

    $(document).on('contextmenu', '.cmyImg', function (e) {
        e.preventDefault();
        _reportar(this, 'contextmenu');
        _swalAviso();
    });

    // ── drag ─────────────────────────────────────────────────────────────────

    $(document).on('dragstart', '.cmyImg', function (e) {
        e.preventDefault();
        _reportar(this, 'dragstart');
    });

    // ── copy (Ctrl+C / Cmd+C enquanto imagem está em foco) ───────────────────

    document.addEventListener('copy', function (e) {
        var active = document.activeElement;
        if (active && active.matches && active.matches('.cmyImg')) {
            e.preventDefault();
            _reportar(active, 'copy');
            _swalAviso();
        }
    }, true);

    // ── teclas proibidas ──────────────────────────────────────────────────────

    document.addEventListener('keydown', function (e) {
        var key  = e.key  || '';
        var code = e.code || '';

        // PrintScreen
        var isPrint = (key === 'PrintScreen' || code === 'PrintScreen');
        // Ctrl+S / Ctrl+U / Ctrl+P
        var isCtrlSave = e.ctrlKey && (key === 's' || key === 'S');
        var isCtrlSrc  = e.ctrlKey && (key === 'u' || key === 'U');
        var isCtrlPrint= e.ctrlKey && (key === 'p' || key === 'P');

        if (isPrint || isCtrlSave || isCtrlSrc || isCtrlPrint) {
            e.preventDefault();
            var img   = _lastImg;
            var label = isPrint    ? 'printscreen'
                      : isCtrlSave ? 'ctrl+s'
                      : isCtrlSrc  ? 'ctrl+u'
                      :              'ctrl+p';

            if (img) _reportar(img, label);

            // watermark temporário sobre a última imagem visualizada
            if (img) {
                if (_removeWatermark) _removeWatermark();
                _removeWatermark = _watermark(img);
                setTimeout(function () {
                    if (_removeWatermark) { _removeWatermark(); _removeWatermark = null; }
                }, 4000);
            }

            _swalAviso();
        }
    }, true);

    // ── beforeprint / afterprint ──────────────────────────────────────────────

    window.addEventListener('beforeprint', function () {
        var img = _lastImg;
        if (img) _reportar(img, 'print');
        document.querySelectorAll('.cmyImg').forEach(function (el) {
            el.style.visibility = 'hidden';
        });
    });

    window.addEventListener('afterprint', function () {
        document.querySelectorAll('.cmyImg').forEach(function (el) {
            el.style.visibility = '';
        });
    });
}

//#endregion