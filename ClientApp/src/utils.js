export function extract(fallback, obj, level, ...rest) {
    if (obj === undefined) return fallback
    if (rest.length === 0 && obj.hasOwnProperty(level)) return obj[level]
    return extract(fallback, obj[level], ...rest)
}