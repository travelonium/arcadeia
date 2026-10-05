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

import { act, createRef } from 'react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { createRoot } from 'react-dom/client';
import { Provider } from 'react-redux';
import { MemoryRouter } from 'react-router';
import { configureStore } from '@reduxjs/toolkit';
import axios from 'axios';
import uiReducer, { pauseUploads } from '../features/ui/slice';
import UploadZone from './UploadZone';

vi.mock('react-toastify', () => ({
    toast: { info: vi.fn(() => 'toast-id'), update: vi.fn(), isActive: vi.fn(() => false), dismiss: vi.fn() },
}));

vi.mock('axios', () => ({ default: { post: vi.fn(), get: vi.fn() } }));

// the uploads are held open until the test finishes them
let requests;

let root;
let container;
let store;
let zone;

async function mount() {
    zone = createRef();
    await act(async () => {
        root.render(
            <Provider store={store}>
                <MemoryRouter initialEntries={['/photos/']}>
                    <UploadZone ref={zone} />
                </MemoryRouter>
            </Provider>
        );
    });
    // let the mount pick up the session start and the batch
    await act(async () => vi.advanceTimersByTimeAsync(200));
}

async function unmount() {
    await act(async () => root.render(<></>));
}

async function wait() {
    await act(async () => vi.advanceTimersByTimeAsync(500));
}

function count(state) {
    return store.getState().ui.uploads.items.filter(item => item.state === state).length;
}

beforeEach(() => {
    vi.useFakeTimers();
    requests = [];
    vi.mocked(axios.post).mockImplementation(() => new Promise((resolve) => requests.push(resolve)));
    document.cookie = `SessionStarted=${encodeURIComponent(new Date(Date.now() - 60000).toISOString())}`;
    store = configureStore({
        reducer: { ui: uiReducer },
        middleware: (getDefaultMiddleware) => getDefaultMiddleware({ serializableCheck: false }),
    });
    container = document.createElement('div');
    document.body.appendChild(container);
    root = createRoot(container);
});

afterEach(() => {
    act(() => root.unmount());
    container.remove();
    vi.useRealTimers();
});

async function queueAndPause() {
    await mount();
    // browsers give files that weren't picked from a folder an empty relative path, jsdom gives none
    const files = Array.from({ length: 6 }, (_, i) => Object.assign(new File(['x'], `clip${i}.mp4`), { webkitRelativePath: '' }));
    await act(async () => zone.current.upload(files));
    await wait();
    expect(count('active')).toBe(4);
    expect(count('queued')).toBe(2);
    await act(async () => store.dispatch(pauseUploads(true)));
    // the active uploads carry on and finish while paused
    await act(async () => requests.splice(0).forEach(resolve => resolve({ data: [] })));
    await wait();
    expect(count('active')).toBe(0);
    expect(count('queued')).toBe(2);
}

async function resume() {
    // what the Resume button of the Uploads dialog does
    await act(async () => store.dispatch(pauseUploads(false)));
    await act(async () => zone.current.upload(null, true));
    await wait();
}

describe('resuming paused uploads', () => {
    it('starts the queued uploads', async () => {
        await queueAndPause();
        await resume();
        expect(count('active')).toBe(2);
        expect(count('queued')).toBe(0);
    });

    it('starts the queued uploads after the upload zone was remounted, e.g. by visiting the settings', async () => {
        await queueAndPause();
        await unmount();
        await mount();
        await resume();
        expect(count('active')).toBe(2);
        expect(count('queued')).toBe(0);
    });
});
