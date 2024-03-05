import { createSlice } from '@reduxjs/toolkit';

const initialState = {
    path: "",
    query: "",
    favorite: false,
    recursive: true,
    sort: {
        fields: [
            { id: "1",  name: "Name",           value: "name",              active: false },
            { id: "2",  name: "Type",           value: "type extension",    active: false },
            { id: "3",  name: "Size",           value: "size",              active: false },
            { id: "4",  name: "Views",          value: "views",             active: false },
            { id: "5",  name: "Duration",       value: "duration",          active: false },
            { id: "6",  name: "Date Added",     value: "dateAdded",         active: false },
            { id: "7",  name: "Date Created",   value: "dateCreated",       active: false },
            { id: "8",  name: "Date Accessed",  value: "dateAccessed",      active: false },
            { id: "9",  name: "Date Modified",  value: "dateModified",      active: false },
            { id: "10", name: "Date Taken",     value: "dateTaken",         active: false },
        ],
        direction: "asc",
    }
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
        setSort: (state, action) => {
            const path = action.payload?.path;
            state.sort.fields = action.payload?.fields ? action.payload.fields : initialState.sort.fields;
            state.sort.direction = action.payload?.direction ? action.payload.direction : initialState.sort.direction;
            if (path) {
                state.sort[path] = {};
                state.sort[path].fields = action.payload?.fields ? action.payload.fields : initialState.sort.fields;
                state.sort[path].direction = action.payload?.direction ? action.payload.direction : initialState.sort.direction;
            }
        },
        setSortFields: (state, action) => {
            state.sort.fields = action.payload ? action.payload : initialState.sort.fields;
        },
        setSortDirection: (state, action) => {
            state.sort.direction = action.payload ? action.payload : initialState.sort.direction;
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
export const { setPath, setQuery, setFavorite, setRecursive, setSort, setSortFields, setSortDirection, reset } = actions;
export default reducer;
