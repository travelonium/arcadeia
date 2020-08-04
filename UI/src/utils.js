export function extract(fallback, obj, level, ...rest) {
    if (obj === undefined) return fallback
    if (rest.length === 0 && obj.hasOwnProperty(level)) return obj[level]
    return extract(fallback, obj[level], ...rest)
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