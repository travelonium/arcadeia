// @vitest-environment jsdom

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

import React from 'react';
import { vi } from 'vitest';
import { act } from 'react';
import { createRoot } from 'react-dom/client';
import { Provider } from 'react-redux';
import { MemoryRouter } from 'react-router';
import { store } from './store';
import App from './App';

vi.mock('@microsoft/signalr', () => ({
    HubConnectionBuilder: vi.fn().mockImplementation(() => ({
        withUrl: vi.fn().mockReturnThis(),
        withHubProtocol: vi.fn().mockReturnThis(),
        configureLogging: vi.fn().mockReturnThis(),
        withAutomaticReconnect: vi.fn().mockReturnThis(),
        build: vi.fn().mockReturnValue({
            start: vi.fn().mockResolvedValue(undefined),
            on: vi.fn(),
            off: vi.fn(),
            stop: vi.fn().mockResolvedValue(undefined),
        }),
    })),
    HttpTransportType: { WebSockets: 1 },
    LogLevel: { Information: 1 },
}));

vi.mock('@microsoft/signalr-protocol-msgpack', () => ({
    MessagePackHubProtocol: vi.fn().mockImplementation(() => ({})),
}));

// jsdom does not implement matchMedia
Object.defineProperty(window, 'matchMedia', {
    writable: true,
    value: (query) => ({
        matches: false,
        media: query,
        onchange: null,
        addEventListener: () => {},
        removeEventListener: () => {},
        dispatchEvent: () => {},
    }),
});

it('renders without crashing', async () => {
    const div = document.createElement('div');
    document.body.appendChild(div);
    const root = createRoot(div);
    await act(async () => {
        root.render(
            <Provider store={store}>
                <MemoryRouter>
                    <App />
                </MemoryRouter>
            </Provider>
        );
    });
    root.unmount();
    document.body.removeChild(div);
});
