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

import App from './App';
import React from 'react';
import { Provider } from 'react-redux';
import { store, persistor } from './store';
import { createRoot } from 'react-dom/client';
import { ToastContainer } from 'react-toastify';
import { BrowserRouter } from 'react-router';
import { registerSW } from "virtual:pwa-register";
import { PersistGate } from 'redux-persist/integration/react';
import './stylesheet.scss';

const baseUrl = document.getElementsByTagName('base')[0].getAttribute('href');
const rootElement = document.getElementById('root');
const root = createRoot(rootElement);

// Only register the service worker on origins where the backend API is
// reachable. This prevents the SW from taking over on proxy/marketing
// domains that serve the frontend but not the API. Once verified, the
// origin is remembered so the SW still works offline on return visits.
const SW_ORIGIN_KEY = 'arcadeia-sw-origin';

async function initServiceWorker() {
    const storedOrigin = localStorage.getItem(SW_ORIGIN_KEY);

    if (storedOrigin === window.location.origin) {
        // previously verified — register (works even if offline)
        registerAppSW();
        return;
    }

    try {
        const response = await fetch('/api/settings', {
            method: "GET",
            headers: {
                accept: "application/json",
            }
        });
        if (response.ok) {
            localStorage.setItem(SW_ORIGIN_KEY, window.location.origin);
            registerAppSW();
        } else {
            await unregisterExistingSW();
        }
    } catch {
        await unregisterExistingSW();
    }
}

function registerAppSW() {
    const updateSW = registerSW({
        onNeedRefresh() {
            updateSW(true); // forces page reload
        },
        onOfflineReady() {
            console.log("PWA is ready to work offline.");
        },
    });
}

async function unregisterExistingSW() {
    if ('serviceWorker' in navigator) {
        const registrations = await navigator.serviceWorker.getRegistrations();
        for (const registration of registrations) {
            await registration.unregister();
        }
    }
}

initServiceWorker();

root.render(
    <BrowserRouter basename={baseUrl}>
        <Provider store={store}>
            <PersistGate loading={null} persistor={persistor}>
                <React.StrictMode>
                    <App />
                </React.StrictMode>
            </PersistGate>
        </Provider>
        <ToastContainer stacked position="bottom-right" theme="colored" />
    </BrowserRouter>
);
