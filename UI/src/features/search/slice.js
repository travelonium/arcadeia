import { createSlice } from '@reduxjs/toolkit';
import initialState from './initialState';

export const searchSlice = createSlice({
    name: 'search',
    initialState: initialState,
    reducers: {
        setPath: (state, action) => {
            return {
                ...state,
                path: action.payload,
                query: initialState.query,
            }
        },
        setQuery: (state, action) => {
            return {
                ...state,
                query: action.payload,
            }
        },
        setFavorite: (state, action) => {
            return {
                ...state,
                favorite: action.payload,
            }
        },
        setRecursive: (state, action) => {
            return {
                ...state,
                recursive: action.payload,
            }
        },
        setSort: (state, action) => {
            let newState = {
                ...state,
                sort: {
                    ...state.sort,
                    fields: action.payload?.fields ? action.payload.fields : initialState.sort.fields,
                    direction: action.payload?.direction ? action.payload.direction : initialState.sort.direction,
                }
            }
            const path = action.payload?.path;
            if (path) {
                newState.sort[path] = {};
                newState.sort[path].fields = action.payload?.fields ? action.payload.fields : initialState.sort.fields;
                newState.sort[path].direction = action.payload?.direction ? action.payload.direction : initialState.sort.direction;
            }
            return newState;
        },
        resetSort: (state, action) => {
            const path = action.payload?.path;
            let newState = {
                ...state,
                sort: {
                    ...state.sort,
                }
            };
            if (path && newState.sort.hasOwnProperty(path)) {
                delete newState.sort[path];
            } else {
                newState.sort.fields = action.payload?.fields ? action.payload.fields : initialState.sort.fields;
                newState.sort.direction = action.payload?.direction ? action.payload.direction : initialState.sort.direction;
            }
            return newState;
        },
        setSortFields: (state, action) => {
            return {
                ...state,
                sort: {
                    ...state.sort,
                    fields: action.payload ? action.payload : initialState.sort.fields,
                }
            }
        },
        setSortDirection: (state, action) => {
            return {
                ...state,
                sort: {
                    ...state.sort,
                    direction: action.payload ? action.payload : initialState.sort.direction,
                }
            }
        },
        reset: (state, action) => {
            let path = action.payload?.split('?')[0];
            let params = new URLSearchParams(action.payload?.split('?')[1] ?? window.location.search);
            let query = params.get("query");
            let flags = parseInt(params.get("flags"));
            let values = parseInt(params.get("values"));
            let recursive = params.get("recursive");
            let newState = {
                ...state,
                query: query ?? initialState.query,
                path: path ?? initialState.path
            };
            if (flags && values) {
                if (flags & (1 << 1)) {
                    newState.favorite = ((values & (1 << 1)) !== 0);
                }
            } else {
                newState.favorite = initialState.favorite;
            }
            if (recursive) {
                newState.recursive = (recursive === "true");
            } else {
                newState.recursive = initialState.recursive;
            }
            return newState;
        },
    },
});

const { actions, reducer } = searchSlice;
export const { setPath, setQuery, setFavorite, setRecursive, setSort, resetSort, setSortFields, setSortDirection, reset } = actions;
export default reducer;
