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