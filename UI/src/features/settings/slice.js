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
import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';

export const readSettings = createAsyncThunk(
    "/api/settings/get",
    async (_, thunkAPI) => {
        const response = await fetch("/api/settings", {
            method: "GET",
            headers: {
                accept: "application/json",
            }
        });
        if (!response.ok) {
            const error = await response.json();
            return thunkAPI.rejectWithValue(error.message ?? error.detail ?? error.title);
        }
        return await response.json();
    }
);

export const writeSettings = createAsyncThunk(
    "/api/settings/post",
    async (updates, thunkAPI) => {
        const response = await fetch("/api/settings", {
            method: "POST",
            headers: {
                accept: "application/json",
                "Content-Type": "application/json",
            },
            body: JSON.stringify(updates)
        });
        if (!response.ok) {
            const error = await response.json();
            return thunkAPI.rejectWithValue(error.message ?? error.detail ?? error.title);
        }
        return await thunkAPI.dispatch(readSettings());
    }
);

export const settingsSlice = createSlice({
    name: 'settings',
    initialState: initialState,
    reducers: {},
    extraReducers: (builder) => {
        builder
        // read settings
        .addCase(readSettings.pending, (state) => {
            state.error = null;
        })
        .addCase(readSettings.fulfilled, (state, action) => {
            state.current = action.payload;
        })
        .addCase(readSettings.rejected, (state, action) => {
            state.error = action.payload;
        })
        // write settings
        .addCase(writeSettings.pending, (state) => {
            state.error = null;
        })
        .addCase(writeSettings.rejected, (state, action) => {
            state.error = action.payload;
        });
    }
});

const { _, reducer } = settingsSlice;
export default reducer;
