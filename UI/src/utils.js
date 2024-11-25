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

export function updateBit(number, bit, value) {
    return (((~(1 << bit)) & number) | ((value ? 1 : 0) << bit));
}

export function clone(object, shallow = false) {
    if (shallow) {
        Object.assign(Array.isArray(object) ? [] : {}, object);
    } else {
        return JSON.parse(JSON.stringify(object));
    }
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
