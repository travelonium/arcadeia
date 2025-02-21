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

export const selectAll = createSelector(
    [items], (uploads) => Object.values(uploads).sort((a, b) => b.timestamp - a.timestamp)
);

export const selectQueued = createSelector(
    [selectAll], (uploads) => uploads.filter((item) => item.state === 'queued')
);

export const selectActive = createSelector(
    [selectAll], (uploads) => uploads.filter((item) => item.state === 'active')
);

export const selectFailed = createSelector(
    [selectAll], (uploads) => uploads.filter((item) => item.state === 'failed')
);

export const selectSucceeded = createSelector(
    [selectAll], (uploads) => uploads.filter((item) => item.state === 'succeeded')
);
