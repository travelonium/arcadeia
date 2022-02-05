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
            let path = action.payload;
            let params = new URLSearchParams(window.location.search);
            let query = params.get("query");
            let flags = parseInt(params.get("flags"));
            let values = parseInt(params.get("values"));
            let recursive = params.get("recursive");
            if (query) {
                state.query = query;
            }
            if (path) {
                state.path = path;
            }
            if (flags && values) {
                if (flags & (1 << 1)) {
                    state.favorite = ((values & (1 << 1)) !== 0);
                }
            }
            if (recursive) {
                state.recursive = (recursive === "true");
            }
        },
    },
});

const { actions, reducer } = searchSlice;
export const { setPath, setQuery, setFavorite, setRecursive, reset } = actions;
export default reducer;
