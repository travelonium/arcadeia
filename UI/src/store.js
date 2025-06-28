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

import { combineReducers, configureStore } from '@reduxjs/toolkit';
import {
    persistStore,
    persistReducer,
    FLUSH,
    REHYDRATE,
    PAUSE,
    PERSIST,
    PURGE,
    REGISTER,
} from 'redux-persist';
import { thunk } from 'redux-thunk'
import uiReducer from './features/ui/slice';
import storage from 'redux-persist/lib/storage';
import searchReducer from './features/search/slice';
import uiInitialState from './features/ui/initialState';
import settingsReducer from './features/settings/slice';
import createMigrate from 'redux-persist/es/createMigrate';
import searchInitialState from './features/search/initialState';

const persistConfig = {
    key: 'root',
    blacklist: ['search', 'settings'],
    version: 8,
    storage,
    migrate: createMigrate({
        2: (state) => {
            return {
                ...state,
                ui: {
                    ...state.ui,
                    history: uiInitialState.history,
                }
            };
        },
        4: (state) => {
            return {
                ...state,
                ui: {
                    ...state.ui,
                    view: {
                        default: state?.view ?? uiInitialState.view.default,
                    }
                }
            };
        },
        5: (state) => {
            return {
                ...state,
                ui: {
                    ...state.ui,
                    uploads: {
                        ...state.ui.uploads,
                        total: state?.ui?.uploads?.total ?? uiInitialState.uploads.total,
                        queued: state?.ui?.uploads?.queued ?? uiInitialState.uploads.queued,
                        active: state?.ui?.uploads?.active ?? uiInitialState.uploads.active,
                        failed: state?.ui?.uploads?.failed ?? uiInitialState.uploads.failed,
                        succeeded: state?.ui?.uploads?.succeeded ?? uiInitialState.uploads.succeeded,
                    }
                }
            };
        },
        6: (state) => {
            if (!state.ui?.uploads) return state;
            const { queued = [], failed = [], succeeded = [] } = state.ui.uploads;
            return {
                ...state,
                ui: {
                    ...state.ui,
                    uploads: {
                        ...state.ui.uploads,
                        queued: undefined,
                        active: undefined,
                        failed: undefined,
                        succeeded: undefined,
                        items: {
                            ...queued.reduce((acc, item) => {
                                acc[item.key] = { ...item, state: 'queued' };
                                return acc;
                            }, {}),
                            ...failed.reduce((acc, item) => {
                                acc[item.key] = { ...item, state: 'failed' };
                                return acc;
                            }, {}),
                            ...succeeded.reduce((acc, item) => {
                                acc[item.key] = { ...item, state: 'succeeded' };
                                return acc;
                            }, {})
                        },
                    }
                }
            };
        },
        7: (state) => {
            if (!state.ui?.uploads) return state;
            return {
                ...state,
                ui: {
                    ...state.ui,
                    uploads: {
                        ...state.ui.uploads,
                        progress: {}
                    }
                }
            }
        },
        8: (state) => {
            return {
                ...state,
                ui: {
                    ...state.ui,
                    uploads: {
                        ...state.ui.uploads,
                        items: Object.values(state.ui.uploads.items).sort((a, b) => b.timestamp - a.timestamp),
                        progress: {}
                    }
                }
            };
        }
    }, { debug: false })
};

const searchPersistConfig = {
    key: 'search',
    blacklist: ['path', 'query'],
    version: 0,
    storage,
    migrate: createMigrate({
        0: (state) => {
            return {
                ...state,
                sort: searchInitialState.sort,
            };
        }
    }, { debug: false })
};

const persistedReducer = persistReducer(persistConfig, combineReducers({
    ui: uiReducer,
    search: persistReducer(searchPersistConfig, searchReducer),
    settings: settingsReducer,
}));

export const store = configureStore({
    reducer: persistedReducer,
    middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware({
        serializableCheck: {
            ignoredActions: [FLUSH, REHYDRATE, PAUSE, PERSIST, PURGE, REGISTER, 'ui/queueUpload'],
            ignoredPaths: ['ui.uploads.items'],
        },
    }).concat(thunk),
});

export const persistor = persistStore(store);
