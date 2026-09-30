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

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { UploadZone } from './UploadZone';

function makeInstance(pathname = '/photos/') {
    return new UploadZone({ location: { pathname }, dispatch: vi.fn() });
}

// ─── isValidHttpUrl ────────────────────────────────────────────────────────────

describe('isValidHttpUrl', () => {
    const instance = makeInstance();

    it.each([
        ['https://example.com',                   true],
        ['http://example.com',                    true],
        ['https://example.com/path?q=1#hash',     true],
        ['ftp://example.com',                     false],
        ['blob:https://example.com/abc',          false],
        ['data:image/png;base64,abc',             false],
        ['/relative/path',                        false],
        ['not-a-url',                             false],
        ['',                                      false],
    ])('%s → %s', (input, expected) => {
        expect(instance.isValidHttpUrl(input)).toBe(expected);
    });
});

// ─── path getter ───────────────────────────────────────────────────────────────

describe('path getter', () => {
    it.each([
        ['/photos/file.jpg',        '/photos/'],
        ['/photos/',                '/photos/'],
        ['/photos/nested/file.jpg', '/photos/nested/'],
        ['/',                       '/'],
        ['/file.jpg',               '/'],
        ['',                        null],
        ['no-slash',                null],
    ])('pathname %s → %s', (pathname, expected) => {
        expect(makeInstance(pathname).path).toBe(expected);
    });

    it('decodes URI-encoded characters', () => {
        expect(makeInstance('/my%20photos/').path).toBe('/my photos/');
    });

    it('returns null for a malformed URI', () => {
        expect(makeInstance('%ZZ/photos/').path).toBeNull();
    });

    it('returns null when pathname is null', () => {
        expect(new UploadZone({ location: { pathname: null }, dispatch: vi.fn() }).path).toBeNull();
    });

    it('returns null when location is absent', () => {
        expect(new UploadZone({ dispatch: vi.fn() }).path).toBeNull();
    });
});

// ─── fetchSessionStartedTimestamp ─────────────────────────────────────────────

describe('fetchSessionStartedTimestamp', () => {
    let instance;

    beforeEach(() => { instance = makeInstance(); });
    afterEach(() => { vi.restoreAllMocks(); });

    it('returns the parsed timestamp from a successful response', async () => {
        const ts = '2024-06-01T12:00:00.000Z';
        vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
            ok: true,
            json: () => Promise.resolve({ SessionStarted: ts }),
        }));
        expect(await instance.fetchSessionStartedTimestamp()).toBe(new Date(ts).getTime());
    });

    it('falls back to Date.now() when the response is not ok', async () => {
        const before = Date.now();
        vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: false }));
        expect(await instance.fetchSessionStartedTimestamp()).toBeGreaterThanOrEqual(before);
    });

    it('falls back to Date.now() when fetch throws', async () => {
        const before = Date.now();
        vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('network error')));
        expect(await instance.fetchSessionStartedTimestamp()).toBeGreaterThanOrEqual(before);
    });
});

// ─── sessionStartedTimestamp ──────────────────────────────────────────────────

describe('sessionStartedTimestamp', () => {
    let instance;

    beforeEach(() => {
        instance = makeInstance();
        document.cookie = 'SessionStarted=; max-age=0; path=/';
    });
    afterEach(() => {
        document.cookie = 'SessionStarted=; max-age=0; path=/';
        vi.restoreAllMocks();
    });

    it('reads and parses the timestamp from the SessionStarted cookie', async () => {
        const ts = '2024-06-01T12:00:00.000Z';
        document.cookie = `SessionStarted=${encodeURIComponent(ts)}; path=/`;
        expect(await instance.sessionStartedTimestamp()).toBe(new Date(ts).getTime());
    });

    it('falls back to fetchSessionStartedTimestamp when the cookie is absent', async () => {
        const expected = 1717200000000;
        instance.fetchSessionStartedTimestamp = vi.fn().mockResolvedValue(expected);
        expect(await instance.sessionStartedTimestamp()).toBe(expected);
        expect(instance.fetchSessionStartedTimestamp).toHaveBeenCalledOnce();
    });
});

// ─── componentDidMount ────────────────────────────────────────────────────────

describe('componentDidMount', () => {
    const STARTED = 1_000_000;
    const NOW = 5_000_000;

    function mount(items) {
        const instance = new UploadZone({
            location: { pathname: '/' },
            dispatch: vi.fn(() => Promise.resolve()),
            ui: {
                uploads: {
                    all: items,
                    active: items.filter(item => item.state === 'active'),
                    queued: items.filter(item => item.state === 'queued'),
                },
            },
        });
        instance.sessionStartedTimestamp = vi.fn().mockResolvedValue(STARTED);
        instance.setState = vi.fn((state, callback) => {
            Object.assign(instance.state, state);
            callback?.();
        });
        return instance;
    }

    beforeEach(() => { vi.spyOn(Date, 'now').mockReturnValue(NOW); });
    afterEach(() => { vi.restoreAllMocks(); });

    it('starts a fresh batch instead of counting uploads finished earlier in the session', async () => {
        const instance = mount([
            { key: 'a', state: 'succeeded', timestamp: STARTED + 100 },
            { key: 'b', state: 'failed', timestamp: STARTED + 200 },
        ]);
        await instance.componentDidMount();
        expect(instance.state.timestamp).toBe(NOW);
        expect(instance.all).toHaveLength(0);
    });

    it('keeps URL uploads still running from this session in the batch', async () => {
        const instance = mount([
            { key: 'a', state: 'succeeded', timestamp: STARTED + 100 },
            { key: 'b', state: 'active', url: 'https://example.com/x', timestamp: STARTED + 200 },
        ]);
        await instance.componentDidMount();
        expect(instance.state.timestamp).toBe(STARTED + 200);
        expect(instance.all.map(item => item.key)).toEqual(['b']);
    });
});

// ─── upload ───────────────────────────────────────────────────────────────────

describe('upload', () => {
    function makeUploading(paused) {
        const items = [
            { key: 'a', state: 'active', timestamp: 2 },
            { key: 'b', state: 'queued', timestamp: 3 },
        ];
        const dispatch = vi.fn(() => Promise.resolve({ key: null, item: null }));
        const instance = new UploadZone({
            location: { pathname: '/' },
            dispatch,
            ui: {
                uploads: {
                    all: items,
                    active: items.filter(item => item.state === 'active'),
                    queued: items.filter(item => item.state === 'queued'),
                    failed: [],
                    simultaneous: 4,
                    paused,
                },
            },
        });
        instance.state.timestamp = 1;
        return { instance, dispatch };
    }

    afterEach(() => { vi.restoreAllMocks(); });

    it('does not start queued uploads while paused', () => {
        const { instance, dispatch } = makeUploading(true);
        instance.upload();
        expect(dispatch).not.toHaveBeenCalled();
    });

    it('starts the next queued upload when not paused', () => {
        vi.spyOn(console, 'error').mockImplementation(() => {});
        const { instance, dispatch } = makeUploading(false);
        instance.upload();
        expect(dispatch).toHaveBeenCalledOnce();
    });
});
