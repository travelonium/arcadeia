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
import { toast } from 'react-toastify';
import { Library } from './Library';

vi.mock('react-toastify', () => ({
    toast: { success: vi.fn(), error: vi.fn(), info: vi.fn(), update: vi.fn(), dismiss: vi.fn() },
}));

const sort = {
    fields: [{ id: 0, name: "Name", value: "name", active: true }],
    direction: "asc",
};

function makeInstance({ pathname = '/photos/', search = '', items = [] } = {}) {
    const instance = new Library({
        location: { pathname, search },
        searchParams: new URLSearchParams(search),
        search: { sort },
        settings: {},
        dispatch: vi.fn(),
    });
    instance.state.items = items;
    // the instance isn't mounted, so apply state updates synchronously
    instance.setState = (state, callback) => {
        Object.assign(instance.state, typeof state === 'function' ? state(instance.state) : state);
        callback?.();
    };
    instance.refresh = vi.fn();
    instance.scrollToItem = vi.fn();
    return instance;
}

function mockFetch(ok, body) {
    const fetch = vi.fn().mockResolvedValue({ ok, json: () => Promise.resolve(body) });
    vi.stubGlobal('fetch', fetch);
    return fetch;
}

function createFolder(instance, name) {
    return new Promise((resolve) => instance.createFolder(name, (source, succeeded) => resolve({ source, succeeded })));
}

// ─── createFolder ─────────────────────────────────────────────────────────────

describe('createFolder', () => {
    beforeEach(() => { vi.clearAllMocks(); });
    afterEach(() => { vi.unstubAllGlobals(); });

    it('posts to the folder endpoint of the current path', async () => {
        const fetch = mockFetch(true, { id: '1', name: 'New Folder' });
        await createFolder(makeInstance({ pathname: '/my photos/#1/' }));
        const [url, options] = fetch.mock.calls[0];
        expect(url).toBe('/api/library/folder/my%20photos/%231/');
        expect(options.method).toBe('POST');
        expect(JSON.parse(options.body)).toEqual({});
    });

    it('sends the name when one is given', async () => {
        const fetch = mockFetch(true, { id: '1', name: 'Holiday' });
        await createFolder(makeInstance(), 'Holiday');
        expect(JSON.parse(fetch.mock.calls[0][1].body)).toEqual({ name: 'Holiday' });
    });

    it('inserts the new folder in sorted order and scrolls to it', async () => {
        mockFetch(true, { id: '2', name: 'Beta' });
        const instance = makeInstance({ items: [{ id: '1', name: 'Alpha' }, { id: '3', name: 'Gamma' }] });
        const { source, succeeded } = await createFolder(instance);
        expect(succeeded).toBe(true);
        expect(source).toMatchObject({ id: '2', name: 'Beta', children: [], duplicates: 0 });
        expect(instance.state.items.map(item => item.id)).toEqual(['1', '2', '3']);
        expect(instance.scrollToItem).toHaveBeenCalledWith(1, true);
        expect(instance.refresh).not.toHaveBeenCalled();
        expect(toast.success).toHaveBeenCalled();
    });

    it.each([
        ['searching',  '?query=cats'],
        ['duplicates', '?duplicates=true'],
    ])('refreshes instead of inserting while %s', async (_, search) => {
        mockFetch(true, { id: '2', name: 'New Folder' });
        const instance = makeInstance({ search, items: [{ id: '1', name: 'Alpha' }] });
        const { succeeded } = await createFolder(instance);
        expect(succeeded).toBe(true);
        expect(instance.refresh).toHaveBeenCalled();
        expect(instance.state.items).toHaveLength(1);
    });

    it('shows the server error and leaves the items untouched on failure', async () => {
        mockFetch(false, { title: 'Already Exists', detail: 'A file or folder with the same name already exists.' });
        const instance = makeInstance({ items: [{ id: '1', name: 'Alpha' }] });
        const { source, succeeded } = await createFolder(instance, 'Alpha');
        expect(succeeded).toBe(false);
        expect(source).toBeNull();
        expect(toast.error).toHaveBeenCalledWith('A file or folder with the same name already exists.');
        expect(instance.state.items).toHaveLength(1);
    });

    it('shows an error when the request fails', async () => {
        vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('network error')));
        const { succeeded } = await createFolder(makeInstance());
        expect(succeeded).toBe(false);
        expect(toast.error).toHaveBeenCalledWith('network error');
    });
});

// ─── delete ───────────────────────────────────────────────────────────────────

describe('delete', () => {
    beforeEach(() => { vi.clearAllMocks(); });
    afterEach(() => { vi.unstubAllGlobals(); });

    it('shows the spinner instead of the default icon on the progress toast', async () => {
        vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, text: () => Promise.resolve('') }));
        const instance = makeInstance({ items: [{ id: '1', name: 'Alpha', fullPath: '/photos/Alpha.jpg' }] });
        instance.selected.add('1');
        instance.forceUpdate = vi.fn();
        await new Promise((resolve) => instance.delete(undefined, resolve));
        expect(toast.info.mock.calls[0][1].icon.props.className).toBe('Toastify__spinner');
    });
});
