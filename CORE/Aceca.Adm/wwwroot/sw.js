// Service Worker do PWA ACECA.
//
// Estratégia deliberadamente conservadora por causa do modelo de auth por
// cookie + sessão com expiração (session-guard.js) e do header global
// "no-cache, no-store" que o próprio Program.cs já aplica em toda página
// renderizada no servidor: NUNCA cacheamos navegação (HTML) nem chamadas de
// dados (grids DataTables, /Auth/*, etc.) — isso evitaria servir uma tela
// autenticada de outro usuário ou dados desatualizados. Só cacheamos
// assets estáticos (css/js/img/fonts/vendor) que já são versionados via
// asp-append-version, e mantemos um fallback offline só para navegação.

const CACHE_VERSION = 'aceca-static-v1';
const OFFLINE_URL = '/offline.html';

const PRECACHE_URLS = [
    OFFLINE_URL,
    '/img/pwa/icon-192.png',
    '/img/pwa/icon-512.png'
];

const STATIC_PATH_PREFIXES = ['/css/', '/js/', '/vendor/', '/img/', '/fonts/'];

self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_VERSION).then(cache => cache.addAll(PRECACHE_URLS))
    );
    self.skipWaiting();
});

self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys().then(keys =>
            Promise.all(keys.filter(key => key !== CACHE_VERSION).map(key => caches.delete(key)))
        )
    );
    self.clients.claim();
});

function isStaticAsset(url) {
    return STATIC_PATH_PREFIXES.some(prefix => url.pathname.startsWith(prefix));
}

self.addEventListener('fetch', event => {
    const { request } = event;

    // Nunca interceptar métodos que não sejam GET (nada de cachear POST de
    // login, formulários, uploads, etc.)
    if (request.method !== 'GET') return;

    const url = new URL(request.url);
    if (url.origin !== self.location.origin) return;

    // Navegação (HTML de página) - sempre rede primeiro; cai no offline.html
    // só se a rede falhar de verdade (sem internet). Nunca serve HTML do cache.
    if (request.mode === 'navigate') {
        event.respondWith(
            fetch(request).catch(() => caches.match(OFFLINE_URL))
        );
        return;
    }

    // Assets estáticos versionados - cache-first, com atualização em segundo
    // plano (stale-while-revalidate) pra pegar deploys novos sem esperar
    // expirar os 7 dias de Cache-Control do servidor.
    if (isStaticAsset(url)) {
        event.respondWith(
            caches.open(CACHE_VERSION).then(cache =>
                cache.match(request).then(cached => {
                    const network = fetch(request)
                        .then(response => {
                            if (response.ok) cache.put(request, response.clone());
                            return response;
                        })
                        .catch(() => cached);
                    return cached || network;
                })
            )
        );
        return;
    }

    // Qualquer outra chamada (dados/API/DataTables/ajax) - direto pra rede,
    // sem passar pelo cache.
});
