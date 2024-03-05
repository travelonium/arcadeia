import { createSlice } from '@reduxjs/toolkit';

const initialState = {
    view: "card",
    theme: "light",
    scrollPosition: {},
    uploads: {
        simultaneous: 4,
        duplicate: true,
        overwrite: false,
    },
    history: {
        items: 10,
    }
};

export const uiSlice = createSlice({
    name: 'ui',
    initialState: initialState,
    reducers: {
        setView: (state, action) => {
            return {
                ...state,
                view: action.payload ?? initialState.view,
            };
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
export const { setView, setTheme, setScrollPosition, setSimultaneousUploads } = actions;
export default reducer;
