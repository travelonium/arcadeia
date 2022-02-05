import { createSlice } from '@reduxjs/toolkit';

const initialState = {
    path: "",
    query: "",
    favorite: false,
    recursive: true,
};

export const searchSlice = createSlice({
    name: 'search',
    initialState: initialState,
    reducers: {
        setPath: (state, action) => {
            state.path = action.payload;
            state.query = initialState.query;
        },
        setQuery: (state, action) => {
            state.query = action.payload;
        },
        setFavorite: (state, action) => {
            state.favorite = action.payload;
        },
        setRecursive: (state, action) => {
            state.recursive = action.payload;
        },
        reset: (state, action) => {
            let path = action.payload?.split('?')[0];
            let params = new URLSearchParams(action.payload?.split('?')[1] ?? window.location.search);
            let query = params.get("query");
            let flags = parseInt(params.get("flags"));
            let values = parseInt(params.get("values"));
            let recursive = params.get("recursive");
            state.query = query ?? initialState.query;
            state.path = path ?? initialState.path;
            if (flags && values) {
                if (flags & (1 << 1)) {
                    state.favorite = ((values & (1 << 1)) !== 0);
                }
            } else {
                state.favorite = initialState.favorite;
            }
            if (recursive) {
                state.recursive = (recursive === "true");
            } else {
                state.recursive = initialState.recursive;
            }
        },
    },
});

const { actions, reducer } = searchSlice;
export const { setPath, setQuery, setFavorite, setRecursive, reset } = actions;
export default reducer;
