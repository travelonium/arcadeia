// This service worker immediately unregisters itself and clears any caches
// left behind by a previously installed service worker on this origin.
self.addEventListener('install', () => self.skipWaiting());
self.addEventListener('activate', async () => {
    const names = await caches.keys();
    await Promise.all(names.map(n => caches.delete(n)));
    await self.registration.unregister();
    const clients = await self.clients.matchAll({ type: 'window' });
    clients.forEach(c => c.navigate(c.url));
});
