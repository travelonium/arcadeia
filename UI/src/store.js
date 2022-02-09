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
import uiReducer from './features/ui/slice';

const persistConfig = {
    key: 'root',
    blacklist: ['search'],
    version: 1,
    storage,
};

const searchPersistConfig = {
    key: 'search',
    blacklist: ['path', 'query'],
    storage,
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
