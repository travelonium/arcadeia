/*
 *  Copyright © 2024 Travelonium AB
 *
 *  This file is part of Arcadeia.
 *
 *  Arcadeia is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Affero General Public License as published
 *  by the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Arcadeia is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU Affero General Public License for more details.
 *
 *  You should have received a copy of the GNU Affero General Public License
 *  along with Arcadeia. If not, see <https://www.gnu.org/licenses/>.
 *
 */

import { precacheAndRoute, cleanupOutdatedCaches } from 'workbox-precaching';

async function isApiReachable() {
    try {
        const response = await fetch('/api/settings', {
            method: 'GET',
            headers: { accept: 'application/json' },
        });
        return response.ok;
    } catch {
        return false;
    }
}

self.addEventListener('install', () => {
    self.skipWaiting();
});

self.addEventListener('activate', (event) => {
    event.waitUntil(
        (async () => {
            if (!(await isApiReachable())) {
                // API not reachable — this SW is on the wrong origin.
                // Clear all caches and self-unregister.
                const names = await caches.keys();
                await Promise.all(names.map(n => caches.delete(n)));
                await self.registration.unregister();
                // Reload controlled pages so they fetch from the network
                const clients = await self.clients.matchAll({ type: 'window' });
                clients.forEach(c => c.navigate(c.url));
                return;
            }
            // API reachable — claim all clients
            await self.clients.claim();
        })()
    );
});

cleanupOutdatedCaches();
precacheAndRoute(self.__WB_MANIFEST);
