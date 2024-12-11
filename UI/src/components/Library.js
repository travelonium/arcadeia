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

import Row from 'react-bootstrap/Row';
import Col from 'react-bootstrap/Col';
import MediaViewer from './MediaViewer';
import update from 'immutability-helper';
import React, { Component } from 'react';
import Button from 'react-bootstrap/Button'
import Container from 'react-bootstrap/Container';
import Breadcrumb from 'react-bootstrap/Breadcrumb';
import AutoSizer from 'react-virtualized-auto-sizer';
import { FixedSizeGrid as Grid } from 'react-window';
import { clone, extract, size, querify, shorten, withRouter, isEqualExcluding, getFlag } from './../utils';
import { setScrollPosition } from '../features/ui/slice';
import { MediaContainer } from './MediaContainer';
import { UploadZone } from './UploadZone';
import { toast } from 'react-toastify';
import { connect } from "react-redux";
import { isEqual } from 'lodash';
import pb from 'path-browserify';
import Spinner from './Spinner';
import cx from 'classnames';
import axios from 'axios';

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
        this.uploadZone = React.createRef();
        this.gridWrapper = React.createRef();
        this.mediaViewer = React.createRef();
        this.controller = new AbortController();
        this.scrollToTopButton = React.createRef();
        this.ignoreScrollUpdateWasRequested = false;
        this.storeScrollPositionTimeout = null;
        this.uploadTimeout = null;
        this.signalRConnection = null;
        this.state = {
            loading: false,
            status: "",
            items: null,    // a null value indicates that no search has yet been performed
            uploads: {
                total: 0,
                queued: [],
                active: {},
                failed: [],
            },
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
        const didMediaViewerShowOrHide = (currentPath === nextPath) && ((currentName && !nextName) || (!currentName && nextName));

        // was this triggered by a browser back / forward?
        const wasBrowserBackOrForward = (nextProps?.navigationType === "POP");

        // determine if the scroll position remains unchanged
        const didScrollPositionChange = !isEqual(this.props.ui.scrollPosition, nextProps.ui.scrollPosition);

        // check if significant differences exist in props or state, excluding specific keys
        const havePropsChanged = !isEqualExcluding(currentProps, nextProps, 'dispatch', 'ref', 'forwardedRef');
        const hasStateChanged = !isEqualExcluding(currentState, nextState, 'uploads');

        const shouldUpdate = (!didMediaViewerShowOrHide || wasBrowserBackOrForward) && !didScrollPositionChange && (havePropsChanged || hasStateChanged);

        return shouldUpdate;
    }

    componentDidUpdate(prevProps) {
        const currentPath = this.path;
        const prevPath = this.getPath(prevProps?.location?.pathname);
        if (this.state.items == null ||
            !isEqualExcluding(prevProps.location, this.props.location, 'key') ||
            !isEqual(prevProps.search, this.props.search)) {
            const name = this.name;
            if (name && (prevPath === currentPath)) {
                // the path has not changed since last update, no refresh necessary
                const index = this.state.items.findIndex(x => x.name === name);
                if (index !== -1) {
                    this.view(this.state.items[index], index);
                }
            } else {
                // hide the media viewer in case this is a history event
                this.mediaViewer.current.hide();

                // the path has changed since last update refresh and view
                this.refresh((succeeded) => {
                    if (succeeded && name) {
                        const index = this.state.items.findIndex(x => x.name === name);
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
            this.mediaContainers[index]?.current.set(source, (_) => {
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

    search(path = this.path, start = 0, callback = undefined) {
        const rows = 10000;
        const deleted = false;
        const searching = this.searching;
        const query = searching ? this.query : "*";
        const recursive = this.recursive;
        const favorite = this.favorite;
        let solr = "/solr/Library/select";
        if (process.env.NODE_ENV !== "production") {
            solr = "http://localhost:8983/solr/Library/select";
        }
        const sort = (searching ? this.props.search.sort['search'] : this.props.search.sort[path]) ?? this.props.search.sort;
        const input = {
            q: query,
            fq: [],
            rows: rows,
            start: start,
            fl: "*,children:[subquery]",
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
            fetch(solr + "?" + querify(input).toString(), {
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
        let solr = "/solr/Library/select";
        if (process.env.NODE_ENV !== "production") {
            solr = "http://localhost:8983/solr/Library/select";
        }
        const input = {
            q: query,
            fq: [
                "id:\"" + id + "\""
            ],
            wt: "json",
        };
        this.controller.abort();
        this.controller = new AbortController();
        fetch(solr + "?" + querify(input).toString(), {
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

    upload(items = null, defer = false) {
        if (defer) {
            if (this.uploadTimeout !== null) clearTimeout(this.uploadTimeout);
            this.uploadTimeout = setTimeout(() => this.upload(), 100);
            return;
        }
        if (items) {
            let queue = (item, path) => {
                let queued = [];
                if (typeof item === 'string') {
                    queued.push({
                        url: item,
                        path: path
                    });
                } else {
                    queued.push({
                        file: item,
                        path: path
                    });
                }
                this.setState(prevState => {
                    return {
                        ...prevState,
                        uploads: update(prevState.uploads, {
                            $merge: {
                                total: prevState.uploads.total + queued.length,
                                queued: update(prevState.uploads.queued, {
                                    $push: queued
                                })
                            }
                        })
                    }
                }, () => {
                    this.upload(null, true);
                });
            }
            let process = (item, path) => {
                if (typeof item === 'string') {
                    // the item is a url, queue it and its destination path if not already queued or active
                    let key = path + item;
                    if (!this.state.uploads.active.hasOwnProperty(key) && this.state.uploads.queued.findIndex(x => (x.path + x.url) === key) === -1) {
                        queue(item, path);
                    }
                } else {
                    // the item is a file object, queue it and its destination path if not already queued or active
                    let key = item.webkitRelativePath ? path + item.webkitRelativePath : item.name;
                    if (!this.state.uploads.active.hasOwnProperty(key) && this.state.uploads.queued.findIndex(x => pb.join(x.path, x.file.name) === key) === -1) {
                        if (item.webkitRelativePath) path = pb.normalize(pb.dirname(pb.join(path, item.webkitRelativePath)) + "/");
                        queue(item, path);
                    }
                }
            };
            let traverse = (item, path = this.path) => {
                if (item.isFile) {
                    item.file(file => process(file, path));
                } else if (item.isDirectory) {
                    let dirReader = item.createReader();
                    dirReader.readEntries(entries => {
                        entries.forEach(entry => {
                            traverse(entry);
                        });
                    });
                }
            };
            items.forEach(item => {
                if (item.webkitGetAsEntry) item = item.webkitGetAsEntry();
                if (item === null) return;
                if (typeof item === 'string') {
                    process(item, this.path);
                } else if (item.isDirectory) {
                    traverse(item);
                } else {
                    if (item instanceof File) process(item, this.path);
                    else item.file(file => process(file, this.path));
                }
            });
            return;
        }
        // do we already have the maximum simultaneous number of active uploads?
        if (Object.keys(this.state.uploads.active).length >= this.props.ui.uploads.simultaneous) return;
        // nope, do we have any items in the queue? if not, log the list of failed uploads if any
        if (!this.state.uploads.queued.length) {
            // if no uploads are currently active either, reset the total
            if (!Object.keys(this.state.uploads.active).length) {
                this.setState(prevState => {
                    return {
                        ...prevState,
                        uploads: update(prevState.uploads, {
                            $merge: {
                                total: 0,
                            }
                        })
                    }
                }, () => {
                    if (this.state.uploads.failed.length > 0) {
                        console.warn("Failed Uploads:", this.state.uploads.failed);
                    }
                });
            }
            return;
        }
        // yes, we can start one more
        const url = this.state.uploads.queued[0].url;
        const file = this.state.uploads.queued[0].file;
        const path = this.state.uploads.queued[0].path;
        // dequeue the first item and start uploading it
        this.setState(prevState => {
            return {
                ...prevState,
                uploads: update(prevState.uploads, {
                    $merge: {
                        queued: update(prevState.uploads.queued, {
                            $splice: [[0, 1]]
                        })
                    }
                })
            }
        }, () => {
            if (file) this.uploadFile(file, path);
            else if (url) this.uploadUrl(url, path);
            else console.error("Neither the file nor the URL were valid!");
        });
    }

    uploadFile(file, path) {
        const key = pb.join(path, file.name);
        const total = this.state.uploads.total;
        const index = this.state.uploads.total - this.state.uploads.queued.length;
        let data = new FormData();
        data.append('files', file, pb.join(path, file.name));
        const prefix = "[" + index + " / " + total + "] ";
        this.setState(prevState => {
            return {
                ...prevState,
                uploads: update(prevState.uploads, {
                    $merge: {
                        active: update(prevState.uploads.active, {
                            $merge: {
                                [key]: {
                                    file: file,
                                    toast: toast.info(this.renderUploadToast(prefix + "Uploading...", file.name),
                                    {
                                        progress: 0,
                                        theme: (this.props.ui.theme === 'dark') ? 'dark' : 'light',
                                        icon: <div className="Toastify__spinner"></div>
                                    }),
                                }
                            }
                        })
                    }
                })
            }
        }, () => {
            let config = {
                headers: {
                    'Content-Type': 'multipart/form-data',
                },
                validateStatus: (status) => {
                    return (status === 200);
                },
                onUploadProgress: this.onUploadFileProgress.bind(this, key, file, index),
            };
            const target = new URL("/api/library" + path, window.location.origin);
            target.searchParams.set('overwrite', this.props.ui.uploads.overwrite);
            target.searchParams.set('duplicate', this.props.ui.uploads.duplicate);
            axios.post(target.toString(), data, config)
            .then((response) => {
                let name = extract(file.name, response, 'data', 0, 'name');
                toast.update(extract(null, this.state.uploads.active, key, 'toast'), {
                    icon: null,
                    theme: null,
                    progress: null,
                    autoClose: null,
                    type: 'success',
                    render: this.renderUploadToast.bind(this, prefix + "Complete", name),
                });
                // remove the successfully uploaded item from the list of active uploads
                this.setState(prevState => {
                    return {
                        ...prevState,
                        uploads: update(prevState.uploads, {
                            $merge: {
                                active: update(prevState.uploads.active, {
                                    $unset: [key]
                                })
                            }
                        })
                    }
                }, () => {
                    // reload the grid items if the uploaded file's path is the current path or is in a subdirectory of the current path
                    if ((path === this.path) || pb.normalize(pb.dirname(path) + "/") === this.path) {
                        this.refresh((succeeded) => {
                            if (succeeded) {
                                const index = this.state.items.findIndex(x => x.name === file.name);
                                if (index !== -1) this.scrollToItem(index, true);
                            }
                        });
                    }
                    // next!
                    this.upload(null, true);
                })
            })
            .catch(error => {
                console.error(error);
                let message = "Upload Failed";
                let toastId = extract(null, this.state.uploads.active, key, 'toast');
                if (error.response) {
                    // the client was given an error response (5xx, 4xx)
                    let title = extract(null, error.response, 'data', 'title');
                    if (title) message += " (" + title + ")";
                } else if (error.request) {
                    // the client never received a response, and the request was never left
                } else {
                    // well, something else must've happened
                }
                toast.update(toastId, {
                    icon: null,
                    progress: 0,
                    theme: null,
                    type: 'error',
                    render: this.renderUploadToast.bind(this, message, file.name),
                });
                let failed = extract(null, this.state.uploads.active, key, 'file');
                // remove the failed upload from the list of active uploads
                this.setState(prevState => {
                    return {
                        ...prevState,
                        uploads: update(prevState.uploads, {
                            $merge: {
                                active: update(prevState.uploads.active, {
                                    $unset: [key]
                                })
                            }
                        })
                    }
                }, () => {
                    // add the failed upload to the list of failed uploads
                    this.setState(prevState => {
                        return {
                            ...prevState,
                            uploads: update(prevState.uploads, {
                                $merge: {
                                    failed: update(prevState.uploads.failed, {
                                        $push: [{
                                            file: failed,
                                            path: path
                                        }]
                                    }),
                                }
                            })
                        }
                    }, () => {
                        // process any remaining queued items
                        this.upload(null, true);
                    });
                });
            });
            // process any remaining queued items
            this.upload(null, true);
        });
    }

    uploadUrl(url, path) {
        const key = path + url;
        const total = this.state.uploads.total;
        const index = this.state.uploads.total - this.state.uploads.queued.length;
        const prefix = "[" + index + " / " + total + "] ";
        this.setState(prevState => {
            return {
                ...prevState,
                uploads: update(prevState.uploads, {
                    $merge: {
                        active: update(prevState.uploads.active, {
                            $merge: {
                                [key]: {
                                    url: url,
                                    toast: toast.info(this.renderUploadToast(prefix + "Resolving...", url),
                                    {
                                        progress: 0,
                                        autoClose: false,
                                        theme: (this.props.ui.theme === 'dark') ? 'dark' : 'light',
                                        icon: <div className="Toastify__spinner"></div>
                                    }),
                                }
                            }
                        })
                    }
                })
            }
        }, async () => {
            let title;
            // eslint-disable-next-line
            let result;
            let fileName;
            let subtitle = url;
            let progress = null;
            let type = undefined;
            let icon = undefined;
            let theme = (this.props.ui.theme === 'dark') ? 'dark' : 'light';
            const target = new URL("/api/library/upload", window.location.origin);
            target.searchParams.set('overwrite', this.props.ui.uploads.overwrite);
            target.searchParams.set('duplicate', this.props.ui.uploads.duplicate);
            target.searchParams.set('path', path);
            target.searchParams.set('url', url);
            fetch(target.toString())
            .then((response) => {
                let show = true;
                const reader = response.body.getReader();
                const process = ({ done, value: chunk }) => {
                    if (done) {
                        progress = 1.0;
                        title = "Complete";
                        this.onUploadUrlProgress(key, index, title, subtitle, progress, type, theme, icon);
                        // remove the successfully uploaded item from the list of active uploads
                        this.setState(prevState => {
                            return {
                                ...prevState,
                                uploads: update(prevState.uploads, {
                                    $merge: {
                                        active: update(prevState.uploads.active, {
                                            $unset: [key]
                                        })
                                    }
                                })
                            }
                        }, () => {
                            // reload the grid items if the uploaded file's path is the current path or is in a subdirectory of the current path
                            if ((path === this.path) || pb.normalize(pb.dirname(path) + "/") === this.path) {
                                this.refresh((succeeded) => {
                                    if (succeeded) {
                                        const index = this.state.items.findIndex(x => x.name === fileName);
                                        if (index !== -1) this.scrollToItem(index, true);
                                    }
                                });
                            }
                            // next!
                            this.upload(null, true);
                        });
                        return;
                    }
                    const lines = new TextDecoder('utf-8').decode(chunk).trim().split('\n');
                    for (const line of lines) {
                        const regex = /^([\w\s]+):\s(.+)$/;
                        const match = line.trim().match(regex);
                        if (match) {
                            const k = match[1].trim();
                            const v = match[2].trim();
                            if (k === 'Name') {
                                progress = 0;
                                title = "Starting...";
                                subtitle = fileName = v;
                            } else if (k === 'Downloading') {
                                title = "Downloading...";
                                if (!isNaN(v)) {
                                    progress = parseFloat(v) !== 1.0 ? parseFloat(v) : 0;
                                }
                            } else if (k === 'Merging') {
                                progress = 0;
                                title = "Merging...";
                                subtitle = fileName = v;
                            } else if (k === 'Moving') {
                                progress = 0;
                                title = "Moving...";
                                subtitle = fileName = v;
                            } else if (k === 'Processing') {
                                title = "Processing...";
                                if (!isNaN(v)) {
                                    progress = parseFloat(v);
                                }
                            } else if (k === 'Result') {
                                icon = null;
                                theme = null;
                                progress = 1.0;
                                type =  'success';
                                title = "Complete";
                                result = JSON.parse(v);
                                show = false;
                            } else if (k === 'Error') {
                                // reject the promise to propagate the error
                                return Promise.reject(new Error(v));
                            }
                            // Update the toast
                            if (show) this.onUploadUrlProgress(key, index, title, subtitle, progress, type, theme, icon);
                        }
                    };
                    return reader.read().then(process);
                };
                return reader.read().then(process);
            })
            .catch(error => {
                console.error(error);
                title = error.message;
                this.onUploadUrlProgress(key, null, title, subtitle, null, 'error', null);
                let failed = extract(null, this.state.uploads.active, key, 'url');
                // remove the failed upload from the list of active uploads
                this.setState(prevState => {
                    return {
                        ...prevState,
                        uploads: update(prevState.uploads, {
                            $merge: {
                                active: update(prevState.uploads.active, {
                                    $unset: [key]
                                })
                            }
                        })
                    }
                }, () => {
                    // add the failed upload to the list of failed uploads
                    this.setState(prevState => {
                        return {
                            ...prevState,
                            uploads: update(prevState.uploads, {
                                $merge: {
                                    failed: update(prevState.uploads.failed, {
                                        $push: [{
                                            url: failed,
                                            path: path
                                        }]
                                    }),
                                }
                            })
                        }
                    }, () => {
                        // process any remaining queued items
                        this.upload(null, true);
                    });
                });
            });
            // process any remaining queued items
            this.upload(null, true);
        });
    }

    open(source) {
        let url = encodeURI(source.fullPath) + this.props.location.search;
        this.props.navigate(url);
    }

    view(source, index = 0, player = true) {
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
        this.viewing = true;
    }

    onMediaViewerHide() {
        this.viewing = false;
        const path = encodeURI(this.props.location?.state?.path || this.path);
        const search = this.props.location?.state?.search || this.props.location.search;
        const url = path + search;
        this.props.navigate(url);
        this.reload(extract(null, this.state.items, this.current, 'id'));
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

    onUploadFileProgress(key, file, index, progressEvent) {
        const progress = progressEvent.loaded / progressEvent.total;
        const toastId = extract(null, this.state.uploads.active, key, 'toast');
        const prefix = `[${index} / ${this.state.uploads.total}] `;
        const status = progress < 1 ? "Uploading" : "Processing";
        toast.update(toastId, {
            progress: progress === 1.0 ? 0 : progress,
            autoClose: progress === 1.0 ? false : null,
            render: this.renderUploadToast.bind(this, `${prefix}${status}...`, file.name),
        });
    }

    onUploadUrlProgress(key, index, title, subtitle, progress, type, theme, icon) {
        const prefix = index != null ? `[${index} / ${this.state.uploads.total}] ` : "";
        const options = {
            autoClose: progress === 0.0 ? false : null,
            render: this.renderUploadToast.bind(this, `${prefix}${title}`, subtitle),
            progress: progress !== 1.0 ? progress : null,
            ...(icon !== undefined && { icon }),
            ...(type !== undefined && { type }),
            ...(theme !== undefined && { theme }),
        };
        const toastId = extract(null, this.state.uploads.active, key, 'toast');
        toast.update(toastId, options);
    }

    async scrollToItem(index, store=false, align="start", rowIndex=undefined, columnIndex=undefined) {
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

    renderUploadToast(title, subtitle) {
        return (
            <>
                <div>
                    <strong>{title}</strong>
                </div>
                <small>{shorten(subtitle, 100)}</small>
            </>
        );
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
                    <UploadZone ref={this.uploadZone} width={width} height={height} onUpload={this.upload.bind(this)}>
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
                    </UploadZone>
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
                    <div ref={this.gridWrapper} className="grid-wrapper d-flex mx-3" style={{flexGrow: 1, flexShrink: 1, flexBasis: 'auto'}}>
                        { this.gridView() }
                        <Container fluid className="scroll-to-top animate__animated animate__faster position-absolute d-flex justify-content-center pe-none pb-5">
                            <Button ref={this.scrollToTopButton} className="animate__animated animate__fast px-3" variant="info" onClick={this.onScrollToTop.bind(this)}>
                                <i className="icon bi bi-arrow-up-square pe-2"></i>Scroll To Top<i className="icon bi bi-arrow-up-square ps-2"></i>
                            </Button>
                        </Container>
                        <MediaViewer ref={this.mediaViewer} library={this.props.forwardedRef} uploadZone={this.uploadZone} onUpdate={this.update.bind(this)} onShow={this.onMediaViewerShow.bind(this)} onHide={this.onMediaViewerHide.bind(this)} />
                        <Container fluid className={cx((loading || status) ? "d-flex" : "d-none", "animate__animated animate__fadeIn animate__faster flex-column align-self-stretch align-items-center")}>
                            <Row className="mt-auto">
                                <Col className={cx(loading ? "d-flex" : "d-none", "text-center mb-2")}>
                                    <Spinner className="loading-spinner" role="status"/>
                                </Col>
                            </Row>
                            <Row className="mb-auto">
                                <Col className={cx(status ? "d-flex" : "d-none", "text-center")}>
                                    <p className="loading-status font-weight-light fs-6 text-body text-uppercase" >{this.state.status}</p>
                                </Col>
                            </Row>
                        </Container>
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
    }
});

export default connect(mapStateToProps, null, null, { forwardRef: true })(
    withRouter(Library)
);
