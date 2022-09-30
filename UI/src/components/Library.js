import Row from 'react-bootstrap/Row';
import Col from 'react-bootstrap/Col';
import update from 'immutability-helper';
import React, { Component } from 'react';
import Button from 'react-bootstrap/Button'
import Spinner from 'react-bootstrap/Spinner';
import Container from 'react-bootstrap/Container';
import Breadcrumb from 'react-bootstrap/Breadcrumb';
import AutoSizer from 'react-virtualized-auto-sizer';
import { FixedSizeGrid as Grid } from 'react-window';
import { clone, extract, size, updateBit } from './../utils';
import { reset, setPath } from '../features/search/slice';
import { setScrollPosition } from '../features/ui/slice';
import { MediaContainer } from './MediaContainer';
import { MediaViewer } from './MediaViewer';
import { UploadZone } from './UploadZone';
import { useParams } from "react-router-dom";
import { toast } from 'react-toastify';
import { connect } from "react-redux";
import cx from 'classnames';
import axios from 'axios';
import _ from 'lodash';

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
        this.gridWrapper = React.createRef();
        this.mediaViewer = React.createRef();
        this.controller = new AbortController();
        this.scrollToTopButton = React.createRef();
        this.ignoreScrollUpdateWasRequested = false;
        this.storeScrollPositionTimeout = null;
        this.state = {
            history: false,
            loading: false,
            status: "",
            items: [],
            uploads: {
                total: 0,
                queued: [],
                active: {},
                failed: {},
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

    componentDidMount() {
        // install the event handler for keydown keyboard events
        document.addEventListener('keydown', this.onKeyDown.bind(this));
        // handle the startup query parsing
        let path = "/" + extract("", this.props, "match", "params", "*") + window.location.search;
        this.props.dispatch(reset(path));
        // handles the browser history operations
        window.onpopstate = (event) => {
            let path = extract("", event, 'state', 'path');
            this.setState({
                history: true, // notify the search function that the call is coming from the history
            }, () => {
                this.props.dispatch(reset(path));
                this.mediaViewer.current.hide();
            });
        };
    }

    shouldComponentUpdate(nextProps, nextState) {
        if (!_.isEqual(this.props.ui.scrollPosition, nextProps.ui.scrollPosition)) return false;
        return (!_.isEqual(this.props, nextProps) || (!_.isEqual(this.state, nextState)));
    }

    componentDidUpdate(prevProps) {
        if (!_.isEqual(this.props.search, prevProps.search)) {
            if (this.props.search.query) {
                this.search(null, 0, (succeeded, name) => {
                    if (succeeded && name) {
                        const index = this.state.items.findIndex(x => x.name === name);
                        if (index !== -1) {
                            this.view(this.state.items[index], index, true);
                        }
                    }
                });
            } else {
                const components = this.props.search.path.match(/(.*\/)(.*)?/);
                if (components) {
                    const path = components[1];
                    const name = components[2];
                    this.list(path, (succeeded, _) => {
                        if (name && succeeded) {
                            const index = this.state.items.findIndex(x => x.name === name);
                            if (index !== -1) {
                                this.view(this.state.items[index], index, true);
                            }
                        }
                    });
                } else {
                    this.list(this.props.search.path);
                }
            }
        }
    }

    list(path = undefined, callback = undefined) {
        const components = this.props.search.path.match(/(.*\/)(.*)?/);
        if (!path && components) path = components[1];
        this.search(path, 0, callback);
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
            this.state.items = items;
            this.mediaContainers[index].current.set(source, (_) => {
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

    /**
     * Update a MediaContainer with new attributes.
     * @param {*} source The modified MediaContainer to update.
     * @param {boolean} refresh Forces a state update and a refresh following the modifications.
     * @param {(source, succeeded) => void} callback The callback function to call with either the updated or unmodified source when the operation is thorough.
     */
    update(source, refresh = true, callback = undefined) {
        let item = clone(source);
        const index = this.state.items.findIndex(x => x.id === item.id);
        if (index === -1) {
            toast.error("Unable to find the item that was to be updated.");
            return;
        }
        fetch("/library" + item.fullPath, {
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
                    throw new Error(error.message);
                });
            } else {
                return response.json();
            }
        })
        .then((response) => {
            this.set(index, response, refresh, callback);
        })
        .catch((error) => {
            console.error(error);
            toast.error(error.message);
            if (callback !== undefined) {
                callback(this.state.items[index], false);
            }
        });
    }

    search(browse = null, start = 0, callback = undefined) {
        const rows = 10000;
        let path = browse;
        let name = null;
        const components = this.props.search.path.match(/(.*\/)(.*)?/);
        if (!browse && components) {
            // this seems to be a search with a query, extract the required details
            path = components[1];
            name = components[2];
        }
        if (!path) return;
        let history = this.state.history;
        let params = new URLSearchParams();
        let query = browse ? "*" : this.props.search.query;
        let flags = parseInt(params.get("flags") ?? 0);
        let values = parseInt(params.get("values") ?? 0);
        if (!browse) {
            // update the query parameter
            params.set("query", query);
        }
        let deleted = false;
        let favorite = this.props.search.favorite;
        let recursive = this.props.search.recursive;
        // update the recursive parameter
        params.set("recursive", recursive);
        // update the favorite flags parameters
        flags = updateBit(flags, 1, favorite);
        values = updateBit(values, 1, favorite);
        // update the deleted flags bits to exclude the deleted files
        flags = updateBit(flags, 0, deleted);
        values = updateBit(values, 0, deleted);
        if (flags) {
            params.set("flags", flags);
        }
        if (values) {
            params.set("values", values);
        }
        let solr = "/search";
        if (process.env.NODE_ENV !== "production") {
            solr = "http://localhost:8983/solr/Library/select";
        }
        const input = {
            q: query,
            fq: [],
            rows: rows,
            start: start,
            "q.op": "AND",
            defType: "edismax",
            qf: "name_ngram^20 description_ngram^10 path_ngram^5",
            wt: "json",
            sort: this.sort(this.props.search.sort.fields, this.props.search.sort.direction),
        };
        if (favorite) {
            input.fq.push("flags:Favorite");
        }
        if (deleted) {
            input.fq.push("flags:Deleted");
        } else {
            input.fq.push("-flags:Deleted");
        }
        if (!recursive || browse) {
            input.fq.push("path:\"" + path.split('?')[0] + "\"");
        } else {
            input.fq.push("path:" + (path.split('?')[0]).replace(/([+\-!(){}[\]^"~*?:\\/ ])/g, "\\$1") + "*");
        }
        path = path.split('?')[0] + (params.toString() ? ("?" + params.toString()) : "");
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
            path: path,
            loading: true,
            history: false,
            status: "Requesting",
        }, () => {
            // change the url and the history before going any further
            if (!history && !start) window.history.pushState({path: path}, "", path);
            fetch(solr + "?" + this.querify(input).toString(), {
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
                const docs = extract([], result, "response", "docs");
                const more = numFound > (rows + start);
                this.items = this.items.concat(docs);
                this.setState({
                    loading: more,
                    status: docs.length ? "" : "No Results",
                    items: more ? this.state.items : this.items
                }, () => {
                    if (more) {
                        this.search(browse, start + rows);
                    } else {
                        let searching = this.props.search.query ? true : false;
                        let index = searching ? 0 : extract(0, this.props.ui.scrollPosition, this.props.search.path);
                        this.showScrollToTop((index && !searching) ? true : false);
                        this.scrollToItem(index);
                        this.items = [];
                        if (callback !== undefined) {
                            callback(true, name);
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
        let solr = "/search";
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
        fetch(solr + "?" + this.querify(input).toString(), {
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
                // looks like the file has been deleted, remove the item from the grid
                this.unset(index, () => {
                    if (this.state.items.length > 0) {
                        // scroll to the previous item
                        this.scrollToItem(Math.max(0, index - 1), true);
                    }
                });
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

    upload(files = null) {
        if (files) {
            let queued = [];
            files.forEach(file => {
                if (!file.type && file.size % 4096 == 0) {
                    // it's a folder, ignore it for now
                } else {
                    // it's a file, queue it
                    queued.push(file);
                }
            });
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
                this.upload();
            });
            return;
        }
        // do we already have the maximum simultaneous number of active uploads?
        if (Object.keys(this.state.uploads.active).length >= this.props.ui.uploads.simultaneous) return;
        // nope, do we have any files in the queue? if not, log the list failed uploads if any
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
                    if (Object.keys(this.state.uploads.failed).length > 0) {
                        console.log(this.state.uploads.failed);
                    }
                });
            }
            return;
        }
        // yes, we can start one more
        let file = this.state.uploads.queued[0];
        // dequeue the first file and start uploading it
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
            let config =  {
                headers: {
                    "Content-Type": "multipart/form-data",
                },
                validateStatus: (status) => {
                    return (status === 200);
                },
                onUploadProgress: this.onUploadProgress.bind(this, file, this.state.uploads.total - this.state.uploads.queued.length),
            };
            let data = new FormData();
            data.append('files', file);
            const prefix = "[" + (this.state.uploads.total - this.state.uploads.queued.length) + " / " + this.state.uploads.total + "] ";
            this.setState(prevState => {
                return {
                    ...prevState,
                    uploads: update(prevState.uploads, {
                        $merge: {
                            active: update(prevState.uploads.active, {
                                $merge: {
                                    [file.name]: {
                                        file: file,
                                        toast: toast.info(this.renderUploadToast(prefix + "Uploading...", file.name),
                                        {
                                            progress: 0,
                                            theme: 'dark',
                                            icon: <div className="Toastify__spinner"></div>
                                        }),
                                    }
                                }
                            })
                        }
                    })
                }
            }, () => {
                let params = new URLSearchParams();
                params.set('overwrite', this.props.ui.uploads.overwrite);
                params.set('duplicate', this.props.ui.uploads.duplicate);
                axios.post("/library" + this.props.search.path + "?" + params.toString(), data, config)
                .then((response) => {
                    toast.update(extract(null, this.state.uploads.active, file.name, 'toast'), {
                        progress: 1,
                        theme: null,
                        render: this.renderUploadToast.bind(this, prefix + "Complete", file.name),
                        type: toast.TYPE.SUCCESS,
                        icon: null,
                    });
                    // remove the successfully uploaded file from the list of active uploads
                    this.setState(prevState => {
                        return {
                            ...prevState,
                            uploads: update(prevState.uploads, {
                                $merge: {
                                    active: update(prevState.uploads.active, {
                                        $unset: [file.name]
                                    })
                                }
                            })
                        }
                    }, () => {
                        this.upload();
                        this.list(undefined, () => {
                            const index = this.state.items.findIndex(x => x.name === file.name);
                            if (index !== -1) this.scrollToItem(index, true);
                        });
                    })
                })
                .catch((error) => {
                    console.error(error);
                    let message = "Upload Failed";
                    let toastId = extract(null, this.state.uploads.active, file.name, 'toast');
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
                        progress: 0,
                        theme: null,
                        render: this.renderUploadToast.bind(this, message, file.name),
                        type: toast.TYPE.ERROR,
                        icon: null,
                    });
                    let failed = extract(null, this.state.uploads.active, file.name);
                    // remove the failed upload from the list of active uploads
                    this.setState(prevState => {
                        return {
                            ...prevState,
                            uploads: update(prevState.uploads, {
                                $merge: {
                                    active: update(prevState.uploads.active, {
                                        $unset: [file.name]
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
                                            $merge: {
                                                [file.name]: failed
                                            }
                                        })
                                    }
                                })
                            }
                        }, () => {
                            // process any remaining queued files
                            this.upload();
                        });
                    });
                });
                // process any remaining queued files
                this.upload();
            });
        });
    }

    open(source) {
        this.props.dispatch(setPath(source.fullPath));
    }

    view(source, index = 0, player = true) {
        this.current = index;
        let params = new URLSearchParams(window.location.search);
        // if (params.has('query')) params.delete('query');
        const path = window.location.pathname + source.name + '?' + params.toString();
        if (!this.viewing) window.history.pushState({path: path}, "", path);
        else window.history.replaceState({path: path}, "", path);
        this.mediaViewer.current.view([source], 0, player);
    }

    previous() {
        if (!this.viewing || (this.current === -1)) return;
        let index = this.current - 1;
        if ((index >= 0) && (index < this.state.items.length)) {
            this.current = index;
            let source = this.state.items[index];
            this.mediaViewer.current.view([source], 0, true);
        }
    }

    next() {
        if (!this.viewing || (this.current === -1)) return;
        let index = this.current + 1;
        if ((index >= 0) && (index < this.state.items.length)) {
            this.current = index;
            let source = this.state.items[index];
            this.mediaViewer.current.view([source], 0, true);
        }
    }

    files() {
        let count = this.state.items.reduce((count, item) => {
            if ((item.type === "Video") || (item.type === "Audio") || (item.type === "Photo")) return (count + 1);
            return count;
        }, 0);
        return (count === 0) ? '' : (count + ((count === 1) ? " File" : " Files"));
    }

    querify(dictionary, query = new URLSearchParams()) {
        for (const key in dictionary) {
            const value = dictionary[key];
            if ((value === null) || (value === undefined)) continue;
            if (Array.isArray(value)) {
                for (const item of value) {
                    query.append(key, item);
                }
            } else {
                query.set(key, value);
            }
        }
        return query;
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

    onUploadProgress(file, index, progressEvent) {
        var progress = progressEvent.loaded / progressEvent.total;
        let toastId = extract(null, this.state.uploads.active, file.name, 'toast');
        const prefix = "[" + index + " / " + this.state.uploads.total + "] ";
        toast.update(toastId, {
            progress: Math.min(progress, 0.99),
            render: this.renderUploadToast.bind(this, prefix + ((progress < 1) ? "Uploading" : "Processing") + "...", file.name),
        });
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
        if (this.props.search.query) return;
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
            if (extract(null, this.props.ui.scrollPosition, this.props.search.path) !== firstVisibleItemIndex) {
                this.props.dispatch(setScrollPosition({
                    path: this.props.search.path,
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
                <small>{subtitle}</small>
            </>
        );
    }

    render() {
        let location = "/";
        let url = "Library".concat(this.props.search.path);
        let path = url.split('?')[0];
        if (path.includes('/')) path = path.match(/.*\//g)[0]; // exclude the file name from the path used for the breadcrumb
        let status = this.state.status;
        let loading = this.state.loading;
        let search = this.props.search.query ? true : false;
        let components = (search) ? (path.concat("/Search Results").split("/")) : (path.split("/"));
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
                                    if (last && search) {
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
                                        if (link) this.props.dispatch(setPath(link));
                                    }} >{component}</Breadcrumb.Item>
                                );
                            })
                        }
                        <div className="statistics d-none d-md-block ms-auto">
                            <span className="statistics-files">{this.files()}</span>
                            <span className="statistics-size ms-1">{size(this.state.items.reduce((sum, item) => sum + item.size, 0), 2, '(', ')')}</span>
                        </div>
                    </Breadcrumb>
                    <div ref={this.gridWrapper} className="grid-wrapper d-flex mx-3" style={{flexGrow: 1, flexShrink: 1, flexBasis: 'auto'}}>
                        <AutoSizer>
                            {({ height, width }) => {
                                let size = (window.innerWidth > 0) ? window.innerWidth : window.screen.width;
                                let offset = (size - this.gridWrapper.current.offsetWidth) / 2;
                                let columnCount = Math.ceil(size / 400);
                                let rowHeight = (width / columnCount);
                                let columnWidth = width / columnCount;
                                let rowCount = Math.ceil(this.state.items.length / columnCount);
                                return (
                                    <UploadZone onUpload={this.upload.bind(this)}>
                                        <Grid ref={this.grid} className="grid" columnCount={columnCount} columnWidth={columnWidth} height={height} rowCount={rowCount} rowHeight={rowHeight} width={width + offset} onScroll={this.onScroll.bind(this)} >
                                        {
                                            ({ columnIndex, rowIndex, style }) => {
                                                let index = (rowIndex * columnCount) + columnIndex;
                                                let source = this.state.items[index];
                                                if (source !== undefined) {
                                                    this.mediaContainers[index] = React.createRef();
                                                    return (
                                                        <div className="grid-item animate__animated animate__fadeIn" style={style}>
                                                            <MediaContainer ref={this.mediaContainers[index]} library={this.props.forwardedRef} source={source} index={index} onOpen={this.open.bind(this)} onView={this.view.bind(this)} onUpdate={this.update.bind(this)} />
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
                        <Container fluid className="scroll-to-top animate__animated animate__faster position-absolute d-flex justify-content-center pe-none pb-5">
                            <Button ref={this.scrollToTopButton} className="animate__animated animate__fast px-3" variant="info" onClick={this.onScrollToTop.bind(this)}>
                                <i className="icon bi bi-arrow-up-square pe-2"></i>Scroll To Top<i className="icon bi bi-arrow-up-square ps-2"></i>
                            </Button>
                        </Container>
                        <MediaViewer ref={this.mediaViewer} library={this.props.forwardedRef} onUpdate={this.update.bind(this)} onShow={this.onMediaViewerShow.bind(this)} onHide={this.onMediaViewerHide.bind(this)} />
                        <Container fluid className={cx((loading || status) ? "d-flex" : "d-none", "flex-column align-self-stretch align-items-center")}>
                            <Row className="mt-auto">
                                <Col className={cx(loading ? "d-flex" : "d-none", "text-center mb-3")}>
                                    <Spinner className="loading-spinner" animation="border" role="status"/>
                                </Col>
                            </Row>
                            <Row className="mb-auto">
                                <Col className={cx(status ? "d-flex" : "d-none", "text-center")}>
                                    <p className="font-weight-light h5 text-uppercase" >{this.state.status}</p>
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
        theme: state.ui.theme,
        scrollPosition: state.ui.scrollPosition,
        uploads: state.ui.uploads,
    },
    search: {
        sort: state.search.sort,
        path: state.search.path,
        query: state.search.query,
        favorite: state.search.favorite,
        recursive: state.search.recursive,
    }
});

export default connect(mapStateToProps, null, null, { forwardRef: true })(React.forwardRef((props, ref) => (
    <Library ref={ref} {...props} match={{ params: useParams() }} />
)));
