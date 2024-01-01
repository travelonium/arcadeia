import { createSlice } from '@reduxjs/toolkit';

const initialState = {
    view: "card",
    theme: "light",
    scrollPosition: {},
    uploads: {
        simultaneous: 4,
        duplicate: true,
        overwrite: false,
    }
};

export const uiSlice = createSlice({
    name: 'ui',
    initialState: initialState,
    reducers: {
        setView: (state, action) => {
            state.view = action.payload ?? initialState.view;
        },
        setTheme: (state, action) => {
            state.theme = action.payload ?? initialState.theme;
            document.documentElement.setAttribute('data-bs-theme', state.theme);
        },
        setScrollPosition: (state, action) => {
            let path = action.payload.path;
            let index = action.payload.index;
            if (index) {
                state.scrollPosition[path] = index;
            } else if (state.scrollPosition.hasOwnProperty(path)) {
                delete state.scrollPosition[path];
            }
        },
        setUploads: (state, action) => {
            state.uploads.simultaneous = action.payload?.simultaneous ? action.payload.simultaneous : initialState.uploads.simultaneous;
            state.uploads.duplicate = action.payload?.duplicate ? action.payload.duplicate : initialState.uploads.duplicate;
            state.uploads.overwrite = action.payload?.overwrite ? action.payload.overwrite : initialState.uploads.overwrite;
        },
        setSimultaneousUploads: (state, action) => {
            state.uploads.simultaneous = action.payload ? action.payload : initialState.uploads.simultaneous;
        },
        setDuplicateUploads: (state, action) => {
            state.uploads.duplicate = action.payload ? action.payload : initialState.uploads.duplicate;
        },
        setOverwriteUploads: (state, action) => {
            state.uploads.overwrite = action.payload ? action.payload : initialState.uploads.overwrite;
        },
    },
});

const { actions, reducer } = uiSlice;
export const { setView, setTheme, setScrollPosition, setSimultaneousUploads } = actions;
export default reducer;
