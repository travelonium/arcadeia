import { createSlice } from '@reduxjs/toolkit';

const initialState = {
    theme: "light",
    scrollPosition: {},
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
        setScrollPosition: (state, action) => {
            state.scrollPosition[action.payload.path] = action.payload.index;
        },
    },
});

const { actions, reducer } = uiSlice;
export const { setTheme, setScrollPosition } = actions;
export default reducer;
