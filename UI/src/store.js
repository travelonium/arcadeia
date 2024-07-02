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
import storage from 'redux-persist/lib/storage';
import searchReducer from './features/search/slice';
import searchInitialState from './features/search/initialState';
import uiReducer from './features/ui/slice';
import uiInitialState from './features/ui/initialState';
import createMigrate from 'redux-persist/es/createMigrate';

const persistConfig = {
    key: 'root',
    blacklist: ['search'],
    version: 4,
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
    search: persistReducer(searchPersistConfig, searchReducer)
}));

export const store = configureStore({
    reducer: persistedReducer,
    middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware({
        serializableCheck: {
            ignoredActions: [FLUSH, REHYDRATE, PAUSE, PERSIST, PURGE, REGISTER],
        },
    }),
});

export const persistor = persistStore(store);
