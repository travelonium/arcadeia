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

import React, { act, createRef } from 'react';
import { describe, it, expect, vi, afterEach } from 'vitest';
import { createRoot } from 'react-dom/client';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import uiReducer, { updateUpload, switchUploadState } from '../../features/ui/slice';
import Uploads from './Uploads';

// jsdom has no layout, so give the virtualized list a size to render its rows into
vi.mock('react-virtualized-auto-sizer', () => ({
    default: ({ children }) => children({ height: 600, width: 800 }),
}));

const PATH = '/photos/';

function item(key, state, extra = {}) {
    return { key, name: `${key}.mp4`, path: PATH, state, timestamp: Date.now(), ...extra };
}

let root;
let container;

async function render(items, progress = {}) {
    const store = configureStore({
        reducer: { ui: uiReducer },
        preloadedState: {
            ui: { ...uiReducer(undefined, { type: '@@init' }), uploads: { ...uiReducer(undefined, { type: '@@init' }).uploads, items, progress } },
        },
    });
    const ref = createRef();
    // without the fade transition there are no timers left to fire after the environment is torn down
    container = document.createElement('div');
    document.body.appendChild(container);
    root = createRoot(container);
    await act(async () => {
        root.render(<Provider store={store}><Uploads ref={ref} animation={false} /></Provider>);
    });
    await act(async () => ref.current.show());
    return store;
}

function row(name) {
    return [...document.querySelectorAll('.uploads .list-group-item')].find(element => element.textContent.includes(name));
}

afterEach(() => {
    act(() => root.unmount());
    container.remove();
});

describe('Uploads', () => {
    it('shows the status and percentage of an active upload', async () => {
        await render([item('clip', 'active', { status: 'Uploading...' })], { clip: { value: 0.42, timestamp: 0 } });

        const element = row('clip.mp4');
        expect(element.querySelector('.status').textContent).toContain('Uploading...');
        expect(element.querySelector('.status .percentage').textContent).toBe('42%');
        expect(element.querySelector('.status .progress')).not.toBeNull();
    });

    it('caps the progress at 100%', async () => {
        await render([item('clip', 'active', { status: 'Processing...' })], { clip: { value: 3, timestamp: 0 } });

        expect(row('clip.mp4').querySelector('.status .percentage').textContent).toBe('100%');
    });

    it('shows the status of an active upload before any progress is reported', async () => {
        await render([item('clip', 'active', { status: 'Resolving...' })]);

        const element = row('clip.mp4');
        expect(element.querySelector('.status').textContent).toBe('Resolving...');
        expect(element.querySelector('.status .progress')).toBeNull();
    });

    it('switches to processing with the bar restarting from zero', async () => {
        const store = await render([item('clip', 'active', { status: 'Uploading...' })], { clip: { value: 0.99, timestamp: 0 } });

        await act(async () => store.dispatch(updateUpload({ key: 'clip', progress: 0, value: { status: 'Processing...' } })));

        const element = row('clip.mp4');
        expect(element.querySelector('.status').textContent).toContain('Processing...');
        expect(element.querySelector('.status .percentage').textContent).toBe('0%');
    });

    it('does not show a status for uploads that are not active', async () => {
        await render([
            item('queued', 'queued'),
            item('done', 'succeeded', { status: 'Upload Complete' }),
            item('broken', 'failed', { status: 'Upload Failed' }),
        ]);

        expect(row('queued.mp4').querySelector('.status')).toBeNull();
        expect(row('done.mp4').querySelector('.status')).toBeNull();
        expect(row('broken.mp4').querySelector('.status')).toBeNull();
    });

    it('gives active uploads taller rows to fit the status', async () => {
        await render([item('clip', 'active', { status: 'Uploading...' }), item('queued', 'queued')], { clip: { value: 0.5, timestamp: 0 } });

        const active = row('clip.mp4');
        const queued = row('queued.mp4');
        expect(parseFloat(active.style.height)).toBeGreaterThan(parseFloat(queued.style.height));
        expect(parseFloat(queued.style.top)).toBe(parseFloat(active.style.height));
    });

    it('resizes the rows when an upload stops being active', async () => {
        const store = await render([item('clip', 'active', { status: 'Uploading...' }), item('queued', 'queued')], { clip: { value: 0.5, timestamp: 0 } });
        const queuedHeight = parseFloat(row('queued.mp4').style.height);

        await act(async () => store.dispatch(switchUploadState({ key: 'clip', to: 'succeeded' })));

        expect(parseFloat(row('clip.mp4').style.height)).toBe(queuedHeight);
    });

    it('clamps the name so long names cannot overflow the row', async () => {
        await render([item('a'.repeat(200), 'active', { status: 'Uploading...' })]);

        const name = document.querySelector('.uploads .list-group-item .name');
        expect(name).not.toBeNull();
        expect(name.getAttribute('title')).toBe(`${'a'.repeat(200)}.mp4`);
    });
});
