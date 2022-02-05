import { createSlice } from '@reduxjs/toolkit';

const initialState = {
    theme: "light",
};

export const uiSlice = createSlice({
    name: 'ui',
    initialState: initialState,
    reducers: {
        setTheme: (state, action) => {
            if (document.documentElement.classList.contains(state.theme)) {
                document.documentElement.classList.remove(state.theme);
            }
            state.theme = action.payload ?? initialState.theme;
            if (!document.documentElement.classList.contains(state.theme)) {
                document.documentElement.className += ` ${state.theme}`;
            }
        },
    },
});

const { actions, reducer } = uiSlice;
export const { setTheme } = actions;
export default reducer;
