import { createSlice } from '@reduxjs/toolkit';
import initialState from './initialState';

export const uiSlice = createSlice({
    name: 'ui',
    initialState: initialState,
    reducers: {
        setView: (state, action) => {
            const path = action.payload?.path;
            const search = path === "search";
            let newState = {
                ...state,
                view: {
                    ...state.view,
                    // don't store the view settings as default when searching
                    default: (search ? state.view.default : action.payload?.value) ?? initialState.view.default,
                }
            }
            if (path) {
                newState.view[path] = {};
                newState.view[path] = action.payload?.value ?? initialState.view.default;
            }
            return newState;
        },
        resetView: (state, action) => {
            const path = action.payload?.path;
            let newState = {
                ...state,
                view: {
                    ...state.view
                }
            }
            if (path && newState.view.hasOwnProperty(path)) {
                delete newState.view[path];
            } else {
                newState.view.default = action.payload?.value ?? initialState.view.default;
            }
            return newState;
        },
        setTheme: (state, action) => {
            const theme = action.payload ?? initialState.theme;
            document.documentElement.setAttribute('data-bs-theme', theme);
            return {
                ...state,
                theme: theme,
            };
        },
        setScrollPosition: (state, action) => {
            let newState = {
                ...state,
                scrollPosition: {
                    ...state.scrollPosition,
                },
            };
            const path = action.payload.path;
            const index = action.payload.index;
            if (index) newState.scrollPosition[path] = index;
            else if (newState.scrollPosition.hasOwnProperty(path)) delete newState.scrollPosition[path];
            return newState;
        },
        setUploads: (state, action) => {
            return {
                ...state,
                uploads: {
                    ...state.uploads,
                    simultaneous: action.payload?.simultaneous ? action.payload.simultaneous : initialState.uploads.simultaneous,
                    duplicate: action.payload?.duplicate ? action.payload.duplicate : initialState.uploads.duplicate,
                    overwrite: action.payload?.overwrite ? action.payload.overwrite : initialState.uploads.overwrite,
                }
            }
        },
        setSimultaneousUploads: (state, action) => {
            return {
                ...state,
                uploads: {
                    ...state.uploads,
                    simultaneous: action.payload ? action.payload : initialState.uploads.simultaneous,
                }
            }
        },
        setDuplicateUploads: (state, action) => {
            return {
                ...state,
                uploads: {
                    ...state.uploads,
                    duplicate: action.payload ? action.payload : initialState.uploads.duplicate,
                }
            }
        },
        setOverwriteUploads: (state, action) => {
            return {
                ...state,
                uploads: {
                    ...state.uploads,
                    overwrite: action.payload ? action.payload : initialState.uploads.overwrite,
                }
            }
        },
        setHistory: (state, action) => {
            return {
                ...state,
                history: {
                    ...state.history,
                    items: action.payload?.items ? action.payload.items : initialState.history.items,
                }
            }
        },
        setHistoryItems: (state, action) => {
            return {
                ...state,
                history: {
                    ...state.history,
                    items: action.payload ? action.payload : initialState.history.items,
                }
            }
        },
    },
});

const { actions, reducer } = uiSlice;
export const { setView, resetView, setTheme, setScrollPosition, setSimultaneousUploads } = actions;
export default reducer;
