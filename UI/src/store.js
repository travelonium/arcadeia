import { configureStore } from '@reduxjs/toolkit';
import searchReducer from './features/search/slice';
import uiReducer from './features/ui/slice';

export const store = configureStore({
    reducer: {
        ui: uiReducer,
        search: searchReducer,
    },
});
