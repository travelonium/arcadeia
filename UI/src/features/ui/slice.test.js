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
import { uiSlice, queueUpload, pauseUploads, removeUploads, updateUpload, switchUploadState } from './slice';

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

describe('pauseUploads', () => {
    it('pauses and resumes without touching the queue', () => {
        const items = [makeItem(URL, PATH_A, 'queued'), makeItem(URL, PATH_B, 'active')];
        const paused = reducer(makeState(items), pauseUploads(true));
        expect(paused.uploads.paused).toBe(true);
        expect(paused.uploads.items).toEqual(items);
        const resumed = reducer(paused, pauseUploads(false));
        expect(resumed.uploads.paused).toBe(false);
        expect(resumed.uploads.items).toEqual(items);
    });
});

describe('removeUploads – pause', () => {
    it('resumes once the last queued upload is removed', () => {
        const queued = makeItem(URL, PATH_A, 'queued');
        const state = { ...makeState([queued, makeItem(URL, PATH_B, 'queued')]), };
        state.uploads.paused = true;
        const one = reducer(state, removeUploads({ key: queued.key }));
        expect(one.uploads.paused).toBe(true);
        const none = reducer(one, removeUploads({ state: 'queued' }));
        expect(none.uploads.paused).toBe(false);
    });
});

describe('updateUpload – progress', () => {
    it('stores a zero progress so processing restarts the bar after uploading', () => {
        const item = makeItem(URL, PATH_A, 'active');
        const state = makeState([item]);
        state.uploads.progress[item.key] = { value: 0.99, timestamp: 0 };
        const next = reducer(state, updateUpload({ key: item.key, progress: 0, value: { status: 'Processing...' } }));
        expect(next.uploads.progress[item.key].value).toBe(0);
        expect(next.uploads.items[0].status).toBe('Processing...');
    });

    it('accepts a zero progress on its own', () => {
        const item = makeItem(URL, PATH_A, 'active');
        const next = reducer(makeState([item]), updateUpload({ key: item.key, progress: 0 }));
        expect(next.uploads.progress[item.key].value).toBe(0);
    });

    it('rejects an update with neither a value nor a progress', () => {
        const item = makeItem(URL, PATH_A, 'active');
        expect(() => reducer(makeState([item]), updateUpload({ key: item.key }))).toThrow();
    });
});

describe('switchUploadState – status', () => {
    it('clears the status of the previous attempt when retried', () => {
        const item = { ...makeItem(URL, PATH_A, 'failed'), status: 'Upload Failed', error: 'boom' };
        const next = reducer(makeState([item]), switchUploadState({ key: item.key, to: 'queued' }));
        expect(next.uploads.items[0].status).toBeUndefined();
        expect(next.uploads.items[0].error).toBeUndefined();
    });

    it('keeps the last status when an upload finishes', () => {
        const item = { ...makeItem(URL, PATH_A, 'active'), status: 'Upload Complete' };
        const next = reducer(makeState([item]), switchUploadState({ key: item.key, to: 'succeeded' }));
        expect(next.uploads.items[0].status).toBe('Upload Complete');
    });
});
