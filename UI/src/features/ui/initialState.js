const initialState = {
    view: {
        default: "card",
    },
    theme: "light",
    scrollPosition: {},
    uploads: {
        simultaneous: 4,
        duplicate: true,
        overwrite: false,
    },
    history: {
        items: 100,
    }
};

export default initialState;
