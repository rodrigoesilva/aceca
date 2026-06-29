/**
 * session-guard.js
 *
 * Gerencia dois cenários distintos de redirecionamento:
 *
 *   401 Unauthorized  — sessão inexistente ou expirada (2h ocioso / 24h absoluto)
 *                       → redireciona para /Auth/SessionExpired
 *
 *   403 Forbidden     — usuário autenticado mas sem permissão (role insuficiente)
 *                       → redireciona para /Auth/AccessDenied
 *
 * Os timers de ociosidade e teto absoluto verificam apenas o estado LOCAL da sessão
 * (timestamps gravados no login). Eles nunca sobrepõem um 403 do servidor.
 *
 * Os prazos vêm do servidor via window.__aceca_session (injetado no _CommonMasterLayout
 * apenas para usuários autenticados).
 */
(function () {
    "use strict";

    var cfg = window.__aceca_session || {};

    var idleMs         = (cfg.idleMinutes || 120) * 60 * 1000;
    var absoluteExpiry = cfg.absoluteExpiry ? new Date(cfg.absoluteExpiry).getTime() : 0;

    var SESSION_EXPIRED_URL = "/Auth/SessionExpired";
    var ACCESS_DENIED_URL   = "/Auth/AccessDenied";
    var ACTIVITY_KEY        = "aceca_last_activity"; // compartilhado entre abas
    var CHECK_INTERVAL      = 15000;                 // verifica a cada 15s
    var WRITE_THROTTLE      = 5000;                  // grava atividade no máx. a cada 5s

    var redirecting = false; // flag única: evita redirecionamentos concorrentes
    var lastWrite   = 0;

    function now() { return new Date().getTime(); }

    // ──────────────────────────────────────────────────────────────────────────
    // Redirecionamentos — cada cenário tem sua própria função e destino

    function redirectToSessionExpired() {
        if (redirecting) return;
        redirecting = true;
        try {
            sessionStorage.removeItem("aceca_sessao");
            document.cookie = "aceca_cookie=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/;";
        } catch (e) { /* ignore */ }
        window.location.href = SESSION_EXPIRED_URL;
    }

    function redirectToAccessDenied() {
        if (redirecting) return;
        redirecting = true;
        window.location.href = ACCESS_DENIED_URL;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Atividade do usuário (sincronização entre abas)

    function markActivity() {
        var t = now();
        if (t - lastWrite < WRITE_THROTTLE) return;
        lastWrite = t;
        try { localStorage.setItem(ACTIVITY_KEY, String(t)); } catch (e) { /* ignore */ }
    }

    function lastActivity() {
        try { return parseInt(localStorage.getItem(ACTIVITY_KEY), 10) || 0; } catch (e) { return 0; }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Timers — apenas verificam expiração LOCAL da sessão (401), nunca 403

    function checkSessionExpiry() {
        if (redirecting) return;

        var t = now();

        // Teto absoluto de 24h a partir do login
        if (absoluteExpiry && t >= absoluteExpiry) {
            redirectToSessionExpired();
            return;
        }

        // Ociosidade de 2h (sincronizada entre abas via localStorage)
        if (idleMs && (t - lastActivity()) >= idleMs) {
            redirectToSessionExpired();
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Interceptação de respostas HTTP

    // jQuery AJAX
    if (window.jQuery) {
        jQuery(document).ajaxComplete(function () { markActivity(); });

        jQuery(document).ajaxError(function (event, xhr) {
            if (!xhr) return;

            if (xhr.status === 401) {
                // Sessão expirada / não autenticado
                redirectToSessionExpired();
            } else if (xhr.status === 403) {
                // Autenticado, porém sem permissão (role insuficiente)
                redirectToAccessDenied();
            }
        });
    }

    // fetch API
    if (window.fetch) {
        var _fetch = window.fetch;
        window.fetch = function () {
            return _fetch.apply(this, arguments).then(function (resp) {
                if (!resp) return resp;

                if (resp.status === 401) {
                    redirectToSessionExpired();
                } else if (resp.status === 403) {
                    redirectToAccessDenied();
                }

                return resp;
            });
        };
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Inicialização

    ["mousemove", "mousedown", "keydown", "scroll", "touchstart", "click"].forEach(function (ev) {
        window.addEventListener(ev, markActivity, { passive: true });
    });

    // Persiste o teto absoluto no localStorage para que pages-auth.js possa
    // verificar expiração mesmo sem ter acesso ao window.__aceca_session
    if (absoluteExpiry) {
        try { localStorage.setItem('aceca_abs_exp', String(absoluteExpiry)); } catch (e) { /* ignore */ }
    }

    markActivity();                            // carregamento conta como atividade
    setInterval(checkSessionExpiry, CHECK_INTERVAL);
})();
