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

export default initialState;
