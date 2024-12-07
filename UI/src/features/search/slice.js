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
