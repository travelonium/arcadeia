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

import cx from 'classnames';
import Spinner from './Spinner';
import Row from 'react-bootstrap/Row';
import Col from 'react-bootstrap/Col';
import UploadZone from './UploadZone';
import { connect } from "react-redux";
import { toast } from 'react-toastify';
import MediaViewer from './MediaViewer';
import Uploads from './toolbar/Uploads';
import update from 'immutability-helper';
import React, { Component } from 'react';
import { isEqual, isEmpty } from 'lodash';
import Button from 'react-bootstrap/Button';
import { MediaContainer } from './MediaContainer';
import Container from 'react-bootstrap/Container';
import Breadcrumb from 'react-bootstrap/Breadcrumb';
import AutoSizer from 'react-virtualized-auto-sizer';
import { FixedSizeGrid as Grid } from 'react-window';
import { setScrollPosition, updateUpload } from '../features/ui/slice';
import { clone, extract, size, querify, withRouter, isEqualExcluding, differenceWith, getFlag } from '../utils';

class Library extends Component {

    static displayName = Library.name;

    constructor(props) {
        super(props);
        this.items = [];            // temporary storage for the state.items while the full results are being retrieved
        this.current = -1;          // the current item index being viewed in the MediaViewer
        this.viewing = false;       // indicates that a media is being viewed in the MediaViewer
        this.editing = false;       // indicates that text editing is in progress and should inhibit low level keyboard input capturing
        this.mediaContainers = {};  // stores the refs to MediaContainers with keys corresponding to indexes
        this.grid = React.createRef();
        this.uploads = React.createRef();
        this.uploadZone = React.createRef();
        this.gridWrapper = React.createRef();
        this.mediaViewer = React.createRef();
        this.controller = new AbortController();
        this.scrollToTopButton = React.createRef();
        this.ignoreScrollUpdateWasRequested = false;
        this.storeScrollPositionTimeout = null;
        this.state = {
            loading: false,
            status: "",
            items: null,    // a null value indicates that no search has yet been performed
            options: {
                videoPlayer: {
                    seekStep: {
                        small: 15,
                        medium: 30,
                        big: 60
                    },
                    volumeStep: 0.1
                },
                photoViewer: {
                    zoom: {
                        ratio: 0.1,
                        hasTooltip: true,
                    },
                }
            }
        };
    }

    get pathname() {
        try {
            return decodeURI(this.props?.location?.pathname || "");
        } catch (error) {
            console.error('Error decoding the URI:', error);
            return null;
        }
    }

    get path() {
        return this.getPath(this.props?.location?.pathname);
    }

    get name() {
        return this.getName(this.props?.location?.pathname)
    }

    get query() {
        return this.props.searchParams.get('query');
    }

    get favorite() {
        return this.getFlag(1);
    }

    get recursive() {
        return this.props.searchParams.get("recursive") === "true";
    }

    get searching() {
        return this.props.searchParams.get('query') ? true : false;
    }

    componentDidMount() {
        console.debug("componentDidMount()");
        // install the event handler for keydown keyboard events
        document.addEventListener('keydown', this.onKeyDown.bind(this));
        // perform a refresh on load
        this.refresh((succeeded) => {
            const name = this.name;
            if (succeeded) {
                const index = this.state.items.findIndex(x => x.name === name);
                if (index !== -1) {
                    this.view(this.state.items[index], index);
                }
            }
        })
    }

    componentWillUnmount() {
        console.debug("componentWillUnmount()");
        // remove the installed event handler
        document.removeEventListener('keydown', this.onKeyDown.bind(this));
    }

    shouldComponentUpdate(nextProps, nextState) {
        const currentProps = this.props;
        const currentState = this.state;

        const nextPath = this.getPath(nextProps?.location?.pathname);
        const nextName = this.getName(nextProps?.location?.pathname);
        const currentPath = this.getPath(currentProps?.location?.pathname);
        const currentName = this.getName(currentProps?.location?.pathname);

        // was it the onMediaViewerShow or onMediaViewerHide that caused the update?
        const wasMediaViewerShow = (currentPath != null && nextPath != null) && (currentName == null && nextName != null);
        const wasMediaViewerHide = (currentPath != null && nextPath != null) && (currentName != null && nextName == null);

        // was this triggered by a browser back / forward?
        const wasBrowserBackOrForward = (nextProps?.navigationType === "POP");

        // check if significant differences exist in props or state, excluding specific keys
        const havePropsChanged = !isEqualExcluding(currentProps, nextProps, 'dispatch', 'ref', 'forwardedRef', 'ui.uploads', 'ui.scrollPosition', 'signalRConnection');
        const updatedProps = differenceWith(currentProps, nextProps, 'dispatch', 'ref', 'forwardedRef', 'ui.uploads', 'ui.scrollPosition', 'signalRConnection');
        const hasStateChanged = !isEqualExcluding(currentState, nextState);
        const updatedState = differenceWith(currentState, nextState);

        const shouldUpdate = ((!wasMediaViewerShow && !wasMediaViewerHide) || wasBrowserBackOrForward) && (havePropsChanged || hasStateChanged);

        if (shouldUpdate) console.group(`shouldComponentUpdate(${shouldUpdate})`); else console.groupCollapsed(`shouldComponentUpdate(${shouldUpdate})`);
        if (!isEmpty(updatedProps)) console.debug("Props: ", updatedProps);
        if (!isEmpty(updatedState)) console.debug("State: ", updatedState);
        console.groupEnd();

        return shouldUpdate;
    }

    componentDidUpdate(prevProps) {
        console.debug("componentDidUpdate()");
        const currentName = this.name;
        if (this.state.items == null ||
            !isEqualExcluding(prevProps.location, this.props.location, 'key') ||
            !isEqual(prevProps.search, this.props.search)) {
            if (currentName && this.state.items != null) {
                // the path has not changed since last update, no refresh necessary
                const index = this.state.items.findIndex(x => x.name === currentName);
                if (index !== -1) {
                    this.view(this.state.items[index], index);
                }
            } else {
                // hide the media viewer in case this is a history event
                this.mediaViewer.current.hide();

                // the path has changed since last update refresh and view
                this.refresh((succeeded) => {
                    if (succeeded && currentName) {
                        const index = this.state.items.findIndex(x => x.name === currentName);
                        if (index !== -1) {
                            this.view(this.state.items[index], index);
                        }
                    }
                })
            }
        }
    }

    getPath(pathname) {
        try {
            const decodedPath = decodeURI(pathname || "");
            const components = decodedPath.match(/(.*\/)(.*)?/);
            if (components) return components[1];
            return null;
        } catch (error) {
            console.error('Error decoding the URI:', error);
            return null;
        }
    }

    getName(pathname) {
        try {
            const decodedPath = decodeURI(pathname || "");
            const components = decodedPath.match(/(.*\/)(.*)?/);
            if (components) return components[2];
            return null;
        } catch (error) {
            console.error('Error decoding the URI', error);
            return null;
        }
    }

    getFlag(bit) {
        const flags = parseInt(this.props.searchParams.get("flags") ?? 0);
        const values = parseInt(this.props.searchParams.get("values") ?? 0);
        return getFlag(flags, values, bit) ?? false;
    }

    set(index, source, refresh = true, callback = undefined) {
        // update the state and the virtual copies of the source
        const items = update(this.state.items, {
            [index]: {$merge: source}
        });
        if (refresh) {
            this.setState({
                items: items
            }, () => {
                if (callback !== undefined) {
                    callback(this.state.items[index], true);
                }
            });
        } else {
            // eslint-disable-next-line
            this.state.items = items;
            this.mediaContainers[index]?.current?.set(source, (_) => {
                if (callback !== undefined) {
                    callback(this.state.items[index], true);
                }
            });
        }
    }

    unset(index, callback = undefined) {
        // update the state and the virtual copies of the source
        const items = update(this.state.items, {
            $unset: [index]
        });
        this.setState({
            items: items
        }, () => {
            if (callback !== undefined) {
                callback();
            }
        });
    }

    refresh(callback = undefined) {
        console.debug("refresh()");
        this.search(this.path, 0, (succeeded) => {
            if (callback !== undefined) {
                callback(succeeded);
            }
        });
    }

    /**
     * Update a MediaContainer with new attributes.
     * @param {*} source The modified MediaContainer to update.
     * @param {boolean} refresh Forces a state update and a refresh following the modifications.
     * @param {(source, succeeded) => void} callback The callback function to call with either the updated or unmodified source when the operation is thorough.
     */
    update(source, refresh = true, callback = undefined) {
        let item = clone({
            // Avoid "One or more validation errors occurred." errors
            flags: [], // Avoid "The Flags field is required."
            ...source
        });
        const index = this.state.items.findIndex(x => x.id === item.id);
        if (index === -1) {
            toast.error("Unable to find the item that was to be updated.");
            return;
        }
        fetch("/api/library" + item.fullPath, {
            method: "PATCH",
            headers: {
                accept: "application/json",
                "Content-Type": "application/json",
            },
            body: JSON.stringify(item)
        })
        .then((response) => {
            if (!response.ok) {
                return response.json().then((error) => {
                    throw new Error(error.message ?? error.detail);
                });
            } else {
                return response.json();
            }
        })
        .then((response) => {
            this.set(index, response, refresh, callback);
        })
        .catch(error => {
            console.error(error);
            toast.error(error.message);
            if (callback !== undefined) {
                callback(this.state.items[index], false);
            }
        });
    }

    retrieve(id, fullPath, callback = undefined) {
        if ((!id && !fullPath) || (id && fullPath)) throw new Error("Either id or fullPath needs to be provided.");
        const solrUrl = this.props.settings?.Solr?.URL;
        if (!solrUrl) return;
        const target = `${solrUrl}/select`;
        const input = {
            q: "*:*",
            fq: [
                id ? `id:'${id}'` : `fullPath:"${fullPath}"`
            ],
            "q.op": "AND",
            defType: "edismax",
            wt: "json",
        };
        fetch(target + "?" + querify(input).toString(), {
            credentials: 'include',
            headers: {
                'Accept': 'application/json',
            },
        })
        .then((response) => {
            if (response.ok) return response.json();
            let message = "Error querying the Solr index!";
            return response.text().then((data) => {
                try {
                    let json = JSON.parse(data);
                    let exception = extract(null, json, "error", "msg");
                    if (exception) message = exception;
                } catch (error) {}
                throw Error(message);
            });
        })
        .then((result) => {
            const numFound = result?.response?.numFound ?? 0;
            const docs = result?.response?.docs ?? [];
            if (callback !== undefined) {
                callback(docs);
            }
        })
        .catch(error => {
            console.error(error);
            if (callback !== undefined) {
                callback();
            }
        });
    }

    search(path = this.path, start = 0, callback = undefined) {
        const rows = 10000;
        const deleted = false;
        const searching = this.searching;
        const query = searching ? this.query : "*";
        const recursive = this.recursive;
        const favorite = this.favorite;
        const solrUrl = this.props.settings?.Solr?.URL;
        if (!solrUrl) return;
        const target = `${solrUrl}/select`;
        const sort = (searching ? this.props.search.sort['search'] : this.props.search.sort[path]) ?? this.props.search.sort;
        const input = {
            q: query,
            fq: [],
            rows: rows,
            start: start,
            fl: "*,children:[subquery],duplicates:[subquery]",
            "q.op": "AND",
            defType: "edismax",
            qf: "name_ngram^20 description_ngram^10 path_ngram^5",
            wt: "json",
            sort: this.sort(sort.fields, sort.direction),
            children: {
                q: "{!prefix f=path v=$row.fullPath}",
                fq: ["-type:Folder", "-type:Drive", "-type:Server"],
                sort: "views desc, dateAdded asc, name asc",
                rows: 3,
            },
            duplicates: {
                q: "{!term f=checksum v=$row.checksum}",
                start: 0,
                rows: 0,
            }
        };
        if (favorite) {
            input.fq.push("flags:Favorite");
        }
        if (deleted) {
            input.fq.push("flags:Deleted");
        } else {
            input.fq.push("-flags:Deleted");
        }
        if (!recursive || !searching) {
            input.fq.push("path:\"" + path.split('?')[0] + "\"");
        } else {
            input.fq.push("path:" + (path.split('?')[0]).replace(/([+\-!(){}[\]^"~*?:\\/ ])/g, "\\$1") + "*");
        }
        this.controller.abort();
        this.controller = new AbortController();
        // clear the update scroll position timeout, it's too late to do that
        if (this.storeScrollPositionTimeout !== null) clearTimeout(this.storeScrollPositionTimeout);
        if (!start) {
            this.items = [];
            this.mediaContainers = {};
        }
        this.setState({
            items: [],
            loading: true,
            status: "Requesting",
        }, () => {
            fetch(target + "?" + querify(input).toString(), {
                signal: this.controller.signal,
                credentials: 'include',
                headers: {
                    'Accept': 'application/json',
                },
            })
            .then((response) => {
                if (!response.ok) {
                    let message = "Error querying the Solr index!";
                    return response.text().then((data) => {
                        try {
                            let json = JSON.parse(data);
                            let exception = extract(null, json, "error", "msg");
                            if (exception) message = exception;
                        } catch (error) {}
                        throw Error(message);
                    });
                } else {
                    this.setState({
                        status: "Loading",
                    });
                }
                return response.json();
            })
            .then((result) => {
                const numFound = extract(0, result, "response", "numFound");
                const docs = extract([], result, "response", "docs").map((doc) => {
                    doc.children = extract([], doc, "children", "docs");
                    doc.duplicates = Math.max(0, extract(0, doc, "duplicates", "numFound") - 1);
                    return doc;
                });
                const more = numFound > (rows + start);
                this.items = this.items.concat(docs);
                this.setState({
                    loading: more,
                    status: docs.length ? "" : "No Results",
                    items: more ? this.state.items : this.items
                }, () => {
                    if (more) {
                        this.search(path, start + rows);
                    } else {
                        let index = (searching ? 0 : this.props.ui.scrollPosition[path]) ?? 0;
                        this.showScrollToTop((index && !searching) ? true : false);
                        this.scrollToItem(index);
                        this.items = [];
                        if (callback !== undefined) {
                            callback(true);
                        }
                    }
                });
            })
            .catch(error => {
                if (error.name === 'AbortError') return;
                this.mediaContainers = {};
                this.items = [];
                this.setState({
                    loading: false,
                    status: error.message,
                    items: []
                }, () => {
                    if (callback !== undefined) {
                        callback(false);
                    }
                });
            });
        });
    }

    reload(id) {
        if (!id) {
            console.error("Invalid id was supplied to the reload() function!");
            return;
        }
        const index = this.state.items.findIndex(x => x.id === id);
        if (index === -1) {
            console.error("Unable to find the index for %s in the items!", id);
            return;
        }
        let query = "*";
        const solrUrl = this.props.settings?.Solr?.URL;
        if (!solrUrl) return;
        const target = `${solrUrl}/select`;
        const input = {
            q: query,
            fq: [
                "id:\"" + id + "\""
            ],
            wt: "json",
        };
        this.controller.abort();
        this.controller = new AbortController();
        fetch(target + "?" + querify(input).toString(), {
            signal: this.controller.signal,
            credentials: 'include',
            headers: {
                'Accept': 'application/json',
            },
        })
        .then((response) => {
            if (!response.ok) {
                let message = "Error querying the Solr index!";
                return response.text().then((data) => {
                    try {
                        let json = JSON.parse(data);
                        let exception = extract(null, json, "error", "msg");
                        if (exception) message = exception;
                    } catch (error) {}
                    throw Error(message);
                });
            }
            return response.json();
        })
        .then((result) => {
            const numFound = extract(0, result, "response", "numFound");
            const source = extract(null, result, "response", "docs", 0);
            if (numFound === 0) {
                // looks like the file has been deleted, refresh the grid and scroll to the previous item
                this.refresh((succeeded) => {
                    if (succeeded && this.state.items.length > 0) {
                        // scroll to the previous item
                        this.scrollToItem(Math.max(0, index - 1), true);
                    }
                })
            } else if (numFound === 1) {
                this.set(index, source, false);
            } else {
                throw Error("Reload request for " + id + " received duplicate results!");
            }
        })
        .catch(error => {
            if (error.name === 'AbortError') return;
            toast.error(error.message);
            console.error(error);
        });
    }

    open(source) {
        let url;
        if (typeof source === 'object') url = encodeURI(source.fullPath) + this.props.location.search;
        else url = encodeURI(source) + this.props.location.search;
        this.props.navigate(url);
    }

    view(source, index = 0, player = true) {
        console.debug("view()");
        this.current = index;
        if (player) {
            const path = source.fullPath;
            // did the view request come from a direct link to the file?
            this.mediaViewer.current.view([source], 0);
            if (path !== this.pathname) {
                const url = encodeURI(path + this.props.location.search);
                this.props.navigate(url, {state: {path: this.path, search: this.props.location.search}});
            }
        } else {
            switch (source.type) {
                case "Photo":
                    window.open(source.fullPath, "_blank");
                    break;
                case "Video":
                    window.open(source.fullPath, "_blank");
                    break;
                default:
                    break;
            }
        }
    }

    previous() {
        if (!this.viewing || (this.current === -1)) return;
        let index = this.current - 1;
        if ((index >= 0) && (index < this.state.items.length)) {
            this.current = index;
            let source = this.state.items[index];
            this.view(source);
        }
    }

    next() {
        if (!this.viewing || (this.current === -1)) return;
        let index = this.current + 1;
        if ((index >= 0) && (index < this.state.items.length)) {
            this.current = index;
            let source = this.state.items[index];
            this.view(source);
        }
    }

    files() {
        const items = this.state.items ?? [];
        let count = items.reduce((count, item) => {
            if ((item.type === "Video") || (item.type === "Audio") || (item.type === "Photo")) return (count + 1);
            return count;
        }, 0);
        return (count === 0) ? '' : (count + ((count === 1) ? " File" : " Files"));
    }

    sort(fields, direction) {
        if (!fields || !direction) return null;
        let items = fields.filter(item => item.active).map(item => item.value);
        return items.map((item) => item.split(' ').map((field) => field + " " + direction)).join(',');
    }

    onMediaViewerShow() {
        console.debug("onMediaViewerShow()");
        this.viewing = true;
    }

    onMediaViewerHide() {
        console.debug("onMediaViewerHide()");
        this.viewing = false;
        const id = extract(null, this.state.items, this.current, 'id');
        const path = encodeURI(this.props.location?.state?.path || this.path);
        const search = this.props.location?.state?.search || this.props.location.search;
        const url = path + search;
        this.props.navigate(url);
        this.reload(id);
    }

    showUploads() {
        console.debug("showUploads()");
        this.uploads.current?.show();
    }

    onUploadsOpen(target) {
        console.debug("onUploadsOpen()");
        if (target.startsWith('http')) {
            window.open(target, "_blank");
        } else {
            this.retrieve(null, target, (docs) => {
                if (docs?.length === 1) {
                    this.view(docs[0]);
                }
            });
        }
    }

    onUploadsShow(event) {
        console.debug("onUploadsShow()");
        toast.dismiss();
        this.props.dispatch(updateUpload({ value: {
            toastId: undefined
        }}));
    }

    onUploadsHide() {
        console.debug("onUploadsHide()");
        this.setState({
            uploads: false
        });
    }

    onKeyDown(event) {
        if (this.editing) return;
        let photoViewer = extract(null, this.mediaViewer, 'current', 'photoViewer', 'current', 'viewer');
        let videoPlayer = extract(null, this.mediaViewer, 'current', 'videoPlayer', 'current', 'player');
        if (this.viewing) {
            // handle the arrow up and down first
            switch (event.code) {
                case 'ArrowUp':
                    this.previous();
                    return;
                case 'ArrowDown':
                    this.next();
                    return;
                default:
                    break;
            }
            // doesn't seem to be a next/prev, what else could it be...
            if (videoPlayer)
            {
                videoPlayer.userActive(true);
                let step = this.state.options.videoPlayer.seekStep.small;
                switch (event.code) {
                    case 'KeyF':
                        if (videoPlayer.isFullscreen()) {
                            videoPlayer.exitFullscreen();
                        } else {
                            videoPlayer.requestFullscreen();
                        }
                        break;
                    case 'KeyM':
                        videoPlayer.muted(videoPlayer.muted() ? false : true);
                        break;
                    case 'KeyK':
                    case 'Space':
                        if (videoPlayer.paused()) videoPlayer.play(); else videoPlayer.pause();
                        break;
                    case 'ArrowLeft':
                        if (event.metaKey) step = this.state.options.videoPlayer.seekStep.medium;
                        if (event.shiftKey) step = this.state.options.videoPlayer.seekStep.big;
                        videoPlayer.currentTime(Math.max(0, videoPlayer.currentTime() - step));
                        break;
                    case 'ArrowRight':
                        if (event.metaKey) step = this.state.options.videoPlayer.seekStep.medium;
                        if (event.shiftKey) step = this.state.options.videoPlayer.seekStep.big;
                        videoPlayer.currentTime(videoPlayer.currentTime() + step);
                        break;
                    case 'ArrowUp':
                        videoPlayer.volume(Math.min(videoPlayer.volume() + this.state.options.videoPlayer.volumeStep, 1.0));
                        break;
                    case 'ArrowDown':
                        videoPlayer.volume(Math.max(videoPlayer.volume() - this.state.options.videoPlayer.volumeStep, 0.0));
                        break;
                    default:
                        return;
                }
            } else if (photoViewer) {
                switch (event.code) {
                    case 'KeyF':
                        if (photoViewer.fulled) {
                            photoViewer.exit();
                        } else {
                            photoViewer.full();
                        }
                        break;
                    case 'ArrowLeft':
                        photoViewer.zoom(-this.state.options.photoViewer.zoom.ratio, this.state.options.photoViewer.zoom.hasTooltip);
                        break;
                    case 'ArrowRight':
                        photoViewer.zoom(this.state.options.photoViewer.zoom.ratio, this.state.options.photoViewer.zoom.hasTooltip);
                        break;
                    default:
                        return;
                }
            } else {
                return;
            }
        } else {
            if (!this.grid.current) return;
            let grid = this.grid.current;
            let height = grid.props.height;
            let rowCount = grid.props.rowCount;
            let rowHeight = grid.props.rowHeight;
            let pageRows = Math.ceil(height / rowHeight);
            let currentRow = Math.floor(grid.state.scrollTop / rowHeight);
            switch (event.code) {
                case 'Home':
                    this.scrollToItem(null, true, "start", 0);
                    break;
                case 'End':
                    this.scrollToItem(null, true, "end", rowCount);
                    break;
                case 'PageUp':
                    this.scrollToItem(null, true, "start", (currentRow - pageRows));
                    break;
                case 'PageDown':
                    this.scrollToItem(null, true, "start", (currentRow + pageRows));
                    break;
                case 'ArrowUp':
                    this.scrollToItem(null, true, "start", (currentRow - 1));
                    break;
                case 'ArrowDown':
                    this.scrollToItem(null, true, "start", (currentRow + 1));
                    break;
                default:
                    return;
            }
        }
        // it appears that the key has been handled, let's ensure no one else gets it
        event.preventDefault();
        event.stopPropagation();
    }

    async scrollToItem(index, store=false, align="start", rowIndex=undefined, columnIndex=undefined) {
        console.debug("scrollToItem()");
        if (!this.grid.current) return;
        let grid = this.grid.current;
        let columnCount = grid.props.columnCount;
        rowIndex = rowIndex ?? Math.floor(index / columnCount);
        this.ignoreScrollUpdateWasRequested = store;
        grid.scrollToItem({ align: align, rowIndex: rowIndex, columnIndex: columnIndex });
    }

    async storeScrollPosition(horizontalScrollDirection, scrollLeft, scrollTop, scrollUpdateWasRequested, verticalScrollDirection, timeout=500) {
        // don't store the position when searching
        if (this.searching) return;
        // hide the scroll to top button
        this.showScrollToTop(false);
        // make sure the user has stopped scrolling for now
        if (this.storeScrollPositionTimeout !== null) clearTimeout(this.storeScrollPositionTimeout);
        this.storeScrollPositionTimeout = setTimeout(() => {
            if (!this.grid.current) return;
            let grid = this.grid.current;
            let rowHeight = grid.props.rowHeight;
            let columnCount = grid.props.columnCount;
            let firstVisibleRowIndex = Math.floor(scrollTop / rowHeight);
            let firstVisibleItemIndex = (firstVisibleRowIndex * columnCount);
            if (this.props.ui.scrollPosition[this.path] !== firstVisibleItemIndex) {
                this.props.dispatch(setScrollPosition({
                    path: this.path,
                    index: firstVisibleItemIndex,
                }));
            }
        }, timeout);
    }

    onScroll({horizontalScrollDirection, scrollLeft, scrollTop, scrollUpdateWasRequested, verticalScrollDirection}) {
        // make sure the scroll update is the result of a user interaction
        if (scrollUpdateWasRequested && !this.ignoreScrollUpdateWasRequested) return;
        // reset the ignoreScrollUpdateWasRequested flag
        this.ignoreScrollUpdateWasRequested = false;
        // queue saving of the scroll position so the UI is not blocked by the work it entails
        this.storeScrollPosition(horizontalScrollDirection, scrollLeft, scrollTop, scrollUpdateWasRequested, verticalScrollDirection)
    }

    showScrollToTop(show) {
        let self = this.scrollToTopButton.current;
        let parent = self.parentElement;
        if (show) {
            self.classList.remove("animate__fadeOutUp");
            parent.classList.remove("animate__fadeOut");
            self.classList.add("animate__fadeInDown");
            parent.classList.add("animate__fadeIn");
            parent.classList.remove("pe-none");
        } else {
            if (self.classList.contains("animate__fadeInDown")) {
                self.classList.remove("animate__fadeInDown");
                parent.classList.remove("animate__fadeIn");
                self.classList.add("animate__fadeOutUp");
                parent.classList.add("animate__fadeOut");
                parent.classList.add("pe-none");
            }
        }
    }

    onScrollToTop() {
        this.scrollToItem(0, true);
    }

    onUploadComplete(name) {
        this.refresh((succeeded) => {
            if (succeeded) {
                const index = this.state.items.findIndex(x => x.name === name);
                if (index !== -1) this.scrollToItem(index, true);
            }
        });
    }

    gridView() {
        return (
            <AutoSizer>
            {({ height, width }) => {
                const items = this.state.items ?? [];
                const size = (window.innerWidth > 0) ? window.innerWidth : window.screen.width;
                const offset = (size - this.gridWrapper.current.offsetWidth) / 2;
                const columnCount = Math.ceil(size / 400);
                const rowHeight = (width / columnCount);
                const columnWidth = width / columnCount;
                const rowCount = Math.ceil(items.length / columnCount);
                return (
                    <Grid ref={this.grid} className="grid" columnCount={columnCount} columnWidth={columnWidth} height={height} rowCount={rowCount} rowHeight={rowHeight} width={width + offset} onScroll={this.onScroll.bind(this)} >
                    {
                        ({ columnIndex, rowIndex, style }) => {
                            let index = (rowIndex * columnCount) + columnIndex;
                            let source = items[index];
                            const view = this.props.ui.view[this.searching ? "search" : this.path] ?? this.props.ui.view.default;
                            if (source !== undefined) {
                                this.mediaContainers[index] = React.createRef();
                                return (
                                    <div className="grid-item animate__animated animate__fadeIn" style={style}>
                                        <MediaContainer ref={this.mediaContainers[index]} library={this.props.forwardedRef} view={view} source={source} index={index} onOpen={this.open.bind(this)} onView={this.view.bind(this)} onUpdate={this.update.bind(this)} />
                                    </div>
                                );
                            } else {
                                return (
                                    <div style={style}></div>
                                );
                            }
                        }
                    }
                    </Grid>
                )}
            }
            </AutoSizer>
        );
    }

    render() {
        let location = "/";
        const status = this.state.status;
        const loading = this.state.loading;
        const items = this.state.items ?? [];
        const searching = this.searching ? true : false;
        let path = "Library".concat(this.path);
        const components = searching ? path.concat("/Search Results").split("/") : path.split("/");
        return (
            <>
                <div className="library d-flex flex-column align-content-stretch" style={{flexGrow: 1, flexShrink: 1, flexBasis: 'auto'}}>
                    <Breadcrumb className="path mx-3" listProps={{ className: "py-2 px-3" }}>
                        {
                            components.filter((component) => component).map((component, index, array) => {
                                let link = location;
                                let active = false;
                                let last = (index === (array.length - 1));
                                if (index) {
                                    if (last && searching) {
                                        link = "";
                                        active = true;
                                    } else {
                                        location += component + "/";
                                        link = location;
                                    }
                                }
                                return (
                                    <Breadcrumb.Item key={"library-path-item-" + index} href={link} active={active} linkProps={{ link: link, className: "text-decoration-none" }} onClick={(event) => {
                                        event.preventDefault();
                                        event.stopPropagation();
                                        let link = event.target.getAttribute("link");
                                        if (link) {
                                            // remove the query parameter before navigating to the new path
                                            const params = new URLSearchParams(this.props.location.search);
                                            params.delete('query');
                                            if (params.size) link += "?" + params.toString();
                                            this.props.navigate(link);
                                        }
                                    }} >{component}</Breadcrumb.Item>
                                );
                            })
                        }
                        <div className="statistics d-none d-md-block ms-auto">
                            <span className="statistics-files">{this.files()}</span>
                            <span className="statistics-size ms-1">{size(items.reduce((sum, item) => sum + item.size, 0), 2, '(', ')')}</span>
                        </div>
                    </Breadcrumb>
                    <div ref={this.gridWrapper} className="grid-wrapper d-flex flex-grow-1 position-relative mx-3">
                        <Container fluid className="scroll-to-top d-flex position-absolute justify-content-center pe-none pb-5 animate__animated animate__faster">
                            <Button ref={this.scrollToTopButton} className="animate__animated animate__fast px-3" variant="info" onClick={this.onScrollToTop.bind(this)}>
                                <i className="icon bi bi-arrow-up-square pe-2"></i>Scroll To Top<i className="icon bi bi-arrow-up-square ps-2"></i>
                            </Button>
                        </Container>
                        <Container fluid className={cx("loading", (loading || status) ? "d-flex" : "d-none", "flex-grow-1 position-absolute animate__animated animate__fadeIn animate__faster flex-column align-self-stretch align-items-center")}>
                            <Row className="mt-auto">
                                <Col className={cx(loading ? "d-flex" : "d-none", "text-center mb-2")}>
                                    <Spinner className="loading-spinner" role="status"/>
                                </Col>
                            </Row>
                            <Row className="mb-auto">
                                <Col className={cx(status ? "d-flex" : "d-none", "text-center")}>
                                    <p className="loading-status font-weight-light fs-6 text-body text-uppercase" >{status}</p>
                                </Col>
                            </Row>
                        </Container>
                        <UploadZone ref={this.uploadZone} signalRConnection={this.props.signalRConnection} uploads={this.uploads} onUploadComplete={this.onUploadComplete.bind(this)}>
                            {this.gridView()}
                            <MediaViewer ref={this.mediaViewer} library={this.props.forwardedRef} uploadZone={this.uploadZone} onUpdate={this.update.bind(this)} onShow={this.onMediaViewerShow.bind(this)} onHide={this.onMediaViewerHide.bind(this)} />
                            <Uploads ref={this.uploads} onShow={this.onUploadsShow.bind(this)} onHide={this.onUploadsHide.bind(this)} onUpload={() => this.uploadZone.current?.upload(null, true)} onOpen={this.onUploadsOpen.bind(this)} />
                        </UploadZone>
                    </div>
                </div>
                {/*
                <Navbar collapseOnSelect expand="sm" bg={(state.ui.theme === "dark") ? "dark" : "light"} variant="dark" className="p-3">
                    <div style={{flexGrow: 1}}>{this.files()}</div>
                    <div>{size(this.state.items.reduce((sum, item) => sum + item.size, 0))}</div>
                </Navbar>
                */}
            </>
        );
    }
}

const mapStateToProps = (state) => ({
    ui: {
        view: state.ui.view,
        theme: state.ui.theme,
        scrollPosition: state.ui.scrollPosition,
        uploads: state.ui.uploads,
    },
    search: {
        sort: state.search.sort
    },
    settings: state.settings.current
});

export default connect(mapStateToProps, null, null, { forwardRef: true })(
    withRouter(Library)
);
