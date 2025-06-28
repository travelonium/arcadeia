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
            let item = {};
            const { key, value } = action.payload;
            const index = state.uploads.items.findIndex(item => item.key === key);
            if (index !== -1) {
                item = state.uploads.items[index];
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
                // remove the item from the array
                state.uploads.items.splice(index, 1);
            }
            Object.assign(item, value, {
                key: key,
                state: 'queued',
                timestamp: Date.now()
            });
            // binary search for the new insertion index
            let low = 0;
            let high = state.uploads.items.length;
            while (low < high) {
                const mid = (low + high) >> 1;
                if (state.uploads.items[mid].timestamp > item.timestamp) {
                    low = mid + 1;
                } else {
                    high = mid;
                }
            }
            // insert the item at the found index
            state.uploads.items.splice(low, 0, item);
            console.debug("Queued:", item.url ?? item.name);
        },
        startUpload: (state, action) => {
            // find the key of the oldest queued item
            let index = null;
            for (let i = state.uploads.items.length - 1; i >= 0; i--) {
                const item = state.uploads.items[i];
                if (item.state === 'queued') {
                    index = i;
                    break;
                }
            }
            if (index !== null) {
                const key = state.uploads.items[index].key;
                let item = {
                    ...state.uploads.items[index],
                    state: 'active',
                    timestamp: Date.now()
                }
                delete item.error;
                delete state.uploads.progress[key];
                // remove the item from the array
                state.uploads.items.splice(index, 1);
                // binary search for the new insertion index
                let low = 0;
                let high = state.uploads.items.length;
                while (low < high) {
                    const mid = (low + high) >> 1;
                    if (state.uploads.items[mid].timestamp > item.timestamp) {
                        low = mid + 1;
                    } else {
                        high = mid;
                    }
                }
                // insert the item at the found index
                state.uploads.items.splice(low, 0, item);
                // return the dequeued item through the action payload
                action.payload = {
                    key: key
                };
                console.debug("Dequeued:", item.url ?? pb.join(item.path, item.file?.webkitRelativePath, item.file?.name));
            }
        },
        updateUpload: (state, action) => {
            const { key, value, progress } = action.payload;
            if (!value && !progress) throw new Error("Either value or progress needs to be supplied.");
            if (key) {
                const index = state.uploads.items.findIndex(item => item.key === key);
                if (index !== -1) {
                    if (value) {
                        // a specific key has been given, update the item
                        Object.assign(state.uploads.items[index], value);
                    }
                    if (progress) {
                        state.uploads.progress[key] = {
                            value: progress,
                            timestamp: Date.now()
                        };
                    }
                }
            } else {
                if (value) {
                    // update all items using the provided value
                    state.uploads.items.forEach((item, index) => {
                        Object.assign(state.uploads.items[index], value);
                    });
                }
            }
        },
        updateUploadProgress: (state, action) => {
            const { key, value } = action.payload;
            if (!key) return;
            state.uploads.progress[key] = {
                value: value,
                timestamp: Date.now()
            };
        },
        switchUploadState: (state, action) => {
            const { key, to } = action.payload;
            delete state.uploads.progress[key];
            const index = state.uploads.items.findIndex(item => item.key === key);
            if (index !== -1) {
                let item = {
                    ...state.uploads.items[index],
                    state: to,
                    timestamp: Date.now()
                };
                if (to !== 'failed') delete item.error;
                // remove the item from the array
                state.uploads.items.splice(index, 1);
                // binary search for the new insertion index
                let low = 0;
                let high = state.uploads.items.length;
                while (low < high) {
                    const mid = (low + high) >> 1;
                    if (state.uploads.items[mid].timestamp > item.timestamp) {
                        low = mid + 1;
                    } else {
                        high = mid;
                    }
                }
                // insert the item at the found index
                state.uploads.items.splice(low, 0, item);
            }
        },
        removeUploads: (state, action) => {
            if (action.payload.key) {
                const key = action.payload.key;
                const index = state.uploads.items.findIndex(item => item.key === key);
                if (index !== -1) state.uploads.items.splice(index, 1);
                delete state.uploads.progress[key];
            } else if (action.payload.state) {
                state.uploads.items = state.uploads.items.filter(item => {
                    const shouldKeep = item.state !== action.payload.state;
                    if (!shouldKeep) delete state.uploads.progress[item.key];
                    return shouldKeep;
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
            const index = updatedState.ui.uploads.items.findIndex(item => item.key === key);
            if (index === -1) {
                resolve({ key: key, item: null });
                return;
            }
            const item = updatedState.ui.uploads.items[index];
            resolve({ key: key, item: item });
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
    updateUploadProgress,
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
