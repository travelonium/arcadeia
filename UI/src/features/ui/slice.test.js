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

import { describe, it, expect } from 'vitest';
import { uiSlice, queueUpload } from './slice';

const reducer = uiSlice.reducer;

function makeState(items = [], reupload = false) {
    return {
        uploads: {
            items,
            progress: {},
            simultaneous: 4,
            duplicate: true,
            overwrite: false,
            reupload,
        },
    };
}

function makeItem(url, path, state) {
    return { key: path + url, url, path, state, timestamp: Date.now() - 1000 };
}

const URL = 'https://example.com/video.mp4';
const PATH_A = '/photos/';
const PATH_B = '/videos/';

describe('queueUpload – URL deduplication', () => {

    it('queues a new URL with no prior history', () => {
        const state = makeState();
        const next = reducer(state, queueUpload({ key: PATH_A + URL, value: { url: URL, path: PATH_A } }));
        expect(next.uploads.items).toHaveLength(1);
        expect(next.uploads.items[0].state).toBe('queued');
    });

    describe('same URL, same path (same key)', () => {
        it('skips when already active', () => {
            const state = makeState([makeItem(URL, PATH_A, 'active')]);
            const next = reducer(state, queueUpload({ key: PATH_A + URL, value: { url: URL, path: PATH_A } }));
            expect(next.uploads.items).toHaveLength(1);
            expect(next.uploads.items[0].state).toBe('active');
        });

        it('skips when already queued', () => {
            const state = makeState([makeItem(URL, PATH_A, 'queued')]);
            const next = reducer(state, queueUpload({ key: PATH_A + URL, value: { url: URL, path: PATH_A } }));
            expect(next.uploads.items).toHaveLength(1);
            expect(next.uploads.items[0].state).toBe('queued');
        });

        it('skips when already succeeded and reupload is off', () => {
            const state = makeState([makeItem(URL, PATH_A, 'succeeded')], false);
            const next = reducer(state, queueUpload({ key: PATH_A + URL, value: { url: URL, path: PATH_A } }));
            expect(next.uploads.items).toHaveLength(1);
            expect(next.uploads.items[0].state).toBe('succeeded');
        });

        it('re-queues when succeeded and reupload is on', () => {
            const state = makeState([makeItem(URL, PATH_A, 'succeeded')], true);
            const next = reducer(state, queueUpload({ key: PATH_A + URL, value: { url: URL, path: PATH_A } }));
            expect(next.uploads.items).toHaveLength(1);
            expect(next.uploads.items[0].state).toBe('queued');
        });
    });

    describe('same URL, different path (different key)', () => {
        it('skips when the same URL is already active at another path', () => {
            const state = makeState([makeItem(URL, PATH_A, 'active')]);
            const next = reducer(state, queueUpload({ key: PATH_B + URL, value: { url: URL, path: PATH_B } }));
            expect(next.uploads.items).toHaveLength(1);
            expect(next.uploads.items[0].state).toBe('active');
            expect(next.uploads.items[0].path).toBe(PATH_A);
        });

        it('skips when the same URL is already queued at another path', () => {
            const state = makeState([makeItem(URL, PATH_A, 'queued')]);
            const next = reducer(state, queueUpload({ key: PATH_B + URL, value: { url: URL, path: PATH_B } }));
            expect(next.uploads.items).toHaveLength(1);
            expect(next.uploads.items[0].state).toBe('queued');
            expect(next.uploads.items[0].path).toBe(PATH_A);
        });

        it('skips when the same URL already succeeded at another path and reupload is off', () => {
            const state = makeState([makeItem(URL, PATH_A, 'succeeded')], false);
            const next = reducer(state, queueUpload({ key: PATH_B + URL, value: { url: URL, path: PATH_B } }));
            expect(next.uploads.items).toHaveLength(1);
            expect(next.uploads.items[0].state).toBe('succeeded');
            expect(next.uploads.items[0].path).toBe(PATH_A);
        });

        it('allows re-upload when the same URL succeeded at another path and reupload is on', () => {
            const state = makeState([makeItem(URL, PATH_A, 'succeeded')], true);
            const next = reducer(state, queueUpload({ key: PATH_B + URL, value: { url: URL, path: PATH_B } }));
            expect(next.uploads.items).toHaveLength(2);
            const newItem = next.uploads.items.find(i => i.path === PATH_B);
            expect(newItem.state).toBe('queued');
        });

        it('allows re-upload when the same URL failed at another path', () => {
            const state = makeState([makeItem(URL, PATH_A, 'failed')]);
            const next = reducer(state, queueUpload({ key: PATH_B + URL, value: { url: URL, path: PATH_B } }));
            expect(next.uploads.items).toHaveLength(2);
            const newItem = next.uploads.items.find(i => i.path === PATH_B);
            expect(newItem.state).toBe('queued');
        });
    });

    it('does not apply URL deduplication to file uploads', () => {
        const existing = { key: '/photos/video.mp4', name: 'video.mp4', path: PATH_A, state: 'succeeded', timestamp: Date.now() - 1000 };
        const state = makeState([existing]);
        const file = { name: 'video.mp4' };
        const next = reducer(state, queueUpload({ key: PATH_B + 'video.mp4', value: { name: 'video.mp4', file, path: PATH_B } }));
        expect(next.uploads.items).toHaveLength(2);
    });

});
