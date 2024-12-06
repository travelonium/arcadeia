import initialState from './initialState';
import { createSlice } from '@reduxjs/toolkit';

export const searchSlice = createSlice({
    name: 'search',
    initialState: initialState,
    reducers: {
        setSort: (state, action) => {
            const path = action.payload?.path;
            const search = path === "search";
            const newState = {
                ...state,
                sort: {
                    ...state.sort,
                    // don't store the sort settings as default when searching
                    fields: (search ? state.sort.fields : action.payload?.fields) ?? initialState.sort.fields,
                    direction: (search ? state.sort.direction : action.payload?.direction) ?? initialState.sort.direction,
                }
            }
            if (path) {
                newState.sort[path] = {};
                newState.sort[path].fields = action.payload?.fields ? action.payload.fields : initialState.sort.fields;
                newState.sort[path].direction = action.payload?.direction ? action.payload.direction : initialState.sort.direction;
            }
            return newState;
        },
        resetSort: (state, action) => {
            const path = action.payload?.path;
            const newState = {
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
    },
});

const { actions, reducer } = searchSlice;
export const { setSort, resetSort, setSortFields, setSortDirection } = actions;
export default reducer;
