self.importScripts('./service-worker-assets.js');

self.addEventListener('install', event => event.waitUntil(onInstall()));
self.addEventListener('activate', event => event.waitUntil(onActivate()));
self.addEventListener('fetch', event => {
    event.respondWith(onFetch(event));
});

const staticCachePrefix = 'capture-static-';
const runtimeCache = 'capture-runtime';
const staticCacheName = `${staticCachePrefix}${self.assetsManifest.version}`;
const offlineAssetsInclude = [/\.dll$/, /\.pdb$/, /\.wasm$/, /\.html$/, /\.js$/, /\.json$/, /\.css$/, /\.woff2?$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/];
const offlineAssetsExclude = [/^service-worker\.js$/];

async function onInstall() {
    const assetsRequests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
        .map(asset => new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' }));

    const cache = await caches.open(staticCacheName);
    await cache.addAll(assetsRequests);
}

async function onActivate() {
    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys
        .filter(key => key.startsWith(staticCachePrefix) && key !== staticCacheName)
        .map(key => caches.delete(key)));
}

async function onFetch(event) {
    const { request } = event;

    if (request.method !== 'GET') {
        return fetch(request);
    }

    const url = new URL(request.url);
    const isApiRequest = url.pathname.startsWith('/api/');

    if (isApiRequest) {
        return staleWhileRevalidate(request);
    }

    if (request.mode === 'navigate') {
        const cache = await caches.open(staticCacheName);
        const cachedIndex = await cache.match('index.html');
        return cachedIndex ?? fetch(request);
    }

    const cache = await caches.open(staticCacheName);
    const cachedResponse = await cache.match(request);
    if (cachedResponse) {
        return cachedResponse;
    }

    const networkResponse = await fetch(request);
    if (networkResponse.ok) {
        await cache.put(request, networkResponse.clone());
    }

    return networkResponse;
}

async function staleWhileRevalidate(request) {
    const cache = await caches.open(runtimeCache);
    const cachedResponse = await cache.match(request);

    const networkFetch = fetch(request)
        .then(response => {
            if (response.ok) {
                cache.put(request, response.clone());
            }
            return response;
        })
        .catch(() => cachedResponse);

    return cachedResponse ?? networkFetch;
}
