import Row from 'react-bootstrap/Row';
import Col from 'react-bootstrap/Col';
import React, { Component } from 'react';
import Navbar from 'react-bootstrap/Navbar';
import Spinner from 'react-bootstrap/Spinner';
import Container from 'react-bootstrap/Container';
import Breadcrumb from 'react-bootstrap/Breadcrumb';
import AutoSizer from 'react-virtualized-auto-sizer';
import { FixedSizeGrid as Grid } from 'react-window';
import { clone, extract, size, breakpoint, updateBit } from './../utils';
import { MediaContainer } from './MediaContainer';
import { MediaViewer } from './MediaViewer';
import { toast } from 'react-toastify';
import cx from 'classnames';

export class Library extends Component {

    static displayName = Library.name;

    constructor(props) {
        super(props);
        this.mediaViewer = React.createRef();
        this.gridWrapper = React.createRef();
        this.controller = new AbortController();
        let path = "/" + extract("", props, "match", "params", 0) + window.location.search;
        this.state = {
            loading: false,
            status: "",
            path: path,
            items: [],
        };
    }

    componentDidMount() {
        // handle the startup query parsing
        this.props.navigation.current.resetSearchParams(() => {
            if (this.props.navigation.current.state.query) {
                this.search();
            } else {
                this.list(this.state.path, true);
            }
        });
        // handles the browser history operations
        window.onpopstate = (event) => {
            let path = extract(null, event, 'state', 'path');
            if (path) {
                this.props.navigation.current.resetSearchParams(() => {
                    this.list(event.state.path, true, true);
                });
            }
        };
    }

    componentDidUpdate() {
    }

    list(path, search = false, history = false) {
        if (!path) return;
        let params = new URLSearchParams(path.split('?')[1]);
        let flags = parseInt(params.get("flags") ?? 0);
        let values = parseInt(params.get("values") ?? 0);
        let favorite = this.props.navigation.current.state.favorite;
        if (!search) {
            this.props.navigation.current.clearSearch();
        }
        // update the favorite flags parameters
        flags = updateBit(flags, 1, favorite);
        values = updateBit(values, 1, favorite);
        // update the deleted flags bits to exclude the deleted files
        flags = updateBit(flags, 0, 1);
        values = updateBit(values, 0, 0);
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
        path = path.split('?')[0] + (params.toString() ? ("?" + params.toString()) : "");
        this.controller.abort();
        this.controller = new AbortController();
        this.setState({
            items: [],
            loading: true,
            status: "Requesting",
        }, () => {
            fetch("/library" + path, {
                signal: this.controller.signal,
            })
            .then((response) => {
                this.setState({
                    status: "Loading",
                    items: []
                });
                return response.json();
            })
            .then((json) => {
                let items = Array.isArray(json) ? json : [json];
                this.setState({
                    loading: false,
                    status: items.length ? "" : "No Items",
                    path: path,
                    items: items
                });
                // now change the history if we have to!
                if (!history) {
                    window.history.pushState({path: path}, "", path);
                }
                // pass on the source to the viewer if this is a file
                return Array.isArray(json) ? null : json;
            })
            .then((source) => {
                if (source !== null) {
                    this.view(source);
                }
            })
            .catch((error) => {
                if (error.name === 'AbortError') return;
                this.setState({
                    loading: false,
                    status: error.message,
                    items: []
                });
            });
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
            toast.error("Unabled to find the item that was to be updated.");
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

    search() {
        let path = this.state.path;
        if (!this.state.path) return;
        let query = this.props.navigation.current.state.query;
        let params = new URLSearchParams(path.split('?')[1]);
        let flags = parseInt(params.get("flags") ?? 0);
        let values = parseInt(params.get("values") ?? 0);
        if (!query) {
            this.props.navigation.current.clearSearch();
            return;
        }
        let deleted = false;
        let favorite = this.props.navigation.current.state.favorite;
        let recursive = this.props.navigation.current.state.recursive;
        // update the recursive parameter
        params.set("recursive", recursive);
        // update the query parameter
        params.set("query", query);
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
            qf: "name^10 path^5",
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
        if (!recursive) {
            input.fq.push("path:\"" + path.split('?')[0] + "\"");
        } else {
            input.fq.push("path:" + (path.split('?')[0]).replace(/([+\-!(){}[\]^"~*?:\\/ ])/g, "\\$1") + "*");
        }
        path = path.split('?')[0] + (params.toString() ? ("?" + params.toString()) : "");
        this.controller.abort();
        this.controller = new AbortController();
        this.setState({
            items: [],
            loading: true,
            status: "Requesting",
        }, () => {
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
                    path: path,
                    items: docs
                });
                // now change the history if we have to!
                window.history.pushState({path: path}, "", path);
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
        this.list(source.fullPath);
    }

    view(source, player = true) {
        this.mediaViewer.current.view(source, player);
    }

    highlight(source) {
        if (source !== null) {
        } else {
        }
    }

    files() {
        return this.state.items.reduce((count, item) =>{
            if ((item.type === "Video") || (item.type === "Audio") || (item.type === "Photo")) return (count + 1);
            return count;
        }, 0) + " Files";
    }

    querify(dictionary, query = new URLSearchParams()) {
        for (const key in dictionary) {
            const value = dictionary[key];
            if ((value === null) || (value === undefined)) continue;
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

    render() {
        let location = "/";
        let url = "Library".concat(this.state.path);
        let path = url.split('?')[0];
        let params = url.split("?")[1];
        let status = this.state.status;
        let loading = this.state.loading;
        let search = (params !== undefined) && (params.indexOf("query=") !== -1);
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
                                        this.list(event.target.getAttribute("link"));
                                    }} >{component}</Breadcrumb.Item>
                                );
                            })
                        }
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
                                    <Grid columnCount={columnCount} columnWidth={columnWidth} height={height} rowCount={rowCount} rowHeight={rowHeight} width={width + offset}>
                                    {
                                        ({ columnIndex, rowIndex, style }) => {
                                            let source = this.state.items[(rowIndex * columnCount) + columnIndex];
                                            if (source !== undefined) {
                                                return (
                                                    <div className="grid-item" style={style}>
                                                        <MediaContainer source={source} onOpen={this.open.bind(this)} onView={this.view.bind(this)} onUpdate={this.update.bind(this)} onHighlight={this.highlight.bind(this)} />
                                                    </div>
                                                );
                                            } else {
                                                return (
                                                    <div style={style}>
                                                    </div>
                                                );
                                            }
                                        }
                                    }
                                    </Grid>
                                )}
                            }
                        </AutoSizer>
                        <MediaViewer ref={this.mediaViewer} onUpdate={this.update.bind(this)} />
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
                <Navbar collapseOnSelect expand="sm" bg={this.props.darkMode ? "dark" : "light"} variant="dark" className="p-3">
                    <div style={{flexGrow: 1}}>{this.files()}</div>
                    <div>{size(this.state.items.reduce((sum, item) => sum + item.size, 0))}</div>
                </Navbar>
            </>
        );
    }
}
