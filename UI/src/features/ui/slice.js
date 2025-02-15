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

import { pick } from 'lodash';
import initialState from './initialState';
import { createSlice } from '@reduxjs/toolkit';

export const uiSlice = createSlice({
    name: 'ui',
    initialState: initialState,
    reducers: {
        setView: (state, action) => {
            const path = action.payload?.path;
            const search = path === "search";
            state.view.default = search ? state.view.default : action.payload?.value ?? initialState.view.default;
            if (path) state.view[path] = action.payload?.value ?? initialState.view.default;
        },
        resetView: (state, action) => {
            const path = action.payload?.path;
            if (path) {
                delete state.view[path];
            } else {
                state.view.default = action.payload?.value ?? initialState.view.default;
            }
        },
        setTheme: (state, action) => {
            const theme = action.payload ?? initialState.theme;
            document.documentElement.setAttribute('data-bs-theme', theme);
            state.theme = theme;
        },
        setScrollPosition: (state, action) => {
            const { path, index } = action.payload;
            if (index) {
                state.scrollPosition[path] = index;
            } else {
                delete state.scrollPosition[path];
            }
        },
        setTotalUploads: (state, action) => {
            state.uploads.total = action.payload ?? initialState.uploads.total;
        },
        queueUploads: (state, action) => {
            state.uploads.total += action.payload.length;
            state.uploads.queued.push(...action.payload);
        },
        dequeueUpload: (state) => {
            if (state.uploads.queued.length > 0) {
                state.uploads.queued.shift();
            }
        },
        addActiveUpload: (state, action) => {
            state.uploads.active = { ...state.uploads.active, ...action.payload };
        },
        removeActiveUpload: (state, action) => {
            const { key, succeeded } = action.payload;
            if (!state.uploads.active[key]) return;
            const upload = {
                key: key,
                ...pick(state.uploads.active[key], ['url', 'path', 'name'])
            };
            if (succeeded) state.uploads.succeeded = [...state.uploads.succeeded, upload];
            else state.uploads.failed = [...state.uploads.failed, upload];
            delete state.uploads.active[key];
        },
        setSimultaneousUploads: (state, action) => {
            state.uploads.simultaneous = action.payload ?? initialState.uploads.simultaneous;
        },
        setDuplicateUploads: (state, action) => {
            state.uploads.duplicate = action.payload ?? initialState.uploads.duplicate;
        },
        setOverwriteUploads: (state, action) => {
            state.uploads.overwrite = action.payload ?? initialState.uploads.overwrite;
        },
        setReupload: (state, action) => {
            state.uploads.reupload = action.payload ?? initialState.uploads.reupload;
        },
        setHistory: (state, action) => {
            state.history.items = action.payload?.items ?? initialState.history.items;
        },
        setHistoryItems: (state, action) => {
            state.history.items = action.payload ?? initialState.history.items;
        },
    },
});

const { actions, reducer } = uiSlice;
export const {
    setView,
    resetView,
    setTheme,
    setScrollPosition,
    setTotalUploads,
    queueUploads,
    dequeueUpload,
    addActiveUpload,
    removeActiveUpload,
    setSimultaneousUploads,
    setDuplicateUploads,
    setOverwriteUploads,
    setReupload,
    setHistory,
    setHistoryItems
} = actions;
export default reducer;
