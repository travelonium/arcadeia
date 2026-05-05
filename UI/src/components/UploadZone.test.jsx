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
