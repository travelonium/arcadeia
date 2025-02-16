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
import { clone as cloneShallow, cloneDeep, isEqual } from 'lodash';
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

export function size(bytes, decimals = 2, prefix = '', suffix = '', zero = false) {
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
        // skip null or undefined values
        if (value === null || value === undefined) continue;

        // construct a new key for nested dictionaries
        const newKey = parentKey ? `${parentKey}.${key}` : key;

        if (typeof value === 'object' && value !== null && !Array.isArray(value)) {
            // recursively handle nested objects
            querify(value, newKey, query);
        } else if (Array.isArray(value)) {
            // handle arrays
            for (const item of value) {
                query.append(newKey, item);
            }
        } else {
            // handle single values
            query.set(newKey, value);
        }
    }
    return query;
}

export function shorten(input, length) {
    if (input.length <= length) return input;

    const ellipsis = "...";
    const isURL = /^(https?:\/\/)/.test(input);

    let protocol = "";
    let domain = "";
    let path = "";
    let fileName = input;

    if (isURL) {
        // handle URLs
        const matches = input.match(/^(https?:\/\/)([^/]+)(\/?.*?)($|\?)/);
        if (matches) {
            protocol = matches[1]; // e.g., "http://"
            domain = matches[2];   // e.g., "example.com"
            path = matches[3];     // e.g., "/path/to/resource"
            const parts = path.split("/");
            fileName = parts.pop(); // extract file name or last path segment
            path = parts.join("/") + (parts.length ? "/" : "");
        }
    } else if (input.includes("/")) {
        // handle file paths
        const parts = input.split("/");
        fileName = parts.pop();
        path = parts.join("/") + "/";
    }

    // calculate the base length (protocol + domain + ellipsis if there's a path + file name)
    const baseLength = protocol.length + domain.length + (path ? ellipsis.length : 0) + fileName.length;

    if (baseLength > length) {
        // if the file name alone exceeds the length, truncate the file name
        const extIndex = fileName.lastIndexOf(".");
        const ext = extIndex !== -1 ? fileName.substring(extIndex) : "";
        const namePart = fileName.substring(0, fileName.length - ext.length);
        const truncatedName = namePart.substring(0, length - ellipsis.length - ext.length - 1) + "…" + ext;
        return protocol + domain + truncatedName;
    }

    // calculate the remaining length for the path
    const remainingLength = length - baseLength;
    let shortenedPath = path;

    if (path.length > remainingLength) {
        // ensure truncation doesn't remove trailing slash after directories
        const parts = path.split("/");
        let totalLength = 0;
        shortenedPath = "";

        for (let i = 0; i < parts.length; i++) {
            const part = parts[i];
            const partWithSlash = part + "/";
            if (totalLength + partWithSlash.length > remainingLength) {
                shortenedPath += ellipsis;
                break;
            }
            shortenedPath += partWithSlash;
            totalLength += partWithSlash.length;
        }
    }

    return protocol + domain + shortenedPath + fileName;
}

/**
 * Recursively remove a nested path from an object.
 * Returns a new (partially) cloned object without mutating the original.
 *
 * @param {Object} object - The source object.
 * @param {string[]} path - An array representing the nested path (e.g. ["ui", "someKey"]).
 * @returns {Object} A new object that excludes the nested path.
 */
function removeNestedPath(object, path) {
    if (!object || typeof object !== 'object') {
        // if it's not an object/array, just return it unchanged.
        return object;
    }

    // if the path is empty, there's nothing to remove.
    if (path.length === 0) {
        return object;
    }

    const [currentKey, ...restPath] = path;

    // if the current key isn't in the object, no removal needed.
    if (!(currentKey in object)) {
        return object;
    }

    // shallow clone so we don't mutate original
    let cloned = Array.isArray(object) ? object.slice() : { ...object };

    // if we are on the last part of the path, remove the key
    if (restPath.length === 0) {
        if (Array.isArray(cloned)) {
            // if it's an array, parse the key as a numeric index if possible
            const index = Number(currentKey);
            if (!Number.isNaN(index)) {
                cloned.splice(index, 1);
            }
        } else {
            delete cloned[currentKey];
        }
    } else {
        // otherwise, recurse deeper
        cloned[currentKey] = removeNestedPath(cloned[currentKey], restPath);
    }

    return cloned;
}

/**
 * Omit an array of dotted paths from an object.
 * Each path can be a single key ("name") or nested ("ui.someKey").
 *
 * @param {Object|Array} object - The source object
 * @param {string[]} paths - Array of dotted strings, e.g. ["ui.someKey", "items.0.price"]
 * @returns {Object|Array} A new object/array excluding the specified nested paths.
 */
function omitNested(object, paths) {
    let result = object;
    for (const dottedPath of paths) {
        const pathArray = dottedPath.split('.');
        result = removeNestedPath(result, pathArray);
    }
    return result;
}

/**
 * Compares two objects/arrays excluding the supplied root or nested keys.
 *
 * @param {*} value - The left-hand side object/array.
 * @param {*} other - The right-hand side object/array.
 * @param {...(string|string[])} exclusions - Root or nested keys to exclude.
 *        Can be multiple dotted strings or an array of them.
 *        E.g.: "ui.someKey", ["ui.anotherKey", "items.0.price"], etc.
 *
 * @returns {boolean} `true` if objects are equal (after exclusions), otherwise `false`.
 */
export function isEqualExcluding(value, other) {
    // collect exclusions from either an array or rest parameters
    const exclusions = Array.isArray(arguments[2])
        ? arguments[2]
        : Array.prototype.slice.call(arguments, 2);

    const newValue = omitNested(value, exclusions);
    const newOther = omitNested(other, exclusions);

    return isEqual(newValue, newOther);
}

/**
 * Returns the differences between two objects (or arrays) excluding the supplied
 * root or nested keys from comparison.
 *
 * @param {*} lhs - The left-hand side object/array.
 * @param {*} rhs - The right-hand side object/array.
 * @param {...(string|string[])} exclusions - Keys (root or nested) to exclude.
 *        Can be multiple dotted strings or an array of them:
 *        e.g. "ui.someKey", ["ui.anotherKey", "items.0.price"], etc.
 *
 * @returns {Object} A dictionary with all the different keys and values.
 */
export function differenceWith(lhs, rhs) {
    // collect exclusions from either an array or rest parameters
    const exclusions = Array.isArray(arguments[2])
        ? arguments[2]
        : Array.prototype.slice.call(arguments, 2);

    // remove excluded nested paths (does not mutate original)
    const lhsClean = omitNested(lhs, exclusions);
    const rhsClean = omitNested(rhs, exclusions);

    // recursively compute differences
    function computeDifference(a, b) {
        const differences = {};

        // compare keys from 'a'
        for (const key in a) {
            if (!(key in b)) {
                differences[key] = { from: a[key], to: undefined };
            } else if (
                typeof a[key] === 'object' &&
                a[key] !== null &&
                typeof b[key] === 'object' &&
                b[key] !== null
            ) {
                // recursively compare nested objects/arrays
                const nestedDiff = computeDifference(a[key], b[key]);
                if (Object.keys(nestedDiff).length > 0) {
                    differences[key] = nestedDiff;
                }
            } else if (a[key] !== b[key]) {
                differences[key] = { from: a[key], to: b[key] };
            }
        }

        // compare keys from 'b' that weren't in 'a'
        for (const key in b) {
            if (!(key in a)) {
                differences[key] = { from: undefined, to: b[key] };
            }
        }

        return differences;
    }

    return computeDifference(lhsClean, rhsClean);
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
