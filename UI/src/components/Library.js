import Row from 'react-bootstrap/Row';
import Col from 'react-bootstrap/Col';
import React, { Component } from 'react';
import Spinner from 'react-bootstrap/Spinner';
import Container from 'react-bootstrap/Container';
import Breadcrumb from 'react-bootstrap/Breadcrumb';
import AutoSizer from 'react-virtualized-auto-sizer';
import { FixedSizeGrid as Grid } from 'react-window';
import { clone, extract, size, updateBit } from './../utils';
import { reset, setPath } from '../features/search/slice';
import { MediaContainer } from './MediaContainer';
import { MediaViewer } from './MediaViewer';
import { toast } from 'react-toastify';
import { connect } from "react-redux";
import cx from 'classnames';
import _ from 'lodash';

class Library extends Component {

    static displayName = Library.name;

    constructor(props) {
        super(props);
        this.current = -1;      // the current item index being viewed in the MediaViewer
        this.viewing = false;   // indicates that a media is being viewed in the MediaViewer
        this.editing = false;   // indicates that text editing is in progress and should inhibit low level keyboard input capturing
        this.grid = React.createRef();
        this.gridWrapper = React.createRef();
        this.mediaViewer = React.createRef();
        this.controller = new AbortController();
        this.state = {
            history: false,
            loading: false,
            status: "",
            items: [],
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
        let path = "/" + extract("", this.props, "match", "params", 0) + window.location.search;
        this.props.dispatch(reset(path));
        // handles the browser history operations
        window.onpopstate = (event) => {
            let path = extract("", event, 'state', 'path');
            this.setState({
                history: true, // notify the search function that the call is coming from the history
            }, () => {
                this.props.dispatch(reset(path));
            });
        };
    }

    componentDidUpdate(prevProps) {
        if (!_.isEqual(this.props.search, prevProps.search)) {
            if (this.props.search.query) {
                this.search();
            } else {
                this.list(this.props.search.path);
            }
        }
    }

    list(path) {
        this.search(path);
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
            method: "PUT",
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
            // update the state and the virtual copies of the source
            this.state.items[index] = Object.assign(this.state.items[index], response);
            if (refresh) {
                this.setState({
                    items: this.state.items
                }, () => {
                    if (callback !== undefined) {
                        callback(this.state.items[index], true);
                    }
                });
            } else {
                if (callback !== undefined) {
                    callback(this.state.items[index], true);
                }
            }
        })
        .catch((error) => {
            console.error(error);
            toast.error(error.message);
            if (callback !== undefined) {
                callback(this.state.items[index], false);
            }
        });
    }

    search(browse = null) {
        let path = browse ?? this.props.search.path;
        if (!path) return;
        let history = this.state.history;
        let query = browse ? "*" : this.props.search.query;
        let params = new URLSearchParams(path.split('?')[1]);
        let flags = parseInt(params.get("flags") ?? 0);
        let values = parseInt(params.get("values") ?? 0);
        if (browse) {
            params.delete("query");
        } else {
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
        } else {
            params.delete("flags");
        }
        if (values) {
            params.set("values", values);
        } else {
            params.delete("values");
        }
        let solr = "/search";
        if (process.env.NODE_ENV !== "production") {
            solr = "http://localhost:8983/solr/Library/select";
        }
        const input = {
            q: query,
            fq: [],
            rows: 10000,
            "q.op": "AND",
            defType: "edismax",
            qf: "name^20 description^10 path^5",
            wt: "json",
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
        this.setState({
            items: [],
            path: path,
            loading: true,
            history: false,
            status: "Requesting",
        }, () => {
            // change the url and the history before going any further
            if (!history) window.history.pushState({path: path}, "", path);
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
                        items: []
                    });
                }
                return response.json();
            })
            .then((result) => {
                const docs = extract([], result, "response", "docs");
                this.setState({
                    loading: false,
                    status: docs.length ? "" : "No Results",
                    items: docs
                });
            })
            .catch(error => {
                if (error.name === 'AbortError') return;
                this.setState({
                    loading: false,
                    status: error.message,
                    items: []
                });
            });
        });
    }

    open(source) {
        this.props.dispatch(setPath(source.fullPath));
    }

    view(source, index = 0, player = true) {
        this.current = index;
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

    onMediaViewerShow() {
        this.viewing = true;
    }

    onMediaViewerHide() {
        this.viewing = false;
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
                    grid.scrollToItem({ align: "start", rowIndex: 0 });
                    break;
                case 'End':
                    grid.scrollToItem({ align: "end", rowIndex: rowCount });
                    break;
                case 'PageUp':
                    grid.scrollToItem({ align: "start", rowIndex: (currentRow - pageRows) });
                    break;
                case 'PageDown':
                    grid.scrollToItem({ align: "start", rowIndex: (currentRow + pageRows) });
                    break;
                case 'ArrowUp':
                    grid.scrollToItem({ align: "start", rowIndex: (currentRow - 1) });
                    break;
                case 'ArrowDown':
                    grid.scrollToItem({ align: "start", rowIndex: (currentRow + 1) });
                    break;
                default:
                    return;
            }
        }
        // it appears that the key has been handled, let's ensure no one else gets it
        event.preventDefault();
        event.stopPropagation();
    }

    render() {
        let location = "/";
        let url = "Library".concat(this.props.search.path);
        let path = url.split('?')[0];
        let status = this.state.status;
        let loading = this.state.loading;
        let search = this.props.search.query ? true : false;
        let components = (search) ? (path.concat("/Search Results").split("/")) : (path.split("/"));
        return (
            <>
                <div className="library d-flex flex-column align-content-stretch" style={{flexGrow: 1, flexShrink: 1, flexBasis: 'auto'}}>
                    <Breadcrumb className="mx-3" listProps={{ className: "py-2 px-3" }}>
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
                        <div className="statistics d-none d-md-block ml-auto">
                            <span className="statistics-files">{this.files()}</span>
                            <span className="statistics-size ml-1">{size(this.state.items.reduce((sum, item) => sum + item.size, 0), 2, '(', ')')}</span>
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
                                    <Grid ref={this.grid} className="grid" columnCount={columnCount} columnWidth={columnWidth} height={height} rowCount={rowCount} rowHeight={rowHeight} width={width + offset}>
                                    {
                                        ({ columnIndex, rowIndex, style }) => {
                                            let index = (rowIndex * columnCount) + columnIndex;
                                            let source = this.state.items[index];
                                            if (source !== undefined) {
                                                return (
                                                    <div className="grid-item animate__animated animate__fadeIn" style={style}>
                                                        <MediaContainer library={this.props.forwardedRef} source={source} index={index} onOpen={this.open.bind(this)} onView={this.view.bind(this)} onUpdate={this.update.bind(this)} />
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
    },
    search: {
        path: state.search.path,
        query: state.search.query,
        favorite: state.search.favorite,
        recursive: state.search.recursive,
    }
});

export default connect(mapStateToProps, null, null, { forwardRef: true })(Library);
