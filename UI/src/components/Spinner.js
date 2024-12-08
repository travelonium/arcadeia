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

import React from 'react';
import cx from 'classnames';

const Spinner = ({ className, ...props }) => {
  return (
    <svg className={cx("spinner-svg", className)} {...props} width="50" height="50" xmlnsXlink="http://www.w3.org/1999/xlink">
        <use xlinkHref="#logo-emblem"></use>
    </svg>
  );
};

export default Spinner;