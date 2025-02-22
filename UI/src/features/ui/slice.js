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

import pb from 'path-browserify';
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
        queueUpload: (state, action) => {
            const { key, value } = action.payload;
            const item = state.uploads.items[key];
            if (item) {
                if (item.state === 'active') {
                    console.log("Already Active:", item.url ?? item.name);
                    return;
                };
                if (item.state === 'queued') {
                    console.log("Already Queued:", item.url ?? item.name);
                    return;
                };
                if (item.state === 'succeeded' && !state.uploads.reupload) {
                    console.log("Already Uploaded:", item.url ?? item.name);
                    return;
                }
            }
            state.uploads.items[key] = {
                ...value,
                key: key,
                state: 'queued',
                timestamp: Date.now(),
            }
            console.debug("Queued:", value.url ?? value.name);
        },
        startUpload: (state, action) => {
            // find the key of the oldest queued item
            const key = Object.keys(state.uploads.items)
                .filter((k) => state.uploads.items[k].state === 'queued')
                .reduce((oldest, k) => !oldest || state.uploads.items[k].timestamp < state.uploads.items[oldest].timestamp ? k : oldest, null);
            if (key) {
                // update state directly
                state.uploads.items[key].state = 'active';
                state.uploads.items[key].timestamp = Date.now();
                // return the dequeued item through the action payload
                action.payload = {
                    key: key
                };
                console.debug("Dequeued:", state.uploads.items[key]?.url ?? pb.join(state.uploads.items[key]?.path, state.uploads.items[key]?.file?.webkitRelativePath, state.uploads.items[key]?.file?.name));
            }
        },
        updateUpload: (state, action) => {
            const { key, value } = action.payload;
            if (!state.uploads.items[key]) return;
            Object.assign(state.uploads.items[key], value);
        },
        switchUploadState: (state, action) => {
            const { key, to } = action.payload;
            if (!state.uploads.items[key]) return;
            Object.assign(state.uploads.items[key], {
                state: to,
                timestamp: Date.now()
            });
        },
        removeUploads: (state, action) => {
            if (action.payload.key) {
                delete state.uploads.items[action.payload.key];
            } else if (action.payload.state) {
                Object.entries(state.uploads.items).forEach(([key, value]) => {
                    if (value.state === action.payload.state) {
                        delete state.uploads.items[key];
                    }
                });
            } else throw new Error("Neither key nor state has been supplied to the removeUploads.");
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

export const startUploadThunk = () => (dispatch, getState) => {
    return new Promise((resolve) => {
        const result = dispatch(startUpload());
        const key = result?.payload?.key;
        if (!key) {
            resolve({ key: null, item: null });
            return;
        }
        setTimeout(() => {
            const updatedState = getState();
            resolve({ key: key, item: updatedState.ui.uploads.items[key] });
        }, 0);
    });
};

export const switchUploadStateThunk = (key, to) => (dispatch, getState) => {
    return new Promise((resolve) => {
        const result = dispatch(switchUploadState({ key, to }));
        setTimeout(() => {
            resolve();
        }, 0);
    });
};

const { actions, reducer } = uiSlice;
export const {
    setView,
    resetView,
    setTheme,
    setScrollPosition,
    queueUpload,
    startUpload,
    updateUpload,
    switchUploadState,
    removeUploads,
    setSimultaneousUploads,
    setDuplicateUploads,
    setOverwriteUploads,
    setReupload,
    setHistory,
    setHistoryItems
} = actions;
export default reducer;
