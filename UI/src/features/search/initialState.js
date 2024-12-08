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

const initialState = {
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
