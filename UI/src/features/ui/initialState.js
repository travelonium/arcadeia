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
    view: {
        default: "card",
    },
    theme: "light",
    scrollPosition: {},
    uploads: {
        items: {
        /*
            [key]: {
                state: 'queued' | 'active' | 'failed' | 'succeeded'
                key: a unique key identifying the upload item in the dictionary
                name: the specified or designated file name of the uploaded item
                url: the URL of the upload item when the dropped item is a URL
                file: the File object of the upload item when the dropped item is file
                path: the destination path where the item is to be uploaded to
                toast: the toast id related to the active upload item,
                status: the last updated status of the upload item,
                error: the last reported error message,
                timestamp: unix epoch when the item was inserted or changed state
            }
        */
        },
        progress: {
        /*
            [key]: {
                value: the progress of the active upload item,
                timestamp: unix epoch when the progress was updated
            }
        */
        },
        simultaneous: 4,
        duplicate: true,
        overwrite: false,
        reupload: false,
    },
    history: {
        items: 100,
    }
};

export default initialState;
