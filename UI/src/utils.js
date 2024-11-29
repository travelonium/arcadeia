import React from 'react';
import { clone as cloneShallow, cloneDeep, isEqual, omit } from 'lodash';
import { useNavigate, useNavigationType, useLocation, useParams, useSearchParams } from 'react-router';

export function extract(fallback, obj, level, ...rest) {
    if ((obj === undefined) || (obj === null)) return fallback;
    if (rest.length === 0 && obj.hasOwnProperty(level)) return obj[level];
    return extract(fallback, obj[level], ...rest);
}

export function duration(seconds) {
    let date = new Date(seconds * 1000);

    let h = date.getUTCHours();
    let m = date.getUTCMinutes();
    let s = date.getSeconds();

    let string = "";

    if (seconds >= 3600) {
        string += h.toString().padStart(2, '0') + ':';
    }

    string += m.toString().padStart(2, '0') + ':' + s.toString().padStart(2, '0');

    return string;
}

export function size(bytes, decimals = 2, prefix='', suffix='', zero=false) {
    if (bytes === 0) return zero ? (prefix + '0 Bytes' + suffix) : '';
    const k = 1024;
    const dm = decimals < 0 ? 0 : decimals;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return prefix + parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i] + suffix;
}

export function breakpoint() {
    let breakpoints = {
        '(min-width: 1200px)': 'xl',                            // Extra large devices (large desktops)
        '(min-width: 992px) and (max-width: 1199.98px)': 'lg',  // Large devices (desktops, less than 1200px)
        '(min-width: 768px) and (max-width: 991.98px)': 'md',   // Medium devices (tablets, less than 992px)
        '(min-width: 576px) and (max-width: 767.98px)': 'sm',   // Small devices (landscape phones, less than 768px)
        '(max-width: 575.98px)': 'xs',                          // Extra small devices (portrait phones, less than 576px)
    }

    for (let media in breakpoints) {
        if (window.matchMedia(media).matches) {
            return breakpoints[media];
        }
    }

    return null;
}

export function getBit(number, bit) {
    return (number & (1 << bit)) ? 1 : 0;
}

export function setBit(number, bit, value) {
    return (((~(1 << bit)) & number) | ((value ? 1 : 0) << bit));
}

export function getFlag(flags, values, bit) {
    return getBit(flags, bit) ? (getBit(values, bit) ? true : false) : null;
}

export function setFlag(flags, values, bit, value) {
    return [setBit(flags, bit, value), setBit(values, bit, value)];
}

export function clone(object, shallow = false) {
    return shallow ? cloneShallow(object) : cloneDeep(object);
}

export function querify(dictionary, parentKey = '', query = new URLSearchParams()) {
    for (const key in dictionary) {
        const value = dictionary[key];
        // Skip null or undefined values
        if (value === null || value === undefined) continue;

        // Construct a new key for nested dictionaries
        const newKey = parentKey ? `${parentKey}.${key}` : key;

        if (typeof value === 'object' && value !== null && !Array.isArray(value)) {
            // Recursively handle nested objects
            querify(value, newKey, query);
        } else if (Array.isArray(value)) {
            // Handle arrays
            for (const item of value) {
                query.append(newKey, item);
            }
        } else {
            // Handle single values
            query.set(newKey, value);
        }
    }
    return query;
}

/**
 * Compares two objects excluding the supplied root keys from comparison.
 * @param {*} value The left hand side object.
 * @param {*} other The right hand side object.
 * @param {*} exclusions The root keys to exclude from comparison either as arguments or an array.
 * @returns true if objects are equal and false otherwise.
 */
export function isEqualExcluding(value, other) {
    const exclusions = Array.isArray(arguments[2]) ? arguments[2] : Array.prototype.slice.call(arguments, 2);
    return isEqual(omit(value, exclusions), omit(other, exclusions));
}

/**
 * Returns the differences between two objects excluding the supplied root keys from comparison.
 * @param {*} lhs The left hand side object.
 * @param {*} rhs The right hand side object.
 * @param {*} exclusions The root keys to exclude from comparison either as arguments or an array.
 * @returns A dictionary with all the different keys and values.
 */
export function differenceWith(lhs, rhs) {
    const differences = {};
    const exclusions = Array.isArray(arguments[2]) ? arguments[2] : Array.prototype.slice.call(arguments, 2);
    if (exclusions.length) {
        lhs = omit(lhs, exclusions);
        rhs = omit(rhs, exclusions);
    }
    for (const key in lhs) {
        if (!(key in rhs)) {
            differences[key] = { from: lhs[key], to: undefined };
        } else if (typeof lhs[key] === "object" && lhs[key] !== null && typeof rhs[key] === "object" && rhs[key] !== null) {
            const nestedDiff = differenceWith(lhs[key], rhs[key]);
            if (Object.keys(nestedDiff).length > 0) {
                differences[key] = nestedDiff;
            }
        } else if (lhs[key] !== rhs[key]) {
            differences[key] = { from: lhs[key], to: rhs[key] };
        }
    }

    for (const key in rhs) {
        if (!(key in lhs)) {
            differences[key] = { from: undefined, to: rhs[key] };
        }
    }

    return differences;
}

export async function getSolrUrl() {
    return fetch("/api/settings", {
        method: "GET",
        headers: {
            accept: "application/json",
        }
    })
    .then((response) => {
        if (response.ok) {
            return response.json();
        } else {
            return response.json().then((error) => {
                throw new Error(error.message ?? error.detail);
            });
        }
    }).then(settings => {
        return extract(null, settings, "Solr", "URL");
    });
}

export function withRouter(Component) {
    return React.forwardRef((props, ref) => {
        const params = useParams();     // gets route parameters
        const location = useLocation(); // for navigation (replaces history.push)
        const navigate = useNavigate(); // provides the current location object
        const navigationType = useNavigationType(); // the navigation type (POP, PUSH, or REPLACE)
        const [searchParams, setSearchParams] = useSearchParams();

        return (
            <Component
                ref={ref}
                {...props}
                params={params}
                location={location}
                navigate={navigate}
                searchParams={searchParams}
                navigationType={navigationType}
                setSearchParams={setSearchParams}
            />
        );
    });
}
