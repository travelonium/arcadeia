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

import { createSelector } from '@reduxjs/toolkit';

const items = (state) => state.ui.uploads.items;
const progress = (state) => state.ui.uploads.progress;

export const selectAll = createSelector(
    [items], (uploads) => {
        console.debug('selectAll()');
        return uploads;
    }
);

export const selectQueued = createSelector(
    [selectAll], (uploads) => {
        console.debug('selectQueued()');
        return uploads.filter((item) => item.state === 'queued');
    }
);

export const selectActive = createSelector(
    [selectAll], (uploads) => {
        console.debug('selectActive()');
        return uploads.filter((item) => item.state === 'active');
    }
);

export const selectFailed = createSelector(
    [selectAll], (uploads) => {
        console.debug('selectFailed()');
        return uploads.filter((item) => item.state === 'failed');
    }
);

export const selectSucceeded = createSelector(
    [selectAll], (uploads) => {
        console.debug('selectSucceeded()');
        return uploads.filter((item) => item.state === 'succeeded');
    }
);

export const selectProgress = createSelector(
    [progress], (progress) => {
        console.debug('selectProgress()');
        return progress;
    }
);
