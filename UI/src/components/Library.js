import Row from 'react-bootstrap/Row';
import Col from 'react-bootstrap/Col';
import React, { Component } from 'react';
import Modal from 'react-bootstrap/Modal';
import Navbar from 'react-bootstrap/Navbar';
import Spinner from 'react-bootstrap/Spinner';
import Container from 'react-bootstrap/Container';
import Breadcrumb from 'react-bootstrap/Breadcrumb';
import AutoSizer from 'react-virtualized-auto-sizer';
import { FixedSizeGrid as Grid } from 'react-window';
import { extract, size, breakpoint, updateBit } from './../utils';
import { MediaContainer } from './MediaContainer';
import { MediaViewer } from './MediaViewer';
import cx from 'classnames';

export class Library extends Component {

    static displayName = Library.name;

    constructor(props) {
        super(props);
        this.rowCount = 1;
        this.columnCount = 1;
        this.mediaViewer = React.createRef();
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
        this.setState({
            items: [],
            loading: true,
            status: "Requesting",
        }, () => {
            fetch("/library" + path)
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
                this.setState({
                    loading: false,
                    status: error,
                    items: []
                });
            });
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
            input.fq.push("path:" + (path.split('?')[0]).replace(/([\+\-!\(\)\{\}\[\]\^\"\~\*\?\:\\\/ ])/g, "\\$1") + "*");
        }
        path = path.split('?')[0] + (params.toString() ? ("?" + params.toString()) : "");
        this.setState({
            items: [],
            loading: true,
            status: "Requesting",
        }, () => {
            fetch(solr + "?" + this.querify(input).toString(), {
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
                console.error(error);
                this.setState({
                    loading: false,
                    status: "Connection Error",
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

    cell({ columnIndex, rowIndex, style }) {
        let source = this.state.items[(rowIndex * this.columnCount) + columnIndex];
        if (source !== undefined) {
            return (
                <div className="p-1" style={style}>
                    <MediaContainer source={source} open={this.open.bind(this)} view={this.view.bind(this)} highlight={this.highlight.bind(this)} />
                </div>
            );
        } else {
            return (
                <div style={style}>
                </div>
            );
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
                <div className="d-flex flex-column align-content-stretch" style={{flexGrow: 1, flexShrink: 1, flexBasis: 'auto'}}>
                    <Breadcrumb className="mx-3" listProps={{ className: "py-2 px-3" }}>
                        {
                            components.filter((component) => component).map((component, index, array) => {
                                let link = location;
                                let active = false;
                                let last = (index === (array.length - 1));
                                if (index) {
                                    if (last) {
                                        link = "";
                                        active = true;
                                    } else {
                                        location += component + "/";
                                        link = location;
                                    }
                                }
                                return (
                                    <Breadcrumb.Item key={"library-path-item-" + index} href="#" active={active} linkProps={{ link: link, className: "text-decoration-none" }} onClick={event => this.list(event.target.getAttribute("link"))} >{component}</Breadcrumb.Item>
                                );
                            })
                        }
                    </Breadcrumb>
                    <div className="d-flex ml-3 mb-3" style={{flexGrow: 1, flexShrink: 1, flexBasis: 'auto'}}>
                        <AutoSizer>
                            {({ height, width }) => {
                                let offset = 0;
                                let size = breakpoint();
                                switch (size) {
                                    case 'xs':
                                        offset = 15;
                                        this.columnCount = 1;
                                        break;
                                    case 'sm':
                                        offset = 8;
                                        this.columnCount = 2;
                                        break;
                                    case 'md':
                                        offset = 6;
                                        this.columnCount = 3;
                                        break;
                                    case 'lg':
                                        offset = 5;
                                        this.columnCount = 4;
                                        break;
                                    case 'xl':
                                        offset = 3;
                                        this.columnCount = 5;
                                        break;
                                    default:
                                        break;
                                }
                                let rowHeight = (width / this.columnCount);
                                let columnWidth = (width / this.columnCount) - offset;
                                this.rowCount = Math.ceil(this.state.items.length / this.columnCount);
                                return (
                                    <Grid columnCount={this.columnCount} columnWidth={columnWidth} height={height} rowCount={this.rowCount} rowHeight={rowHeight} width={width}>
                                        {this.cell.bind(this)}
                                    </Grid>
                                )}
                            }
                        </AutoSizer>
                        <MediaViewer ref={this.mediaViewer} />
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
